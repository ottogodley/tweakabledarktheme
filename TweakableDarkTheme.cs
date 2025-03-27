using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

using HarmonyLib;
using Lib;

namespace TweakableDarkTheme
{
    public class TweakableDarkTheme : AuroraPatch.Patch
    {
        public override string Description => "Tweakable Dark Aurora Theme";
        public override IEnumerable<string> Dependencies => new[] { "ThemeCreator", "Lib" };

        // The global font used.
        public static Font font = null;
        public static Color textColourFromSettings = Color.FromArgb(210, 210, 210);
        public static Color backgroundColourFromSettings = Color.FromArgb(12, 12, 12);
        private static Lib.Lib lib;

        // Fonts
        private static readonly FontFamily fontFamily = new FontFamily("Tahoma");
        private static readonly Font mainFont = new Font(fontFamily, 8.25f);
        private static Font singleLineTextBoxFont = new Font(fontFamily, 8);
        private static Font buttonFont = new Font(fontFamily, 7, FontStyle.Bold);

        // Dynamic Button Bitmaps (SpaceMaster Mode, Auto turns)
        private static readonly Bitmap spaceMasterOnImage = new Bitmap(@"Patches\TweakableDarkTheme\Icons\SMActive.png");
        private static readonly Bitmap spaceMasterOffImage = new Bitmap(@"Patches\TweakableDarkTheme\Icons\SMInactive.png");
        private static readonly Bitmap autoTurnsOnImage = new Bitmap(@"Patches\TweakableDarkTheme\Icons\AutoOn.png");
        private static readonly Bitmap autoTurnsOffImage = new Bitmap(@"Patches\TweakableDarkTheme\Icons\AutoOff.png");

        // Our new colors
        private static Color mainBackgroundColor = Color.FromArgb(12, 12, 12);
        private static Color mainTextColor = Color.FromArgb(210, 210, 210);
        private static Color disabledTextColor = ControlPaint.Dark(mainTextColor, 0.1f);
        private static Color buttonBackgroundColor = Color.FromArgb(23, 26, 39);
        private static readonly Color planetColor = Color.FromArgb(128, 128, 128);
        private static readonly Color orbitColor = Color.FromArgb(128, planetColor);
        private static readonly Color enabledSpaceMasterButtonColor = Color.FromArgb(248, 231, 28);
        private static readonly Color enabledAutoTurnsButtonColor = Color.FromArgb(126, 211, 33);

        // Toolbar button background colors
        private static readonly Color economicsButtonBackgroundColor = Color.FromArgb(26, 45, 46);
        private static readonly Color designButtonBackgroundColor = Color.FromArgb(41, 44, 46);
        private static readonly Color fleetButtonBackgroundColor = Color.FromArgb(45, 26, 26);
        private static readonly Color groundForcesButtonBackgroundColor = Color.FromArgb(42, 45, 28);
        private static readonly Color intelligenceButtonBackgroundColor = Color.FromArgb(47, 38, 47);
        private static readonly Color explorationButtonBackgroundColor = Color.FromArgb(24, 27, 78);
        private static readonly Color personnelButtonBackgroundColor = Color.FromArgb(18, 41, 58);
        private static readonly Color surveyButtonBackgroundColor = Color.FromArgb(40, 31, 24);
        private static readonly Color technologyButtonBackgroundColor = Color.FromArgb(42, 22, 45);
        private static readonly Color sectorButtonBackgroundColor = Color.FromArgb(20, 45, 31);

        // Old colors
        private static readonly Color oldTextColor = Color.FromArgb(255, 255, 192);
        private static readonly Color oldBackgroundColor = Color.FromArgb(0, 0, 64);
        private static readonly Color oldPlayerContactColor = Color.FromArgb(255, 255, 192);
        private static readonly Color oldNeutralContactColor = Color.FromArgb(144, 238, 144);
        private static readonly Color oldCivilianContactColor = Color.FromArgb(0, 206, 209);
        private static readonly Color oldHostileContactColor = Color.FromArgb(255, 0, 0);
        private static readonly Color oldCometPathColor = Color.LimeGreen;
        private static readonly Color oldOrbitColor = Color.LimeGreen;
        private static readonly Color oldDisabledTextColor = Color.LightGray;
        private static readonly Color oldEnabledButtonBackgroundColor = Color.FromArgb(0, 0, 120);

