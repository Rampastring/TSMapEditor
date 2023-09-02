using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectEventWindow : SelectObjectWindow<TriggerEventType>
    {
        public SelectEventWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(SelectEventWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (TriggerEventType)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (TriggerEventType triggerEventType in map.EditorConfig.TriggerEventTypes.Values)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{triggerEventType.ID} {triggerEventType.Name}", Tag = triggerEventType });
                if (triggerEventType == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
