using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectBuildingTypeWindow : SelectObjectWindow<BuildingType>
    {
        public SelectBuildingTypeWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(SelectBuildingTypeWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (BuildingType)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            lbObjectList.AddItem(new XNAListBoxItem() { Text = "None" });

            foreach (BuildingType buildingType in map.Rules.BuildingTypes)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{buildingType.Index} {buildingType.GetEditorDisplayName()} ({buildingType.ININame})", Tag = buildingType });
                if (buildingType == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
