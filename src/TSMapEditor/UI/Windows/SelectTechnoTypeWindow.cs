using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectTechnoTypeWindow : SelectObjectWindow<TechnoType>
    {
        public SelectTechnoTypeWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public bool IncludeNone { get; set; }

        public override void Initialize()
        {
            Name = nameof(SelectTechnoTypeWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (TechnoType)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            if (IncludeNone)
                lbObjectList.AddItem("None");

            var technoTypes = map.GetAllTechnoTypes();

            foreach (var technoType in technoTypes)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{technoType.Index} {technoType.Name} ({technoType.ININame})", Tag = technoType });
                if (technoType == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