        private static string lastActiveTimeIncrement;
        private static string activeSubPulse;
        private static bool isSpaceMasterEnabled = false;
        private static bool isAutoTurnsEnabled = false;

        private const int EM_SETMARGINS = 0xd3;
        private const int EC_RIGHTMARGIN = 2;
        private const int EC_LEFTMARGIN = 1;

        [DllImport("User32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private static void SetTextBoxHorizontalPadding(TextBox textBox, int padding)
        {
            SendMessage(textBox.Handle, EM_SETMARGINS, EC_RIGHTMARGIN, padding << 16);
            SendMessage(textBox.Handle, EM_SETMARGINS, EC_LEFTMARGIN, padding);
        }

        private static bool IsTimeIncrementButton(Button button)
        {
            return button.Name.StartsWith("cmdIncrement");
        }

        private static bool IsSubPulseButton(Button button)
        {
            return button.Name.StartsWith("cmdSubPulse");
        }

        private static bool IsSpaceMasterButton(Button button)
        {
            return button.Name == lib.KnowledgeBase.GetButtonName(AuroraButton.SM);
        }

        private static bool IsAutoTurnsButton(Button button)
        {
            return button.Name == lib.KnowledgeBase.GetButtonName(AuroraButton.ToolbarAuto);
        }

        protected override void Loaded(Harmony harmony)
        {
            lib = GetDependency<Lib.Lib>("Lib");

            LogInfo("Loading Theme...");

            // -- Settings -- //
            try
            {
                font = Deserialize<Font>("font");
                if (File.Exists(@"Patches\TweakableDarkTheme\background.json")) mainBackgroundColor = Deserialize<Color>("background");
                if (File.Exists(@"Patches\TweakableDarkTheme\foreground.json")) mainTextColor = Deserialize<Color>("foreground");
            }
            catch (Exception)
            {
                LogInfo("Saved settings not found");
            }
            // set some other fonts based on (potentially custom) main font
            if (font != null) {
                buttonFont = new Font(font.FontFamily, 7, FontStyle.Bold);
                singleLineTextBoxFont = new Font(font.FontFamily, 8);
            }
            // refresh colours for any that need updating dynamically from the above 2
            updateColours();

            // -- Images -- //
            string imagePath = @"Patches\TweakableDarkTheme\Icons\";
            List<string> icons = Directory.EnumerateFiles(imagePath).ToList();
            foreach (KeyValuePair<AuroraButton, string> auroraButtons in lib.KnowledgeBase.GetKnownButtonNames())
            {
                string iconPath = imagePath + auroraButtons.Value + ".BackgroundImage.png";
                if (icons.Contains(iconPath))
                {
                    Bitmap tempIcon = new Bitmap(iconPath);
                    
                    ChangeButtonStyle(auroraButtons.Key, tempIcon, mainTextColor, mainBackgroundColor);
                }
            }

            // Buttons
            ThemeCreator.ThemeCreator.AddColorChange(
                typeof(Button),
                new ThemeCreator.ColorChange { BackgroundColor = buttonBackgroundColor }
            );

            lastActiveTimeIncrement = lib.KnowledgeBase.GetButtonName(AuroraButton.Increment);
            activeSubPulse = lib.KnowledgeBase.GetButtonName(AuroraButton.SubPulse);

            if (font != null) {
                ThemeCreator.ThemeCreator.AddFontChange(font);
            } else {
                ThemeCreator.ThemeCreator.AddFontChange(mainFont);
            }
            ThemeCreator.ThemeCreator.AddFontChange(typeof(Button), buttonFont);

            // Use slightly different text box font size for better alignment and to fix some
            // overflow issues in System view form ("Specify Minerals" feature in SM mode).
            ThemeCreator.ThemeCreator.AddFontChange((Control control) =>
            {
                return control.GetType() == typeof(TextBox) && !((TextBox)control).Multiline;
            }, singleLineTextBoxFont);

            ThemeCreator.ThemeCreator.SetCometTailColor(orbitColor);
            ThemeCreator.ThemeCreator.SetPlanetColor(planetColor);

            ThemeCreator.ThemeCreator.DrawEllipsePrefixAction((graphics, pen) =>
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Note that the same color circles are used to mark colonies as well, not just orbits
                if (pen.Color == oldOrbitColor)
                {
                    pen.Color = orbitColor;
                }
            });

            ThemeCreator.ThemeCreator.FillEllipsePrefixAction((graphics, brush) =>
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                if (brush.GetType() == typeof(SolidBrush))
                {
                    var solidBrush = brush as SolidBrush;

                    // This is being overriden by global color contructor hook, but we want to keep
                    // the old yellow color for player contacts, so restore.
                    if (solidBrush.Color == mainTextColor)
                    {
                        solidBrush.Color = oldPlayerContactColor;
                    }
                }
            });

            ThemeCreator.ThemeCreator.DrawLinePrefixAction((graphics, pen) =>
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Movement tails
                if (pen.Color == oldCivilianContactColor || pen.Color == oldHostileContactColor
                    || pen.Color == mainTextColor || pen.Color == oldNeutralContactColor)
                {
                    // Restore player contact movement tail color to the old yellow one (was overriden
                    // by global color contructor hook).
                    var newColor = pen.Color == mainTextColor ? oldPlayerContactColor : pen.Color;

                    pen.Color = ControlPaint.Dark(newColor, 0.5f);
                }
                // Comet path (distance ruler also uses the same color but has pen.Width > 1)
                else if (pen.Color == oldCometPathColor && pen.Width == 1)
                {
                    pen.Color = orbitColor;
                }
            });

