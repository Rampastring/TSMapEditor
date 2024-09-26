using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class InfantryOptionsWindow : INItializableWindow
    {
        public InfantryOptionsWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        public event EventHandler<TagEventArgs> TagOpened;

        private readonly Map map;

        private XNATrackbar trbStrength;
        private XNALabel lblStrengthValue;
        private XNADropDown ddMission;
        private XNADropDown ddVeterancy;
        private EditorNumberTextBox tbGroup;
        private XNACheckBox chkOnBridge;
        private XNACheckBox chkAutocreateNoRecruitable;
        private XNACheckBox chkAutocreateYesRecruitable;
        private EditorPopUpSelector attachedTagSelector;
        private XNALabel lblHeader;

        private List<Infantry> infantry = [];

        private SelectTagWindow selectTagWindow;

        public override void Initialize()
        {
            Name = nameof(InfantryOptionsWindow);
            base.Initialize();

            trbStrength = FindChild<XNATrackbar>(nameof(trbStrength));
            lblStrengthValue = FindChild<XNALabel>(nameof(lblStrengthValue));
            ddMission = FindChild<XNADropDown>(nameof(ddMission));
            ddVeterancy = FindChild<XNADropDown>(nameof(ddVeterancy));
            tbGroup = FindChild<EditorNumberTextBox>(nameof(tbGroup));
            chkOnBridge = FindChild<XNACheckBox>(nameof(chkOnBridge));
            chkAutocreateNoRecruitable = FindChild<XNACheckBox>(nameof(chkAutocreateNoRecruitable));
            chkAutocreateYesRecruitable = FindChild<XNACheckBox>(nameof(chkAutocreateYesRecruitable));
            attachedTagSelector = FindChild<EditorPopUpSelector>(nameof(attachedTagSelector));
            lblHeader = FindChild<XNALabel>(nameof(lblHeader));

            trbStrength.ValueChanged += TrbStrength_ValueChanged;
            attachedTagSelector.LeftClick += AttachedTagSelector_LeftClick;

            FindChild<EditorButton>("btnOpenAttachedTrigger").LeftClick += BtnOpenAttachedTrigger_LeftClick;

            selectTagWindow = new SelectTagWindow(WindowManager, map);
            var tagDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTagWindow);
            tagDarkeningPanel.Hidden += (s, e) =>
            {
                var tag = selectTagWindow.SelectedObject;
                attachedTagSelector.Tag = tag;
                attachedTagSelector.Text = tag == null ? string.Empty : tag.GetDisplayString();
            };

            try
            {
                ddVeterancy.Items.ForEach(ddItem => ddItem.Tag = int.Parse(ddItem.Text.Substring(0, ddItem.Text.IndexOf(' ')), CultureInfo.InvariantCulture));
            }
            catch (FormatException)
            {
                throw new INIConfigException($"Invalid options specified for {nameof(ddVeterancy)} in {nameof(InfantryOptionsWindow)}. Options must start with a number followed by a space.");
            }

            FindChild<EditorButton>("btnOK").LeftClick += BtnOK_LeftClick;
        }

        private void TrbStrength_ValueChanged(object sender, EventArgs e)
        {
            lblStrengthValue.Text = trbStrength.Value.ToString(CultureInfo.InvariantCulture);
        }

        private void BtnOpenAttachedTrigger_LeftClick(object sender, EventArgs e)
        {
            foreach (var infantryUnit in infantry)
            {
                if (infantryUnit.AttachedTag == null)
                    continue;                

                TagOpened?.Invoke(this, new TagEventArgs(infantryUnit.AttachedTag));
                PutOnBackground();
                return;
            }
        }

        private void AttachedTagSelector_LeftClick(object sender, EventArgs e)
        {
            selectTagWindow.Open(infantry[0].AttachedTag);
        }

        public void Open(List<Infantry> infantry)
        {
            this.infantry = infantry;
            RefreshValues();
            Show();
        }

        private void RefreshValues()
        {
            var amountIndex = lblHeader.Text.IndexOf(" (");
            if (amountIndex >= 0)
                lblHeader.Text = lblHeader.Text.Substring(0, amountIndex);
            
            lblHeader.Text += $" (x{infantry.Count})";

            trbStrength.Value = infantry[0].HP;
            ddMission.SelectedIndex = ddMission.Items.FindIndex(item => item.Text == infantry[0].Mission);
            int veterancyIndex = ddVeterancy.Items.FindIndex(i => (int)i.Tag == infantry[0].Veterancy);
            ddVeterancy.SelectedIndex = Math.Max(0, veterancyIndex);
            tbGroup.Value = infantry[0].Group;
            chkOnBridge.Checked = infantry[0].High;
            chkAutocreateNoRecruitable.Checked = infantry[0].AutocreateNoRecruitable;
            chkAutocreateYesRecruitable.Checked = infantry[0].AutocreateYesRecruitable;
            attachedTagSelector.Text = infantry[0].AttachedTag == null ? string.Empty : infantry[0].AttachedTag.GetDisplayString();
            attachedTagSelector.Tag = infantry[0].AttachedTag;
        }

        private void BtnOK_LeftClick(object sender, EventArgs e)
        {
            foreach (var infantryUnit in infantry)
            {
                infantryUnit.HP = Math.Min(Constants.ObjectHealthMax, Math.Max(trbStrength.Value, 0));
                infantryUnit.Mission = ddMission.SelectedItem == null ? infantryUnit.Mission : ddMission.SelectedItem.Text;
                infantryUnit.Veterancy = (int)ddVeterancy.SelectedItem.Tag;
                infantryUnit.Group = tbGroup.Value;
                infantryUnit.High = chkOnBridge.Checked;
                infantryUnit.AutocreateNoRecruitable = chkAutocreateNoRecruitable.Checked;
                infantryUnit.AutocreateYesRecruitable = chkAutocreateYesRecruitable.Checked;
                infantryUnit.AttachedTag = (Tag)attachedTagSelector.Tag;
            }

            Hide();
        }
    }
}

