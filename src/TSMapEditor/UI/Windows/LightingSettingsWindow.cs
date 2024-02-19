using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class LightingSettingsWindow : INItializableWindow
    {
        public LightingSettingsWindow(WindowManager windowManager, Map map, EditorState state) : base(windowManager)
        {
            this.map = map;
            this.state = state;
        }

        private readonly Map map;
        private readonly EditorState state;

        private EditorNumberTextBox tbAmbientNormal;
        private EditorNumberTextBox tbLevelNormal;
        private EditorNumberTextBox tbGroundNormal;
        private EditorNumberTextBox tbRedNormal;
        private EditorNumberTextBox tbGreenNormal;
        private EditorNumberTextBox tbBlueNormal;

        private EditorNumberTextBox tbAmbientIS;
        private EditorNumberTextBox tbLevelIS;
        private EditorNumberTextBox tbGroundIS;
        private EditorNumberTextBox tbRedIS;
        private EditorNumberTextBox tbGreenIS;
        private EditorNumberTextBox tbBlueIS;

        private EditorNumberTextBox tbAmbientDominator;
        private EditorNumberTextBox tbAmbientChangeRateDominator;
        private EditorNumberTextBox tbLevelDominator;
        private EditorNumberTextBox tbGroundDominator;
        private EditorNumberTextBox tbRedDominator;
        private EditorNumberTextBox tbGreenDominator;
        private EditorNumberTextBox tbBlueDominator;

        private XNADropDown ddLightingPreview;

        public override void Initialize()
        {
            Name = nameof(LightingSettingsWindow);
            base.Initialize();

            tbAmbientNormal = FindChild<EditorNumberTextBox>(nameof(tbAmbientNormal));
            tbLevelNormal = FindChild<EditorNumberTextBox>(nameof(tbLevelNormal));
            tbGroundNormal = FindChild<EditorNumberTextBox>(nameof(tbGroundNormal));
            tbRedNormal = FindChild<EditorNumberTextBox>(nameof(tbRedNormal));
            tbGreenNormal = FindChild<EditorNumberTextBox>(nameof(tbGreenNormal));
            tbBlueNormal = FindChild<EditorNumberTextBox>(nameof(tbBlueNormal));

            tbAmbientIS = FindChild<EditorNumberTextBox>(nameof(tbAmbientIS));
            tbLevelIS = FindChild<EditorNumberTextBox>(nameof(tbLevelIS));
            tbGroundIS = FindChild<EditorNumberTextBox>(nameof(tbGroundIS));
            tbRedIS = FindChild<EditorNumberTextBox>(nameof(tbRedIS));
            tbGreenIS = FindChild<EditorNumberTextBox>(nameof(tbGreenIS));
            tbBlueIS = FindChild<EditorNumberTextBox>(nameof(tbBlueIS));

            tbAmbientDominator = FindChild<EditorNumberTextBox>(nameof(tbAmbientDominator));
            tbAmbientChangeRateDominator = FindChild<EditorNumberTextBox>(nameof(tbAmbientChangeRateDominator));
            tbLevelDominator = FindChild<EditorNumberTextBox>(nameof(tbLevelDominator));
            tbGroundDominator = FindChild<EditorNumberTextBox>(nameof(tbGroundDominator));
            tbRedDominator = FindChild<EditorNumberTextBox>(nameof(tbRedDominator));
            tbGreenDominator = FindChild<EditorNumberTextBox>(nameof(tbGreenDominator));
            tbBlueDominator = FindChild<EditorNumberTextBox>(nameof(tbBlueDominator));

            ddLightingPreview = FindChild<XNADropDown>(nameof(ddLightingPreview));

            FindChild<EditorButton>("btnApply").LeftClick += BtnApply_LeftClick;
        }

        public void Open()
        {
            const string format = "0.000";

            tbAmbientNormal.Text = map.Lighting.Ambient.ToString(format);
            tbLevelNormal.Text = map.Lighting.Level.ToString(format);
            tbGroundNormal.Text = map.Lighting.Ground.ToString(format);
            tbRedNormal.Text = map.Lighting.Red.ToString(format);
            tbGreenNormal.Text = map.Lighting.Green.ToString(format);
            tbBlueNormal.Text = map.Lighting.Blue.ToString(format);

            tbAmbientIS.Text = map.Lighting.IonAmbient.ToString(format);
            tbLevelIS.Text = map.Lighting.IonLevel.ToString(format);
            tbGroundIS.Text = map.Lighting.IonGround.ToString(format);
            tbRedIS.Text = map.Lighting.IonRed.ToString(format);
            tbGreenIS.Text = map.Lighting.IonGreen.ToString(format);
            tbBlueIS.Text = map.Lighting.IonBlue.ToString(format);

            if (Constants.UseCountries)
            {
                tbAmbientDominator.Text = (map.Lighting.DominatorAmbient ?? 0).ToString(format);
                tbAmbientChangeRateDominator.Text = (map.Lighting.DominatorAmbientChangeRate ?? 0).ToString(format);
                tbLevelDominator.Text = (map.Lighting.DominatorLevel ?? 0).ToString(format);
                tbGroundDominator.Text = (map.Lighting.DominatorGround ?? 0).ToString(format);
                tbRedDominator.Text = (map.Lighting.DominatorRed ?? 0).ToString(format);
                tbGreenDominator.Text = (map.Lighting.DominatorGreen ?? 0).ToString(format);
                tbBlueDominator.Text = (map.Lighting.DominatorBlue ?? 0).ToString(format);
            }

            ddLightingPreview.SelectedIndex = state.LightingPreviewState switch
            {
                LightingPreviewMode.NoLighting => 0,
                LightingPreviewMode.Normal => 1,
                LightingPreviewMode.IonStorm => 2,
                LightingPreviewMode.Dominator => 3,
                _ => throw new System.NotImplementedException(),
            };

            Show();
        }

        private void BtnApply_LeftClick(object sender, System.EventArgs e)
        {
            map.Lighting.Ambient = tbAmbientNormal.DoubleValue;
            map.Lighting.Level = tbLevelNormal.DoubleValue;
            map.Lighting.Ground = tbGroundNormal.DoubleValue;
            map.Lighting.Red = tbRedNormal.DoubleValue;
            map.Lighting.Green = tbGreenNormal.DoubleValue;
            map.Lighting.Blue = tbBlueNormal.DoubleValue;

            map.Lighting.IonAmbient = tbAmbientIS.DoubleValue;
            map.Lighting.IonLevel = tbLevelIS.DoubleValue;
            map.Lighting.IonGround = tbGroundIS.DoubleValue;
            map.Lighting.IonRed = tbRedIS.DoubleValue;
            map.Lighting.IonGreen = tbGreenIS.DoubleValue;
            map.Lighting.IonBlue = tbBlueIS.DoubleValue;

            if (Constants.UseCountries)
            {
                map.Lighting.DominatorAmbient = tbAmbientDominator.DoubleValue;
                map.Lighting.DominatorAmbientChangeRate = tbAmbientChangeRateDominator.DoubleValue;
                map.Lighting.DominatorLevel = tbLevelDominator.DoubleValue;
                map.Lighting.DominatorGround = tbGroundDominator.DoubleValue;
                map.Lighting.DominatorRed = tbRedDominator.DoubleValue;
                map.Lighting.DominatorGreen = tbGreenDominator.DoubleValue;
                map.Lighting.DominatorBlue = tbBlueDominator.DoubleValue;
            }

            state.LightingPreviewState = ddLightingPreview.SelectedIndex switch
            {
                0 => LightingPreviewMode.NoLighting,
                1 => LightingPreviewMode.Normal,
                2 => LightingPreviewMode.IonStorm,
                3 => LightingPreviewMode.Dominator,
                _ => LightingPreviewMode.NoLighting,
            };

            map.Lighting.RefreshLightingColors();

            Hide();
        }
    }
}
