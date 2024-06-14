using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class TeamTypeEventArgs : EventArgs
    {
        public TeamTypeEventArgs(TeamType teamType)
        {
            TeamType = teamType;
        }

        public TeamType TeamType { get; }
    }

    public class AITriggersWindow : INItializableWindow
    {
        public AITriggersWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public event EventHandler<TeamTypeEventArgs> TeamTypeOpened;

        private EditorListBox lbAITriggers;
        private EditorTextBox tbName;
        private XNADropDown ddSide;
        private XNADropDown ddHouseType;
        private XNADropDown ddConditionType;
        private XNADropDown ddComparator;
        private EditorNumberTextBox tbQuantity;
        private EditorTextBox tbComparisonObjectType;
        private EditorPopUpSelector selPrimaryTeam;
        private EditorPopUpSelector selSecondaryTeam;
        private EditorNumberTextBox tbInitial;
        private EditorNumberTextBox tbMinimum;
        private EditorNumberTextBox tbMaximum;
        private XNACheckBox chkEnabledOnEasy;
        private XNACheckBox chkEnabledOnMedium;
        private XNACheckBox chkEnabledOnHard;

        private SelectTeamTypeWindow selectTeamTypeWindow;

        private AITriggerType editedAITrigger;

        public override void Initialize()
        {
            Name = nameof(AITriggersWindow);
            base.Initialize();

            lbAITriggers = FindChild<EditorListBox>(nameof(lbAITriggers));
            tbName = FindChild<EditorTextBox>(nameof(tbName));
            ddSide = FindChild<XNADropDown>(nameof(ddSide));
            ddHouseType = FindChild<XNADropDown>(nameof(ddHouseType));
            ddConditionType = FindChild<XNADropDown>(nameof(ddConditionType));
            ddComparator = FindChild<XNADropDown>(nameof(ddComparator));
            tbQuantity = FindChild<EditorNumberTextBox>(nameof(tbQuantity));
            tbComparisonObjectType = FindChild<EditorTextBox>(nameof(tbComparisonObjectType));
            selPrimaryTeam = FindChild<EditorPopUpSelector>(nameof(selPrimaryTeam));
            selSecondaryTeam = FindChild<EditorPopUpSelector>(nameof(selSecondaryTeam));
            tbInitial = FindChild<EditorNumberTextBox>(nameof(tbInitial));
            tbMinimum = FindChild<EditorNumberTextBox>(nameof(tbMinimum));
            tbMaximum = FindChild<EditorNumberTextBox>(nameof(tbMaximum));
            chkEnabledOnEasy = FindChild<XNACheckBox>(nameof(chkEnabledOnEasy));
            chkEnabledOnMedium = FindChild<XNACheckBox>(nameof(chkEnabledOnMedium));
            chkEnabledOnHard = FindChild<XNACheckBox>(nameof(chkEnabledOnHard));

            FindChild<EditorButton>("btnNew").LeftClick += BtnNew_LeftClick;
            FindChild<EditorButton>("btnDelete").LeftClick += BtnDelete_LeftClick;
            FindChild<EditorButton>("btnClone").LeftClick += BtnClone_LeftClick;

            FindChild<EditorButton>("btnOpenPrimaryTeam").LeftClick += BtnOpenPrimaryTeam_LeftClick;
            FindChild<EditorButton>("btnOpenSecondaryTeam").LeftClick += BtnOpenSecondaryTeam_LeftClick;

            selectTeamTypeWindow = new SelectTeamTypeWindow(WindowManager, map);
            selectTeamTypeWindow.IncludeNone = true;
            var teamTypeWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTeamTypeWindow);
            teamTypeWindowDarkeningPanel.Hidden += TeamTypeWindowDarkeningPanel_Hidden;

            lbAITriggers.SelectedIndexChanged += LbAITriggers_SelectedIndexChanged;
        }

        private void BtnOpenPrimaryTeam_LeftClick(object sender, EventArgs e)
        {
            if (editedAITrigger == null || editedAITrigger.PrimaryTeam == null)
                return;

            OpenTeamType(editedAITrigger.PrimaryTeam);
        }

        private void BtnOpenSecondaryTeam_LeftClick(object sender, EventArgs e)
        {
            if (editedAITrigger == null || editedAITrigger.SecondaryTeam == null)
                return;

            OpenTeamType(editedAITrigger.SecondaryTeam);
        }

        private void OpenTeamType(TeamType teamType)
        {
            TeamTypeOpened?.Invoke(this, new TeamTypeEventArgs(teamType));
            PutOnBackground();
        }

        private void BtnNew_LeftClick(object sender, EventArgs e)
        {
            var aiTrigger = new AITriggerType(map.GetNewUniqueInternalId());
            aiTrigger.Name = "New AITrigger";
            aiTrigger.OwnerName = "<all>";
            aiTrigger.ConditionObjectString = string.Empty;
            map.AITriggerTypes.Add(aiTrigger);
            ListAITriggers();
            SelectAITrigger(aiTrigger);
        }

        private void BtnDelete_LeftClick(object sender, EventArgs e)
        {
            if (editedAITrigger == null)
                return;

            map.AITriggerTypes.Remove(editedAITrigger);
            editedAITrigger = null;

            ListAITriggers();
        }

        private void BtnClone_LeftClick(object sender, EventArgs e)
        {
            if (editedAITrigger == null)
                return;

            var clone = editedAITrigger.Clone(map.GetNewUniqueInternalId());
            map.AITriggerTypes.Add(clone);
            ListAITriggers();
            SelectAITrigger(clone);
        }

        private void SelectAITrigger(AITriggerType aiTrigger)
        {
            lbAITriggers.SelectedIndex = lbAITriggers.Items.FindIndex(item => item.Tag == aiTrigger);

            if (lbAITriggers.LastIndex < lbAITriggers.SelectedIndex)
                lbAITriggers.ScrollToBottom(); // TODO we don't actually have a good way to scroll the listbox into a specific place right now
            else if (lbAITriggers.TopIndex > lbAITriggers.SelectedIndex)
                lbAITriggers.TopIndex = lbAITriggers.SelectedIndex;
        }

        private void TeamTypeWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectTeamTypeWindow.IsForSecondaryTeam)
            {
                editedAITrigger.SecondaryTeam = selectTeamTypeWindow.SelectedObject;
            }
            else
            {
                editedAITrigger.PrimaryTeam = selectTeamTypeWindow.SelectedObject;
            }

            EditAITrigger(editedAITrigger);
        }

        private void LbAITriggers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbAITriggers.SelectedItem == null)
            {
                EditAITrigger(null);
                return;
            }

            EditAITrigger((AITriggerType)lbAITriggers.SelectedItem.Tag);
        }

        private void EditAITrigger(AITriggerType aiTriggerType)
        {
            tbName.TextChanged -= TbName_TextChanged;
            ddSide.SelectedIndexChanged -= DdSide_SelectedIndexChanged;
            ddHouseType.SelectedIndexChanged -= DdHouse_SelectedIndexChanged;
            ddConditionType.SelectedIndexChanged -= DdConditionType_SelectedIndexChanged;
            ddComparator.SelectedIndexChanged -= DdComparator_SelectedIndexChanged;
            tbQuantity.TextChanged -= TbQuantity_TextChanged;
            tbComparisonObjectType.TextChanged -= TbComparisonObjectType_TextChanged;
            selPrimaryTeam.LeftClick -= SelPrimaryTeam_LeftClick;
            selSecondaryTeam.LeftClick -= SelSecondaryTeam_LeftClick;
            tbInitial.TextChanged -= TbInitial_TextChanged;
            tbMinimum.TextChanged -= TbMinimum_TextChanged;
            tbMaximum.TextChanged -= TbMaximum_TextChanged;
            chkEnabledOnEasy.CheckedChanged -= ChkEnabledOnEasy_CheckedChanged;
            chkEnabledOnMedium.CheckedChanged -= ChkEnabledOnMedium_CheckedChanged;
            chkEnabledOnHard.CheckedChanged -= ChkEnabledOnHard_CheckedChanged;

            editedAITrigger = aiTriggerType;

            if (editedAITrigger == null)
            {
                tbName.Text = string.Empty;
                ddSide.SelectedIndex = -1;
                ddHouseType.SelectedIndex = -1;
                ddConditionType.SelectedIndex = -1;
                ddComparator.SelectedIndex = -1;
                tbQuantity.Text = string.Empty;
                tbComparisonObjectType.Text = string.Empty;
                selPrimaryTeam.Text = string.Empty;
                selSecondaryTeam.Text = string.Empty;
                selPrimaryTeam.Tag = null;
                selSecondaryTeam.Tag = null;
                tbInitial.Text = string.Empty;
                tbMinimum.Text = string.Empty;
                tbMaximum.Text = string.Empty;
                chkEnabledOnEasy.Checked = false;
                chkEnabledOnMedium.Checked = false;
                chkEnabledOnHard.Checked = false;
                return;
            }

            tbName.Text = editedAITrigger.Name;
            ddSide.SelectedIndex = editedAITrigger.Side < ddSide.Items.Count ? editedAITrigger.Side : 0;
            ddHouseType.SelectedIndex = ddHouseType.Items.FindIndex(ddi => ddi.Text == editedAITrigger.OwnerName);
            ddConditionType.SelectedIndex = ((int)aiTriggerType.ConditionType + 1);
            ddComparator.SelectedIndex = (int)aiTriggerType.Comparator.ComparatorOperator;
            tbQuantity.Value = aiTriggerType.Comparator.Quantity;
            tbComparisonObjectType.Text = string.IsNullOrWhiteSpace(aiTriggerType.ConditionObjectString) ? string.Empty : aiTriggerType.ConditionObjectString;
            selPrimaryTeam.Text = aiTriggerType.PrimaryTeam != null ? aiTriggerType.PrimaryTeam.GetDisplayName() : string.Empty;
            selPrimaryTeam.Tag = aiTriggerType.PrimaryTeam;
            selSecondaryTeam.Text = aiTriggerType.SecondaryTeam != null ? aiTriggerType.SecondaryTeam.GetDisplayName() : string.Empty;
            selSecondaryTeam.Tag = aiTriggerType.SecondaryTeam;
            tbInitial.DoubleValue = aiTriggerType.InitialWeight;
            tbMinimum.DoubleValue = aiTriggerType.MinimumWeight;
            tbMaximum.DoubleValue = aiTriggerType.MaximumWeight;
            chkEnabledOnEasy.Checked = aiTriggerType.Easy;
            chkEnabledOnMedium.Checked = aiTriggerType.Medium;
            chkEnabledOnHard.Checked = aiTriggerType.Hard;

            tbName.TextChanged += TbName_TextChanged;
            ddSide.SelectedIndexChanged += DdSide_SelectedIndexChanged;
            ddHouseType.SelectedIndexChanged += DdHouse_SelectedIndexChanged;
            ddConditionType.SelectedIndexChanged += DdConditionType_SelectedIndexChanged;
            ddComparator.SelectedIndexChanged += DdComparator_SelectedIndexChanged;
            tbQuantity.TextChanged += TbQuantity_TextChanged;
            tbComparisonObjectType.TextChanged += TbComparisonObjectType_TextChanged;
            selPrimaryTeam.LeftClick += SelPrimaryTeam_LeftClick;
            selSecondaryTeam.LeftClick += SelSecondaryTeam_LeftClick;
            tbInitial.TextChanged += TbInitial_TextChanged;
            tbMinimum.TextChanged += TbMinimum_TextChanged;
            tbMaximum.TextChanged += TbMaximum_TextChanged;
            chkEnabledOnEasy.CheckedChanged += ChkEnabledOnEasy_CheckedChanged;
            chkEnabledOnMedium.CheckedChanged += ChkEnabledOnMedium_CheckedChanged;
            chkEnabledOnHard.CheckedChanged += ChkEnabledOnHard_CheckedChanged;
        }

        private void TbName_TextChanged(object sender, EventArgs e)
        {
            editedAITrigger.Name = tbName.Text;
            lbAITriggers.SelectedItem.Text = tbName.Text;
        }

        private void DdSide_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedAITrigger.Side = ddSide.SelectedIndex;
            lbAITriggers.SelectedItem.TextColor = GetAITriggerUIColor(editedAITrigger);
        }

        private void DdHouse_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedAITrigger.OwnerName = ddHouseType.SelectedItem.Text;
            lbAITriggers.SelectedItem.TextColor = GetAITriggerUIColor(editedAITrigger);
        }

        private void DdConditionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedAITrigger.ConditionType = (AITriggerConditionType)(ddConditionType.SelectedIndex - 1);
        }

        private void DdComparator_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedAITrigger.Comparator = new AITriggerComparator((AITriggerComparatorOperator)ddComparator.SelectedIndex, editedAITrigger.Comparator.Quantity);
        }

        private void TbQuantity_TextChanged(object sender, EventArgs e)
        {
            editedAITrigger.Comparator = new AITriggerComparator(editedAITrigger.Comparator.ComparatorOperator, tbQuantity.Value);
        }

        private void TbComparisonObjectType_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbComparisonObjectType.Text))
                editedAITrigger.ConditionObjectString = Constants.NoneValue1;
            else
                editedAITrigger.ConditionObjectString = tbComparisonObjectType.Text;
        }

        private void SelPrimaryTeam_LeftClick(object sender, EventArgs e)
        {
            selectTeamTypeWindow.IsForSecondaryTeam = false;
            selectTeamTypeWindow.Open(editedAITrigger.PrimaryTeam);
        }

        private void SelSecondaryTeam_LeftClick(object sender, EventArgs e)
        {
            selectTeamTypeWindow.IsForSecondaryTeam = true;
            selectTeamTypeWindow.Open(editedAITrigger.SecondaryTeam);
        }

        private void TbInitial_TextChanged(object sender, EventArgs e)
        {
            editedAITrigger.InitialWeight = tbInitial.DoubleValue;
        }

        private void TbMinimum_TextChanged(object sender, EventArgs e)
        {
            editedAITrigger.MinimumWeight = tbMinimum.DoubleValue;
        }

        private void TbMaximum_TextChanged(object sender, EventArgs e)
        {
            editedAITrigger.MaximumWeight = tbMaximum.DoubleValue;
        }

        private void ChkEnabledOnEasy_CheckedChanged(object sender, EventArgs e)
        {
            editedAITrigger.Easy = chkEnabledOnEasy.Checked;
        }

        private void ChkEnabledOnMedium_CheckedChanged(object sender, EventArgs e)
        {
            editedAITrigger.Medium = chkEnabledOnMedium.Checked;
        }

        private void ChkEnabledOnHard_CheckedChanged(object sender, EventArgs e)
        {
            editedAITrigger.Hard = chkEnabledOnHard.Checked;
        }

        public void Open()
        {
            ListAITriggers();
            Show();
        }

        private void ListAITriggers()
        {
            lbAITriggers.Clear();
            ddSide.Items.Clear();
            ddHouseType.Items.Clear();

            map.AITriggerTypes.ForEach(aitt =>
            {
                lbAITriggers.AddItem(new XNAListBoxItem() { Text = aitt.Name, Tag = aitt, TextColor = GetAITriggerUIColor(aitt) });
            });

            ddSide.AddItem("0 all sides");
            for (int i = 0; i < map.Rules.Sides.Count; i++)
            {
                ddSide.AddItem((i + 1).ToString() + " " + map.Rules.Sides[i]);
            }

            ddHouseType.AddItem("<all>");
            map.GetHouseTypes().ForEach(houseType => ddHouseType.AddItem(houseType.ININame, Helpers.GetHouseTypeUITextColor(houseType)));

            LbAITriggers_SelectedIndexChanged(this, EventArgs.Empty);
        }

        private Color GetAITriggerUIColor(AITriggerType aitt)
        {
            if (!string.IsNullOrWhiteSpace(aitt.OwnerName))
            {
                var houseType = map.FindHouseType(aitt.OwnerName);
                if (houseType != null)
                {
                    return Helpers.GetHouseTypeUITextColor(houseType);
                }
            }

            if (aitt.Side > 0)
            {
                string sideName = aitt.Side > 0 && aitt.Side - 1 < map.Rules.Sides.Count ? map.Rules.Sides[aitt.Side - 1] : null;
                if (sideName != null)
                {
                    var houseTypeFromSide = map.GetHouseTypes().Find(ht => ht.Side == sideName);

                    if (houseTypeFromSide != null)
                    {
                        return Helpers.GetHouseTypeUITextColor(houseTypeFromSide);
                    }
                }
            }

            return UISettings.ActiveSettings.AltColor;
        }
    }
}
