using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Globalization;
using System.Linq;
using TSMapEditor.Initialization;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that allows the user to configure houses of the map.
    /// </summary>
    public class HousesWindow : INItializableWindow
    {
        public HousesWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private XNADropDown ddHouseOfHumanPlayer;
        private EditorListBox lbHouseList;
        private EditorTextBox tbName;
        private XNADropDown ddIQ;
        private XNADropDown ddMapEdge;
        private XNADropDown ddSide;
        private XNADropDown ddActsLike;
        private XNADropDown ddCountry;
        private XNADropDown ddColor;
        private XNADropDown ddTechnologyLevel;
        private XNADropDown ddPercentBuilt;
        private EditorPopUpSelector selAllies;
        private EditorNumberTextBox tbMoney;
        private XNACheckBox chkPlayerControl;

        private XNALabel lblStatsValue;

        private House editedHouse;

        private GenerateStandardHousesWindow generateStandardHousesWindow;
        private EditHouseTypeWindow editHouseTypeWindow;
        private NewHouseWindow newHouseWindow;
        private ConfigureAlliesWindow configureAlliesWindow;

        public override void Initialize()
        {
            Name = nameof(HousesWindow);
            base.Initialize();

            ddHouseOfHumanPlayer = FindChild<XNADropDown>(nameof(ddHouseOfHumanPlayer));
            lbHouseList = FindChild<EditorListBox>(nameof(lbHouseList));
            tbName = FindChild<EditorTextBox>(nameof(tbName));
            ddIQ = FindChild<XNADropDown>(nameof(ddIQ));
            ddMapEdge = FindChild<XNADropDown>(nameof(ddMapEdge));
            ddSide = FindChild<XNADropDown>(nameof(ddSide));
            ddActsLike = FindChild<XNADropDown>(nameof(ddActsLike));
            ddCountry = FindChild<XNADropDown>(nameof(ddCountry));
            ddColor = FindChild<XNADropDown>(nameof(ddColor));
            ddTechnologyLevel = FindChild<XNADropDown>(nameof(ddTechnologyLevel));
            ddPercentBuilt = FindChild<XNADropDown>(nameof(ddPercentBuilt));
            selAllies = FindChild<EditorPopUpSelector>(nameof(selAllies));
            tbMoney = FindChild<EditorNumberTextBox>(nameof(tbMoney));
            chkPlayerControl = FindChild<XNACheckBox>(nameof(chkPlayerControl));

            lblStatsValue = FindChild<XNALabel>(nameof(lblStatsValue));
            lblStatsValue.Text = "";

            for (int i = 0; i < map.Rules.Sides.Count; i++)
            {
                string sideName = map.Rules.Sides[i];
                string sideString = i + " " + sideName;
                ddSide.AddItem(new XNADropDownItem() { Text = sideString, Tag = sideName });
            }

            foreach (RulesColor rulesColor in map.Rules.Colors.OrderBy(c => c.Name))
            {
                ddColor.AddItem(rulesColor.Name, rulesColor.XNAColor);
            }

            FindChild<EditorButton>("btnAddHouse").LeftClick += BtnAddHouse_LeftClick;
            FindChild<EditorButton>("btnDeleteHouse").LeftClick += BtnDeleteHouse_LeftClick;
            FindChild<EditorButton>("btnStandardHouses").LeftClick += BtnStandardHouses_LeftClick;
            var btnEditHouseType = FindChild<EditorButton>("btnEditHouseType");
            btnEditHouseType.LeftClick += BtnEditHouseType_LeftClick;
            FindChild<EditorButton>("btnMakeHouseRepairBuildings").LeftClick += BtnMakeHouseRepairBuildings_LeftClick;
            FindChild<EditorButton>("btnMakeHouseNotRepairBuildings").LeftClick += BtnMakeHouseNotRepairBuildings_LeftClick;

            ddHouseOfHumanPlayer.SelectedIndexChanged += DdHouseOfHumanPlayer_SelectedIndexChanged;
            lbHouseList.SelectedIndexChanged += LbHouseList_SelectedIndexChanged;

            generateStandardHousesWindow = new GenerateStandardHousesWindow(WindowManager, map);
            var standardHousesWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, generateStandardHousesWindow);
            standardHousesWindowDarkeningPanel.Hidden += (s, e) => ListHouses();

            editHouseTypeWindow = new EditHouseTypeWindow(WindowManager, map);
            var editHouseTypeWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, editHouseTypeWindow);
            editHouseTypeWindowDarkeningPanel.Hidden += (s, e) => RefreshHouseInfo();

            configureAlliesWindow = new ConfigureAlliesWindow(WindowManager, map);
            var configureAlliesWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, configureAlliesWindow);
            configureAlliesWindow.AlliesUpdated += (s, e) => RefreshHouseInfo();

            if (Constants.IsRA2YR)
            {
                btnEditHouseType.Text = "Edit Country";
                newHouseWindow = new NewHouseWindow(WindowManager, map);
                var newHouseWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, newHouseWindow);
                newHouseWindowDarkeningPanel.Hidden += NewHouseWindowDarkeningPanel_Hidden;
            }
        }

        private void NewHouseWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (newHouseWindow.Success)
                ListHouses();
        }

        private void DdHouseOfHumanPlayer_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (ddHouseOfHumanPlayer.SelectedItem == null || ddHouseOfHumanPlayer.SelectedIndex == 0)
            {
                map.Basic.Player = null;
                return;
            }

            map.Basic.Player = ddHouseOfHumanPlayer.SelectedItem.Text;
        }

        private void BtnAddHouse_LeftClick(object sender, System.EventArgs e)
        {
            if (Constants.IsRA2YR)
            {
                newHouseWindow.Open();
                return;
            }

            HouseType houseType = new HouseType("NewHouse");
            houseType.Index = map.HouseTypes.Count;
            Helpers.FindDefaultSideForNewHouseType(houseType, map.Rules);
            map.HouseTypes.Add(houseType);

            map.AddHouse(new House("NewHouse", houseType) 
            {
                ActsLike = 0,
                Allies = "NewHouse",
                Color = map.Rules.Colors[0].Name,
                Credits = 0, 
                Edge = "North",
                ID = map.Houses.Count,
                IQ = 0, 
                PercentBuilt = 100, 
                PlayerControl = false, 
                TechLevel = 10,
                XNAColor = Color.White
            });

            ListHouses();
            lbHouseList.SelectedIndex = lbHouseList.Items.Count - 1;
        }

        private void BtnDeleteHouse_LeftClick(object sender, System.EventArgs e)
        {
            if (editedHouse != null)
            {
                if (map.DeleteHouse(editedHouse))
                {
                    if (Constants.IsRA2YR)
                    {
                        // Also delete the associated HouseType if the HouseType is non-standard and is not used by any other House
                        if (map.HouseTypes.Contains(editedHouse.HouseType) && !map.Houses.Exists(h => h.HouseType == editedHouse.HouseType))
                            map.DeleteHouseType(editedHouse.HouseType);
                    }
                    else
                    {
                        // In Tiberian Sun, each House has one unique HouseType associated with it.
                        // We need to always delete the associated HouseType, if that fails for some reason then
                        // something has gone terribly wrong in our internal editor logic.
                        if (!map.DeleteHouseType(editedHouse.HouseType))
                            throw new InvalidOperationException("Failed to delete HouseType associated with house " + editedHouse.ININame);
                    }

                    editedHouse = null;
                    lbHouseList.SelectedIndex = -1;
                    ListHouses();
                }
            }
        }

        private void BtnStandardHouses_LeftClick(object sender, System.EventArgs e)
        {
            if (map.Houses.Count > 0)
            {
                EditorMessageBox.Show(WindowManager,
                    "Houses already exist",
                    "Cannot generate standard because the map already has one or more houses specified." + Environment.NewLine + Environment.NewLine +
                    "If you want to generate standard houses, please delete the existing houses first.", MessageBoxButtons.OK);

                return;
            }

            generateStandardHousesWindow.Open();
        }

        private void BtnEditHouseType_LeftClick(object sender, EventArgs e)
        {
            if (editedHouse == null)
                return;

            editHouseTypeWindow.Open(editedHouse.HouseType);
        }

        private void BtnMakeHouseRepairBuildings_LeftClick(object sender, EventArgs e)
        {
            if (editedHouse == null)
            {
                EditorMessageBox.Show(WindowManager, "No House Selected", "Select a house first.", MessageBoxButtons.OK);
                return;
            }

            var dialog = EditorMessageBox.Show(WindowManager,
                "Are you sure?",
                "This enables the \"AI Repairs\" flag on all buildings of the house, which makes the AI repair them." + Environment.NewLine + Environment.NewLine +
                "No un-do is available. Do you wish to continue?", MessageBoxButtons.YesNo);
            dialog.YesClickedAction = _ => map.Structures.FindAll(s => s.Owner == editedHouse).ForEach(b => b.AIRepairable = true);
        }

        private void BtnMakeHouseNotRepairBuildings_LeftClick(object sender, EventArgs e)
        {
            if (editedHouse == null)
            {
                EditorMessageBox.Show(WindowManager, "No House Selected", "Select a house first.", MessageBoxButtons.OK);
                return;
            }

            var dialog = EditorMessageBox.Show(WindowManager,
                "Are you sure?",
                "This disables the \"AI Repairs\" flag on all buildings of the house, which makes the AI NOT repair them." + Environment.NewLine + Environment.NewLine +
                "No un-do is available. Do you wish to continue?", MessageBoxButtons.YesNo);
            dialog.YesClickedAction = _ => map.Structures.FindAll(s => s.Owner == editedHouse).ForEach(b => b.AIRepairable = false);
        }

        private void LbHouseList_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (lbHouseList.SelectedItem == null)
            {
                editedHouse = null;
            }
            else
            {
                editedHouse = (House)lbHouseList.SelectedItem.Tag;
            }
            
            RefreshHouseInfo();
        }

        private void RefreshHouseInfo()
        {
            RefreshHouseStats();

            tbName.TextChanged -= TbName_TextChanged;
            ddIQ.SelectedIndexChanged -= DdIQ_SelectedIndexChanged;
            ddMapEdge.SelectedIndexChanged -= DdMapEdge_SelectedIndexChanged;
            ddSide.SelectedIndexChanged -= DdSide_SelectedIndexChanged;
            ddActsLike.SelectedIndexChanged -= DdActsLike_SelectedIndexChanged;
            ddCountry.SelectedIndexChanged -= DdCountry_SelectedIndexChanged;
            ddColor.SelectedIndexChanged -= DdColor_SelectedIndexChanged;
            ddTechnologyLevel.SelectedIndexChanged -= DdTechnologyLevel_SelectedIndexChanged;
            ddPercentBuilt.SelectedIndexChanged -= DdPercentBuilt_SelectedIndexChanged;
            selAllies.LeftClick -= SelAllies_LeftClick;
            tbMoney.TextChanged -= TbMoney_TextChanged;
            chkPlayerControl.CheckedChanged -= ChkPlayerControl_CheckedChanged;

            if (editedHouse == null)
            {
                tbName.Text = string.Empty;
                ddIQ.SelectedIndex = -1;
                ddMapEdge.SelectedIndex = -1;
                ddSide.SelectedIndex = -1;
                ddActsLike.SelectedIndex = -1;
                ddCountry.SelectedIndex = -1;
                ddColor.SelectedIndex = -1;
                ddTechnologyLevel.SelectedIndex = -1;
                ddPercentBuilt.SelectedIndex = -1;
                selAllies.Text = string.Empty;
                tbMoney.Text = string.Empty;
                chkPlayerControl.Checked = false;
                lblStatsValue.Text = string.Empty;
                return;
            }

            tbName.Text = editedHouse.ININame;
            ddIQ.SelectedIndex = ddIQ.Items.FindIndex(item => Conversions.IntFromString(item.Text, -1) == editedHouse.IQ);
            ddMapEdge.SelectedIndex = ddMapEdge.Items.FindIndex(item => item.Text == editedHouse.Edge);
            ddSide.SelectedIndex = map.Rules.Sides.FindIndex(s => s == editedHouse.HouseType?.Side);
            if (editedHouse.ActsLike.GetValueOrDefault() < map.GetHouses().Count)
                ddActsLike.SelectedIndex = editedHouse.ActsLike.GetValueOrDefault();
            else
                ddActsLike.SelectedIndex = -1;

            if (editedHouse.HouseType != null)
                ddCountry.SelectedIndex = ddCountry.Items.FindIndex(ddi => ddi.Tag == editedHouse.HouseType);
            else
                ddCountry.SelectedIndex = -1;

            ddSide.AllowDropDown = !Constants.IsRA2YR;

            ddColor.SelectedIndex = ddColor.Items.FindIndex(item => item.Text == editedHouse.Color);
            ddTechnologyLevel.SelectedIndex = ddTechnologyLevel.Items.FindIndex(item => Conversions.IntFromString(item.Text, -1) == editedHouse.TechLevel);
            ddPercentBuilt.SelectedIndex = ddPercentBuilt.Items.FindIndex(item => Conversions.IntFromString(item.Text, -1) == editedHouse.PercentBuilt);
            selAllies.Text = editedHouse.Allies ?? "";
            tbMoney.Value = editedHouse.Credits;
            chkPlayerControl.Checked = editedHouse.PlayerControl;

            tbName.TextChanged += TbName_TextChanged;
            ddIQ.SelectedIndexChanged += DdIQ_SelectedIndexChanged;
            ddMapEdge.SelectedIndexChanged += DdMapEdge_SelectedIndexChanged;
            ddSide.SelectedIndexChanged += DdSide_SelectedIndexChanged;
            ddActsLike.SelectedIndexChanged += DdActsLike_SelectedIndexChanged;
            ddCountry.SelectedIndexChanged += DdCountry_SelectedIndexChanged;
            ddColor.SelectedIndexChanged += DdColor_SelectedIndexChanged;
            ddTechnologyLevel.SelectedIndexChanged += DdTechnologyLevel_SelectedIndexChanged;
            ddPercentBuilt.SelectedIndexChanged += DdPercentBuilt_SelectedIndexChanged;
            selAllies.LeftClick += SelAllies_LeftClick;
            tbMoney.TextChanged += TbMoney_TextChanged;
            chkPlayerControl.CheckedChanged += ChkPlayerControl_CheckedChanged;
        }

        private void TbName_TextChanged(object sender, System.EventArgs e)
        {
            editedHouse.ININame = tbName.Text;

            if (!Constants.IsRA2YR)
                editedHouse.HouseType.ININame = editedHouse.ININame;

            if (!string.IsNullOrWhiteSpace(editedHouse.ININame))
            {
                editedHouse.Allies = string.Join(',',
                    new string[] { editedHouse.ININame }
                    .Concat(editedHouse.Allies.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[1..]));

                selAllies.Text = editedHouse.Allies;
            }

            ListHouses();
        }

        private void DdIQ_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            editedHouse.IQ = Conversions.IntFromString(ddIQ.SelectedItem.Text, -1);
        }

        private void DdMapEdge_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            editedHouse.Edge = ddMapEdge.SelectedItem?.Text;
        }

        private void DdSide_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            editedHouse.HouseType.Side = (string)ddSide.SelectedItem.Tag;
        }

        private void DdActsLike_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (Constants.IsRA2YR)
                return;

            editedHouse.ActsLike = ddActsLike.SelectedIndex;
        }

        private void DdCountry_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Constants.IsRA2YR)
                return;

            var ddItem = ddCountry.SelectedItem;
            editedHouse.Country = ddItem.Text;
            editedHouse.HouseType = (HouseType)ddItem.Tag;
        }

        private void DdColor_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            editedHouse.Color = ddColor.SelectedItem.Text;
            editedHouse.XNAColor = ddColor.SelectedItem.TextColor.Value;

            if (!Constants.IsRA2YR)
            {
                editedHouse.HouseType.Color = editedHouse.Color;
                editedHouse.HouseType.XNAColor = editedHouse.XNAColor;
            }

            map.HouseColorUpdated(editedHouse);
            ListHouses();
        }

        private void DdTechnologyLevel_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            editedHouse.TechLevel = Conversions.IntFromString(ddTechnologyLevel.SelectedItem.Text, -1);
        }

        private void DdPercentBuilt_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            editedHouse.PercentBuilt = Conversions.IntFromString(ddPercentBuilt.SelectedItem.Text, 100);
        }

        private void SelAllies_LeftClick(object sender, EventArgs e)
        {
            configureAlliesWindow.Open(editedHouse);
        }

        private void TbMoney_TextChanged(object sender, System.EventArgs e)
        {
            editedHouse.Credits = tbMoney.Value;
        }

        private void ChkPlayerControl_CheckedChanged(object sender, System.EventArgs e)
        {
            editedHouse.PlayerControl = chkPlayerControl.Checked;
        }

        public void Open()
        {
            Show();
            ListHouses();
        }

        private void ListHouses()
        {
            lbHouseList.Clear();
            ddHouseOfHumanPlayer.Items.Clear();

            ddHouseOfHumanPlayer.AddItem("None");

            ddActsLike.Items.Clear();

            foreach (House house in map.Houses)
            {
                lbHouseList.AddItem(
                    new XNAListBoxItem()
                    {
                        Text = house.ININame,
                        TextColor = house.XNAColor,
                        Tag = house
                    }
                );

                ddActsLike.AddItem(new XNADropDownItem() { Text = house.ID.ToString(CultureInfo.InvariantCulture) + " " + house.ININame, Tag = house.ID });

                ddHouseOfHumanPlayer.AddItem(house.ININame, house.XNAColor);
            }

            ddCountry.Items.Clear();
            foreach (var houseType in map.GetHouseTypes())
            {
                ddCountry.AddItem(new XNADropDownItem() { Text = houseType.Index.ToString(CultureInfo.InvariantCulture) + " " + houseType.ININame, TextColor = houseType.XNAColor, Tag = houseType });
            }

            ddHouseOfHumanPlayer.SelectedIndex = map.Houses.FindIndex(h => h.ININame == map.Basic.Player) + 1;
        }

        private void RefreshHouseStats()
        {
            if (editedHouse == null)
            {
                lblStatsValue.Text = "";
                return;
            }

            string stats = "Power: " + map.Structures.Aggregate<Structure, int>(0, (value, structure) => 
            {
                if (structure.Owner == editedHouse)
                    return value + structure.ObjectType.Power;

                return value;
            }) + Environment.NewLine;

            stats += Environment.NewLine + "Aircraft: " + map.Aircraft.Count(s => s.Owner == editedHouse);
            stats += Environment.NewLine + "Infantry: " + map.Infantry.Count(s => s.Owner == editedHouse);
            stats += Environment.NewLine + "Vehicles: " + map.Units.Count(s => s.Owner == editedHouse);
            stats += Environment.NewLine + "Buildings: " + map.Structures.Count(s => s.Owner == editedHouse);

            lblStatsValue.Text = stats;
        }
    }
}
