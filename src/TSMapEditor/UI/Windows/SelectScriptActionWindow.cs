using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectScriptActionWindow : SelectObjectWindow<ScriptAction>
    {
        public SelectScriptActionWindow(WindowManager windowManager, EditorConfig editorConfig) : base(windowManager)
        {
            this.editorConfig = editorConfig;
        }

        private EditorConfig editorConfig;

        public override void Initialize()
        {
            Name = nameof(SelectScriptActionWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (ScriptAction)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (ScriptAction scriptAction in editorConfig.ScriptActions.Values)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{scriptAction.Index} {scriptAction.Name}", Tag = scriptAction });
                if (scriptAction == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
