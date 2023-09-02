using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.UI.Windows
{
    public enum TriggerSortMode
    {
        ID,
        Name,
        Color
    }

    public class TriggersWindow : INItializableWindow
    {
        private const int EVENT_PARAM_FIRST = 0;
        private const int EVENT_PARAM_SECOND = 1;

        public TriggersWindow(WindowManager windowManager, Map map, EditorState editorState, ICursorActionTarget cursorActionTarget) : base(windowManager)
        {
            this.map = map;
            this.editorState = editorState;

            placeCellTagCursorAction = new PlaceCellTagCursorAction(cursorActionTarget);
            changeAttachedTagCursorAction = new ChangeAttachedTagCursorAction(cursorActionTarget);
        }

        private readonly Map map;
        private readonly PlaceCellTagCursorAction placeCellTagCursorAction;
        private readonly ChangeAttachedTagCursorAction changeAttachedTagCursorAction;
        private readonly EditorState editorState;

        private XNADropDown ddActions;

        // Trigger list
        private EditorListBox lbTriggers;

        // General trigger settings
        private EditorTextBox tbName;
        private XNADropDown ddHouse;
        private XNADropDown ddType;
        private EditorPopUpSelector selAttachedTrigger;
        private XNADropDown ddTriggerColor;
        private XNACheckBox chkDisabled;
        private XNACheckBox chkEasy;
        private XNACheckBox chkMedium;
        private XNACheckBox chkHard;

        // Events
        private EditorListBox lbEvents;
        private EditorPopUpSelector selEventType;
        private EditorDescriptionPanel panelEventDescription;
        private EditorListBox lbEventParameters;
        private EditorNumberTextBox tbEventParameterValue;

        // Actions
        private EditorListBox lbActions;
        private EditorPopUpSelector selActionType;
        private EditorDescriptionPanel panelActionDescription;
        private EditorListBox lbActionParameters;
        private EditorTextBox tbActionParameterValue;

        private SelectEventWindow selectEventWindow;
        private SelectActionWindow selectActionWindow;
        private SelectAnimationWindow selectAnimationWindow;
        private SelectBuildingTypeWindow selectBuildingTypeWindow;
        private SelectTeamTypeWindow selectTeamTypeWindow;
        private SelectTriggerWindow selectTriggerWindow;
        private SelectGlobalVariableWindow selectGlobalVariableWindow;
        private SelectLocalVariableWindow selectLocalVariableWindow;
        private SelectHouseWindow selectHouseWindow;
        private SelectTutorialLineWindow selectTutorialLineWindow;
        private SelectThemeWindow selectThemeWindow;

        private XNAContextMenu actionContextMenu;
        private XNAContextMenu eventContextMenu;
        private XNAContextMenu triggerListContextMenu;

        private Trigger editedTrigger;

        /// <summary>
        /// Used to determine what we should do when the trigger selection window closes.
        /// (apply selected trigger to a parameter for an action or attach selected trigger to our trigger)
        /// </summary>
        private bool isAttachingTrigger;

        private TriggerSortMode _triggerSortMode;
        private TriggerSortMode TriggerSortMode
        {
            get => _triggerSortMode;
            set
            {
                if (value != _triggerSortMode)
                {
                    _triggerSortMode = value;
                    ListTriggers();
                }
            }
        }

        public override void Initialize()
        {
            Name = nameof(TriggersWindow);
            base.Initialize();

            lbTriggers = FindChild<EditorListBox>(nameof(lbTriggers));
            tbName = FindChild<EditorTextBox>(nameof(tbName));
            tbName.AllowComma = false;

            ddHouse = FindChild<XNADropDown>(nameof(ddHouse));
            ddType = FindChild<XNADropDown>(nameof(ddType));
            selAttachedTrigger = FindChild<EditorPopUpSelector>(nameof(selAttachedTrigger));
            chkDisabled = FindChild<XNACheckBox>(nameof(chkDisabled));
            chkEasy = FindChild<XNACheckBox>(nameof(chkEasy));
            chkMedium = FindChild<XNACheckBox>(nameof(chkMedium));
            chkHard = FindChild<XNACheckBox>(nameof(chkHard));

            // Init color dropdown options
            ddTriggerColor = FindChild<XNADropDown>(nameof(ddTriggerColor));
            ddTriggerColor.AddItem("None");
            Array.ForEach(Trigger.SupportedColors, sc =>
            {
                ddTriggerColor.AddItem(sc.Name, sc.Value);
            });

            lbEvents = FindChild<EditorListBox>(nameof(lbEvents));
            selEventType = FindChild<EditorPopUpSelector>(nameof(selEventType));
            panelEventDescription = FindChild<EditorDescriptionPanel>(nameof(panelEventDescription));
            lbEventParameters = FindChild<EditorListBox>(nameof(lbEventParameters));
            tbEventParameterValue = FindChild<EditorNumberTextBox>(nameof(tbEventParameterValue));

            lbActions = FindChild<EditorListBox>(nameof(lbActions));
            selActionType = FindChild<EditorPopUpSelector>(nameof(selActionType));
            panelActionDescription = FindChild<EditorDescriptionPanel>(nameof(panelActionDescription));
            lbActionParameters = FindChild<EditorListBox>(nameof(lbActionParameters));
            tbActionParameterValue = FindChild<EditorTextBox>(nameof(tbActionParameterValue));

            ddType.AddItem("0 - one-time, single-object condition");
            ddType.AddItem("1 - one-time, multi-object condition");
            ddType.AddItem("2 - repeating, single-object condition");

            lbEvents.AllowMultiLineItems = false;
            lbActions.AllowMultiLineItems = false;

            FindChild<EditorButton>("btnNewTrigger").LeftClick += BtnNewTrigger_LeftClick;
            FindChild<EditorButton>("btnDeleteTrigger").LeftClick += BtnDeleteTrigger_LeftClick;
            FindChild<EditorButton>("btnCloneTrigger").LeftClick += BtnCloneTrigger_LeftClick;
            ddActions = FindChild<XNADropDown>(nameof(ddActions));
            ddActions.AddItem("Advanced...");
            ddActions.AddItem("Place CellTag");
            ddActions.AddItem("Attach to Objects");
            ddActions.AddItem("View Attached Objects");
            ddActions.AddItem(new XNADropDownItem() { Text = string.Empty, Selectable = false });
            ddActions.AddItem("Re-generate trigger IDs");
            ddActions.AddItem("Clone for easier Difficulties");
            ddActions.SelectedIndex = 0;
            ddActions.SelectedIndexChanged += DdActions_SelectedIndexChanged;

            FindChild<EditorButton>("btnAddEvent").LeftClick += BtnAddEvent_LeftClick;
            FindChild<EditorButton>("btnDeleteEvent").LeftClick += BtnDeleteEvent_LeftClick;

            FindChild<EditorButton>("btnAddAction").LeftClick += BtnAddAction_LeftClick;
            FindChild<EditorButton>("btnDeleteAction").LeftClick += BtnDeleteAction_LeftClick;

            FindChild<EditorButton>("btnActionParameterValuePreset").LeftClick += BtnActionParameterValuePreset_LeftClick;
            FindChild<EditorButton>("btnEventParameterValuePreset").LeftClick += BtnEventParameterValuePreset_LeftClick;

            selectEventWindow = new SelectEventWindow(WindowManager, map);
            var eventWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectEventWindow);
            eventWindowDarkeningPanel.Hidden += EventWindowDarkeningPanel_Hidden;

            selectActionWindow = new SelectActionWindow(WindowManager, map);
            var actionWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectActionWindow);
            actionWindowDarkeningPanel.Hidden += ActionWindowDarkeningPanel_Hidden;

            selectAnimationWindow = new SelectAnimationWindow(WindowManager, map);
            var animationWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectAnimationWindow);
            animationWindowDarkeningPanel.Hidden += AnimationWindowDarkeningPanel_Hidden;

            selectBuildingTypeWindow = new SelectBuildingTypeWindow(WindowManager, map);
            var buildingTypeWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectBuildingTypeWindow);
            buildingTypeWindowDarkeningPanel.Hidden += BuildingTypeWindowDarkeningPanel_Hidden;

            selectTeamTypeWindow = new SelectTeamTypeWindow(WindowManager, map);
            var teamTypeWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTeamTypeWindow);
            teamTypeWindowDarkeningPanel.Hidden += TeamTypeWindowDarkeningPanel_Hidden;

            selectTriggerWindow = new SelectTriggerWindow(WindowManager, map);
            var triggerWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTriggerWindow);
            triggerWindowDarkeningPanel.Hidden += TriggerWindowDarkeningPanel_Hidden;

            selectGlobalVariableWindow = new SelectGlobalVariableWindow(WindowManager, map);
            var globalVariableDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectGlobalVariableWindow);
            globalVariableDarkeningPanel.Hidden += GlobalVariableDarkeningPanel_Hidden;

            selectLocalVariableWindow = new SelectLocalVariableWindow(WindowManager, map);
            var localVariableDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectLocalVariableWindow);
            localVariableDarkeningPanel.Hidden += LocalVariableDarkeningPanel_Hidden;

            selectHouseWindow = new SelectHouseWindow(WindowManager, map);
            var houseDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectHouseWindow);
            houseDarkeningPanel.Hidden += HouseDarkeningPanel_Hidden;

            selectTutorialLineWindow = new SelectTutorialLineWindow(WindowManager, map);
            var tutorialDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTutorialLineWindow);
            tutorialDarkeningPanel.Hidden += TutorialDarkeningPanel_Hidden;

            selectThemeWindow = new SelectThemeWindow(WindowManager, map);
            var themeDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectThemeWindow);
            themeDarkeningPanel.Hidden += ThemeDarkeningPanel_Hidden;

            eventContextMenu = new XNAContextMenu(WindowManager);
            eventContextMenu.Name = nameof(eventContextMenu);
            eventContextMenu.Width = lbEvents.Width;
            eventContextMenu.AddItem("Move Up", EventContextMenu_MoveUp, () => editedTrigger != null && lbEvents.SelectedItem != null && lbEvents.SelectedIndex > 0);
            eventContextMenu.AddItem("Move Down", EventContextMenu_MoveDown, () => editedTrigger != null && lbEvents.SelectedItem != null && lbEvents.SelectedIndex < lbEvents.Items.Count - 1);
            eventContextMenu.AddItem("Clone Event", EventContextMenu_CloneEvent, () => editedTrigger != null && lbEvents.SelectedItem != null);
            eventContextMenu.AddItem("Delete Event", () => BtnDeleteEvent_LeftClick(this, EventArgs.Empty), () => editedTrigger != null && lbEvents.SelectedItem != null);
            AddChild(eventContextMenu);

            lbEvents.AllowRightClickUnselect = false;
            lbEvents.RightClick += (s, e) => { if (editedTrigger != null) { lbEvents.OnMouseLeftDown(); eventContextMenu.Open(GetCursorPoint()); } };

            actionContextMenu = new XNAContextMenu(WindowManager);
            actionContextMenu.Name = nameof(actionContextMenu);
            actionContextMenu.Width = lbActions.Width;
            actionContextMenu.AddItem("Move Up", ActionContextMenu_MoveUp, () => editedTrigger != null && lbActions.SelectedItem != null && lbActions.SelectedIndex > 0);
            actionContextMenu.AddItem("Move Down", ActionContextMenu_MoveDown, () => editedTrigger != null && lbActions.SelectedItem != null && lbActions.SelectedIndex < lbActions.Items.Count - 1);
            actionContextMenu.AddItem("Clone Action", ActionContextMenu_CloneAction, () => editedTrigger != null && lbActions.SelectedItem != null);
            actionContextMenu.AddItem("Delete Action", () => BtnDeleteAction_LeftClick(this, EventArgs.Empty), () => editedTrigger != null && lbActions.SelectedItem != null);
            AddChild(actionContextMenu);

            lbActions.AllowRightClickUnselect = false;
            lbActions.RightClick += (s, e) => { if (editedTrigger != null) { lbActions.OnMouseLeftDown(); actionContextMenu.Open(GetCursorPoint()); } };

            triggerListContextMenu = new XNAContextMenu(WindowManager);
            triggerListContextMenu.Name = nameof(triggerListContextMenu);
            triggerListContextMenu.Width = lbTriggers.Width;
            triggerListContextMenu.AddItem("Sort by ID", () => TriggerSortMode = TriggerSortMode.ID);
            triggerListContextMenu.AddItem("Sort by Name", () => TriggerSortMode = TriggerSortMode.Name);
            triggerListContextMenu.AddItem("Sort by Color", () => TriggerSortMode = TriggerSortMode.Color);
            AddChild(triggerListContextMenu);

            lbTriggers.AllowRightClickUnselect = false;
            lbTriggers.RightClick += (s, e) => triggerListContextMenu.Open(GetCursorPoint());
            lbTriggers.SelectedIndexChanged += LbTriggers_SelectedIndexChanged;
        }

        private void DdActions_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (ddActions.SelectedIndex)
            {
                case 1:
                    PlaceCellTag();
                    break;
                case 2:
                    AttachTagToObjects();
                    break;
                case 3:
                    ShowAttachedObjects();
                    break;
                case 5:
                    RegenerateIDs();
                    break;
                case 6:
                    CloneForEasierDifficulties();
                    break;
                case 0:
                default:
                    return;
            }

            ddActions.SelectedIndexChanged -= DdActions_SelectedIndexChanged;
            ddActions.SelectedIndex = 0;
            ddActions.SelectedIndexChanged += DdActions_SelectedIndexChanged;
        }

        private void PlaceCellTag()
        {
            if (editedTrigger == null)
                return;

            Tag tag = map.Tags.Find(t => t.Trigger == editedTrigger);

            if (tag == null)
            {
                return;
            }

            placeCellTagCursorAction.Tag = tag;
            editorState.CursorAction = placeCellTagCursorAction;
        }

        private void AttachTagToObjects()
        {
            if (editedTrigger == null)
                return;

            Tag tag = map.Tags.Find(t => t.Trigger == editedTrigger);

            if (tag == null)
            {
                EditorMessageBox.Show(WindowManager, "No tag found",
                    $"The selected trigger '{editedTrigger.Name}' has no" +
                    $"associated tag. As such, it cannot be attached to any objects." + Environment.NewLine + Environment.NewLine +
                    "This should never happen, have you modified the map with another editor?",
                    MessageBoxButtons.OK);

                return;
            }

            changeAttachedTagCursorAction.TagToAttach = tag;
            editorState.CursorAction = changeAttachedTagCursorAction;
        }

        #region Viewing linked objects

        private void ShowAttachedObjects()
        {
            if (editedTrigger == null)
                return;

            var tag = map.Tags.Find(t => t.Trigger == editedTrigger);
            if (tag == null)
            {
                EditorMessageBox.Show(WindowManager, "No tag found", 
                    $"The selected trigger '{editedTrigger.Name}' has no" +
                    $"associated tag. As such, it is not attached to any objects.",
                    MessageBoxButtons.OK);

                return;
            }

            var objectList = new List<TechnoBase>();
            map.Infantry.ForEach(inf => AddObjectToListIfLinkedToTag(inf, objectList, tag));
            map.Units.ForEach(unit => AddObjectToListIfLinkedToTag(unit, objectList, tag));
            map.Structures.ForEach(structure => AddObjectToListIfLinkedToTag(structure, objectList, tag));
            map.Aircraft.ForEach(aircraft => AddObjectToListIfLinkedToTag(aircraft, objectList, tag));

            var stringBuilder = new StringBuilder();

            if (objectList.Count > 0)
            {
                stringBuilder.Append($"The selected trigger '{editedTrigger.Name}' is linked to the following objects:\r\n");

                objectList.ForEach(techno =>
                {
                    switch (techno.WhatAmI())
                    {
                        case RTTIType.Aircraft:
                            AppendToStringBuilder((Aircraft)techno, stringBuilder);
                            break;
                        case RTTIType.Building:
                            AppendToStringBuilder((Structure)techno, stringBuilder);
                            break;
                        case RTTIType.Infantry:
                            AppendToStringBuilder((Infantry)techno, stringBuilder);
                            break;
                        case RTTIType.Unit:
                            AppendToStringBuilder((Unit)techno, stringBuilder);
                            break;
                        default:
                            throw new NotImplementedException("Unknown RTTI type encountered when listing linked objects for a trigger.");
                    }
                });
            }

            var celltag = map.CellTags.Find(ct => ct.Tag == tag);
            if (celltag != null)
            {
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append("The trigger is linked to one or more celltags (first match at " + celltag.Position + ").");
            }

            // Check other triggers to see whether this trigger is referenced to by them
            var allReferringTriggers = map.Triggers.FindAll(trig => {
                foreach (var triggerAction in trig.Actions)
                {
                    var actionType = map.EditorConfig.TriggerActionTypes[triggerAction.ActionIndex];

                    for (int i = 0; i < triggerAction.Parameters.Length && i < actionType.Parameters.Length; i++)
                    {
                        string paramValue = triggerAction.Parameters[i];
                        if (actionType.Parameters[i].TriggerParamType == TriggerParamType.Trigger && paramValue == editedTrigger.ID)
                        {
                            return true;
                        }
                    }
                }

                if (trig.LinkedTrigger == editedTrigger)
                    return true;

                return false;
            });

            if (allReferringTriggers.Count > 0)
            {
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append("The trigger is referenced to by the following other triggers:");
                allReferringTriggers.ForEach(trig => stringBuilder.Append(Environment.NewLine + $"    - {trig.Name} ({trig.ID})"));
            }

            if (stringBuilder.Length == 0)
            {
                EditorMessageBox.Show(WindowManager, "Linked Objects",
                    $"The selected trigger '{editedTrigger.Name}' is not linked to any objects, CellTags or other triggers.",
                    MessageBoxButtons.OK);
            }
            else
            {
                if (stringBuilder[0] == Environment.NewLine[0])
                    stringBuilder.Remove(0, Environment.NewLine.Length);

                EditorMessageBox.Show(WindowManager, "Linked Objects", stringBuilder.ToString(), MessageBoxButtons.OK);
            }

            return;
        }

        private void AppendToStringBuilder<T>(Techno<T> techno, StringBuilder stringBuilder) where T : TechnoType
        {
            string rtti = techno.WhatAmI().ToString();
            string name = techno.ObjectType.Name;
            string position = techno.Position.ToString();

            stringBuilder.Append($"    - {rtti}: {name} at {position}{Environment.NewLine}");
        }

        private void AddObjectToListIfLinkedToTag(TechnoBase techno, List<TechnoBase> technoList, Tag tag)
        {
            if (techno.AttachedTag == tag)
                technoList.Add(techno);
        }

        #endregion

        private void RegenerateIDs()
        {
            var messageBox = EditorMessageBox.Show(WindowManager, "Are you sure?",
                "This will re-generate the internal IDs (01000000, 01000001 etc.) for ALL* of your map's script elements" + Environment.NewLine +
                "that start their ID with 0100 (all editor-generated script elements do)." + Environment.NewLine + Environment.NewLine +
                "It might make the list more sensible in case there are deleted triggers. However, this feature is" + Environment.NewLine +
                "experimental and if it goes wrong, it can destroy all of your scripting. Do you want to continue?" + Environment.NewLine + Environment.NewLine +
                "* AITriggers are not yet handled by the editor, so you might need to update them manually afterwards.",
                MessageBoxButtons.YesNo);

            messageBox.YesClickedAction = _ => map.RegenerateInternalIds();
            ListTriggers();
        }

        private void CloneForEasierDifficulties()
        {
            if (editedTrigger == null)
                return;

            var messageBox = EditorMessageBox.Show(WindowManager,
                "Are you sure?",
                "Cloning this trigger for easier difficulties will create duplicate instances" + Environment.NewLine +
                "of this trigger for Medium and Easy difficulties, replacing Hard-mode globals" + Environment.NewLine +
                "with respective globals of easier difficulties." + Environment.NewLine + Environment.NewLine +
                "In case the trigger references TeamTypes, duplicates of the TeamTypes" + Environment.NewLine +
                "and their TaskForces are also created for the easier-difficulty triggers." + Environment.NewLine + Environment.NewLine +
                "No un-do is available. Do you want to continue?", MessageBoxButtons.YesNo);

            messageBox.YesClickedAction = _ => DoCloneForEasierDifficulties();
        }

        private void DoCloneForEasierDifficulties()
        {
            var originalTag = map.Tags.Find(t => t.Trigger == editedTrigger);

            var mediumDifficultyTrigger = editedTrigger.Clone(map.GetNewUniqueInternalId());
            map.AddTrigger(mediumDifficultyTrigger);
            map.Tags.Add(new Tag()
            {
                ID = map.GetNewUniqueInternalId(),
                Name = mediumDifficultyTrigger.Name + " (tag)",
                Trigger = mediumDifficultyTrigger,
                Repeating = originalTag == null ? 0 : originalTag.Repeating
            });

            var easyDifficultyTrigger = editedTrigger.Clone(map.GetNewUniqueInternalId());
            map.AddTrigger(easyDifficultyTrigger);
            map.Tags.Add(new Tag()
            {
                ID = map.GetNewUniqueInternalId(),
                Name = easyDifficultyTrigger.Name + " (tag)",
                Trigger = easyDifficultyTrigger,
                Repeating = originalTag == null ? 0 : originalTag.Repeating
            });


            mediumDifficultyTrigger.Name = editedTrigger.Name.Replace("H ", "M ").Replace(" H", " M").Replace("Hard", "Medium");
            easyDifficultyTrigger.Name = editedTrigger.Name.Replace("H ", "E ").Replace(" H", " E").Replace("Hard", "Easy");

            int mediumDiffGlobalVariableIndex = map.Rules.GlobalVariables.FindIndex(gv => gv.Name == "Difficulty Medium");
            int easyDiffGlobalVariableIndex = map.Rules.GlobalVariables.FindIndex(gv => gv.Name == "Difficulty Easy");

            if (mediumDiffGlobalVariableIndex < 0)
            {
                Logger.Log($"{nameof(TriggersWindow)}.{nameof(DoCloneForEasierDifficulties)}: Medium difficulty global variable not found!");
            }

            if (easyDiffGlobalVariableIndex < 0)
            {
                Logger.Log($"{nameof(TriggersWindow)}.{nameof(DoCloneForEasierDifficulties)}: Medium difficulty global variable not found!");
            }

            // Go through used events. If there's a reference to the
            // "Difficulty Hard" global, replace it
            // with references to the Medium and Easy globals.
            for (int i = 0; i < editedTrigger.Conditions.Count; i++)
            {
                TriggerCondition condition = editedTrigger.Conditions[i];

                if (map.EditorConfig.TriggerEventTypes[condition.ConditionIndex].P2Type == TriggerParamType.GlobalVariable)
                {
                    if (condition.Parameter2 < 0 && condition.Parameter2 >= map.Rules.GlobalVariables.Count)
                        continue;

                    if (map.Rules.GlobalVariables[condition.Parameter2].Name == "Difficulty Hard")
                    {
                        if (mediumDiffGlobalVariableIndex > -1)
                        {
                            mediumDifficultyTrigger.Conditions[i].Parameter2 = mediumDiffGlobalVariableIndex;
                        }

                        if (easyDiffGlobalVariableIndex > -1)
                        {
                            easyDifficultyTrigger.Conditions[i].Parameter2 = easyDiffGlobalVariableIndex;
                        }
                    }
                }
            }

            // Go through used actions. If they refer to any TeamTypes, clone the
            // TeamTypes and replace the references
            for (int i = 0; i < editedTrigger.Actions.Count; i++)
            {
                TriggerAction action = editedTrigger.Actions[i];

                TriggerActionType triggerActionType = map.EditorConfig.TriggerActionTypes[action.ActionIndex];

                for (int j = 0; j < triggerActionType.Parameters.Length; j++)
                {
                    var param = triggerActionType.Parameters[j];

                    if (param != null && param.TriggerParamType == TriggerParamType.TeamType)
                    {
                        TeamType teamType = map.TeamTypes.Find(tt => tt.ININame == action.ParamToString(j));

                        if (teamType != null && teamType.TaskForce != null)
                        {
                            TaskForce mediumTaskForce = teamType.TaskForce.Clone(map.GetNewUniqueInternalId());
                            map.AddTaskForce(mediumTaskForce);

                            TaskForce easyTaskForce = teamType.TaskForce.Clone(map.GetNewUniqueInternalId());
                            map.AddTaskForce(easyTaskForce);

                            mediumTaskForce.Name = teamType.TaskForce.Name.Replace("H ", "M ").Replace(" H", " M").Replace("Hard", "Medium");
                            easyTaskForce.Name = teamType.TaskForce.Name.Replace("H ", "E ").Replace(" H", " E").Replace("Hard", "Easy");

                            TeamType mediumTeamType = teamType.Clone(map.GetNewUniqueInternalId());
                            map.AddTeamType(mediumTeamType);

                            TeamType easyTeamType = teamType.Clone(map.GetNewUniqueInternalId());
                            map.AddTeamType(easyTeamType);

                            mediumTeamType.Name = teamType.Name.Replace("H ", "M ").Replace(" H", " M").Replace("Hard", "Medium");
                            easyTeamType.Name = teamType.Name.Replace("H ", "E ").Replace(" H", " E").Replace("Hard", "Easy");

                            mediumTeamType.TaskForce = mediumTaskForce;
                            easyTeamType.TaskForce = easyTaskForce;

                            mediumDifficultyTrigger.Actions[i].Parameters[j] = mediumTeamType.ININame;
                            easyDifficultyTrigger.Actions[i].Parameters[j] = easyTeamType.ININame;
                        }
                    }
                }
            }

            ListTriggers();
        }

        #region Event and action context menus

        private void ActionContextMenu_MoveUp() => MoveUpEventOrAction(lbActions, editedTrigger?.Actions);

        private void EventContextMenu_MoveUp() => MoveUpEventOrAction(lbEvents, editedTrigger?.Conditions);

        private void MoveUpEventOrAction<T>(XNAListBox listBox, List<T> objectList)
        {
            if (editedTrigger == null || objectList == null || listBox.SelectedItem == null || listBox.SelectedIndex < 1)
                return;

            var tmp = objectList[listBox.SelectedIndex - 1];
            objectList[listBox.SelectedIndex - 1] = objectList[listBox.SelectedIndex];
            objectList[listBox.SelectedIndex] = tmp;

            EditTrigger(editedTrigger);
            listBox.SelectedIndex--;
        }

        private void ActionContextMenu_MoveDown() => MoveDownEventOrAction(lbActions, editedTrigger?.Actions);

        private void EventContextMenu_MoveDown() => MoveDownEventOrAction(lbEvents, editedTrigger?.Conditions);

        private void MoveDownEventOrAction<T>(XNAListBox listBox, List<T> objectList)
        {
            if (editedTrigger == null || listBox.SelectedItem == null || listBox.SelectedIndex >= listBox.Items.Count - 1)
                return;

            var tmp = objectList[listBox.SelectedIndex + 1];
            objectList[listBox.SelectedIndex + 1] = objectList[listBox.SelectedIndex];
            objectList[listBox.SelectedIndex] = tmp;

            EditTrigger(editedTrigger);
            listBox.SelectedIndex++;
        }

        private void EventContextMenu_CloneEvent() => CloneEventOrAction(lbEvents, editedTrigger?.Conditions);

        private void ActionContextMenu_CloneAction() => CloneEventOrAction(lbActions, editedTrigger?.Actions);

        private void CloneEventOrAction<T>(XNAListBox listBox, List<T> objectList) where T : ICloneable
        {
            if (editedTrigger == null || listBox.SelectedItem == null)
                return;

            var tag = (T)listBox.SelectedItem.Tag;
            var clone = (T)tag.Clone();
            objectList.Insert(listBox.SelectedIndex + 1, clone);
            EditTrigger(editedTrigger);
            listBox.SelectedIndex++;
        }

        #endregion

        private void BtnEventParameterValuePreset_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null || lbEvents.SelectedItem == null || lbEventParameters.SelectedItem == null)
                return;

            var triggerEvent = (TriggerCondition)lbEvents.SelectedItem.Tag;
            var triggerEventType = GetTriggerEventType(triggerEvent.ConditionIndex);
            int paramIndex = (int)lbEventParameters.SelectedItem.Tag;

            if (triggerEventType == null)
                return;

            TriggerParamType triggerParamType = paramIndex == EVENT_PARAM_FIRST ? triggerEventType.P1Type : triggerEventType.P2Type;
            int paramValue = paramIndex == EVENT_PARAM_FIRST ? triggerEvent.Parameter1 : triggerEvent.Parameter2;

            switch (triggerParamType)
            {
                case TriggerParamType.GlobalVariable:
                    GlobalVariable existingGlobalVariable = map.Rules.GlobalVariables.Find(gv => gv.Index == paramValue);
                    selectGlobalVariableWindow.IsForEvent = true;
                    selectGlobalVariableWindow.Open(existingGlobalVariable);
                    break;
                case TriggerParamType.LocalVariable:
                    LocalVariable existingLocalVariable = map.LocalVariables.Find(lv => lv.Index == paramValue);
                    selectLocalVariableWindow.IsForEvent = true;
                    selectLocalVariableWindow.Open(existingLocalVariable);
                    break;
                case TriggerParamType.House:
                    selectHouseWindow.IsForEvent = true;
                    if (paramValue > -1 && paramValue < map.GetHouses().Count)
                        selectHouseWindow.Open(map.GetHouses()[paramValue]);
                    else
                        selectHouseWindow.Open(null);
                    break;
                case TriggerParamType.Building:
                    BuildingType existingBuilding = paramValue < 0 || paramValue >= map.Rules.BuildingTypes.Count ? null : map.Rules.BuildingTypes[paramValue];
                    selectBuildingTypeWindow.IsForEvent = true;
                    selectBuildingTypeWindow.Open(existingBuilding);
                    break;
                default:
                    break;
            }
        }

        private void BtnActionParameterValuePreset_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null || lbActions.SelectedItem == null || lbActionParameters.SelectedItem == null)
                return;

            var triggerAction = (TriggerAction)lbActions.SelectedItem.Tag;
            var triggerActionType = GetTriggerActionType(triggerAction.ActionIndex);
            int paramIndex = (int)lbActionParameters.SelectedItem.Tag;

            if (triggerActionType == null)
                return;

            switch (triggerActionType.Parameters[paramIndex].TriggerParamType)
            {
                case TriggerParamType.Animation:
                    AnimType existingAnimType = map.Rules.AnimTypes.Find(at => at.Index == Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1));
                    selectAnimationWindow.IsForEvent = false;
                    selectAnimationWindow.Open(existingAnimType);
                    break;
                case TriggerParamType.TeamType:
                    TeamType existingTeamType = map.TeamTypes.Find(tt => tt.ININame == triggerAction.Parameters[paramIndex]);
                    selectTeamTypeWindow.IsForEvent = false;
                    selectTeamTypeWindow.Open(existingTeamType);
                    break;
                case TriggerParamType.Trigger:
                    Trigger existingTrigger = map.Triggers.Find(tt => tt.ID == triggerAction.Parameters[paramIndex]);
                    isAttachingTrigger = false;
                    selectTriggerWindow.Open(existingTrigger);
                    break;
                case TriggerParamType.GlobalVariable:
                    GlobalVariable existingGlobalVariable = map.Rules.GlobalVariables.Find(gv => gv.Index == Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1));
                    selectGlobalVariableWindow.IsForEvent = false;
                    selectGlobalVariableWindow.Open(existingGlobalVariable);
                    break;
                case TriggerParamType.LocalVariable:
                    LocalVariable existingLocalVariable = map.LocalVariables.Find(lv => lv.Index == Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1));
                    selectLocalVariableWindow.IsForEvent = false;
                    selectLocalVariableWindow.Open(existingLocalVariable);
                    break;
                case TriggerParamType.House:
                    int houseIndex = Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1);
                    selectHouseWindow.IsForEvent = false;
                    if (houseIndex > -1 && houseIndex < map.GetHouses().Count)
                        selectHouseWindow.Open(map.GetHouses()[houseIndex]);
                    else
                        selectHouseWindow.Open(null);
                    break;
                case TriggerParamType.Text:
                    selectTutorialLineWindow.Open(new TutorialLine(Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1), string.Empty));
                    break;
                case TriggerParamType.Theme:
                    selectThemeWindow.Open(map.Rules.Themes.GetByIndex(Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1)));
                    break;
                default:
                    break;
            }
        }

        private void AnimationWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectAnimationWindow.SelectedObject == null)
                return;

            AssignParamValue(selectAnimationWindow.IsForEvent, selectAnimationWindow.SelectedObject.Index);
        }

        private void BuildingTypeWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectBuildingTypeWindow.SelectedObject == null)
                return;

            AssignParamValue(selectBuildingTypeWindow.IsForEvent, selectBuildingTypeWindow.SelectedObject.Index);
        }

        private void ThemeDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectThemeWindow.SelectedObject == null)
                return;

            AssignParamValue(selectThemeWindow.IsForEvent, selectThemeWindow.SelectedObject.Index);
        }

        private void TutorialDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectTutorialLineWindow.SelectedObject.ID < 0 || selectTutorialLineWindow.SelectedObject.Text == null)
                return;

            AssignParamValue(selectTutorialLineWindow.IsForEvent, selectTutorialLineWindow.SelectedObject.ID);
        }

        private void HouseDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectHouseWindow.SelectedObject == null)
                return;

            int houseIndex = map.GetHouses().FindIndex(h => h == selectHouseWindow.SelectedObject);
            AssignParamValue(selectHouseWindow.IsForEvent, houseIndex);
        }

        private void LocalVariableDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectLocalVariableWindow.SelectedObject == null)
                return;

            int localVariableIndex = selectLocalVariableWindow.SelectedObject.Index;
            AssignParamValue(selectLocalVariableWindow.IsForEvent, localVariableIndex);
        }

        private void GlobalVariableDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectGlobalVariableWindow.SelectedObject == null)
                return;

            int globalVariableIndex = selectGlobalVariableWindow.SelectedObject.Index;
            AssignParamValue(selectGlobalVariableWindow.IsForEvent, globalVariableIndex);
        }

        private void AssignParamValue(bool isForEvent, int paramValue)
        {
            if (isForEvent)
            {
                GetTriggerEventAndParamIndex(out TriggerCondition triggerCondition, out int paramIndex);
                if (paramIndex == EVENT_PARAM_FIRST)
                    triggerCondition.Parameter1 = paramValue;
                else
                    triggerCondition.Parameter2 = paramValue;
            }
            else
            {
                GetTriggerActionAndParamIndex(out TriggerAction triggerAction, out int paramIndex);
                triggerAction.Parameters[paramIndex] = paramValue.ToString();
            }

            EditTrigger(editedTrigger);
        }

        private void TriggerWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (isAttachingTrigger)
            {
                editedTrigger.LinkedTrigger = selectTriggerWindow.SelectedObject;
                EditTrigger(editedTrigger);
                return;
            }

            if (selectTriggerWindow.SelectedObject == null)
                return;

            GetTriggerActionAndParamIndex(out TriggerAction triggerAction, out int paramIndex);
            triggerAction.Parameters[paramIndex] = selectTriggerWindow.SelectedObject.ID;
            EditTrigger(editedTrigger);
        }

        private void TeamTypeWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectTeamTypeWindow.SelectedObject == null)
                return;

            GetTriggerActionAndParamIndex(out TriggerAction triggerAction, out int paramIndex);
            triggerAction.Parameters[paramIndex] = selectTeamTypeWindow.SelectedObject.ININame;
            EditTrigger(editedTrigger);
        }

        private void GetTriggerActionAndParamIndex(out TriggerAction triggerAction, out int paramIndex)
        {
            triggerAction = (TriggerAction)lbActions.SelectedItem.Tag;
            paramIndex = (int)lbActionParameters.SelectedItem.Tag;
        }

        private void GetTriggerEventAndParamIndex(out TriggerCondition triggerEvent, out int paramIndex)
        {
            triggerEvent = (TriggerCondition)lbEvents.SelectedItem.Tag;
            paramIndex = (int)lbEventParameters.SelectedItem.Tag;
        }

        private void BtnNewTrigger_LeftClick(object sender, EventArgs e)
        {
            var newTrigger = new Trigger(map.GetNewUniqueInternalId()) { Name = "New trigger", House = "Neutral" };
            map.Triggers.Add(newTrigger);
            map.Tags.Add(new Tag() { ID = map.GetNewUniqueInternalId(), Name = "New tag", Trigger = newTrigger });
            ListTriggers();
            SelectTrigger(newTrigger);
        }

        private void BtnCloneTrigger_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null)
                return;

            var originalTag = map.Tags.Find(t => t.Trigger == editedTrigger);

            var clone = editedTrigger.Clone(map.GetNewUniqueInternalId());
            map.Triggers.Add(clone);
            map.Tags.Add(new Tag() { ID = map.GetNewUniqueInternalId(), Name = clone.Name + " (tag)", Trigger = clone, Repeating = originalTag == null ? 0 : originalTag.Repeating });
            ListTriggers();
            SelectTrigger(clone);
        }

        private void BtnDeleteTrigger_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null)
                return;

            map.Triggers.Remove(editedTrigger);
            map.Triggers.ForEach(t => { if (t.LinkedTrigger == editedTrigger) t.LinkedTrigger = null; });
            map.Tags.RemoveAll(t => t.Trigger == editedTrigger);
            editedTrigger = null;

            ListTriggers();
        }

        private void SelectTrigger(Trigger trigger)
        {
            lbTriggers.SelectedIndex = lbTriggers.Items.FindIndex(item => item.Tag == trigger);

            if (lbTriggers.LastIndex < lbTriggers.SelectedIndex)
                lbTriggers.ScrollToBottom(); // TODO we don't actually have a good way to scroll the listbox into a specific place right now
            else if (lbTriggers.TopIndex > lbTriggers.SelectedIndex)
                lbTriggers.TopIndex = lbTriggers.SelectedIndex;
        }

        private void EventWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (editedTrigger == null || lbEvents.SelectedItem == null || selectEventWindow.SelectedObject == null)
                return;

            TriggerCondition condition = editedTrigger.Conditions[lbEvents.SelectedIndex];
            condition.ConditionIndex = selectEventWindow.SelectedObject.ID;

            if (selectEventWindow.SelectedObject.P1Type == TriggerParamType.Unused)
                condition.Parameter1 = 0;

            if (selectEventWindow.SelectedObject.P2Type == TriggerParamType.Unused)
                condition.Parameter2 = 0;

            EditTrigger(editedTrigger);
        }

        private void ActionWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (editedTrigger == null || lbActions.SelectedItem == null || selectActionWindow.SelectedObject == null)
                return;

            TriggerActionType triggerActionType = selectActionWindow.SelectedObject;
            TriggerAction action = editedTrigger.Actions[lbActions.SelectedIndex];
            action.ActionIndex = selectActionWindow.SelectedObject.ID;

            for (int i = 0; i < TriggerActionType.MAX_PARAM_COUNT; i++)
            {
                if ((int)triggerActionType.Parameters[i].TriggerParamType < 0)
                {
                    action.Parameters[i] = Math.Abs((int)triggerActionType.Parameters[i].TriggerParamType).ToString(CultureInfo.InvariantCulture);
                    continue;
                }

                if (triggerActionType.Parameters[i].TriggerParamType == TriggerParamType.Unused)
                {
                    action.Parameters[i] = "0";
                    continue;
                }
            }

            EditTrigger(editedTrigger);
        }

        private void BtnAddAction_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null)
                return;

            editedTrigger.Actions.Add(new TriggerAction());
            EditTrigger(editedTrigger);
            lbActions.SelectedIndex = lbActions.Items.Count - 1;

            if (Keyboard.IsCtrlHeldDown())
                SelActionType_LeftClick(this, EventArgs.Empty);
        }

        private void BtnDeleteAction_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null || lbActions.SelectedItem == null)
                return;

            editedTrigger.Actions.RemoveAt(lbActions.SelectedIndex);
            EditTrigger(editedTrigger);
        }

        private void BtnAddEvent_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null)
                return;

            editedTrigger.Conditions.Add(new TriggerCondition());
            EditTrigger(editedTrigger);
            lbEvents.SelectedIndex = lbEvents.Items.Count - 1;

            if (Keyboard.IsCtrlHeldDown())
                SelEventType_LeftClick(this, EventArgs.Empty);
        }

        private void BtnDeleteEvent_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null || lbEvents.SelectedItem == null)
                return;

            editedTrigger.Conditions.RemoveAt(lbEvents.SelectedIndex);
            EditTrigger(editedTrigger);
        }

        private void LbTriggers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbTriggers.SelectedItem == null)
            {
                EditTrigger(null);
                return;
            }

            EditTrigger((Trigger)lbTriggers.SelectedItem.Tag);
        }

        public void Open()
        {
            ListTriggers();
            RefreshHouses();
            Show();
        }

        private void RefreshHouses()
        {
            ddHouse.Items.Clear();
            map.GetHouses().ForEach(h => ddHouse.AddItem(h.ININame, h.XNAColor));
        }

        private void ListTriggers()
        {
            lbTriggers.Clear();

            List<Trigger> sortedTriggers = map.Triggers;
            switch (TriggerSortMode)
            {
                case TriggerSortMode.Color:
                    sortedTriggers = sortedTriggers.OrderBy(t => t.EditorColor).ThenBy(t => t.ID).ToList();
                    break;
                case TriggerSortMode.Name:
                    sortedTriggers = sortedTriggers.OrderBy(t => t.Name).ThenBy(t => t.ID).ToList();
                    break;
                case TriggerSortMode.ID:
                default:
                    sortedTriggers = sortedTriggers.OrderBy(t => t.ID).ToList();
                    break;
            }

            foreach (Trigger trigger in sortedTriggers)
            {
                lbTriggers.AddItem(new XNAListBoxItem()
                { 
                    Text = trigger.Name, 
                    Tag = trigger, 
                    TextColor = trigger.EditorColor == null ? lbTriggers.DefaultItemColor : trigger.XNAColor 
                });
            }

            LbTriggers_SelectedIndexChanged(this, EventArgs.Empty);
        }

        private void EditTrigger(Trigger trigger)
        {
            lbEvents.SelectedIndexChanged -= LbEvents_SelectedIndexChanged;
            lbActions.SelectedIndexChanged -= LbActions_SelectedIndexChanged;
            tbName.TextChanged -= TbName_TextChanged;
            ddHouse.SelectedIndexChanged -= DdHouse_SelectedIndexChanged;
            ddType.SelectedIndexChanged -= DdType_SelectedIndexChanged;
            chkDisabled.CheckedChanged -= ChkDisabled_CheckedChanged;
            selAttachedTrigger.LeftClick -= SelAttachedTrigger_LeftClick;
            ddTriggerColor.SelectedIndexChanged -= DdTriggerColor_SelectedIndexChanged;

            editedTrigger = trigger;

            if (editedTrigger == null)
            {
                tbName.Text = string.Empty;
                ddHouse.SelectedIndex = -1;
                ddType.SelectedIndex = -1;
                selAttachedTrigger.Text = string.Empty;

                lbEvents.Clear();
                selEventType.Text = string.Empty;
                panelEventDescription.Text = string.Empty;
                lbEventParameters.Clear();
                tbEventParameterValue.Text = string.Empty;

                lbActions.Clear();
                selActionType.Text = string.Empty;
                panelActionDescription.Text = string.Empty;
                lbActionParameters.Clear();
                tbActionParameterValue.Text = string.Empty;

                return;
            }

            var tag = map.Tags.Find(t => t.Trigger == editedTrigger);
            if (tag == null)
            {
                ddType.AllowDropDown = false;

                if (ddType.Items.Count < 4)
                    ddType.AddItem("Error: No tag exists for this trigger!");
            }
            else
            {
                ddType.AllowDropDown = true;

                if (ddType.Items.Count > 3)
                    ddType.Items.RemoveAt(3);
            }

            tbName.Text = editedTrigger.Name;
            ddHouse.SelectedIndex = map.GetHouses().FindIndex(h => h.ININame == trigger.House);
            ddType.SelectedIndex = tag == null ? 3 : tag.Repeating;
            selAttachedTrigger.Text = editedTrigger.LinkedTrigger == null ? Constants.NoneValue1 : editedTrigger.LinkedTrigger.Name;
            selAttachedTrigger.Tag = editedTrigger.LinkedTrigger;
            chkDisabled.Checked = editedTrigger.Disabled;
            chkEasy.Checked = editedTrigger.Easy;
            chkMedium.Checked = editedTrigger.Normal;
            chkHard.Checked = editedTrigger.Hard;
            ddTriggerColor.SelectedIndex = ddTriggerColor.Items.FindIndex(item => item.Text == editedTrigger.EditorColor);
            if (ddTriggerColor.SelectedIndex < 0)
                ddTriggerColor.SelectedIndex = 0;

            lbEvents.ViewTop = 0;
            lbActions.ViewTop = 0;

            lbEvents.Clear();
            editedTrigger.Conditions.ForEach(c => AddEvent(c));

            lbActions.Clear();
            editedTrigger.Actions.ForEach(a => AddAction(a));

            LbEvents_SelectedIndexChanged(this, EventArgs.Empty);
            LbActions_SelectedIndexChanged(this, EventArgs.Empty);

            lbEvents.SelectedIndexChanged += LbEvents_SelectedIndexChanged;
            lbActions.SelectedIndexChanged += LbActions_SelectedIndexChanged;
            tbName.TextChanged += TbName_TextChanged;
            ddHouse.SelectedIndexChanged += DdHouse_SelectedIndexChanged;
            ddType.SelectedIndexChanged += DdType_SelectedIndexChanged;
            chkDisabled.CheckedChanged += ChkDisabled_CheckedChanged;
            selAttachedTrigger.LeftClick += SelAttachedTrigger_LeftClick;
            ddTriggerColor.SelectedIndexChanged += DdTriggerColor_SelectedIndexChanged;
        }

        private void SelAttachedTrigger_LeftClick(object sender, EventArgs e)
        {
            isAttachingTrigger = true;
            selectTriggerWindow.Open((Trigger)selAttachedTrigger.Tag);
        }

        private void DdTriggerColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddTriggerColor.SelectedIndex < 1)
            {
                editedTrigger.EditorColor = null;
                lbTriggers.SelectedItem.TextColor = lbTriggers.DefaultItemColor;
                return;
            }

            editedTrigger.EditorColor = ddTriggerColor.SelectedItem.Text;
            lbTriggers.SelectedItem.TextColor = ddTriggerColor.SelectedItem.TextColor.Value;
        }

        private void DdType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tag = map.Tags.Find(t => t.Trigger == editedTrigger);
            if (tag != null)
                tag.Repeating = ddType.SelectedIndex;
        }

        private void DdHouse_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedTrigger.House = ddHouse.SelectedItem.Text;
        }

        private void ChkDisabled_CheckedChanged(object sender, EventArgs e)
        {
            editedTrigger.Disabled = chkDisabled.Checked;
        }

        private void TbName_TextChanged(object sender, EventArgs e)
        {
            var tag = map.Tags.Find(t => t.Trigger == editedTrigger);
            if (tag != null)
                tag.Name = tbName.Text + " (tag)";

            editedTrigger.Name = tbName.Text;
            lbTriggers.SelectedItem.Text = tbName.Text;
        }

        private void LbActions_SelectedIndexChanged(object sender, EventArgs e)
        {
            lbActionParameters.SelectedIndexChanged -= LbActionParameters_SelectedIndexChanged;
            selActionType.LeftClick -= SelActionType_LeftClick;

            if (lbActions.SelectedItem == null)
            {
                selActionType.Text = string.Empty;
                panelActionDescription.Text = string.Empty;
                lbActionParameters.Clear();
                tbActionParameterValue.Text = string.Empty;
                return;
            }

            var triggerAction = (TriggerAction)lbActions.SelectedItem.Tag;
            TriggerActionType triggerActionType = map.EditorConfig.TriggerActionTypes.GetValueOrDefault(triggerAction.ActionIndex);

            selActionType.Text = triggerAction.ActionIndex + " " + (triggerActionType == null ? "Unknown" : triggerActionType.Name);
            panelActionDescription.Text = triggerActionType == null ? "Unknown action. It has most likely been added with another editor." : triggerActionType.Description;

            lbActionParameters.Clear();
            if (triggerActionType == null)
            {
                for (int i = 0; i < TriggerActionType.MAX_PARAM_COUNT; i++)
                {
                    lbActionParameters.AddItem(new XNAListBoxItem() { Text = $"Parameter {i}", Tag = i });
                }
            }
            else
            {
                for (int i = 0; i < triggerActionType.Parameters.Length; i++)
                {
                    var param = triggerActionType.Parameters[i];
                    if (param.TriggerParamType == TriggerParamType.Unused || (int)param.TriggerParamType < 0)
                        continue;

                    lbActionParameters.AddItem(new XNAListBoxItem() { Text = param.NameOverride ?? param.TriggerParamType.ToString(), Tag = i });
                }
            }

            if (lbActionParameters.SelectedItem == null && lbActionParameters.Items.Count > 0)
                lbActionParameters.SelectedIndex = 0;

            LbActionParameters_SelectedIndexChanged(this, EventArgs.Empty);

            lbActionParameters.SelectedIndexChanged += LbActionParameters_SelectedIndexChanged;
            selActionType.LeftClick += SelActionType_LeftClick;
        }

        private void SelActionType_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null || lbActions.SelectedItem == null)
                return;

            int actionTypeIndex = editedTrigger.Actions[lbActions.SelectedIndex].ActionIndex;
            selectActionWindow.Open(GetTriggerActionType(actionTypeIndex));
        }

        private void LbActionParameters_SelectedIndexChanged(object sender, EventArgs e)
        {
            tbActionParameterValue.TextChanged -= TbActionParameterValue_TextChanged;

            if (lbActionParameters.SelectedItem == null || editedTrigger == null || lbActions.SelectedItem == null)
            {
                tbActionParameterValue.Text = string.Empty;
                return;
            }

            TriggerAction triggerAction = editedTrigger.Actions[lbActions.SelectedIndex];
            int paramNumber = (int)lbActionParameters.SelectedItem.Tag;
            var triggerActionType = GetTriggerActionType(triggerAction.ActionIndex);
            var triggerParamType = TriggerParamType.Unknown;
            if (triggerActionType != null)
            {
                triggerParamType = triggerActionType.Parameters[paramNumber].TriggerParamType;
            }

            tbActionParameterValue.Text = GetParamValueText(triggerAction.Parameters[paramNumber], triggerParamType);
            tbActionParameterValue.TextColor = GetParamValueColor(triggerAction.Parameters[paramNumber], triggerParamType);

            tbActionParameterValue.TextChanged += TbActionParameterValue_TextChanged;
        }

        private void TbActionParameterValue_TextChanged(object sender, EventArgs e)
        {
            if (lbActionParameters.SelectedItem == null || editedTrigger == null || lbActions.SelectedItem == null)
            {
                return;
            }

            int paramNumber = (int)lbActionParameters.SelectedItem.Tag;
            var triggerAction = (TriggerAction)lbActions.SelectedItem.Tag;

            int spaceIndex = tbActionParameterValue.Text.IndexOf(' ');
            string value = tbActionParameterValue.Text;
            if (spaceIndex > -1)
                value = value.Substring(0, spaceIndex);

            var triggerActionType = GetTriggerActionType(triggerAction.ActionIndex);

            if (triggerActionType != null && 
                triggerActionType.Parameters[paramNumber].TriggerParamType == TriggerParamType.WaypointZZ)
            {
                // Write waypoint with A-ZZ notation
                value = Helpers.WaypointNumberToAlphabeticalString(Conversions.IntFromString(value, 0));
            }

            triggerAction.Parameters[paramNumber] = value;
        }

        private void AddAction(TriggerAction action)
        {
            var triggerActionType = map.EditorConfig.TriggerActionTypes.GetValueOrDefault(action.ActionIndex);

            if (triggerActionType == null)
            {
                lbActions.AddItem(new XNAListBoxItem() { Text = action.ActionIndex + " Unknown", Tag = action });
                return;
            }

            lbActions.AddItem(new XNAListBoxItem() { Text = action.ActionIndex + " " + triggerActionType.Name, Tag = action });
        }

        private void LbEvents_SelectedIndexChanged(object sender, EventArgs e)
        {
            lbEventParameters.SelectedIndexChanged -= LbEventParameters_SelectedIndexChanged;
            selEventType.LeftClick -= SelEventType_LeftClick;

            if (lbEvents.SelectedItem == null)
            {
                selEventType.Text = string.Empty;
                panelEventDescription.Text = string.Empty;
                lbEventParameters.Clear();
                tbEventParameterValue.Text = string.Empty;
                return;
            }

            var triggerCondition = (TriggerCondition)lbEvents.SelectedItem.Tag;
            TriggerEventType triggerEventType = map.EditorConfig.TriggerEventTypes.GetValueOrDefault(triggerCondition.ConditionIndex);

            selEventType.Text = triggerCondition.ConditionIndex + " " + (triggerEventType == null ? "Unknown" : triggerEventType.Name);
            panelEventDescription.Text = triggerEventType == null ? "Unknown event. It has most likely been added with another editor." : triggerEventType.Description;

            lbEventParameters.Clear();
            if (triggerEventType == null)
            {
                lbEventParameters.AddItem(new XNAListBoxItem() { Text = "Parameter 0", Tag = EVENT_PARAM_FIRST });
                lbEventParameters.AddItem(new XNAListBoxItem() { Text = "Parameter 1", Tag = EVENT_PARAM_SECOND });
            }
            else
            {
                if (triggerEventType.P1Type != TriggerParamType.Unused)
                    lbEventParameters.AddItem(new XNAListBoxItem() { Text = triggerEventType.P1Type.ToString(), Tag = EVENT_PARAM_FIRST });

                if (triggerEventType.P2Type != TriggerParamType.Unused)
                    lbEventParameters.AddItem(new XNAListBoxItem() { Text = triggerEventType.P2Type.ToString(), Tag = EVENT_PARAM_SECOND });
            }

            if (lbEventParameters.SelectedItem == null && lbEventParameters.Items.Count > 0)
                lbEventParameters.SelectedIndex = 0;

            LbEventParameters_SelectedIndexChanged(this, EventArgs.Empty);

            lbEventParameters.SelectedIndexChanged += LbEventParameters_SelectedIndexChanged;
            selEventType.LeftClick += SelEventType_LeftClick;
        }

        private void SelEventType_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null || lbEvents.SelectedItem == null)
                return;

            int eventTypeIndex = editedTrigger.Conditions[lbEvents.SelectedIndex].ConditionIndex;
            selectEventWindow.Open(GetTriggerEventType(eventTypeIndex));
        }

        private void LbEventParameters_SelectedIndexChanged(object sender, EventArgs e)
        {
            tbEventParameterValue.TextChanged -= TbEventParameterValue_TextChanged;

            if (lbEventParameters.SelectedItem == null || editedTrigger == null || lbEvents.SelectedItem == null)
            {
                tbEventParameterValue.Text = string.Empty;
                return;
            }

            var triggerEventType = GetTriggerEventType(editedTrigger.Conditions[lbEvents.SelectedIndex].ConditionIndex);
            var triggerParamType = TriggerParamType.Unknown;
            int paramValue = -1;
            int paramNumber = (int)lbEventParameters.SelectedItem.Tag;
            if (paramNumber == EVENT_PARAM_FIRST)
            {
                paramValue = editedTrigger.Conditions[lbEvents.SelectedIndex].Parameter1;

                if (triggerEventType != null)
                    triggerParamType = triggerEventType.P1Type;
            }
            else
            {
                paramValue = editedTrigger.Conditions[lbEvents.SelectedIndex].Parameter2;

                if (triggerEventType != null)
                    triggerParamType = triggerEventType.P2Type;
            }

            tbEventParameterValue.Text = GetParamValueText(paramValue.ToString(), triggerParamType);
            tbEventParameterValue.TextColor = GetParamValueColor(paramValue.ToString(), triggerParamType);

            tbEventParameterValue.TextChanged += TbEventParameterValue_TextChanged;
        }

        private void TbEventParameterValue_TextChanged(object sender, EventArgs e)
        {
            if (lbEventParameters.SelectedItem == null || editedTrigger == null || lbEvents.SelectedItem == null)
            {
                return;
            }

            int paramNumber = (int)lbEventParameters.SelectedItem.Tag;

            var triggerCondition = (TriggerCondition)lbEvents.SelectedItem.Tag;
            if (paramNumber == EVENT_PARAM_FIRST)
                triggerCondition.Parameter1 = tbEventParameterValue.Value;
            else
                triggerCondition.Parameter2 = tbEventParameterValue.Value;
        }

        private void AddEvent(TriggerCondition condition)
        {
            var triggerEventType = map.EditorConfig.TriggerEventTypes.GetValueOrDefault(condition.ConditionIndex);

            if (triggerEventType == null)
            {
                lbEvents.AddItem(new XNAListBoxItem() { Text = condition.ConditionIndex + " Unknown", Tag = condition });
                return;
            }

            lbEvents.AddItem(new XNAListBoxItem() { Text = condition.ConditionIndex + " " + triggerEventType.Name, Tag = condition });
        }

        private TriggerEventType GetTriggerEventType(int index)
        {
            return map.EditorConfig.TriggerEventTypes.GetValueOrDefault(index);
        }

        private TriggerActionType GetTriggerActionType(int index)
        {
            return map.EditorConfig.TriggerActionTypes.GetValueOrDefault(index);
        }

        private Color GetParamValueColor(string paramValue, TriggerParamType paramType)
        {
            bool intParseSuccess = int.TryParse(paramValue, NumberStyles.None, CultureInfo.InvariantCulture, out int intValue);

            switch (paramType)
            {
                case TriggerParamType.House:
                    if (intParseSuccess)
                    {
                        var houses = map.GetHouses();
                        if (intValue >= houses.Count)
                            goto case TriggerParamType.Unused;

                        return houses[intValue].XNAColor;
                    }
                    goto case TriggerParamType.Unused;

                case TriggerParamType.Unused:
                default:
                    return UISettings.ActiveSettings.AltColor;
            }
        }

        private string GetParamValueText(string paramValue, TriggerParamType paramType)
        {
            bool intParseSuccess = int.TryParse(paramValue, NumberStyles.None, CultureInfo.InvariantCulture, out int intValue);
            if (paramValue == null)
                paramValue = string.Empty;

            switch (paramType)
            {
                case TriggerParamType.Animation:
                    if (!intParseSuccess)
                        return paramValue;

                    if (intValue >= map.Rules.AnimTypes.Count)
                        return intValue + " - nonexistent animation";

                    return intValue + " " + map.Rules.AnimTypes[intValue].ININame;
                case TriggerParamType.House:
                    if (intParseSuccess)
                    {
                        var houses = map.GetHouses();
                        if (intValue >= houses.Count)
                            return intValue.ToString() + " - Unknown House";

                        return intValue + " " + houses[intValue].ININame;
                    }

                    return paramValue;
                case TriggerParamType.GlobalVariable:
                    if (!intParseSuccess)
                        return paramValue;

                    if (intValue >= map.Rules.GlobalVariables.Count)
                        return intValue + " - nonexistent variable";

                    return intValue + " " + map.Rules.GlobalVariables[intValue].Name;
                case TriggerParamType.LocalVariable:
                    if (!intParseSuccess)
                        return paramValue;

                    if (intValue >= map.LocalVariables.Count)
                        return intValue + " - nonexistent variable";

                    return intValue + " " + map.LocalVariables[intValue].Name;
                case TriggerParamType.WaypointZZ:
                    if (!intParseSuccess)
                        return Helpers.GetWaypointNumberFromAlphabeticalString(paramValue).ToString();

                    return intValue.ToString();
                case TriggerParamType.TeamType:
                    TeamType teamType = map.TeamTypes.Find(t => t.ININame == paramValue);
                    if (teamType == null)
                        return paramValue;

                    return paramValue + " " + teamType.Name;
                case TriggerParamType.Trigger:
                    Trigger trigger = map.Triggers.Find(t => t.ID == paramValue);
                    if (trigger == null)
                        return paramValue;

                    return paramValue + " " + trigger.Name;
                case TriggerParamType.Building:
                    return GetObjectValueText(RTTIType.Building, map.Rules.BuildingTypes, paramValue);
                case TriggerParamType.Aircraft:
                    return GetObjectValueText(RTTIType.Aircraft, map.Rules.AircraftTypes, paramValue);
                case TriggerParamType.Infantry:
                    return GetObjectValueText(RTTIType.Infantry, map.Rules.InfantryTypes, paramValue);
                case TriggerParamType.Unit:
                    return GetObjectValueText(RTTIType.Unit, map.Rules.UnitTypes, paramValue);
                case TriggerParamType.Text:
                    if (!intParseSuccess)
                        return paramValue + " Unknown text line";

                    return paramValue + " " + map.Rules.TutorialLines.GetStringByIdOrEmptyString(intValue);
                case TriggerParamType.Theme:
                    if (!intParseSuccess)
                        return paramValue;

                    Theme theme = map.Rules.Themes.GetByIndex(intValue);
                    if (theme == null)
                        return paramValue + " - nonexistent theme";

                    return paramValue + " " + theme.Name;
                case TriggerParamType.Boolean:
                default:
                    return paramValue;
            }
        }

        private string GetObjectValueText<T>(RTTIType rtti, List<T> objectTypeList, string paramValue) where T : TechnoType
        {
            bool intParseSuccess = int.TryParse(paramValue, NumberStyles.None, CultureInfo.InvariantCulture, out int intValue);

            if (!intParseSuccess)
                return paramValue;

            if (intValue >= objectTypeList.Count)
            {
                switch (rtti)
                {
                    case RTTIType.Aircraft:
                        return intValue + " - Unknown Aircraft";
                    case RTTIType.Building:
                        return intValue + " - Unknown Building";
                    case RTTIType.Infantry:
                        return intValue + " - Unknown Infantry";
                    case RTTIType.Unit:
                        return intValue + " - Unknown Unit";
                    default:
                        return intValue + " - Unknown Object";
                }
            }

            return intValue + " " + objectTypeList[intValue].GetEditorDisplayName();
        }
    }
}
