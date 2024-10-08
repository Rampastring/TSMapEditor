using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.TerrainGenerator
{
    /// <summary>
    /// A window that allows the user to configure the terrain generator (see <see cref="TerrainGeneratorConfiguration"/>).
    /// </summary>
    public class TerrainGeneratorConfigWindow : EditorWindow
    {
        public TerrainGeneratorConfigWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        public event EventHandler ConfigApplied;

        private readonly Map map;

        public TerrainGeneratorConfiguration TerrainGeneratorConfig { get; private set; }

        private TerrainGeneratorTerrainTypeGroupsPanel terrainTypeGroupsPanel;
        private TerrainGeneratorTileGroupsPanel tileGroupsPanel;
        private TerrainGeneratorOverlayGroupsPanel overlayGroupsPanel;
        private TerrainGeneratorSmudgeGroupsPanel smudgeGroupsPanel;

        private TerrainGeneratorUserPresets terrainGeneratorUserPresets;
        private InputTerrainGeneratorPresetNameWindow inputTerrainGeneratorPresetNameWindow;
        private DeleteTerrainGeneratorPresetWindow deleteTerrainGeneratorPresetWindow;

        private XNADropDown ddPresets;

        private XNAPanel[] panels;

        public override void Initialize()
        {
            Width = 800;
            Name = nameof(TerrainGeneratorConfigWindow);

            var lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = nameof(lblHeader);
            lblHeader.FontIndex = Constants.UIBoldFont;
            lblHeader.Text = "TERRAIN GENERATOR CONFIGURATION";
            lblHeader.Y = Constants.UIEmptyTopSpace;
            AddChild(lblHeader);
            lblHeader.CenterOnParentHorizontally();

            var lblPresets = new XNALabel(WindowManager);
            lblPresets.Name = nameof(lblPresets);
            lblPresets.Y = lblHeader.Bottom + Constants.UIEmptyTopSpace;
            lblPresets.X = Constants.UIEmptySideSpace;
            lblPresets.Text = "Load Preset Config:";
            AddChild(lblPresets);

            ddPresets = new XNADropDown(WindowManager);
            ddPresets.Name = nameof(ddPresets);
            ddPresets.Width = 250;
            ddPresets.Y = lblPresets.Y - 1;
            ddPresets.X = lblPresets.Right + Constants.UIHorizontalSpacing;
            AddChild(ddPresets);
            ddPresets.SelectedIndexChanged += DdPresets_SelectedIndexChanged;
            InitPresets();

            var btnSaveConfig = new EditorButton(WindowManager);
            btnSaveConfig.Name = nameof(btnSaveConfig);
            btnSaveConfig.Width = 160;
            btnSaveConfig.X = ddPresets.Right + Constants.UIHorizontalSpacing * 2;
            btnSaveConfig.Y = ddPresets.Y;
            btnSaveConfig.Text = "Save Custom Preset...";
            AddChild(btnSaveConfig);
            btnSaveConfig.LeftClick += BtnSaveConfig_LeftClick;

            var btnDeleteConfig = new EditorButton(WindowManager);
            btnDeleteConfig.Name = nameof(btnSaveConfig);
            btnDeleteConfig.Width = 160;
            btnDeleteConfig.X = btnSaveConfig.Right + Constants.UIHorizontalSpacing;
            btnDeleteConfig.Y = btnSaveConfig.Y;
            btnDeleteConfig.Text = "Delete Custom Preset...";
            AddChild(btnDeleteConfig);
            btnDeleteConfig.LeftClick += BtnDeleteConfig_LeftClick;

            var customUISettings = UISettings.ActiveSettings as CustomUISettings;

            const int tabWidth = 150;
            const int tabHeight = 24;

            var idleTexture = Helpers.CreateUITexture(GraphicsDevice, tabWidth, tabHeight,
                customUISettings.ButtonMainBackgroundColor,
                customUISettings.ButtonSecondaryBackgroundColor,
                customUISettings.ButtonTertiaryBackgroundColor);

            var selectedTexture = Helpers.CreateUITexture(GraphicsDevice, tabWidth, tabHeight,
                new Color(128, 128, 128, 196),
                new Color(128, 128, 128, 255), Color.White);

            var tabControl = new XNATabControl(WindowManager);
            tabControl.Name = nameof(tabControl);
            tabControl.X = Constants.UIEmptySideSpace;
            tabControl.Y = ddPresets.Bottom + Constants.UIEmptyTopSpace;
            tabControl.Width = Width;
            tabControl.Height = idleTexture.Height;
            tabControl.FontIndex = Constants.UIBoldFont;
            tabControl.AddTab("Terrain Types", idleTexture, selectedTexture);
            tabControl.AddTab("Terrain Tiles", idleTexture, selectedTexture);
            tabControl.AddTab("Overlays", idleTexture, selectedTexture);
            tabControl.AddTab("Smudges", idleTexture, selectedTexture);
            AddChild(tabControl);
            tabControl.SelectedIndexChanged += (s, e) => { HideAllPanels(); panels[tabControl.SelectedTab].Enable(); };

            panels = new XNAPanel[4];

            terrainTypeGroupsPanel = new TerrainGeneratorTerrainTypeGroupsPanel(WindowManager, map);
            terrainTypeGroupsPanel.Name = nameof(terrainTypeGroupsPanel);
            terrainTypeGroupsPanel.X = Constants.UIEmptySideSpace;
            terrainTypeGroupsPanel.Y = tabControl.Bottom + 1;
            terrainTypeGroupsPanel.Width = Width - Constants.UIEmptySideSpace * 2;
            AddChild(terrainTypeGroupsPanel);
            panels[0] = terrainTypeGroupsPanel;

            tileGroupsPanel = new TerrainGeneratorTileGroupsPanel(WindowManager, map);
            tileGroupsPanel.Name = nameof(tileGroupsPanel);
            tileGroupsPanel.X = terrainTypeGroupsPanel.X;
            tileGroupsPanel.Y = terrainTypeGroupsPanel.Y;
            tileGroupsPanel.Width = terrainTypeGroupsPanel.Width;
            tileGroupsPanel.Height = terrainTypeGroupsPanel.Height;
            AddChild(tileGroupsPanel);
            panels[1] = tileGroupsPanel;

            overlayGroupsPanel = new TerrainGeneratorOverlayGroupsPanel(WindowManager, map);
            overlayGroupsPanel.Name = nameof(overlayGroupsPanel);
            overlayGroupsPanel.X = terrainTypeGroupsPanel.X;
            overlayGroupsPanel.Y = terrainTypeGroupsPanel.Y;
            overlayGroupsPanel.Width = terrainTypeGroupsPanel.Width;
            overlayGroupsPanel.Height = terrainTypeGroupsPanel.Height;
            AddChild(overlayGroupsPanel);
            panels[2] = overlayGroupsPanel;

            smudgeGroupsPanel = new TerrainGeneratorSmudgeGroupsPanel(WindowManager, map);
            smudgeGroupsPanel.Name = nameof(smudgeGroupsPanel);
            smudgeGroupsPanel.X = terrainTypeGroupsPanel.X;
            smudgeGroupsPanel.Y = terrainTypeGroupsPanel.Y;
            smudgeGroupsPanel.Width = terrainTypeGroupsPanel.Width;
            smudgeGroupsPanel.Height = terrainTypeGroupsPanel.Height;
            AddChild(smudgeGroupsPanel);
            panels[3] = smudgeGroupsPanel;

            HideAllPanels();

            tabControl.SelectedTab = 0;
            panels[0].Enable();

            var btnApply = new EditorButton(WindowManager);
            btnApply.Name = nameof(btnApply);
            btnApply.Y = terrainTypeGroupsPanel.Bottom + Constants.UIEmptyTopSpace;
            btnApply.Width = 100;
            btnApply.Text = "Apply";
            AddChild(btnApply);
            btnApply.CenterOnParentHorizontally();
            btnApply.LeftClick += BtnApply_LeftClick;

            Height = btnApply.Bottom + Constants.UIEmptyBottomSpace;

            var closeButton = new EditorButton(WindowManager);
            closeButton.Name = "btnCloseX";
            closeButton.Width = Constants.UIButtonHeight;
            closeButton.Height = Constants.UIButtonHeight;
            closeButton.Text = "X";
            closeButton.X = Width - closeButton.Width;
            closeButton.Y = 0;
            AddChild(closeButton);
            closeButton.LeftClick += (s, e) => Hide();

            terrainGeneratorUserPresets = new TerrainGeneratorUserPresets(map);
            terrainGeneratorUserPresets.Load();

            inputTerrainGeneratorPresetNameWindow = new InputTerrainGeneratorPresetNameWindow(WindowManager, terrainGeneratorUserPresets, map);
            var presetNameWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, inputTerrainGeneratorPresetNameWindow);
            inputTerrainGeneratorPresetNameWindow.SaveAccepted += InputTerrainGeneratorPresetNameWindow_SaveAccepted;

            InitUserPresets();

            deleteTerrainGeneratorPresetWindow = new DeleteTerrainGeneratorPresetWindow(WindowManager, terrainGeneratorUserPresets);
            var deletePresetWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, deleteTerrainGeneratorPresetWindow);
            deletePresetWindowDarkeningPanel.Hidden += DeletePresetWindowDarkeningPanel_Hidden;

            base.Initialize();
        }

        private void BtnDeleteConfig_LeftClick(object sender, EventArgs e) => deleteTerrainGeneratorPresetWindow.Open(null);

        private void BtnSaveConfig_LeftClick(object sender, EventArgs e) => inputTerrainGeneratorPresetNameWindow.Open();

        private void InputTerrainGeneratorPresetNameWindow_SaveAccepted(object sender, string presetName)
        {
            var config = GatherConfiguration(presetName);
            terrainGeneratorUserPresets.AddConfig(config);
            InitUserPresets();

            bool success = terrainGeneratorUserPresets.SaveIfDirty();

            if (!success)
            {
                EditorMessageBox.Show(WindowManager, "Failed to save presets", 
                    "Failed to save terrain generator presets. Please see the map editor logfile for details.", MessageBoxButtons.OK);
            }
        }

        private void DeletePresetWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            var configToDelete = deleteTerrainGeneratorPresetWindow.SelectedObject;

            if (configToDelete == null)
                return;

            terrainGeneratorUserPresets.DeleteConfig(configToDelete.Name);
            InitUserPresets();

            bool success = terrainGeneratorUserPresets.SaveIfDirty();

            if (!success)
            {
                EditorMessageBox.Show(WindowManager, "Failed to save presets",
                    "Failed to save terrain generator presets. Please see the map editor logfile for details.", MessageBoxButtons.OK);
            }
        }

        private void HideAllPanels()
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null)
                    panels[i].Disable();
            }
        }

        private void InitPresets()
        {
            ddPresets.Items.Clear();

            var presetsIni = new IniFile(Environment.CurrentDirectory + "/Config/TerrainGeneratorPresets.ini");
            foreach (string sectionName in presetsIni.GetSections())
            {
                string theater = presetsIni.GetStringValue(sectionName, "Theater", string.Empty);
                if (!string.IsNullOrWhiteSpace(theater) && !theater.Equals(map.TheaterName, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var presetConfiguration = TerrainGeneratorConfiguration.FromConfigSection(
                    presetsIni.GetSection(sectionName),
                    false,
                    map.Rules.TerrainTypes,
                    map.TheaterInstance.Theater.TileSets,
                    map.Rules.OverlayTypes,
                    map.Rules.SmudgeTypes);

                if (presetConfiguration != null)
                    ddPresets.AddItem(new XNADropDownItem() { Text = presetConfiguration.Name, Tag = presetConfiguration, TextColor = presetConfiguration.Color });
            }
        }

        private void InitUserPresets()
        {
            // Remove all existing user presets from the dropdown
            var itemsCopy = new List<XNADropDownItem>(ddPresets.Items);

            foreach (var item in itemsCopy)
            {
                var config = (TerrainGeneratorConfiguration)item.Tag;
                if (config.IsUserConfiguration)
                    ddPresets.Items.Remove(item);
            }

            var configs = terrainGeneratorUserPresets.GetConfigurationsForCurrentTheater();
            configs.ForEach(c => ddPresets.AddItem(new XNADropDownItem() { Text = c.Name, Tag = c }));
        }

        private void DdPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddPresets.SelectedIndex < 0)
                return;

            var config = ddPresets.SelectedItem.Tag as TerrainGeneratorConfiguration;
            LoadConfig(config);
            ddPresets.SelectedIndex = -1;
        }

        private void BtnApply_LeftClick(object sender, EventArgs e)
        {
            TerrainGeneratorConfig = GatherConfiguration("Customized Configuration");

            if (TerrainGeneratorConfig != null)
            {
                // Do not close the window if there's an error condition
                Hide();

                ConfigApplied?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Open()
        {
            Show();

            if (TerrainGeneratorConfig == null)
            {
                // Load first preset as default config if one exists

                if (ddPresets.Items.Count > 0)
                {
                    var config = ddPresets.Items[0].Tag as TerrainGeneratorConfiguration;
                    LoadConfig(config);
                }
                else
                {
                    LoadConfig(new TerrainGeneratorConfiguration("Blank Config",
                        map.LoadedTheaterName,
                        true,
                        new List<TerrainGeneratorTerrainTypeGroup>(),
                        new List<TerrainGeneratorTileGroup>(),
                        new List<TerrainGeneratorOverlayGroup>(),
                        new List<TerrainGeneratorSmudgeGroup>()));
                }
            }
        }

        private void LoadConfig(TerrainGeneratorConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            terrainTypeGroupsPanel.LoadConfig(configuration);
            tileGroupsPanel.LoadConfig(configuration);
            overlayGroupsPanel.LoadConfig(configuration);
            smudgeGroupsPanel.LoadConfig(configuration);
        }

        private TerrainGeneratorConfiguration GatherConfiguration(string name)
        {
            var terrainTypeGroups = terrainTypeGroupsPanel.GetTerrainTypeGroups();
            var tileGroups = tileGroupsPanel.GetTileGroups();
            var overlayGroups = overlayGroupsPanel.GetOverlayGroups();
            var smudgeGroups = smudgeGroupsPanel.GetSmudgeGroups();

            // One of the panels returning null means there's an error condition
            if (terrainTypeGroups == null || tileGroups == null || overlayGroups == null || smudgeGroups == null)
                return null;

            return new TerrainGeneratorConfiguration(name, map.LoadedTheaterName, true, terrainTypeGroups, tileGroups, overlayGroups, smudgeGroups);
        }
    }
}
