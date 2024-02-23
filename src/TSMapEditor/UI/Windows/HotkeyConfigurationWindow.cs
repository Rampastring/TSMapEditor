using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A dialog that allows the user to configure their hotkeys.
    /// </summary>
    public class HotkeyConfigurationWindow : INItializableWindow
    {
        public HotkeyConfigurationWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        private XNAMultiColumnListBox lbKeyboardCommands;
        private XNALabel lblCommandCaption;
        private XNALabel lblDescription;
        private XNALabel lblCurrentHotkeyValue;
        private XNALabel lblNewHotkeyValue;
        private XNALabel lblCurrentlyAssignedTo;
        private XNALabel lblDefaultHotkeyValue;

        private IList<KeyboardCommand> keyboardCommands;
        private KeyboardCommandInput newHotkeyInput = new KeyboardCommandInput(Keys.None, KeyboardModifiers.None);

        public override void Initialize()
        {
            Name = nameof(HotkeyConfigurationWindow);
            base.Initialize();

            FindChild<XNADropDown>("ddCategory").SelectedIndex = 0;
            lbKeyboardCommands = FindChild<XNAMultiColumnListBox>(nameof(lbKeyboardCommands));
            lblCommandCaption = FindChild<XNALabel>(nameof(lblCommandCaption));
            lblDescription = FindChild<XNALabel>(nameof(lblDescription));
            lblCurrentHotkeyValue = FindChild<XNALabel>(nameof(lblCurrentHotkeyValue));
            lblNewHotkeyValue = FindChild<XNALabel>(nameof(lblNewHotkeyValue));
            lblCurrentlyAssignedTo = FindChild<XNALabel>(nameof(lblCurrentlyAssignedTo));
            lblDefaultHotkeyValue = FindChild<XNALabel>(nameof(lblDefaultHotkeyValue));
            keyboardCommands = KeyboardCommands.Instance.Commands;

            FindChild<EditorButton>("btnAssign").LeftClick += (e, s) => AssignHotkey();
            FindChild<EditorButton>("btnResetKey").LeftClick += (e, s) => ResetHotkey();
            FindChild<EditorButton>("btnResetAllKeys").LeftClick += (e, s) => ResetAllKeys();
            FindChild<EditorButton>("btnSave").LeftClick += (e, s) => Save();

            RefreshKeyboardCommands();

            lbKeyboardCommands.SelectedIndexChanged += LbKeyboardCommands_SelectedIndexChanged;
            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;
        }

        public override void Kill()
        {
            lbKeyboardCommands.SelectedIndexChanged -= LbKeyboardCommands_SelectedIndexChanged;
            Keyboard.OnKeyPressed -= Keyboard_OnKeyPressed;

            base.Kill();
        }

        public void Open()
        {
            Show();
        }

        private void AssignHotkey()
        {
            KeyboardCommand command = GetSelectedCommand();
            if (command == null)
                return;

            command.Key = new KeyboardCommandInput(newHotkeyInput.Key, newHotkeyInput.Modifiers);
            // hack, XNAListBoxItem does not update TextLines when Text is changed
            var textLines = lbKeyboardCommands.GetItem(1, lbKeyboardCommands.SelectedIndex).TextLines;
            textLines.Clear();
            textLines.Add(newHotkeyInput.GetKeyDisplayString(command.AllowedWithModifiersOnly));
        }

        private void ResetHotkey()
        {
            KeyboardCommand command = GetSelectedCommand();
            if (command == null)
                return;

            command.Key = command.DefaultKey;
            newHotkeyInput.Key = command.Key.Key;
            newHotkeyInput.Modifiers = command.Key.Modifiers;
            lbKeyboardCommands.GetItem(1, lbKeyboardCommands.SelectedIndex).Text = command.Key.GetKeyDisplayString(command.AllowedWithModifiersOnly);
            UpdateAlsoAssigned();
        }

        private void ResetAllKeys()
        {
            foreach (KeyboardCommand command in keyboardCommands)
            {
                command.Key = command.DefaultKey;
            }

            RefreshKeyboardCommands();
        }

        private void Save()
        {
            KeyboardCommands.Instance.WriteToSettings();
            var _ = UserSettings.Instance.SaveSettingsAsync();
            Hide();
        }

        /// <summary>
        /// Refreshes the list of keyboard commands.
        /// </summary>
        private void RefreshKeyboardCommands()
        {
            lbKeyboardCommands.SelectedIndex = -1;
            lbKeyboardCommands.ClearItems();

            foreach (KeyboardCommand hotkey in keyboardCommands)
            {
                lbKeyboardCommands.AddItem(new string[] { hotkey.UIName, hotkey.Key.GetKeyDisplayString(hotkey.AllowedWithModifiersOnly) }, true);
            }

            ClearInfo();
        }

        private void Keyboard_OnKeyPressed(object sender, Rampastring.XNAUI.Input.KeyPressEventArgs e)
        {
            if (!Enabled || GetSelectedCommand() == null)
            {
                return;
            }

            KeyboardCommand command = GetSelectedCommand();
            if (command == null)
                return;

            // We don't need to react to modifier keys unless this command allows modifier keys only
            if (!command.AllowedWithModifiersOnly &&
                (e.PressedKey == Keys.LeftAlt || e.PressedKey == Keys.RightAlt ||
                e.PressedKey == Keys.LeftControl || e.PressedKey == Keys.RightControl ||
                e.PressedKey == Keys.LeftShift || e.PressedKey == Keys.RightShift))
                return;

            Keys newHotkey = e.PressedKey;

            KeyboardModifiers newHotkeyModifiers = KeyboardModifiers.None;
            if (Keyboard.IsCtrlHeldDown())
                newHotkeyModifiers |= KeyboardModifiers.Ctrl;
            if (Keyboard.IsAltHeldDown())
                newHotkeyModifiers |= KeyboardModifiers.Alt;
            if (Keyboard.IsShiftHeldDown())
                newHotkeyModifiers |= KeyboardModifiers.Shift;

            newHotkeyInput.Key = newHotkey;
            newHotkeyInput.Modifiers = newHotkeyModifiers;

            lblNewHotkeyValue.Text = newHotkeyInput.GetKeyDisplayString(true);
            UpdateAlsoAssigned();
        }

        private void LbKeyboardCommands_SelectedIndexChanged(object sender, EventArgs e)
        {
            KeyboardCommand hotkey = GetSelectedCommand();

            if (hotkey == null)
            {
                ClearInfo();
                return;
            }

            lblCommandCaption.Text = hotkey.UIName;
            lblDescription.Text = "";
            lblCurrentHotkeyValue.Text = hotkey.Key.GetKeyDisplayString(true);
            lblNewHotkeyValue.Text = "Press a key...";
            lblDefaultHotkeyValue.Text = hotkey.DefaultKey.GetKeyDisplayString(true);
            lblCurrentlyAssignedTo.Text = "";
            newHotkeyInput.Key = hotkey.Key.Key;
            newHotkeyInput.Modifiers = hotkey.Key.Modifiers;

            UpdateAlsoAssigned();
        }

        /// <summary>
        /// Updates the text of the "Also assigned to" label if necessary.
        /// </summary>
        private void UpdateAlsoAssigned()
        {
            KeyboardCommand hotkey = keyboardCommands[lbKeyboardCommands.SelectedIndex];

            lblCurrentlyAssignedTo.Text = "";

            if (hotkey.Key.Key == Keys.None)
                return;

            foreach (KeyboardCommand otherHotkey in keyboardCommands)
            {
                if (hotkey == otherHotkey)
                    continue;

                if (newHotkeyInput.Key == otherHotkey.Key.Key && newHotkeyInput.Modifiers == otherHotkey.Key.Modifiers)
                {
                    if (string.IsNullOrEmpty(lblCurrentlyAssignedTo.Text))
                    {
                        lblCurrentlyAssignedTo.Text = "Also assigned to: " + otherHotkey.UIName;
                    }
                    else
                    {
                        lblCurrentlyAssignedTo.Text += " (and more)";
                    }
                }
            }
        }

        private void ClearInfo()
        {
            lblCommandCaption.Text = "Select a command...";
            lblDescription.Text = "";
            lblCurrentHotkeyValue.Text = "";
            lblNewHotkeyValue.Text = "";
            lblCurrentlyAssignedTo.Text = "";
            lblDefaultHotkeyValue.Text = "";
        }

        private KeyboardCommand GetSelectedCommand()
        {
            if (lbKeyboardCommands.SelectedIndex < 0 || lbKeyboardCommands.SelectedIndex >= lbKeyboardCommands.ItemCount)
            {
                return null;
            }

            return keyboardCommands[lbKeyboardCommands.SelectedIndex];
        }
    }
}
