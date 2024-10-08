using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that allows the user to edit the map's TaskForces.
    /// </summary>
    public class TaskforcesWindow : INItializableWindow
    {
        public TaskforcesWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorListBox lbTaskForces;
        private EditorTextBox tbTaskForceName;
        private EditorNumberTextBox tbGroup;
        private EditorListBox lbUnitEntries;
        private XNALabel lblCost;
        private EditorNumberTextBox tbUnitCount;
        private EditorSuggestionTextBox tbSearchUnit;
        private EditorListBox lbUnitType;

        private XNAContextMenu unitListContextMenu;

        private TaskForce editedTaskForce;


        public override void Initialize()
        {
            Name = nameof(TaskforcesWindow);

            base.Initialize();

            lbTaskForces = FindChild<EditorListBox>(nameof(lbTaskForces));
            tbTaskForceName = FindChild<EditorTextBox>(nameof(tbTaskForceName));
            tbGroup = FindChild<EditorNumberTextBox>(nameof(tbGroup));
            lbUnitEntries = FindChild<EditorListBox>(nameof(lbUnitEntries));
            lblCost = FindChild<XNALabel>(nameof(lblCost));
            tbUnitCount = FindChild<EditorNumberTextBox>(nameof(tbUnitCount));
            tbSearchUnit = FindChild<EditorSuggestionTextBox>(nameof(tbSearchUnit));
            UIHelpers.AddSearchTipsBoxToControl(tbSearchUnit);

            lbUnitType = FindChild<EditorListBox>(nameof(lbUnitType));

            var btnNewTaskForce = FindChild<EditorButton>("btnNewTaskForce");
            btnNewTaskForce.LeftClick += BtnNewTaskForce_LeftClick;

            var btnDeleteTaskForce = FindChild<EditorButton>("btnDeleteTaskForce");
            btnDeleteTaskForce.LeftClick += BtnDeleteTaskForce_LeftClick;

            var btnCloneTaskForce = FindChild<EditorButton>("btnCloneTaskForce");
            btnCloneTaskForce.LeftClick += BtnCloneTaskForce_LeftClick;

            var btnAddUnit = FindChild<EditorButton>("btnAddUnit");
            btnAddUnit.LeftClick += BtnAddUnit_LeftClick;

            var btnDeleteUnit = FindChild<EditorButton>("btnDeleteUnit");
            btnDeleteUnit.LeftClick += BtnDeleteUnit_LeftClick;

            ListUnits();

            lbTaskForces.SelectedIndexChanged += LbTaskForces_SelectedIndexChanged;
            lbUnitEntries.SelectedIndexChanged += LbUnitEntries_SelectedIndexChanged;

            tbSearchUnit.TextChanged += TbSearchUnit_TextChanged;
            tbSearchUnit.EnterPressed += TbSearchUnit_EnterPressed;

            lbUnitType.SelectedIndexChanged += LbUnitType_SelectedIndexChanged;
            tbUnitCount.TextChanged += TbUnitCount_TextChanged;

            tbTaskForceName.TextChanged += TbTaskForceName_TextChanged;
            tbGroup.TextChanged += TbGroup_TextChanged;

            unitListContextMenu = new XNAContextMenu(WindowManager);
            unitListContextMenu.Name = nameof(unitListContextMenu);
            unitListContextMenu.Width = 150;
            unitListContextMenu.AddItem("Move Up", UnitListContextMenu_MoveUp, () => editedTaskForce != null && lbUnitEntries.SelectedItem != null && lbUnitEntries.SelectedIndex > 0);
            unitListContextMenu.AddItem("Move Down", UnitListContextMenu_MoveDown, () => editedTaskForce != null && lbUnitEntries.SelectedItem != null && lbUnitEntries.SelectedIndex < lbUnitEntries.Items.Count - 1);
            unitListContextMenu.AddItem("Clone Unit Entry", UnitListContextMenu_CloneEntry, () => editedTaskForce != null && lbUnitEntries.SelectedItem != null && editedTaskForce.HasFreeTechnoSlot());
            unitListContextMenu.AddItem("Insert New Unit Here", UnitListContextMenu_Insert, () => editedTaskForce != null && lbUnitEntries.SelectedItem != null && editedTaskForce.HasFreeTechnoSlot());
            unitListContextMenu.AddItem("Delete Unit Entry", UnitListContextMenu_Delete, () => editedTaskForce != null && lbUnitEntries.SelectedItem != null);
            AddChild(unitListContextMenu);
            lbUnitEntries.AllowRightClickUnselect = false;
            lbUnitEntries.RightClick += (s, e) => { if (editedTaskForce != null) { lbUnitEntries.SelectedIndex = lbUnitEntries.HoveredIndex; unitListContextMenu.Open(GetCursorPoint()); } };
        }

        private void UnitListContextMenu_MoveUp()
        {
            if (editedTaskForce == null || lbUnitEntries.SelectedItem == null || lbUnitEntries.SelectedIndex <= 0)
                return;

            int viewTop = lbUnitEntries.ViewTop;
            editedTaskForce.TechnoTypes.Swap(lbUnitEntries.SelectedIndex - 1, lbUnitEntries.SelectedIndex);
            EditTaskForce(editedTaskForce);
            lbUnitEntries.SelectedIndex--;
            lbUnitEntries.ViewTop = viewTop;
        }

        private void UnitListContextMenu_MoveDown()
        {
            if (editedTaskForce == null || lbUnitEntries.SelectedItem == null || lbUnitEntries.SelectedIndex >= lbUnitEntries.Items.Count - 1)
                return;

            int viewTop = lbUnitEntries.ViewTop;
            editedTaskForce.TechnoTypes.Swap(lbUnitEntries.SelectedIndex, lbUnitEntries.SelectedIndex + 1);
            EditTaskForce(editedTaskForce);
            lbUnitEntries.SelectedIndex++;
            lbUnitEntries.ViewTop = viewTop;
        }

        private void UnitListContextMenu_CloneEntry()
        {
            if (editedTaskForce == null || lbUnitEntries.SelectedItem == null || !editedTaskForce.HasFreeTechnoSlot())
                return;

            int viewTop = lbUnitEntries.ViewTop;
            int newIndex = lbUnitEntries.SelectedIndex + 1;

            var clonedEntry = editedTaskForce.TechnoTypes[lbUnitEntries.SelectedIndex].Clone();
            editedTaskForce.InsertTechnoEntry(newIndex, clonedEntry);
            EditTaskForce(editedTaskForce);
            lbUnitEntries.SelectedIndex = newIndex;
            lbUnitEntries.ViewTop = viewTop;
        }

        private void UnitListContextMenu_Insert()
        {
            if (editedTaskForce == null || lbUnitEntries.SelectedItem == null)
                return;

            int viewTop = lbUnitEntries.ViewTop;
            int newIndex = lbUnitEntries.SelectedIndex;

            editedTaskForce.InsertTechnoEntry(lbUnitEntries.SelectedIndex,
                new TaskForceTechnoEntry()
                {
                    Count = 1,
                    TechnoType = (TechnoType)lbUnitType.Items[0].Tag
                });

            EditTaskForce(editedTaskForce);
            lbUnitEntries.SelectedIndex = newIndex;
            lbUnitEntries.ViewTop = viewTop;
        }

        private void UnitListContextMenu_Delete()
        {
            if (editedTaskForce == null || lbUnitEntries.SelectedItem == null)
                return;

            int viewTop = lbUnitEntries.ViewTop;
            editedTaskForce.RemoveTechnoEntry(lbUnitEntries.SelectedIndex);
            EditTaskForce(editedTaskForce);
            lbUnitEntries.ViewTop = viewTop;
        }

        private void BtnDeleteUnit_LeftClick(object sender, System.EventArgs e)
        {
            if (editedTaskForce == null || lbUnitEntries.SelectedItem == null)
                return;

            editedTaskForce.RemoveTechnoEntry(lbUnitEntries.SelectedIndex);
            EditTaskForce(editedTaskForce);
        }

        private void BtnAddUnit_LeftClick(object sender, System.EventArgs e)
        {
            if (editedTaskForce == null)
                return;

            if (!editedTaskForce.HasFreeTechnoSlot())
                return;

            editedTaskForce.AddTechnoEntry(
                new TaskForceTechnoEntry() 
                { 
                    Count = 1, 
                    TechnoType = (TechnoType)lbUnitType.Items[0].Tag 
                });

            EditTaskForce(editedTaskForce);
            lbUnitEntries.SelectedIndex = lbUnitEntries.Items.Count - 1;
        }

        private void BtnCloneTaskForce_LeftClick(object sender, System.EventArgs e)
        {
            if (editedTaskForce == null)
                return;

            map.TaskForces.Add(editedTaskForce.Clone(map.GetNewUniqueInternalId()));
            AddTaskForceToList(map.TaskForces[map.TaskForces.Count - 1]);
            SelectLastTaskForce();
        }

        private void BtnDeleteTaskForce_LeftClick(object sender, System.EventArgs e)
        {
            if (editedTaskForce == null)
                return;

            if (Keyboard.IsShiftHeldDown())
            {
                DeleteTaskForce();
            }
            else
            {
                var messageBox = EditorMessageBox.Show(WindowManager,
                    "Confirm",
                    $"Are you sure you wish to delete '{editedTaskForce.Name}'?" + Environment.NewLine + Environment.NewLine +
                    $"You'll need to manually fix any TeamTypes using the TaskForce." + Environment.NewLine + Environment.NewLine +
                    "(You can hold Shift to skip this confirmation dialog.)",
                    MessageBoxButtons.YesNo);
                messageBox.YesClickedAction = _ => DeleteTaskForce();
            }
        }

        private void DeleteTaskForce()
        {
            map.RemoveTaskForce(editedTaskForce);
            map.TeamTypes.ForEach(tt =>
            {
                if (tt.TaskForce == editedTaskForce)
                    tt.TaskForce = null;
            });
            ListTaskForces();
            RefreshSelectedTaskForce();
        }

        private void BtnNewTaskForce_LeftClick(object sender, System.EventArgs e)
        {
            map.TaskForces.Add(new TaskForce(map.GetNewUniqueInternalId()) { Name = "New TaskForce" });
            AddTaskForceToList(map.TaskForces[map.TaskForces.Count - 1]);
            SelectLastTaskForce();
        }

        private void SelectLastTaskForce()
        {
            lbTaskForces.SelectedIndex = lbTaskForces.Items.Count - 1;
            lbTaskForces.ScrollToBottom();
        }

        private void TbGroup_TextChanged(object sender, System.EventArgs e)
        {
            if (editedTaskForce == null)
                return;

            editedTaskForce.Group = tbGroup.Value;
        }

        private void TbTaskForceName_TextChanged(object sender, System.EventArgs e)
        {
            if (editedTaskForce == null)
                return;

            editedTaskForce.Name = tbTaskForceName.Text;
            if (lbTaskForces.SelectedItem != null)
            {
                lbTaskForces.SelectedItem.Text = editedTaskForce.Name;
            }
        }

        private void LbUnitType_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (lbUnitType.SelectedItem == null)
                return;

            var unitEntry = lbUnitEntries.SelectedItem;
            if (unitEntry == null)
            {
                return;
            }

            var taskForceTechno = editedTaskForce.TechnoTypes[lbUnitEntries.SelectedIndex];
            taskForceTechno.TechnoType = (TechnoType)lbUnitType.SelectedItem.Tag;
            unitEntry.Text = GetUnitEntryText(taskForceTechno);
            RefreshTaskForceCost();
        }

        private void TbUnitCount_TextChanged(object sender, System.EventArgs e)
        {
            var unitEntry = lbUnitEntries.SelectedItem;
            if (unitEntry == null)
            {
                return;
            }

            var taskForceTechno = editedTaskForce.TechnoTypes[lbUnitEntries.SelectedIndex];
            taskForceTechno.Count = tbUnitCount.Value;
            unitEntry.Text = GetUnitEntryText(taskForceTechno);
            RefreshTaskForceCost();
        }

        private void LbUnitEntries_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            var unitEntry = lbUnitEntries.SelectedItem;
            if (unitEntry == null)
            {
                tbUnitCount.Text = string.Empty;
                return;
            }

            var taskForceTechno = editedTaskForce.TechnoTypes[lbUnitEntries.SelectedIndex];

            lbUnitType.SelectedIndexChanged -= LbUnitType_SelectedIndexChanged;
            lbUnitType.SelectedIndex = lbUnitType.Items.FindIndex(u => ((TechnoType)u.Tag) == taskForceTechno.TechnoType);
            lbUnitType.ViewTop = lbUnitType.SelectedIndex * lbUnitType.LineHeight;
            lbUnitType.SelectedIndexChanged += LbUnitType_SelectedIndexChanged;

            tbUnitCount.TextChanged -= TbUnitCount_TextChanged;
            tbUnitCount.Value = taskForceTechno.Count;
            tbUnitCount.TextChanged += TbUnitCount_TextChanged;
        }

        private void LbTaskForces_SelectedIndexChanged(object sender, EventArgs e) => RefreshSelectedTaskForce();

        private void RefreshSelectedTaskForce()
        {
            var selectedItem = lbTaskForces.SelectedItem;
            if (selectedItem == null)
            {
                lbTaskForces.SelectedIndex = -1;
                EditTaskForce(null);
                return;
            }

            EditTaskForce((TaskForce)selectedItem.Tag);
        }

        private void TbSearchUnit_EnterPressed(object sender, System.EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbSearchUnit.Text) || tbSearchUnit.Text == tbSearchUnit.Suggestion)
                return;

            FindNextMatchingUnit();
        }

        private void TbSearchUnit_TextChanged(object sender, System.EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbSearchUnit.Text) || tbSearchUnit.Text == tbSearchUnit.Suggestion)
                return;

            lbUnitType.SelectedIndex = -1;
            FindNextMatchingUnit();
        }

        private void FindNextMatchingUnit()
        {
            for (int i = lbUnitType.SelectedIndex + 1; i < lbUnitType.Items.Count; i++)
            {
                var gameObjectType = (TechnoType)lbUnitType.Items[i].Tag;

                if (gameObjectType.ININame.ToUpperInvariant().Contains(tbSearchUnit.Text.ToUpperInvariant()) ||
                    gameObjectType.GetEditorDisplayName().ToUpperInvariant().Contains(tbSearchUnit.Text.ToUpperInvariant()))
                {
                    lbUnitType.SelectedIndex = i;
                    lbUnitType.ViewTop = lbUnitType.SelectedIndex * lbUnitType.LineHeight;
                    break;
                }
            }
        }

        private void ListUnits()
        {
            var gameObjectTypeList = new List<GameObjectType>();
            gameObjectTypeList.AddRange(map.Rules.AircraftTypes);
            gameObjectTypeList.AddRange(map.Rules.InfantryTypes);
            gameObjectTypeList.AddRange(map.Rules.UnitTypes);
            gameObjectTypeList = gameObjectTypeList.OrderBy(g => g.ININame).ToList();

            foreach (GameObjectType objectType in gameObjectTypeList)
            {
                lbUnitType.AddItem(new XNAListBoxItem() { Text = objectType.ININame + " (" + objectType.GetEditorDisplayName() + ")", Tag = objectType });
            }
        }

        public void Open()
        {
            Show();
            ListTaskForces();
        }

        public void SelectTaskForce(TaskForce taskForce)
        {
            int index = lbTaskForces.Items.FindIndex(lbi => lbi.Tag == taskForce);

            if (index > -1)
                lbTaskForces.SelectedIndex = index;
        }

        private void ListTaskForces()
        {
            lbTaskForces.SelectedIndexChanged -= LbTaskForces_SelectedIndexChanged;
            lbTaskForces.Clear();

            foreach (TaskForce taskForce in map.TaskForces)
            {
                AddTaskForceToList(taskForce);
            }

            lbTaskForces.SelectedIndexChanged += LbTaskForces_SelectedIndexChanged;
        }

        private void AddTaskForceToList(TaskForce taskForce)
        {
            lbTaskForces.AddItem(new XNAListBoxItem() { Text = taskForce.Name, Tag = taskForce });
        }

        private void EditTaskForce(TaskForce taskForce)
        {
            editedTaskForce = taskForce;

            RefreshTaskForceCost();

            if (taskForce == null)
            {
                tbTaskForceName.Text = string.Empty;
                tbGroup.Text = string.Empty;
                lbUnitEntries.Clear();
                tbUnitCount.Text = string.Empty;
                return;
            }

            tbSearchUnit.Text = tbSearchUnit.Suggestion;

            tbTaskForceName.Text = taskForce.Name;
            tbGroup.Value = taskForce.Group;

            lbUnitEntries.SelectedIndexChanged -= LbUnitEntries_SelectedIndexChanged;
            lbUnitEntries.Clear();

            for (int i = 0; i < taskForce.TechnoTypes.Length; i++)
            {
                var taskForceTechno = taskForce.TechnoTypes[i];
                if (taskForceTechno == null)
                    break;

                lbUnitEntries.AddItem(GetUnitEntryText(taskForceTechno));
            }

            lbUnitEntries.SelectedIndexChanged += LbUnitEntries_SelectedIndexChanged;

            if (lbUnitEntries.SelectedItem == null && lbUnitEntries.Items.Count > 0)
            {
                lbUnitEntries.SelectedIndex = 0;
            }
            else
            {
                LbUnitEntries_SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        private void RefreshTaskForceCost()
        {
            if (editedTaskForce == null)
            {
                lblCost.Text = string.Empty;
                return;
            }

            int cost = 0;
            foreach (var technoEntry in editedTaskForce.TechnoTypes)
            {
                if (technoEntry != null)
                    cost += technoEntry.TechnoType.Cost * technoEntry.Count;
            }

            lblCost.Text = cost.ToString(CultureInfo.InvariantCulture) + "$";
        }

        private string GetUnitEntryText(TaskForceTechnoEntry taskForceTechno)
        {
            return $"{taskForceTechno.Count} {taskForceTechno.TechnoType.ININame} ({taskForceTechno.TechnoType.GetEditorDisplayName()})";
        }
    }
}
