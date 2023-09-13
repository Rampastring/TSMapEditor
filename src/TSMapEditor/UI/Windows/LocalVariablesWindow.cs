using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class LocalVariablesWindow : INItializableWindow
    {
        public LocalVariablesWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorListBox lbLocalVariables;
        private EditorTextBox tbName;
        private XNACheckBox chkInitialState;
        private XNALabel lblInitialState;
        private EditorNumberTextBox tbInitialState;

        private LocalVariable editedLocalVariable;

        public override void Initialize()
        {
            Name = nameof(LocalVariablesWindow);
            base.Initialize();

            lbLocalVariables = FindChild<EditorListBox>(nameof(lbLocalVariables));
            tbName = FindChild<EditorTextBox>(nameof(tbName));
            chkInitialState = FindChild<XNACheckBox>(nameof(chkInitialState));
            lblInitialState = FindChild<XNALabel>(nameof(lblInitialState));
            tbInitialState = FindChild<EditorNumberTextBox>(nameof(tbInitialState));

            if (Constants.IntegerVariables)
            {
                chkInitialState.Disable();
            }
            else
            {
                tbInitialState.Disable();
                lblInitialState.Disable();
            }

            FindChild<EditorButton>("btnNewLocalVariable").LeftClick += BtnNewLocalVariable_LeftClick;
            FindChild<EditorButton>("btnViewVariableUsages").LeftClick += BtnViewVariableUsages_LeftClick;


            lbLocalVariables.SelectedIndexChanged += LbLocalVariables_SelectedIndexChanged;
        }

        private void BtnNewLocalVariable_LeftClick(object sender, EventArgs e)
        {
            map.LocalVariables.Add(new LocalVariable(map.LocalVariables.Count) { Name = "New Local Variable" });
            ListLocalVariables();
            lbLocalVariables.SelectedIndex = map.LocalVariables.Count - 1;
            lbLocalVariables.ScrollToBottom();
        }

        private void BtnViewVariableUsages_LeftClick(object sender, EventArgs e)
        {
            if (editedLocalVariable == null)
            {
                EditorMessageBox.Show(WindowManager, "Select a variable", "Please select a variable first.", MessageBoxButtons.OK);
                return;
            }

            var list = new List<string>();

            map.Triggers.ForEach(trigger =>
            {
                foreach (var action in trigger.Actions)
                {
                    var actionType = map.EditorConfig.TriggerActionTypes.GetValueOrDefault(action.ActionIndex);
                    if (actionType == null)
                        continue;

                    for (int i = 0; i < actionType.Parameters.Length; i++)
                    {
                        var parameter = actionType.Parameters[i];
                        if (parameter.TriggerParamType == TriggerParamType.LocalVariable)
                        {
                            if (Conversions.IntFromString(action.Parameters[i], -1) == editedLocalVariable.Index)
                            {
                                list.Add($"Trigger action of '{trigger.Name}' ({trigger.ID})");
                                break;
                            }
                        }
                    }
                }

                foreach (var triggerEvent in trigger.Conditions)
                {
                    var eventType = map.EditorConfig.TriggerEventTypes.GetValueOrDefault(triggerEvent.ConditionIndex);
                    if (eventType == null)
                        continue;

                    if (eventType.P1Type == TriggerParamType.LocalVariable)
                    {
                        if (triggerEvent.Parameter1 == editedLocalVariable.Index)
                        {
                            list.Add($"Trigger event of '{trigger.Name}' ({trigger.ID})");
                        }
                    }

                    if (eventType.P2Type == TriggerParamType.LocalVariable)
                    {
                        if (triggerEvent.Parameter2 == editedLocalVariable.Index)
                        {
                            list.Add($"Trigger event of '{trigger.Name}' ({trigger.ID})");
                        }
                    }
                }
            });

            map.Scripts.ForEach(script =>
            {
                foreach (var scriptAction in script.Actions)
                {
                    var scriptActionType = map.EditorConfig.ScriptActions.GetValueOrDefault(scriptAction.Action);

                    if (scriptActionType == null)
                        continue;

                    if (scriptActionType.ParamType == TriggerParamType.LocalVariable &&
                        scriptAction.Argument == editedLocalVariable.Index)
                    {
                        list.Add($"Script action of '{script.Name}' ({script.ININame})");
                    }
                }
            });


            if (list.Count == 0)
            {
                EditorMessageBox.Show(WindowManager, "No usages found",
                    $"No triggers or scripts make use of the selected local variable '{editedLocalVariable.Name}'", MessageBoxButtons.OK);
            }
            else
            {
                EditorMessageBox.Show(WindowManager,
                    "Local Variable Usages",
                    $"The following usages were found for the selected local variable '{editedLocalVariable.Name}':" + Environment.NewLine + Environment.NewLine +
                    string.Join(Environment.NewLine, list.Select(e => "- " + e)),
                    MessageBoxButtons.OK);
            }
        }

        private void LbLocalVariables_SelectedIndexChanged(object sender, EventArgs e)
        {
            tbName.TextChanged -= TbName_TextChanged;
            chkInitialState.CheckedChanged -= ChkInitialState_CheckedChanged;
            tbInitialState.TextChanged -= TbInitialState_TextChanged;

            if (lbLocalVariables.SelectedItem == null)
            {
                editedLocalVariable = null;
                tbName.Text = string.Empty;
                return;
            }

            editedLocalVariable = (LocalVariable)lbLocalVariables.SelectedItem.Tag;
            tbName.Text = editedLocalVariable.Name;
            chkInitialState.Checked = editedLocalVariable.InitialState > 0;
            tbInitialState.Value = editedLocalVariable.InitialState;

            tbName.TextChanged += TbName_TextChanged;
            chkInitialState.CheckedChanged += ChkInitialState_CheckedChanged;
            tbInitialState.TextChanged += TbInitialState_TextChanged;
        }

        private void ChkInitialState_CheckedChanged(object sender, EventArgs e)
        {
            editedLocalVariable.InitialState = chkInitialState.Checked ? 1 : 0;
        }

        private void TbInitialState_TextChanged(object sender, EventArgs e)
        {
            editedLocalVariable.InitialState = tbInitialState.Value;
        }

        private void TbName_TextChanged(object sender, EventArgs e)
        {
            editedLocalVariable.Name = tbName.Text;
            ListLocalVariables();
        }

        public void Open()
        {
            Show();
            ListLocalVariables();
        }

        private void ListLocalVariables()
        {
            lbLocalVariables.Clear();

            foreach (var localVariable in map.LocalVariables)
            {
                lbLocalVariables.AddItem(new XNAListBoxItem() { Text = localVariable.Index + " " + localVariable.Name, Tag = localVariable });
            }
        }
    }
}
