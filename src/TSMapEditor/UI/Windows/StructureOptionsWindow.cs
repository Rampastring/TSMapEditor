using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that allows the user to edit the properties of a building.
    /// </summary>
    public class StructureOptionsWindow : INItializableWindow
    {
        public StructureOptionsWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        public event EventHandler<TagEventArgs> TagOpened;

        private readonly Map map;

        private XNATrackbar trbStrength;
        private XNALabel lblStrengthValue;
        private XNACheckBox chkSellable;
        private XNACheckBox chkRebuild;
        private XNACheckBox chkPowered;
        private XNACheckBox chkAIRepairable;
        private XNACheckBox chkNominal;
        private XNADropDown ddSpotlight;
        private XNADropDown ddUpgrade1;
        private XNADropDown ddUpgrade2;
        private XNADropDown ddUpgrade3;
        private EditorPopUpSelector attachedTagSelector;

        private Structure structure;

        private SelectTagWindow selectTagWindow;

        public override void Initialize()
        {
            Name = nameof(StructureOptionsWindow);
            base.Initialize();

            trbStrength = FindChild<XNATrackbar>(nameof(trbStrength));
            lblStrengthValue = FindChild<XNALabel>(nameof(lblStrengthValue));
            chkSellable = FindChild<XNACheckBox>(nameof(chkSellable));
            chkRebuild = FindChild<XNACheckBox>(nameof(chkRebuild));
            chkPowered = FindChild<XNACheckBox>(nameof(chkPowered));
            chkAIRepairable = FindChild<XNACheckBox>(nameof(chkAIRepairable));
            chkNominal = FindChild<XNACheckBox>(nameof(chkNominal));
            ddSpotlight = FindChild<XNADropDown>(nameof(ddSpotlight));
            ddUpgrade1 = FindChild<XNADropDown>(nameof(ddUpgrade1));
            ddUpgrade2 = FindChild<XNADropDown>(nameof(ddUpgrade2));
            ddUpgrade3 = FindChild<XNADropDown>(nameof(ddUpgrade3));
            attachedTagSelector = FindChild<EditorPopUpSelector>(nameof(attachedTagSelector));

            trbStrength.ValueChanged += TrbStrength_ValueChanged;
            attachedTagSelector.LeftClick += AttachedTagSelector_LeftClick;

            FindChild<EditorButton>("btnOpenAttachedTrigger").LeftClick += BtnOpenAttachedTrigger_LeftClick;

            selectTagWindow = new SelectTagWindow(WindowManager, map);
            var tagDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTagWindow);
            tagDarkeningPanel.Hidden += (s, e) => SelectionWindow_ApplyEffect(w => structure.AttachedTag = w.SelectedObject, selectTagWindow);

            FindChild<EditorButton>("btnOK").LeftClick += BtnOK_LeftClick;
        }

        private void TrbStrength_ValueChanged(object sender, EventArgs e)
        {
            lblStrengthValue.Text = trbStrength.Value.ToString(CultureInfo.InvariantCulture);
        }

        private void BtnOpenAttachedTrigger_LeftClick(object sender, EventArgs e)
        {
            if (structure.AttachedTag == null)
                return;

            TagOpened?.Invoke(this, new TagEventArgs(structure.AttachedTag));
            PutOnBackground();
        }

        private void SelectionWindow_ApplyEffect<T>(Action<T> action, T window)
        {
            action(window);
            RefreshValues();
        }

        private void AttachedTagSelector_LeftClick(object sender, EventArgs e)
        {
            selectTagWindow.Open(structure.AttachedTag);
        }

        public void Open(Structure structure)
        {
            this.structure = structure;
            RefreshValues();
            Show();
        }

        private void FillUpgradesForDropDown(XNADropDown dropDown, int index, List<BuildingType> upgrades)
        {
            dropDown.Items.Clear();
            dropDown.AddItem("None");
            dropDown.AllowDropDown = false;

            if (structure.ObjectType.Upgrades > index)
            {
                dropDown.AllowDropDown = upgrades.Count > 0;
                upgrades.ForEach(ubt => dropDown.AddItem(new XNADropDownItem() { Text = ubt.Name, Tag = ubt }));
            }
        }

        private void FetchUpgrade(XNADropDown dropDown, int upgradeIndex, List<BuildingType> upgrades)
        {
            var upgrade = structure.Upgrades[upgradeIndex];

            if (upgrade == null || upgradeIndex >= structure.ObjectType.Upgrades)
            {
                dropDown.SelectedIndex = 0;
                return;
            }

            int index = upgrades.FindIndex(ubt => upgrade == ubt);
            // "None" takes one slot, so we need to add one.
            // In case the upgrade was invalid, -1 + 1 is conveniently 0,
            // which makes "None" get selected.
            dropDown.SelectedIndex = index + 1;
        }

        private void RefreshValues()
        {
            var possibleUpgrades = map.Rules.BuildingTypes.FindAll(bt => !string.IsNullOrWhiteSpace(bt.PowersUpBuilding) &&
                bt.PowersUpBuilding.Equals(structure.ObjectType.ININame, StringComparison.OrdinalIgnoreCase));

            FillUpgradesForDropDown(ddUpgrade1, 0, possibleUpgrades);
            FillUpgradesForDropDown(ddUpgrade2, 1, possibleUpgrades);
            FillUpgradesForDropDown(ddUpgrade3, 2, possibleUpgrades);

            trbStrength.Value = structure.HP;
            chkSellable.Checked = structure.AISellable;
            chkRebuild.Checked = structure.AIRebuildable;
            chkPowered.Checked = structure.Powered;
            chkAIRepairable.Checked = structure.AIRepairable;
            chkNominal.Checked = structure.Nominal;
            ddSpotlight.SelectedIndex = (int)structure.Spotlight;
            FetchUpgrade(ddUpgrade1, 0, possibleUpgrades);
            FetchUpgrade(ddUpgrade2, 1, possibleUpgrades);
            FetchUpgrade(ddUpgrade3, 2, possibleUpgrades);
            attachedTagSelector.Text = structure.AttachedTag == null ? string.Empty : structure.AttachedTag.GetDisplayString();
            attachedTagSelector.Tag = structure.AttachedTag;
        }

        private void BtnOK_LeftClick(object sender, EventArgs e)
        {
            structure.HP = Math.Max(0, Math.Min(trbStrength.Value, Constants.ObjectHealthMax));
            structure.AISellable = chkSellable.Checked;
            structure.AIRebuildable = chkRebuild.Checked;
            structure.Powered = chkPowered.Checked;
            structure.AIRepairable = chkAIRepairable.Checked;
            structure.Nominal = chkNominal.Checked;
            structure.Spotlight = (SpotlightType)ddSpotlight.SelectedIndex;
            structure.Upgrades[0] = (BuildingType)ddUpgrade1.SelectedItem.Tag;
            structure.Upgrades[1] = (BuildingType)ddUpgrade2.SelectedItem.Tag;
            structure.Upgrades[2] = (BuildingType)ddUpgrade3.SelectedItem.Tag;
            structure.AttachedTag = (Tag)attachedTagSelector.Tag;

            structure.UpdatePowerUpAnims();

            Hide();
        }
    }
}