            ThemeCreator.ThemeCreator.DrawStringPrefixAction((graphics, s, font, brush) =>
            {
                if (brush.GetType() == typeof(SolidBrush))
                {
                    var solidBrush = brush as SolidBrush;

                    if (solidBrush.Color == oldPlayerContactColor)
                    {
                        solidBrush.Color = mainTextColor;
                    }
                }
            });

            var colorConstructorPostfix = new HarmonyMethod(GetType().GetMethod("ColorConstructorPostfix", AccessTools.all));

            // Patch all Color.FromArgb overloads for color overrides
            foreach (var method in typeof(Color).GetMethods(AccessTools.all))
            {
                if (method.Name == "FromArgb")
                {
                    harmony.Patch(method, postfix: colorConstructorPostfix);
                }
            }

            // Also hook into some predefined/named color properties
            harmony.Patch(typeof(Color).GetMethod("get_LightGray"), postfix: colorConstructorPostfix);

            // Hook into Aurora forms constructors for some more advanced overrides
            var formConstructorPostfix = new HarmonyMethod(GetType().GetMethod("FormConstructorPostfix", AccessTools.all));

            foreach (var form in AuroraAssembly.GetTypes().Where(t => typeof(Form).IsAssignableFrom(t)))
            {
                foreach (var ctor in form.GetConstructors())
                {
                    harmony.Patch(ctor, postfix: formConstructorPostfix);
                }
            }
        }

        private static void ColorConstructorPostfix(ref Color __result)
        {
            if (__result == oldTextColor)
            {
                __result = mainTextColor;
            }
            else if (__result == oldBackgroundColor || __result == oldEnabledButtonBackgroundColor)
            {
                __result = mainBackgroundColor;
            }
            else if (__result == oldDisabledTextColor)
            {
                __result = disabledTextColor;
            }
        }

        private static void FormConstructorPostfix(Form __instance)
        {
            __instance.HandleCreated += (Object sender, EventArgs e) =>
            {
                IterateControls((Control)sender);
            };
        }

        private static void IterateControls(Control control)
        {
            ApplyChanges(control);

            foreach (Control childControl in control.Controls)
            {
                IterateControls(childControl);
            }
        }

