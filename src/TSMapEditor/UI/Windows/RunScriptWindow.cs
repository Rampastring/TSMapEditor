using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
using TSMapEditor.Models;
using TSMapEditor.Scripts;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class RunScriptWindow : INItializableWindow
    {
        public RunScriptWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        public event EventHandler ScriptRun;

        private readonly Map map;

        private EditorListBox lbScriptFiles;

        private string scriptPath;

        public override void Initialize()
        {
            Name = nameof(RunScriptWindow);
            base.Initialize();

            lbScriptFiles = FindChild<EditorListBox>(nameof(lbScriptFiles));
            FindChild<EditorButton>("btnRunScript").LeftClick += BtnRunScript_LeftClick;
        }

        private void BtnRunScript_LeftClick(object sender, EventArgs e)
        {
            if (lbScriptFiles.SelectedItem == null)
                return;

            string filePath = (string)lbScriptFiles.SelectedItem.Tag;
            if (!File.Exists(filePath))
            {
                EditorMessageBox.Show(WindowManager, "Can't find file",
                    "The selected file does not exist! Maybe it was deleted?", MessageBoxButtons.OK);

                return;
            }

            scriptPath = filePath;

            (string error, string confirmation) = ScriptRunner.GetDescriptionFromScript(map, filePath);

            if (error != null)
            {
                Logger.Log("Compilation error when attempting to run fetch script description: " + error);
                EditorMessageBox.Show(WindowManager, "Error",
                    "Compiling the script failed! Check its syntax, or contact its author for support." + Environment.NewLine + Environment.NewLine +
                    "Returned error was: " + error, MessageBoxButtons.OK);
                return;
            }

            if (confirmation == null)
            {
                EditorMessageBox.Show(WindowManager, "Error", "The script provides no description!", MessageBoxButtons.OK);
                return;
            }

            confirmation = Renderer.FixText(confirmation, Constants.UIDefaultFont, Width).Text;

            var messageBox = EditorMessageBox.Show(WindowManager, "Are you sure?",
                confirmation, MessageBoxButtons.YesNo);
            messageBox.YesClickedAction = (_) => ApplyCode();
        }

        private void ApplyCode()
        {
            if (scriptPath == null)
                throw new InvalidOperationException("Pending script path is null!");

            string result = ScriptRunner.RunScript(map, scriptPath);
            result = Renderer.FixText(result, Constants.UIDefaultFont, Width).Text;

            EditorMessageBox.Show(WindowManager, "Result", result, MessageBoxButtons.OK);
            ScriptRun?.Invoke(this, EventArgs.Empty);
        }

        public void Open()
        {
            lbScriptFiles.Clear();

            string directoryPath = Path.Combine(Environment.CurrentDirectory, "Config", "Scripts");

            if (!Directory.Exists(directoryPath))
            {
                Logger.Log("WAE scipts directory not found!");
                EditorMessageBox.Show(WindowManager, "Error", "Scripts directory not found!\r\n\r\nExpected path: " + directoryPath, MessageBoxButtons.OK);
                return;
            }

            var iniFiles = Directory.GetFiles(directoryPath, "*.cs");

            foreach (string filePath in iniFiles)
            {
                lbScriptFiles.AddItem(new XNAListBoxItem(Path.GetFileName(filePath)) { Tag = filePath });
            }

            Show();
        }
    }
}
