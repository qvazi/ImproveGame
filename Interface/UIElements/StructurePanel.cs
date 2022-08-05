﻿using ImproveGame.Common.ConstructCore;
using ImproveGame.Common.Systems;
using ImproveGame.Interface.UIElements_Shader;
using Microsoft.Xna.Framework.Input;
using Terraria.GameInput;

namespace ImproveGame.Interface.UIElements
{
    internal class StructurePanel : SUIPanel
    {
        public string FilePath { get; private set; }
        public string Name { get; private set; }

        private string _inputName;  // 用于先装输入缓存的
        private bool _renaming;
        private int _cursorTimer;
        private bool _oldMouseLeft;

        public static readonly Color BorderSelectedColor = new(89, 116, 213);
        public static readonly Color BorderUnselectedColor = new(39, 46, 100);
        public static readonly Color SelectedColor = new(73, 94, 171);
        public static readonly Color UnselectedColor = new(62, 80, 146);

        public UIText NameText;
        public UIText PathText;
        public UIImageButton RenameButton;
        public SUIPanel PathPanel;

        public StructurePanel(string filePath) : base(BorderUnselectedColor, UnselectedColor, CalculateBorder: false)
        {
            FilePath = filePath;

            _oldMouseLeft = true;

            Width = StyleDimension.FromPixels(540f);

            string name = FilePath.Split('\\').Last();
            name = name[..^FileOperator.Extension.Length]; // name.Substring(0, name.Length - FileOperator.Extension.Length)
            Name = name;
            NameText = new(name, 1.05f)
            {
                Left = StyleDimension.FromPixels(2f),
                Height = StyleDimension.FromPixels(24f)
            };
            Append(NameText);

            UIHorizontalSeparator separator = new()
            {
                Top = StyleDimension.FromPixels(NameText.Height.Pixels - 2f),
                Width = StyleDimension.FromPercent(1f),
                Color = Color.Lerp(Color.White, new Color(63, 65, 151, 255), 0.85f) * 0.9f
            };
            Append(separator);

            var detailButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay"))
            {
                Top = new StyleDimension(separator.Height.Pixels + separator.Top.Pixels + 3f, 0f),
                Left = new StyleDimension(-20f, 1f)
            }.SetSize(24f, 24f);
            detailButton.OnClick += DetailButtonClick;
            Append(detailButton);

            var deleteButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete"))
            {
                Top = detailButton.Top,
                Left = new StyleDimension(detailButton.Left.Pixels - 24f, 1f)
            }.SetSize(24f, 24f);
            deleteButton.OnClick += DeleteButtonClick;
            Append(deleteButton);

            RenameButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonRename"))
            {
                Top = detailButton.Top,
                Left = new StyleDimension(deleteButton.Left.Pixels - 24f, 1f)
            };
            RenameButton.SetSize(24f, 24f);
            Append(RenameButton);

            PathPanel = new(new Color(35, 40, 83), new Color(35, 40, 83), radius: 10, CalculateBorder: false)
            {
                Top = detailButton.Top,
                OverflowHidden = true,
                PaddingLeft = 6f,
                PaddingRight = 6f,
                PaddingBottom = 0f,
                PaddingTop = 0f
            };
            PathPanel.SetSize(new(Width.Pixels + RenameButton.Left.Pixels - 34f, 23f));
            Append(PathPanel);
            PathText = new($"Path: {FilePath}", 0.7f)
            {
                Left = StyleDimension.FromPixels(2f),
                HAlign = 0f,
                VAlign = 0.5f,
                TextColor = Color.Gray
            };
            PathPanel.Append(PathText);
            SetSizedText();

            Height = StyleDimension.FromPixels(PathPanel.Top.Pixels + PathPanel.Height.Pixels + 22f);
        }

        private void DetailButtonClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (UISystem.Instance.StructureGUI is not null)
            {
                UISystem.Instance.StructureGUI.CacheSetupStructureInfos = true;
                UISystem.Instance.StructureGUI.CacheStructureInfoPath = FilePath;
            }
        }

