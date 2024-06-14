using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that prompts the user for the name and parent country of the new house.
    /// </summary>
    public class NewHouseWindow : INItializableWindow
    {
        public NewHouseWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private EditorTextBox tbHouseName;
        private XNADropDown ddParentCountry;
        private EditorButton btnAdd;

        public HouseType ParentCountry { get; set; }
        public bool Success { get; set; }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(NewHouseWindow);
            base.Initialize();

            tbHouseName = FindChild<EditorTextBox>(nameof(tbHouseName));
            ddParentCountry = FindChild<XNADropDown>(nameof(ddParentCountry));
            btnAdd = FindChild<EditorButton>(nameof(btnAdd));

            ddParentCountry.SelectedIndexChanged += DdParentCountry_SelectedIndexChanged;
            btnAdd.LeftClick += BtnAdd_LeftClick;

            if (!Constants.IsRA2YR)
            {
                ddParentCountry.Visible = false;
                FindChild<XNALabel>("lblParentCountry").Visible = false;
            }
        }

        private void DdParentCountry_SelectedIndexChanged(object sender, EventArgs e)
        {
            ParentCountry = (HouseType)ddParentCountry.SelectedItem.Tag;
        }

        private void BtnAdd_LeftClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbHouseName.Text))
            {
                EditorMessageBox.Show(WindowManager, "House Name Required",
                    "Please input a name for the house.", MessageBoxButtons.OK);

                return;
            }

            string houseName = tbHouseName.Text;
            string houseTypeName;
            if (houseName.EndsWith("House"))
                houseTypeName = houseName.Replace("House", "Country");
            else
                houseTypeName = houseName + "Country";

            var newHouseType = new HouseType(houseTypeName)
            {
                ParentCountry = ParentCountry.ININame,
                Index = map.Rules.RulesHouseTypes.Count + map.HouseTypes.Count,
                Side = ParentCountry.Side,
                Color = ParentCountry.Color,
                XNAColor = ParentCountry.XNAColor
            };

            Helpers.FindDefaultSideForNewHouseType(newHouseType, map.Rules);
            map.AddHouseType(newHouseType);

            var newHouse = new House(houseName)
            {
                Allies = houseName,
                Credits = 0,
                Edge = "West",
                IQ = 0,
                PercentBuilt = 100,
                PlayerControl = false,
                TechLevel = 10,
                ID = map.Houses.Count
            };

            newHouse.Color = newHouseType.Color;
            newHouse.XNAColor = newHouseType.XNAColor;
            newHouse.Country = houseTypeName;

            newHouse.HouseType = newHouseType;

            map.AddHouse(newHouse);

            Success = true;

            Hide();
        }

        private void ListParentCountries()
        {
            ddParentCountry.Items.Clear();

            map.Rules.RulesHouseTypes.ForEach(
                houseType => ddParentCountry.AddItem(new XNADropDownItem() 
            { 
                Text = houseType.ININame,
                TextColor = houseType.XNAColor,
                Tag = houseType 
            }));
        }

        public void Open()
        {
            if (!Constants.IsRA2YR)
                throw new NotSupportedException(nameof(NewHouseWindow) + " should only be used with Countries.");

            Show();
            ListParentCountries();

            ddParentCountry.SelectedIndex = 0;
            tbHouseName.Text = "NewHouse";

            Success = false;
        }
    }
}