using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using TSMapEditor.CCEngine;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;
using TSMapEditor.UI.Notifications;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that allows the user to edit map scripts.
    /// </summary>
    public class ScriptsWindow : INItializableWindow
    {
        public ScriptsWindow(WindowManager windowManager, Map map, EditorState editorState,
            INotificationManager notificationManager, ICursorActionTarget cursorActionTarget) : base(windowManager)
        {
            this.map = map;
            this.editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
            this.notificationManager = notificationManager ?? throw new ArgumentNullException(nameof(notificationManager));
            selectCellCursorAction = new SelectCellCursorAction(cursorActionTarget);
        }

        private readonly Map map;
        private readonly EditorState editorState;
        private readonly INotificationManager notificationManager;
        private SelectCellCursorAction selectCellCursorAction;

        private EditorListBox lbScriptTypes;
        private EditorTextBox tbName;
        private EditorListBox lbActions;
        private EditorPopUpSelector selTypeOfAction;
        private XNALabel lblParameterDescription;
        private EditorNumberTextBox tbParameterValue;
        private MenuButton btnEditorPresetValues;
        private XNALabel lblActionDescriptionValue;

        private SelectScriptActionWindow selectScriptActionWindow;
        private XNAContextMenu actionListContextMenu;

        private SelectBuildingTargetWindow selectBuildingTargetWindow;

        private Script editedScript;

        public override void Initialize()
        {
            Name = nameof(ScriptsWindow);
            base.Initialize();

            lbScriptTypes = FindChild<EditorListBox>(nameof(lbScriptTypes));
            tbName = FindChild<EditorTextBox>(nameof(tbName));
            lbActions = FindChild<EditorListBox>(nameof(lbActions));
            selTypeOfAction = FindChild<EditorPopUpSelector>(nameof(selTypeOfAction));
            lblParameterDescription = FindChild<XNALabel>(nameof(lblParameterDescription));
            tbParameterValue = FindChild<EditorNumberTextBox>(nameof(tbParameterValue));
            btnEditorPresetValues = FindChild<MenuButton>(nameof(btnEditorPresetValues));
            lblActionDescriptionValue = FindChild<XNALabel>(nameof(lblActionDescriptionValue));

            var presetValuesContextMenu = new XNAContextMenu(WindowManager);
            presetValuesContextMenu.Width = 250;
            btnEditorPresetValues.ContextMenu = presetValuesContextMenu;
            btnEditorPresetValues.ContextMenu.OptionSelected += ContextMenu_OptionSelected;
            btnEditorPresetValues.LeftClick += BtnEditorPresetValues_LeftClick;

            tbName.TextChanged += TbName_TextChanged;
            tbParameterValue.TextChanged += TbParameterValue_TextChanged;
            lbScriptTypes.SelectedIndexChanged += LbScriptTypes_SelectedIndexChanged;
            lbActions.SelectedIndexChanged += LbActions_SelectedIndexChanged;

            selectScriptActionWindow = new SelectScriptActionWindow(WindowManager, map.EditorConfig);
            var selectScriptActionDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectScriptActionWindow);
            selectScriptActionDarkeningPanel.Hidden += SelectScriptActionDarkeningPanel_Hidden;

            selectBuildingTargetWindow = new SelectBuildingTargetWindow(WindowManager, map);
            var buildingTargetWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectBuildingTargetWindow);
            buildingTargetWindowDarkeningPanel.Hidden += BuildingTargetWindowDarkeningPanel_Hidden;

            selTypeOfAction.MouseLeftDown += SelTypeOfAction_MouseLeftDown;

            FindChild<EditorButton>("btnAddScript").LeftClick += BtnAddScript_LeftClick;
            FindChild<EditorButton>("btnDeleteScript").LeftClick += BtnDeleteScript_LeftClick;
            FindChild<EditorButton>("btnCloneScript").LeftClick += BtnCloneScript_LeftClick;
            FindChild<EditorButton>("btnAddAction").LeftClick += BtnAddAction_LeftClick;
            FindChild<EditorButton>("btnDeleteAction").LeftClick += BtnDeleteAction_LeftClick;

            selectCellCursorAction.CellSelected += SelectCellCursorAction_CellSelected;

            actionListContextMenu = new XNAContextMenu(WindowManager);
            actionListContextMenu.Name = nameof(actionListContextMenu);
            actionListContextMenu.Width = 150;
            actionListContextMenu.AddItem("Move Up", ActionListContextMenu_MoveUp, () => editedScript != null && lbActions.SelectedItem != null && lbActions.SelectedIndex > 0);
            actionListContextMenu.AddItem("Move Down", ActionListContextMenu_MoveDown, () => editedScript != null && lbActions.SelectedItem != null && lbActions.SelectedIndex < lbActions.Items.Count - 1);
            actionListContextMenu.AddItem("Clone Action", ActionListContextMenu_CloneAction, () => editedScript != null && lbActions.SelectedItem != null);
            actionListContextMenu.AddItem("Insert New Action Here", ActionListContextMenu_Insert, () => editedScript != null && lbActions.SelectedItem != null);
            actionListContextMenu.AddItem("Delete Action", ActionListContextMenu_Delete, () => editedScript != null && lbActions.SelectedItem != null);
            AddChild(actionListContextMenu);
            lbActions.AllowRightClickUnselect = false;
            lbActions.RightClick += (s, e) => { if (editedScript != null) { lbActions.SelectedIndex = lbActions.HoveredIndex; actionListContextMenu.Open(GetCursorPoint()); } };
        }

        private void BuildingTargetWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (editedScript == null || lbActions.SelectedItem == null)
                return;

            if (selectBuildingTargetWindow.SelectedObject > -1)
            {
                tbParameterValue.Text = GetBuildingWithPropertyText(selectBuildingTargetWindow.SelectedObject, selectBuildingTargetWindow.Property);
            }
        }

        private void ActionListContextMenu_MoveUp()
        {
            if (editedScript == null || lbActions.SelectedItem == null || lbActions.SelectedIndex <= 0)
                return;

            int viewTop = lbActions.ViewTop;
            editedScript.Actions.Swap(lbActions.SelectedIndex - 1, lbActions.SelectedIndex);
            EditScript(editedScript);
            lbActions.SelectedIndex--;
            lbActions.ViewTop = viewTop;
        }

        private void ActionListContextMenu_MoveDown()
        {
            if (editedScript == null || lbActions.SelectedItem == null || lbActions.SelectedIndex >= editedScript.Actions.Count - 1)
                return;

            int viewTop = lbActions.ViewTop;
            editedScript.Actions.Swap(lbActions.SelectedIndex, lbActions.SelectedIndex + 1);
            EditScript(editedScript);
            lbActions.SelectedIndex++;
            lbActions.ViewTop = viewTop;
        }

        private void ActionListContextMenu_CloneAction()
        {
            if (editedScript == null || lbActions.SelectedItem == null)
                return;

            int viewTop = lbActions.ViewTop;
            int index = lbActions.SelectedIndex + 1;

            var clonedEntry = editedScript.Actions[lbActions.SelectedIndex].Clone();
            editedScript.Actions.Insert(index, clonedEntry);
            EditScript(editedScript);
            lbActions.SelectedIndex = index;
            lbActions.ViewTop = viewTop;
        }

        private void ActionListContextMenu_Insert()
        {
            if (editedScript == null || lbActions.SelectedItem == null)
                return;

            int viewTop = lbActions.ViewTop;

            int index = lbActions.SelectedIndex;
            editedScript.Actions.Insert(index, new ScriptActionEntry());
            EditScript(editedScript);
            lbActions.SelectedIndex = index;

            lbActions.ViewTop = viewTop;
        }

        private void ActionListContextMenu_Delete()
        {
            if (editedScript == null || lbActions.SelectedItem == null)
                return;

            int viewTop = lbActions.ViewTop;
            editedScript.Actions.RemoveAt(lbActions.SelectedIndex);
            EditScript(editedScript);
            lbActions.ViewTop = viewTop;
        }

        private void SelectCellCursorAction_CellSelected(object sender, GameMath.Point2D e)
        {
            tbParameterValue.Text = ((e.Y * 1000) + e.X).ToString(CultureInfo.InvariantCulture);
        }

        private void BtnEditorPresetValues_LeftClick(object sender, EventArgs e)
        {
            if (editedScript == null)
                return;

            if (lbActions.SelectedItem == null)
                return;

            ScriptActionEntry entry = editedScript.Actions[lbActions.SelectedIndex];
            ScriptAction action = map.EditorConfig.ScriptActions.GetValueOrDefault(entry.Action);

            if (action == null)
                return;

            if (action.ParamType == TriggerParamType.Cell)
            {
                editorState.CursorAction = selectCellCursorAction;
                notificationManager.AddNotification("Select a cell from the map.");
            }
            else if (action.ParamType == TriggerParamType.BuildingWithProperty)
            {
                var (index, property) = SplitBuildingWithProperty(entry.Argument);
                selectBuildingTargetWindow.Open(index, property);
            }
        }

        private void BtnAddScript_LeftClick(object sender, EventArgs e)
        {
            map.Scripts.Add(new Script(map.GetNewUniqueInternalId()) { Name = "New script" });
            ListScripts();
            SelectLastScript();
        }

        private void BtnDeleteScript_LeftClick(object sender, EventArgs e)
        {
            if (lbScriptTypes.SelectedItem == null)
                return;

            map.RemoveScript((Script)lbScriptTypes.SelectedItem.Tag);
            lbScriptTypes.SelectedIndex = -1;
            ListScripts();
        }

        private void BtnCloneScript_LeftClick(object sender, EventArgs e)
        {
            if (editedScript == null)
                return;

            map.Scripts.Add(editedScript.Clone(map.GetNewUniqueInternalId()));
            ListScripts();
            SelectLastScript();
        }

        private void SelectLastScript()
        {
            lbScriptTypes.SelectedIndex = map.Scripts.Count - 1;
            lbScriptTypes.ScrollToBottom();
        }

        private void BtnAddAction_LeftClick(object sender, EventArgs e)
        {
            if (editedScript == null)
                return;

            editedScript.Actions.Add(new ScriptActionEntry(0, 0));
            EditScript(editedScript);
            lbActions.SelectedIndex = lbActions.Items.Count - 1;
            lbActions.ScrollToBottom();
        }

        private void BtnDeleteAction_LeftClick(object sender, EventArgs e)
        {
            if (editedScript == null || lbActions.SelectedItem == null)
                return;

            editedScript.Actions.RemoveAt(lbActions.SelectedIndex);
            EditScript(editedScript);
        }

        private void TbName_TextChanged(object sender, EventArgs e)
        {
            if (editedScript == null)
                return;

            editedScript.Name = tbName.Text;
            lbScriptTypes.SelectedItem.Text = tbName.Text;
        }

        private void TbParameterValue_TextChanged(object sender, EventArgs e)
        {
            if (lbActions.SelectedItem == null || editedScript == null)
                return;

            ScriptActionEntry entry = editedScript.Actions[lbActions.SelectedIndex];
            entry.Argument = tbParameterValue.Value;
            lbActions.SelectedItem.Text = GetActionEntryText(lbActions.SelectedIndex, entry);
        }

        private void ContextMenu_OptionSelected(object sender, ContextMenuItemSelectedEventArgs e)
        {
            if (lbActions.SelectedItem == null || editedScript == null)
            {
                return;
            }

            tbParameterValue.Text = btnEditorPresetValues.ContextMenu.Items[e.ItemIndex].Text;
        }

        private void SelTypeOfAction_MouseLeftDown(object sender, EventArgs e)
        {
            if (lbActions.SelectedItem == null || editedScript == null)
            {
                return;
            }

            ScriptActionEntry entry = editedScript.Actions[lbActions.SelectedIndex];

            ScriptAction scriptAction = map.EditorConfig.ScriptActions.GetValueOrDefault(entry.Action);

            selectScriptActionWindow.Open(scriptAction);
        }

        private void SelectScriptActionDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (lbActions.SelectedItem == null || editedScript == null)
            {
                return;
            }

            if (selectScriptActionWindow.SelectedObject != null)
            {
                ScriptActionEntry entry = editedScript.Actions[lbActions.SelectedIndex];
                entry.Action = selectScriptActionWindow.SelectedObject.Index;
                lbActions.Items[lbActions.SelectedIndex].Text = GetActionEntryText(lbActions.SelectedIndex, entry);
            }

            LbActions_SelectedIndexChanged(this, EventArgs.Empty);
        }

        private void LbActions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbActions.SelectedItem == null || editedScript == null)
            {
                selTypeOfAction.Text = string.Empty;
                selTypeOfAction.Tag = null;
                tbParameterValue.Text = string.Empty;
                lblActionDescriptionValue.Text = string.Empty;
                return;
            }

            ScriptActionEntry entry = editedScript.Actions[lbActions.SelectedIndex];
            ScriptAction action = map.EditorConfig.ScriptActions.GetValueOrDefault(entry.Action);

            selTypeOfAction.Text = GetActionNameFromIndex(entry.Action);

            tbParameterValue.TextChanged -= TbParameterValue_TextChanged;
            SetParameterEntryText(entry, action);
            tbParameterValue.TextChanged += TbParameterValue_TextChanged;

            lblParameterDescription.Text = action == null ? "Parameter:" : action.ParamDescription + ":";
            lblActionDescriptionValue.Text = GetActionDescriptionFromIndex(entry.Action);

            FillPresetContextMenu(entry, action);
        }

        private void SetParameterEntryText(ScriptActionEntry scriptActionEntry, ScriptAction action)
        {
            if (action.ParamType == TriggerParamType.BuildingWithProperty)
            {
                tbParameterValue.Text = GetBuildingWithPropertyText(scriptActionEntry.Argument);
                return;
            }

            int presetIndex = action.PresetOptions.FindIndex(p => p.Value == scriptActionEntry.Argument);

            if (presetIndex > -1)
            {
                tbParameterValue.Text = action.PresetOptions[presetIndex].GetOptionText();
            }
            else
            {
                tbParameterValue.Value = scriptActionEntry.Argument;
            }
        }

        private static Tuple<int, BuildingWithPropertyType> SplitBuildingWithProperty(int argument)
        {
            var property = argument switch
            {
                < (int)BuildingWithPropertyType.HighestThreat => BuildingWithPropertyType.LeastThreat,
                < (int)BuildingWithPropertyType.Nearest => BuildingWithPropertyType.HighestThreat,
                < (int)BuildingWithPropertyType.Farthest => BuildingWithPropertyType.Nearest,
                _ => BuildingWithPropertyType.Farthest,
            };
            return new(argument - (int)property, property);
        }

        private string GetBuildingWithPropertyText(int buildingTypeIndex, BuildingWithPropertyType property)
        {
            string description = property.ToDescription();
            BuildingType buildingType = map.Rules.BuildingTypes.GetElementIfInRange(buildingTypeIndex);
            int value = buildingTypeIndex + (int)property;

            if (buildingType == null)
                return value + " - invalid value";

            return value + " - " + buildingType.GetEditorDisplayName() + " (" + description + ")";
        }

        private string GetBuildingWithPropertyText(int argument)
        {
            var (index, property) = SplitBuildingWithProperty(argument);
            return GetBuildingWithPropertyText(index, property);
        }

        private void FillPresetContextMenu(ScriptActionEntry entry, ScriptAction action)
        {
            btnEditorPresetValues.ContextMenu.ClearItems();

            action.PresetOptions.ForEach(p => btnEditorPresetValues.ContextMenu.AddItem(new XNAContextMenuItem() { Text = p.GetOptionText() }));

            if (action.ParamType == TriggerParamType.LocalVariable)
            {
                for (int i = 0; i < map.LocalVariables.Count; i++)
                {
                    btnEditorPresetValues.ContextMenu.AddItem(new XNAContextMenuItem() { Text = i + " - " + map.LocalVariables[i].Name });
                }
            }
            else if (action.ParamType == TriggerParamType.Waypoint)
            {
                foreach (Waypoint waypoint in map.Waypoints)
                {
                    btnEditorPresetValues.ContextMenu.AddItem(new XNAContextMenuItem() { Text = waypoint.Identifier.ToString() });
                }
            }
            else if (action.ParamType == TriggerParamType.HouseType)
            {
                foreach (var houseType in map.GetHouseTypes())
                {
                    btnEditorPresetValues.ContextMenu.AddItem(new XNAContextMenuItem() { Text = houseType.Index + " " + houseType.ININame, TextColor = Helpers.GetHouseTypeUITextColor(houseType) });
                }
            }
            else if (action.ParamType == TriggerParamType.House)
            {
                foreach (var house in map.GetHouses())
                {
                    btnEditorPresetValues.ContextMenu.AddItem(new XNAContextMenuItem() { Text = house.ID + " " + house.ININame, TextColor = Helpers.GetHouseUITextColor(house) });
                }
            }

            var fittingItem = btnEditorPresetValues.ContextMenu.Items.Find(item => item.Text.StartsWith(entry.Argument.ToString()));
            if (fittingItem != null)
                tbParameterValue.Text = fittingItem.Text;
        }

        private void LbScriptTypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbScriptTypes.SelectedItem == null)
            {
                EditScript(null);
                return;
            }

            EditScript((Script)lbScriptTypes.SelectedItem.Tag);
        }

        public void Open()
        {
            ListScripts();

            Show();
        }

        public void SelectScript(Script script)
        {
            int index = lbScriptTypes.Items.FindIndex(lbi => lbi.Tag == script);

            if (index > -1)
                lbScriptTypes.SelectedIndex = index;
        }

        private void ListScripts()
        {
            lbScriptTypes.Clear();

            foreach (var script in map.Scripts)
            {
                lbScriptTypes.AddItem(new XNAListBoxItem() { Text = script.Name, Tag = script });
            }
        }

        private void EditScript(Script script)
        {
            editedScript = script;

            lbActions.Clear();
            lbActions.ViewTop = 0;

            if (editedScript == null)
            {
                tbName.Text = string.Empty;
                selTypeOfAction.Text = string.Empty;
                selTypeOfAction.Tag = null;
                tbParameterValue.Text = string.Empty;
                btnEditorPresetValues.ContextMenu.ClearItems();
                lblActionDescriptionValue.Text = string.Empty;

                return;
            }

            tbName.Text = editedScript.Name;
            for (int i = 0; i < editedScript.Actions.Count; i++)
            {
                var actionEntry = editedScript.Actions[i];
                lbActions.AddItem(new XNAListBoxItem() 
                { 
                    Text = GetActionEntryText(i, actionEntry), 
                    Tag = actionEntry
                });
            }

            LbActions_SelectedIndexChanged(this, EventArgs.Empty);
        }

        private string GetActionEntryText(int index, ScriptActionEntry entry)
        {
            ScriptAction action = GetScriptAction(entry.Action);
            if (action == null)
                return "#" + index + " - Unknown (" +  entry.Argument.ToString(CultureInfo.InvariantCulture) + ")";

            return "#" + index + " - " + action.Name + " (" + entry.Argument.ToString(CultureInfo.InvariantCulture) + ")";
        }

        private string GetActionNameFromIndex(int index)
        {
            ScriptAction action = GetScriptAction(index);
            if (action == null)
                return index + " Unknown";

            return index + " " + action.Name;
        }

        private string GetActionDescriptionFromIndex(int index)
        {
            ScriptAction action = GetScriptAction(index);
            if (action == null)
                return string.Empty;

            return Renderer.FixText(action.Description,
                lblActionDescriptionValue.FontIndex,
                lblActionDescriptionValue.Parent.Width - lblActionDescriptionValue.X * 2).Text;
        }

        private ScriptAction GetScriptAction(int index)
        {
            return map.EditorConfig.ScriptActions.GetValueOrDefault(index);
        }
    }
}