        private void RenameButtonClick()
        {
            _inputName = Name;
            _renaming = true;
            Main.blockInput = true;
            Main.clrInput();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        private void EndRename()
        {
            _renaming = false;
            Main.blockInput = false;
            string newPath = FilePath.Replace(Name, _inputName);
            if (File.Exists(newPath) && Name != _inputName)
            {
                Main.NewText("已经存在一个相同名称的文件");
                NameText.SetText(Name);
                return;
            }
            if (File.Exists(FilePath) && UISystem.Instance.StructureGUI is not null)
            {
                File.Move(FilePath, newPath);
                UISystem.Instance.StructureGUI.CacheSetupStructures = true;
            }
        }

        private void DeleteButtonClick(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            if (File.Exists(FilePath))
                File.Delete(FilePath);
            if (UISystem.Instance.StructureGUI is not null)
                UISystem.Instance.StructureGUI.CacheSetupStructures = true;
        }

        public override void Update(GameTime gameTime)
        {
            _cursorTimer++;
            _cursorTimer %= 60;
            if (Main.mouseLeft && !_oldMouseLeft)
            {
                var renameDimensions = RenameButton.GetOuterDimensions();
                if (renameDimensions.ToRectangle().Contains(Main.MouseScreen.ToPoint()) && !_renaming)
                {
                    RenameButtonClick();
                }
                else if (_renaming)
                {
                    EndRename();
                }
            }

            base.Update(gameTime);

            if (IsMouseHovering)
            {
                borderColor = BorderSelectedColor;
                backgroundColor = SelectedColor;
                NameText.TextColor = Color.White;
            }
            else
            {
                borderColor = BorderUnselectedColor;
                backgroundColor = UnselectedColor;
                NameText.TextColor = Color.LightGray;
            }
            if (WandSystem.ConstructFilePath == FilePath)
            {
                NameText.TextColor = new(255, 231, 69);
            }
            SetSizedText();
            _oldMouseLeft = Main.mouseLeft;
        }

        public override void MouseDown(UIMouseEvent evt)
        {
            base.MouseDown(evt);

            if (Children.Any(i => i is UIImageButton && i.IsMouseHovering))
                return;

            SoundEngine.PlaySound(SoundID.MenuTick);
            if (WandSystem.ConstructFilePath == FilePath)
            {
                WandSystem.ConstructFilePath = string.Empty;
                return;
            }
            WandSystem.ConstructFilePath = FilePath;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // 放Update，不行。放Draw，行！ReLogic我囸你_
            if (_renaming)
            {
                PlayerInput.WritingText = true;
                Main.instance.HandleIME();

                var inputText = Main.GetInputText(_inputName);
                if (inputText.Length > 40)
                {
                    inputText = inputText[..40];
                    Main.NewText("名称不能超过40个字符");
                }
                if (inputText.Contains('\\') || inputText.Contains('/') || inputText.Contains(':') || inputText.Contains('*') || inputText.Contains('?') || inputText.Contains('\"') || inputText.Contains('\'') || inputText.Contains('<') || inputText.Contains('>') || inputText.Contains('|'))
                {
                    Main.NewText("文件名不能包含以下字符: \\、/、:、*、?、\"、<、>、|");
                    return;
                }
                else
                {
                    _inputName = inputText;
                    NameText.SetText(_inputName + (_cursorTimer >= 30 ? "|" : ""));
                    NameText.Recalculate();
                }

                // Enter 或者 Esc
                if (KeyTyped(Keys.Enter) || KeyTyped(Keys.Tab) || KeyTyped(Keys.Escape))
                {
                    EndRename();
                }
            }

            base.Draw(spriteBatch);
        }

        public void SetSizedText()
        {
            var innerDimensions = PathPanel.GetInnerDimensions();
            var font = FontAssets.MouseText.Value;
            float scale = 0.7f;
            float dotWidth = font.MeasureString("...").X * scale;
            float pathWidth = font.MeasureString("Path: ").X * scale;
            if (font.MeasureString(FilePath).X * scale >= innerDimensions.Width - 16f - pathWidth - dotWidth)
            {
                float width = 0f;
                int i;
                for (i = FilePath.Length - 1; i > 0; i--)
                {
                    width += font.MeasureString(FilePath[i].ToString()).X * scale;
                    if (width >= innerDimensions.Width - 16f - pathWidth - dotWidth)
                    {
                        break;
                    }
                }
                PathText.SetText($"Path: ...{FilePath[i..]}");
            }
        }

        public static bool KeyTyped(Keys key) => Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);
    }
}