        private static void ApplyChanges(Control control)
        {
            if (control.GetType() == typeof(TabControl))
            {
                ApplyTabControlChanges(control as TabControl);
            }
            else if (control.GetType() == typeof(Button))
            {
                ApplyButtonChanges(control as Button);
            }
            else if (control.GetType() == typeof(ComboBox))
            {
                ApplyComboBoxChanges(control as ComboBox);
            }
            else if (control.GetType() == typeof(TreeView))
            {
                ApplyTreeViewChanges(control as TreeView);
            }
            else if (control.GetType() == typeof(ListView))
            {
                ApplyListViewChanges(control as ListView);
            }
            else if (control.GetType() == typeof(ListBox))
            {
                ApplyListBoxChanges(control as ListBox);
            }
            else if (control.GetType() == typeof(FlowLayoutPanel))
            {
                ApplyFlowLayoutPanelChanges(control as FlowLayoutPanel);
            }
            else if (control.GetType() == typeof(Label))
            {
                ApplyLabelChanges(control as Label);
            }
            else if (control.GetType() == typeof(TextBox))
            {
                ApplyTextBoxChanges(control as TextBox);
            }
            else if (control is Form)
            {
                ApplyFormChanges(control as Form);
            }
        }

        private static void ApplyTabControlChanges(TabControl tabControl)
        {
            tabControl.SizeMode = TabSizeMode.FillToRight;

            // Patch tactical map tabs to fit on two lines (necessary due to custom font)
            if (tabControl.Name == "tabSidebar")
            {
                tabControl.Padding = new Point(5, 3);
            }
        }

        private static void ApplyButtonChanges(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = mainBackgroundColor;
            button.FlatAppearance.BorderSize = 2;

            // With some exceptions just enable auto size for buttons (necessary due to custom font)
            if (button.Name != lib.KnowledgeBase.GetButtonName(AuroraButton.SubPulse)
                && button.Name != lib.KnowledgeBase.GetButtonName(AuroraButton.Increment))
            {
                button.AutoSize = true;
            }

            if (IsTimeIncrementButton(button))
            {
                button.Click += OnTimeIncrementButtonClick;
                ApplyActiveButtonStyle(button, button.Name == lastActiveTimeIncrement);
            }
            else if (IsSubPulseButton(button))
            {
                button.Click += OnSubPulseButtonClick;
                ApplyActiveButtonStyle(button, button.Name == activeSubPulse);
            }
            else if (IsSpaceMasterButton(button))
            {
                ApplySpaceMasterButtonStyle(button);
                button.BackgroundImageChanged += OnSpaceMasterButtonBackgroundImageChanged;
            }
            else if (IsAutoTurnsButton(button))
            {
                ApplyAutoTurnsButtonStyle(button);
                button.BackgroundImageChanged += OnAutoTurnsButtonBackgroundImageChanged;
            }
        }

