using Rampastring.Tools;
using System.Collections.Generic;
using static TSMapEditor.Misc.NamedColors;
using TSMapEditor.Misc;
using System;
using Microsoft.Xna.Framework;

namespace TSMapEditor.Models
{
    public class ScriptActionEntry
    {
        public ScriptActionEntry() { }

        public ScriptActionEntry(int action, int argument)
        {
            Action = action;
            Argument = argument;
        }

        public int Action { get; set; }
        public int Argument { get; set; }

        public static ScriptActionEntry ParseScriptActionEntry(string data)
        {
            string[] parts = data.Split(',');
            if (parts.Length != 2)
                return null;

            int action = Conversions.IntFromString(parts[0], -1);
            if (action < 0)
                return null;

            int argument = Conversions.IntFromString(parts[1], -1);

            return new ScriptActionEntry() { Action = action, Argument = argument };
        }

        public ScriptActionEntry Clone()
        {
            return (ScriptActionEntry)MemberwiseClone();
        }
    }

    public class Script : IIDContainer
    {
        public const int MaxActionCount = 50;

        public Script(string iniName)
        {
            ININame = iniName;
        }

        private string _editorColor;
        /// <summary>
        /// Editor-only. The color of the script in the UI.
        /// If null, the script should be displayed with the default UI text color.
        /// </summary>
        public string EditorColor
        {
            get => _editorColor;
            set
            {
                _editorColor = value;

                if (_editorColor != null)
                {
                    int index = Array.FindIndex(SupportedColors, c => c.Name == value);
                    if (index > -1)
                    {
                        XNAColor = SupportedColors[index].Value;
                    }
                    else
                    {
                        // Only allow assigning colors that actually exist in the color table
                        _editorColor = null;
                    }
                }
            }
        }

        public static NamedColor[] SupportedColors = NamedColors.GenericSupportedNamedColors;

        public Color XNAColor;

        public string GetInternalID() => ININame;
        public void SetInternalID(string id) => ININame = id;

        public string ININame { get; private set; }

        public string Name { get; set; }

        public List<ScriptActionEntry> Actions = new List<ScriptActionEntry>();

        /// <summary>
        /// Creates and returns a clone of this script.
        /// </summary>
        /// <param name="iniName">The INI name of the cloned script.</param>
        /// <returns>The created script.</returns>
        public Script Clone(string iniName)
        {
            var script = new Script(iniName);
            script.Name = Name + " (Clone)";

            foreach (var action in Actions)
            {
                script.Actions.Add(new ScriptActionEntry() 
                { 
                    Action = action.Action, 
                    Argument = action.Argument 
                });
            }

            return script;
        }

        public void WriteToIniSection(IniFile iniFile, IniSection scriptSection)
        {
            for (int i = 0; i < Actions.Count; i++)
            {
                scriptSection.SetStringValue(i.ToString(), $"{Actions[i].Action},{Actions[i].Argument}");
            }

            scriptSection.SetStringValue("Name", Name);

            if (EditorColor != null)
                iniFile.SetStringValue("EditorScriptInfo", ININame, EditorColor);
        }

        public static Script ParseScript(string id, IniSection scriptSection)
        {
            if (string.IsNullOrWhiteSpace(id) || scriptSection == null)
                return null;

            var script = new Script(id);
            script.Name = scriptSection.GetStringValue("Name", string.Empty);

            for (int i = 0; i < MaxActionCount; i++)
            {
                if (!scriptSection.KeyExists(i.ToString()))
                    break;

                var scriptActionEntry = ScriptActionEntry.ParseScriptActionEntry(scriptSection.GetStringValue(i.ToString(), "-1,-1"));
                if (scriptActionEntry != null)
                    script.Actions.Add(scriptActionEntry);
            }

            return script;
        }
    }
}
