using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using System;
using TSMapEditor.Models;
using TSMapEditor.CCEngine;

namespace TSMapEditor.UI.Windows
{
    public class SelectBuildingTargetWindow : SelectObjectWindow<int>
    {
        public SelectBuildingTargetWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private XNADropDown ddTarget;

        public BuildingWithPropertyType Property { get; private set; }

        public override void Initialize()
        {
            Name = nameof(SelectBuildingTargetWindow);
            base.Initialize();

            ddTarget = FindChild<XNADropDown>(nameof(ddTarget));
            ddTarget.AddItem(new XNADropDownItem { Text = BuildingWithPropertyType.LeastThreat.ToDescription(), Tag = BuildingWithPropertyType.LeastThreat });
            ddTarget.AddItem(new XNADropDownItem { Text = BuildingWithPropertyType.HighestThreat.ToDescription(), Tag = BuildingWithPropertyType.HighestThreat });
            ddTarget.AddItem(new XNADropDownItem { Text = BuildingWithPropertyType.Nearest.ToDescription(), Tag = BuildingWithPropertyType.Nearest });
            ddTarget.AddItem(new XNADropDownItem { Text = BuildingWithPropertyType.Farthest.ToDescription(), Tag = BuildingWithPropertyType.Farthest });
            ddTarget.SelectedIndexChanged += DdTarget_SelectedIndexChanged;
            ddTarget.SelectedIndex = 0;
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem?.Tag == null)
            {
                SelectedObject = -1;
                return;
            }

            SelectedObject = (int)lbObjectList.SelectedItem.Tag;
        }

        protected void DdTarget_SelectedIndexChanged(object sender, EventArgs e)
        {
            Property = (BuildingWithPropertyType)ddTarget.SelectedItem?.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (BuildingType buildingType in map.Rules.BuildingTypes)
            {
                int number = buildingType.Index;
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{number} {buildingType.GetEditorDisplayName()} ({buildingType.ININame})", Tag = number });

                if (SelectedObject == number)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
        
        public void Open(int buildingTypeIndex, BuildingWithPropertyType property)
        {
            Open(buildingTypeIndex);
            Property = property;
        }
    }
}
