using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.UI.Windows
{
    public class SelectScriptActionPresetOptionWindow : SelectObjectWindow<ScriptActionPresetOption>
    {
        public SelectScriptActionPresetOptionWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private Map map;
        public List<ScriptActionPresetOption> presetOptions { get; } = new List<ScriptActionPresetOption>(0);

        public override void Initialize()
        {
            Name = nameof(SelectScriptActionPresetOptionWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (ScriptActionPresetOption)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (ScriptActionPresetOption presetOption in presetOptions)
            {
                var item = new XNAListBoxItem() { Text = $"{presetOption.GetOptionText()}", Tag = presetOption };

                if (presetOption.Color != null)
                    item.TextColor = (Color)presetOption.Color;

                lbObjectList.AddItem(item);

                if (presetOption == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }

        public string FillPresetOptions(ScriptActionEntry entry, ScriptAction action)
        {
            presetOptions.Clear();
            presetOptions.AddRange(action.PresetOptions);

            var fittingItem = presetOptions.Find(item => item.Text.StartsWith(entry.Argument.ToString()));

            if (fittingItem != null)
                return fittingItem.Text;

            return null;
        }

        public ScriptActionPresetOption GetMatchingItem(string text)
        {
            return presetOptions.Find(item => item.GetOptionText().Equals(text, StringComparison.Ordinal));
        }

        public string GetSelectedItemText()
        {
            if (SelectedObject != null)
                return SelectedObject.GetOptionText();

            return string.Empty;
        }
    }
}