        private static void ApplyComboBoxChanges(ComboBox comboBox)
        {
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private static void ApplyTreeViewChanges(TreeView treeView)
        {
            if (treeView.BorderStyle == BorderStyle.Fixed3D)
            {
                treeView.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        private static void ApplyListViewChanges(ListView listView)
        {
            if (listView.BorderStyle == BorderStyle.Fixed3D)
            {
                listView.BorderStyle = BorderStyle.FixedSingle;
            }

            if (listView.View == View.Details && listView.Columns.Count > 1)
            {
                listView.FullRowSelect = true;
            }
        }

        private static void ApplyListBoxChanges(ListBox listBox)
        {
            if (listBox.BorderStyle == BorderStyle.Fixed3D)
            {
                listBox.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        private static void ApplyFlowLayoutPanelChanges(FlowLayoutPanel flowLayoutPanel)
        {
            if (flowLayoutPanel.BorderStyle == BorderStyle.Fixed3D)
            {
                flowLayoutPanel.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        private static void ApplyTextBoxChanges(TextBox textBox)
        {
            if (textBox.BorderStyle == BorderStyle.Fixed3D)
            {
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }

            // Minor tweak for consistency - align TextBox horizontal padding to match
            // ListView and other controls.
            SetTextBoxHorizontalPadding(textBox, 4);
        }

        private static void ApplyLabelChanges(Label label)
        {
            // Fix mass driver label overflow on top of combo box issue
            if (label.Name == "label17" && label.Text == "Mass Driver Destination")
            {
                label.Location = new Point(label.Location.X - 10, label.Location.Y);
            }
        }

        private static void ApplyFormChanges(Form form)
        {
            form.ShowIcon = false; // Aurora uses default Windows Forms icons
        }

        private static void OnTimeIncrementButtonClick(Object sender, EventArgs e)
        {
            var button = sender as Button;
            lastActiveTimeIncrement = button.Name;

            List<Button> timeIncrementButtons = lib.KnowledgeBase.GetTimeIncrementButtons().ToList();
            timeIncrementButtons.AddRange(lib.KnowledgeBase.GetTimeIncrementButtonsGalacticMap().ToList());

            foreach (Button timeIncrementButton in timeIncrementButtons)
            {
                ApplyActiveButtonStyle(timeIncrementButton, timeIncrementButton.Name == lastActiveTimeIncrement);
            }
        }

        private static void OnSubPulseButtonClick(Object sender, EventArgs e)
        {
            var button = sender as Button;
            activeSubPulse = button.Name;

            foreach (Button subPulseButton in lib.KnowledgeBase.GetSubPulseButtons())
            {
                ApplyActiveButtonStyle(subPulseButton, subPulseButton.Name == activeSubPulse);
            }
        }

        private static void ApplyActiveButtonStyle(Button button, bool isActive)
        {
            button.FlatAppearance.BorderColor = isActive
                ? ControlPaint.Light(buttonBackgroundColor, 0.5f)
                : mainBackgroundColor;
        }

        private static void OnSpaceMasterButtonBackgroundImageChanged(Object sender, EventArgs e)
        {
            var button = sender as Button;

            button.BackgroundImageChanged -= OnSpaceMasterButtonBackgroundImageChanged;

            // NOTE: This guard is needed as you can have both tactical and galactic maps
            // open at the same time (with duplicated buttons between the two).
            if (button.FindForm() == Form.ActiveForm)
            {
                isSpaceMasterEnabled = !isSpaceMasterEnabled;
            }

            ApplySpaceMasterButtonStyle(button);
            button.BackgroundImageChanged += OnSpaceMasterButtonBackgroundImageChanged;
        }

        private static void ApplySpaceMasterButtonStyle(Button button)
        {
            Bitmap image = isSpaceMasterEnabled ? spaceMasterOnImage : spaceMasterOffImage;
            Color color = isSpaceMasterEnabled ? enabledSpaceMasterButtonColor : mainTextColor;

            button.BackgroundImage = ColorizeImage(image, color);

            foreach (var form in lib.GetOpenForms())
            {
                var buttonCopy = form.Controls.Find(button.Name, true).FirstOrDefault();
                if (buttonCopy != null && buttonCopy != button)
                {
                    buttonCopy.BackgroundImageChanged -= OnSpaceMasterButtonBackgroundImageChanged;
                    buttonCopy.BackgroundImage = button.BackgroundImage;
                    buttonCopy.BackgroundImageChanged += OnSpaceMasterButtonBackgroundImageChanged;
                }
            }
        }

        private static void OnAutoTurnsButtonBackgroundImageChanged(Object sender, EventArgs e)
        {
            var button = sender as Button;

            button.BackgroundImageChanged -= OnAutoTurnsButtonBackgroundImageChanged;

            if (button.FindForm() == Form.ActiveForm)
            {
                isAutoTurnsEnabled = !isAutoTurnsEnabled;
            }

            ApplyAutoTurnsButtonStyle(button);
            button.BackgroundImageChanged += OnAutoTurnsButtonBackgroundImageChanged;
        }

        private static void ApplyAutoTurnsButtonStyle(Button button)
        {
            Bitmap image = isAutoTurnsEnabled ? autoTurnsOnImage : autoTurnsOffImage;
            Color color = isAutoTurnsEnabled ? enabledAutoTurnsButtonColor : mainTextColor;

            button.BackgroundImage = ColorizeImage(image, color);

            foreach (var form in lib.GetOpenForms())
            {
                var buttonCopy = form.Controls.Find(button.Name, true).FirstOrDefault();
                if (buttonCopy != null && buttonCopy != button)
                {
                    buttonCopy.BackgroundImageChanged -= OnAutoTurnsButtonBackgroundImageChanged;
                    buttonCopy.BackgroundImage = button.BackgroundImage;
                    buttonCopy.BackgroundImageChanged += OnAutoTurnsButtonBackgroundImageChanged;
                }
            }
        }

        private static void ChangeButtonStyle(AuroraButton button, Bitmap image, Color textColor, Color? backgroundColor = null)
        {
            Bitmap colorizedImage = ColorizeImage(image, textColor);

            ThemeCreator.ThemeCreator.AddImageChange(button, colorizedImage);

            if (backgroundColor != null)
            {
                ThemeCreator.ThemeCreator.AddColorChange(
                    (Control control) =>
                    {
                        return control.GetType() == typeof(Button) && control.Name == lib.KnowledgeBase.GetButtonName(button);
                    },
                    new ThemeCreator.ColorChange { BackgroundColor = backgroundColor }
                );
            }
        }

        private static Bitmap ColorizeImage(Bitmap image, Color color)
        {
            var imageAttributes = new ImageAttributes();

            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;

            float[][] colorMatrixElements = {
               new float[] {0, 0, 0, 0, 0},
               new float[] {0, 0, 0, 0, 0},
               new float[] {0, 0, 0, 0, 0},
               new float[] {0, 0, 0, 1, 0},
               new float[] {r, g, b, 0, 1}
            };

            var colorMatrix = new ColorMatrix(colorMatrixElements);

            imageAttributes.SetColorMatrix(colorMatrix);

            var colorizedImage = new Bitmap(image.Width, image.Height);
            var graphics = Graphics.FromImage(colorizedImage);
            var rect = new Rectangle(0, 0, image.Width, image.Height);

            graphics.DrawImage(image, rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttributes);

            return colorizedImage;
        }

        protected override void ChangeSettings()
        {
            // Font
            Button fontButton = new Button();
            fontButton.Text = "Font";
            fontButton.Click += (sender, e) => selectFontDialog();
            fontButton.Anchor = (AnchorStyles.Top | AnchorStyles.Left);
            // Background
            Button backgroundButton = new Button();
            backgroundButton.Text = "Background";
            backgroundButton.Click += (sender, e) => selectBackgroundDialog();
            backgroundButton.Anchor = (AnchorStyles.Top);
            // Foreground
            Button foregroundButton = new Button();
            foregroundButton.Text = "Foreground";
            foregroundButton.Click += (sender, e) => selectForegroundDialog();
            foregroundButton.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            // Settings
            Form settingsDialog = new Form();
            settingsDialog.Text = "Customise Theme";
            FlowLayoutPanel flPanel = new FlowLayoutPanel();
            flPanel.Size = new System.Drawing.Size(104, 32);
            flPanel.AutoSize = true;
            flPanel.Controls.Add(fontButton);
            flPanel.Controls.Add(backgroundButton);
            flPanel.Controls.Add(foregroundButton);
            settingsDialog.Controls.Add(flPanel);
            settingsDialog.ShowDialog();
            settingsDialog.Size = new System.Drawing.Size(104, 32);
            // Write settings to files            
            if(font != null) Serialize("font", font);
            if (mainBackgroundColor != Color.FromArgb(12, 12, 12)) Serialize("background", mainBackgroundColor);
            if (mainTextColor != Color.FromArgb(210, 210, 210)) Serialize("foreground", mainTextColor);
            updateColours();
        }

        private void selectFontDialog() {
            var dialog = new FontDialog();
            if (font != null) dialog.Font = font;
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                font = dialog.Font;
            }
        }

        private void selectBackgroundDialog() {
            var dialog = new ColorDialog();
            if (mainBackgroundColor != null) dialog.Color = mainBackgroundColor;
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                mainBackgroundColor = dialog.Color;
            }
        }

        private void selectForegroundDialog() {
            var dialog = new ColorDialog();
            if (mainTextColor != null) dialog.Color = mainTextColor;
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                mainTextColor = dialog.Color;
            }
        }

        private void updateColours() {
            buttonBackgroundColor = ControlPaint.Light(mainBackgroundColor, 0.1f);
            disabledTextColor = ControlPaint.Dark(mainTextColor, 0.1f);
        }
    }
}
