using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that allows the user to edit a <see cref="HouseType"/> (also known as Country in RA2/YR).
    /// </summary>
    public class EditHouseTypeWindow : INItializableWindow
    {
        public EditHouseTypeWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private EditorTextBox tbName;
        private XNADropDown ddParentCountry;
        private EditorTextBox tbSuffix;
        private EditorTextBox tbPrefix;
        private XNADropDown ddColor;
        private XNADropDown ddSide;
        private EditorListBox lbMultipliers;
        private EditorNumberTextBox tbSelectedMultiplier;
        private XNACheckBox chkSmartAI;
        private XNACheckBox chkMultiplay;
        private XNACheckBox chkMultiplayPassive;
        private XNACheckBox chkWallOwner;

        private readonly Map map;

        private HouseType editedCountry { get; set; }

        public override void Initialize()
        {
            Name = nameof(EditHouseTypeWindow);
            base.Initialize();

            tbName = FindChild<EditorTextBox>(nameof(tbName));
            ddParentCountry = FindChild<XNADropDown>(nameof(ddParentCountry));
            tbSuffix = FindChild<EditorTextBox>(nameof(tbSuffix));
            tbPrefix = FindChild<EditorTextBox>(nameof(tbPrefix));
            ddColor = FindChild<XNADropDown>(nameof(ddColor));
            ddSide = FindChild<XNADropDown>(nameof(ddSide));
            lbMultipliers = FindChild<EditorListBox>(nameof(lbMultipliers));
            tbSelectedMultiplier = FindChild<EditorNumberTextBox>(nameof(tbSelectedMultiplier));
            chkSmartAI = FindChild<XNACheckBox>(nameof(chkSmartAI));
            chkMultiplay = FindChild<XNACheckBox>(nameof(chkMultiplay));
            chkMultiplayPassive = FindChild<XNACheckBox>(nameof(chkMultiplayPassive));
            chkWallOwner = FindChild<XNACheckBox>(nameof(chkWallOwner));

            tbName.InputEnabled = false;
            tbSuffix.AllowComma = false;
            tbPrefix.AllowComma = false;
            tbSelectedMultiplier.AllowDecimals = true;

            for (int i = 0; i < map.Rules.Sides.Count; i++)
            {
                string sideName = map.Rules.Sides[i];
                string sideString = $"{i} {sideName}";
                ddSide.AddItem(new XNADropDownItem() { Text = sideString, Tag = sideName });
            }

            foreach (RulesColor rulesColor in map.Rules.Colors.OrderBy(c => c.Name))
                ddColor.AddItem(rulesColor.Name, rulesColor.XNAColor);

            if (Constants.IsRA2YR)
            {
                foreach (var property in typeof(HouseType).GetProperties())
                {
                    if (property.Name.EndsWith("Mult") || property.Name == "ROF" || property.Name == "Firepower")
                        lbMultipliers.AddItem(new XNAListBoxItem() { Text = property.Name, Tag = property });
                }
            }
            else
            {
                var tsMultipliers = new List<string>() { "Airspeed", "Armor", "Cost", "Firepower", "Groundspeed", "ROF", "BuildTime" };

                foreach (var property in typeof(HouseType).GetProperties())
                {
                    if (tsMultipliers.Contains(property.Name))
                        lbMultipliers.AddItem(new XNAListBoxItem() { Text = property.Name, Tag = property });
                }
            }

            tbSelectedMultiplier.DoubleDefaultValue = 1.0;
            tbName.InputEnabled = false;
        }

        private void ChkWallOwner_CheckedChanged(object sender, EventArgs e)
        {
            editedCountry.WallOwner = chkWallOwner.Checked;
            CheckAddRulesHouseType(editedCountry);
        }

        private void ChkMultiplayPassive_CheckedChanged(object sender, EventArgs e)
        {
            editedCountry.MultiplayPassive = chkMultiplayPassive.Checked;
            CheckAddRulesHouseType(editedCountry);
        }

        private void ChkMultiplay_CheckedChanged(object sender, EventArgs e)
        {
            editedCountry.Multiplay = chkMultiplay.Checked;
            CheckAddRulesHouseType(editedCountry);
        }

        private void ChkSmartAI_CheckedChanged(object sender, EventArgs e)
        {
            editedCountry.SmartAI = chkSmartAI.Checked;
            CheckAddRulesHouseType(editedCountry);
        }

        private void TbSelectedMultiplier_TextChanged(object sender, EventArgs e)
        {
            if (lbMultipliers.SelectedItem == null)
                return;

            var property = (PropertyInfo)lbMultipliers.SelectedItem.Tag;

            if (property.PropertyType == typeof(float?))
            {
                property.SetValue(editedCountry, (float)tbSelectedMultiplier.DoubleValue);
            }
            else if (property.PropertyType == typeof(double?))
            {
                property.SetValue(editedCountry, tbSelectedMultiplier.DoubleValue);
            }

            CheckAddRulesHouseType(editedCountry);
        }

        private void LbMultipliers_SelectedIndexChanged(object sender, EventArgs e)
        {
            tbSelectedMultiplier.TextChanged -= TbSelectedMultiplier_TextChanged;

            var property = (PropertyInfo)lbMultipliers.SelectedItem.Tag;

            if (property.PropertyType == typeof(float?))
            {
                var propertyValue = (float?)property.GetValue(editedCountry);
                tbSelectedMultiplier.Text = propertyValue != null ? propertyValue.ToString() : string.Empty;
            }
            else if (property.PropertyType == typeof(double?))
            {
                var propertyValue = (double?)property.GetValue(editedCountry);
                tbSelectedMultiplier.Text = propertyValue != null ? propertyValue.ToString() : string.Empty;
            }

            tbSelectedMultiplier.TextChanged += TbSelectedMultiplier_TextChanged;
        }

        private void DdSide_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedCountry.Side = (string)ddSide.SelectedItem.Tag;
            CheckAddRulesHouseType(editedCountry);
        }

        private void DdColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedCountry.Color = ddColor.SelectedItem.Text;
            editedCountry.XNAColor = ddColor.SelectedItem.TextColor.Value;
            CheckAddRulesHouseType(editedCountry);
        }

        private void TbPrefix_TextChanged(object sender, EventArgs e)
        {
            editedCountry.Prefix = tbPrefix.Text;
            CheckAddRulesHouseType(editedCountry);
        }

        private void TbSuffix_TextChanged(object sender, EventArgs e)
        {
            editedCountry.Suffix = tbSuffix.Text;
            CheckAddRulesHouseType(editedCountry);
        }

        private void DdParentCountry_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedCountry.ParentCountry = (string)ddParentCountry.SelectedItem.Tag;
            CheckAddRulesHouseType(editedCountry);
        }

        private void LoadHouseTypeInfo()
        {
            ddParentCountry.SelectedIndexChanged -= DdParentCountry_SelectedIndexChanged;
            tbSuffix.TextChanged -= TbSuffix_TextChanged;
            tbPrefix.TextChanged -= TbPrefix_TextChanged;
            ddColor.SelectedIndexChanged -= DdColor_SelectedIndexChanged;
            ddSide.SelectedIndexChanged -= DdSide_SelectedIndexChanged;
            lbMultipliers.SelectedIndexChanged -= LbMultipliers_SelectedIndexChanged;
            tbSelectedMultiplier.TextChanged -= TbSelectedMultiplier_TextChanged;
            chkSmartAI.CheckedChanged -= ChkSmartAI_CheckedChanged;
            chkMultiplay.CheckedChanged -= ChkMultiplay_CheckedChanged;
            chkMultiplayPassive.CheckedChanged -= ChkMultiplayPassive_CheckedChanged;
            chkWallOwner.CheckedChanged -= ChkWallOwner_CheckedChanged;

            if (!map.Rules.RulesHouseTypes.Contains(editedCountry))
            {
                foreach (var houseType in map.Rules.RulesHouseTypes)
                    ddParentCountry.AddItem(new XNADropDownItem() { Text = houseType.ININame, Tag = houseType.ININame, TextColor = houseType.XNAColor });

                ddParentCountry.SelectedIndex = map.Rules.RulesHouseTypes.FindIndex(c => c.ININame == editedCountry.ParentCountry);
                ddParentCountry.AllowDropDown = true;
            }
            else
            {
                ddParentCountry.Items.Clear();
                ddParentCountry.AddItem("Standard country - no parent");
                ddParentCountry.SelectedIndex = 0;
                ddParentCountry.AllowDropDown = false;
            }

            tbName.Text = editedCountry.ININame;
            tbSuffix.Text = editedCountry.Suffix ?? string.Empty;
            tbPrefix.Text = editedCountry.Prefix ?? string.Empty;
            ddColor.SelectedIndex = ddColor.Items.FindIndex(item => item.Text == editedCountry.Color);
            ddSide.SelectedIndex = map.Rules.Sides.FindIndex(s => s == editedCountry.Side);
            lbMultipliers.SelectedIndex = -1;
            tbSelectedMultiplier.Text = string.Empty;
            chkSmartAI.Checked = editedCountry.SmartAI ?? false;
            chkMultiplay.Checked = editedCountry.Multiplay ?? false;
            chkMultiplayPassive.Checked = editedCountry.MultiplayPassive ?? false;
            chkWallOwner.Checked = editedCountry.WallOwner ?? false;

            ddParentCountry.SelectedIndexChanged += DdParentCountry_SelectedIndexChanged;
            tbSuffix.TextChanged += TbSuffix_TextChanged;
            tbPrefix.TextChanged += TbPrefix_TextChanged;
            ddColor.SelectedIndexChanged += DdColor_SelectedIndexChanged;
            ddSide.SelectedIndexChanged += DdSide_SelectedIndexChanged;
            lbMultipliers.SelectedIndexChanged += LbMultipliers_SelectedIndexChanged;
            tbSelectedMultiplier.TextChanged += TbSelectedMultiplier_TextChanged;
            chkSmartAI.CheckedChanged += ChkSmartAI_CheckedChanged;
            chkMultiplay.CheckedChanged += ChkMultiplay_CheckedChanged;
            chkMultiplayPassive.CheckedChanged += ChkMultiplayPassive_CheckedChanged;
            chkWallOwner.CheckedChanged += ChkWallOwner_CheckedChanged;
        }

        /// <summary>
        /// Checks if the given HouseType is a HouseType specified in Rules.
        /// If yes, marks it as modified in the current map.
        /// </summary>
        private void CheckAddRulesHouseType(HouseType houseType)
        {
            if (map.Rules.RulesHouseTypes.Contains(houseType))
                houseType.ModifiedInMap = true;
        }

        public void Open(HouseType editedCountry)
        {
            this.editedCountry = editedCountry;
            LoadHouseTypeInfo();

            Show();
        }
    }
}