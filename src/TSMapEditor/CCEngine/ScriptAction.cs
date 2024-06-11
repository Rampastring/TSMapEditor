using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.CCEngine
{
    public class ScriptActionPresetOption
    {
        public int Value;
        public string Text;
        public Color? Color;

        public ScriptActionPresetOption()
        {
        }

        public ScriptActionPresetOption(int value, string text, Color? color = null)
        {
            Value = value;
            Text = text;
            Color = color;
        }

        public string GetOptionText()
        {
            if (string.IsNullOrEmpty(Text))
                return Value.ToString();
            else
                return Value + " - " + Text;
        }
    }

    public class ScriptAction
    {
        public ScriptAction(int id)
        {
            ID = id;
        }

        public int ID { get; set; }
        public string Name { get; set; } = "Unknown action";
        public string Description { get; set; } = "No description";
        public string ParamDescription { get; set; } = "Use 0";
        public TriggerParamType ParamType { get; set; } = TriggerParamType.Unknown;
        public List<ScriptActionPresetOption> PresetOptions { get; } = new List<ScriptActionPresetOption>(0);
        public bool UseWindowSelection { get; set; } = false;

        public void ReadIniSection(IniSection iniSection)
        {
            ID = iniSection.GetIntValue("IDOverride", ID);
            Name = iniSection.GetStringValue(nameof(Name), Name);
            Description = iniSection.GetStringValue(nameof(Description), Description);
            ParamDescription = iniSection.GetStringValue(nameof(ParamDescription), ParamDescription);
            UseWindowSelection = iniSection.GetBooleanValue(nameof(UseWindowSelection), UseWindowSelection);
            if (Enum.TryParse(iniSection.GetStringValue(nameof(ParamType), "Unknown"), out TriggerParamType result))
            {
                ParamType = result;
            }

            int i = 0;
            while (true)
            {
                string key = "Option" + i;

                if (!iniSection.KeyExists(key))
                    break;

                string value = iniSection.GetStringValue(key, null);
                if (string.IsNullOrWhiteSpace(value))
                {
                    Logger.Log($"Invalid {key}= in ScriptAction " + iniSection.SectionName);
                    break;
                }

                int commaIndex = value.IndexOf(',');
                if (commaIndex < 0)
                {
                    Logger.Log($"Invalid {key}= in ScriptAction " + iniSection.SectionName);
                    break;
                }

                int presetValue = Conversions.IntFromString(value.Substring(0, commaIndex), 0);
                string presetText = value.Substring(commaIndex + 1);

                PresetOptions.Add(new ScriptActionPresetOption(presetValue, presetText));

                i++;
            }
        }
    }
}
