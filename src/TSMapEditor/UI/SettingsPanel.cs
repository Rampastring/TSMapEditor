using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Globalization;
using System.Windows.Forms;
using TSMapEditor.GameMath;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI
{
    /// <summary>
    /// A single screen resolution.
    /// </summary>
    sealed class ScreenResolution : IComparable<ScreenResolution>
    {
        public ScreenResolution(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// The width of the resolution in pixels.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The height of the resolution in pixels.
        /// </summary>
        public int Height { get; set; }

        public override string ToString()
        {
            return Width + "x" + Height;
        }

        public int CompareTo(ScreenResolution res2)
        {
            if (this.Width < res2.Width)
                return -1;
            else if (this.Width > res2.Width)
                return 1;
            else // equal
            {
                if (this.Height < res2.Height)
                    return -1;
                else if (this.Height > res2.Height)
                    return 1;
                else return 0;
            }
        }

        public override bool Equals(object obj)
        {
            var resolution = obj as ScreenResolution;

            if (resolution == null)
                return false;

            return CompareTo(resolution) == 0;
        }

        public override int GetHashCode()
        {
            return Width * 10000 + Height;
        }
    }

    public class SettingsPanel : EditorPanel
    {
        public SettingsPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        private XNADropDown ddRenderScale;
        private XNACheckBox chkBorderless;
        private XNADropDown ddTheme;
        private XNADropDown ddScrollRate;
        private XNACheckBox chkUseBoldFont;
        private XNACheckBox chkGraphicsLevel;
        private EditorTextBox tbTextEditorPath;

        public override void Initialize()
        {
            Width = 230;

            var lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = nameof(lblHeader);
            lblHeader.FontIndex = Constants.UIBoldFont;
            lblHeader.Text = "Settings";
            lblHeader.Y = Constants.UIEmptyTopSpace;
            AddChild(lblHeader);
            lblHeader.CenterOnParentHorizontally();

            var lblRenderScale = new XNALabel(WindowManager);
            lblRenderScale.Name = nameof(lblRenderScale);
            lblRenderScale.Text = "Render Scale:";
            lblRenderScale.X = Constants.UIEmptySideSpace;
            lblRenderScale.Y = lblHeader.Bottom + Constants.UIEmptyTopSpace + 1;
            AddChild(lblRenderScale);

            const int MinWidth = 1024;
            const int MinHeight = 600;
            int MaxWidth = Screen.PrimaryScreen.Bounds.Width;
            int MaxHeight = Screen.PrimaryScreen.Bounds.Height;

            ddRenderScale = new XNADropDown(WindowManager);
            ddRenderScale.Name = nameof(ddRenderScale);
            ddRenderScale.X = 120;
            ddRenderScale.Y = lblRenderScale.Y - 1;
            ddRenderScale.Width = Width - ddRenderScale.X - Constants.UIEmptySideSpace;
            AddChild(ddRenderScale);
            var renderScales = new double[] { 4.0, 2.5, 3.0, 2.5, 2.0, 1.75, 1.5, 1.25, 1.0, 0.75, 0.5 };
            for (int i = 0; i < renderScales.Length; i++)
            {
                Point2D screenSize = new Point2D((int)(MaxWidth / renderScales[i]), (int)(MaxHeight / renderScales[i]));
                if (screenSize.X > MinWidth && screenSize.Y > MinHeight)
                {
                    ddRenderScale.AddItem(new XNADropDownItem() { Text = renderScales[i].ToString("F2", CultureInfo.InvariantCulture) + "x", Tag = renderScales[i] });
                }
            }

            var lblTheme = new XNALabel(WindowManager);
            lblTheme.Name = nameof(lblTheme);
            lblTheme.Text = "Theme:";
            lblTheme.X = lblRenderScale.X;
            lblTheme.Y = ddRenderScale.Bottom + Constants.UIEmptyTopSpace;
            AddChild(lblTheme);

            ddTheme = new XNADropDown(WindowManager);
            ddTheme.Name = nameof(ddTheme);
            ddTheme.X = ddRenderScale.X;
            ddTheme.Y = lblTheme.Y - 1;
            ddTheme.Width = ddRenderScale.Width;
            AddChild(ddTheme);
            foreach (var theme in EditorThemes.Themes)
                ddTheme.AddItem(theme.Key);

            var lblScrollRate = new XNALabel(WindowManager);
            lblScrollRate.Name = nameof(lblScrollRate);
            lblScrollRate.Text = "Scroll Rate:";
            lblScrollRate.X = lblRenderScale.X;
            lblScrollRate.Y = ddTheme.Bottom + Constants.UIEmptyTopSpace;
            AddChild(lblScrollRate);

            ddScrollRate = new XNADropDown(WindowManager);
            ddScrollRate.Name = nameof(ddScrollRate);
            ddScrollRate.X = ddRenderScale.X;
            ddScrollRate.Y = lblScrollRate.Y - 1;
            ddScrollRate.Width = ddRenderScale.Width;
            AddChild(ddScrollRate);
            var scrollRateNames = new string[] { "Fastest", "Faster", "Fast", "Normal", "Slow", "Slower", "Slowest" };
            var scrollRateValues = new int[] { 21, 18, 15, 12, 9, 6, 3 };
            for (int i = 0; i < scrollRateNames.Length; i++)
            {
                ddScrollRate.AddItem(new XNADropDownItem() { Text = scrollRateNames[i], Tag = scrollRateValues[i] });
            }

            chkBorderless = new XNACheckBox(WindowManager);
            chkBorderless.Name = nameof(chkBorderless);
            chkBorderless.X = Constants.UIEmptySideSpace;
            chkBorderless.Y = ddScrollRate.Bottom + Constants.UIVerticalSpacing;
            chkBorderless.Text = "Start In Borderless Mode";
            AddChild(chkBorderless);

            chkUseBoldFont = new XNACheckBox(WindowManager);
            chkUseBoldFont.Name = nameof(chkUseBoldFont);
            chkUseBoldFont.X = Constants.UIEmptySideSpace;
            chkUseBoldFont.Y = chkBorderless.Bottom + Constants.UIVerticalSpacing;
            chkUseBoldFont.Text = "Use Bold Font";
            AddChild(chkUseBoldFont);

            chkGraphicsLevel = new XNACheckBox(WindowManager);
            chkGraphicsLevel.Name = nameof(chkGraphicsLevel);
            chkGraphicsLevel.X = Constants.UIEmptySideSpace;
            chkGraphicsLevel.Y = chkUseBoldFont.Bottom + Constants.UIVerticalSpacing;
            chkGraphicsLevel.Text = "Enhanced Graphical Quality";
            AddChild(chkGraphicsLevel);

            var lblTextEditorPath = new XNALabel(WindowManager);
            lblTextEditorPath.Name = nameof(lblTextEditorPath);
            lblTextEditorPath.Text = "Text Editor Path:";
            lblTextEditorPath.X = Constants.UIEmptySideSpace;
            lblTextEditorPath.Y = chkGraphicsLevel.Bottom + Constants.UIVerticalSpacing * 2;
            AddChild(lblTextEditorPath);

            tbTextEditorPath = new EditorTextBox(WindowManager);
            tbTextEditorPath.Name = nameof(tbTextEditorPath);
            tbTextEditorPath.AllowSemicolon = true;
            tbTextEditorPath.X = Constants.UIEmptySideSpace;
            tbTextEditorPath.Y = lblTextEditorPath.Bottom + Constants.UIVerticalSpacing;
            tbTextEditorPath.Width = Width - tbTextEditorPath.X - Constants.UIEmptySideSpace;
            AddChild(tbTextEditorPath);

            LoadSettings();

            base.Initialize();
        }

        private void LoadSettings()
        {
            var userSettings = UserSettings.Instance;

            ddRenderScale.SelectedIndex = ddRenderScale.Items.FindIndex(i => (double)i.Tag == userSettings.RenderScale.GetValue());

            int selectedTheme = ddTheme.Items.FindIndex(i => i.Text == userSettings.Theme);
            if (selectedTheme == -1)
                selectedTheme = ddTheme.Items.FindIndex(i => i.Text == "Default");
            ddTheme.SelectedIndex = selectedTheme;
            ddScrollRate.SelectedIndex = ddScrollRate.Items.FindIndex(item => (int)item.Tag == userSettings.ScrollRate.GetValue());

            chkBorderless.Checked = userSettings.Borderless;
            chkUseBoldFont.Checked = userSettings.UseBoldFont;
            chkGraphicsLevel.Checked = userSettings.GraphicsLevel > 0;

            tbTextEditorPath.Text = userSettings.TextEditorPath;
        }

        public void ApplySettings()
        {
            var userSettings = UserSettings.Instance;

            userSettings.UseBoldFont.UserDefinedValue = chkUseBoldFont.Checked;
            userSettings.GraphicsLevel.UserDefinedValue = chkGraphicsLevel.Checked ? 1 : 0;

            userSettings.Theme.UserDefinedValue = ddTheme.SelectedItem.Text;
            if (ddScrollRate.SelectedItem != null)
                userSettings.ScrollRate.UserDefinedValue = (int)ddScrollRate.SelectedItem.Tag;

            userSettings.Borderless.UserDefinedValue = chkBorderless.Checked;
            userSettings.FullscreenWindowed.UserDefinedValue = chkBorderless.Checked;

            if (ddRenderScale.SelectedItem != null)
            {
                userSettings.RenderScale.UserDefinedValue = (double)ddRenderScale.SelectedItem.Tag;
            }

            userSettings.TextEditorPath.UserDefinedValue = tbTextEditorPath.Text;
        }
    }
}
