using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.CCEngine
{
    public struct ScriptActionPresetOption
    {
        public int Value;
        public string Text;

        public ScriptActionPresetOption(int value, string text)
        {
            Value = value;
            Text = text;
        }

        public string GetOptionText()
        {
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
        public string OptionsSectionName { get; set; } = string.Empty;
        public TriggerParamType ParamType { get; set; } = TriggerParamType.Unknown;
        public List<ScriptActionPresetOption> PresetOptions { get; } = new List<ScriptActionPresetOption>(0);

        public void ReadIniSection(IniFile iniFile, string sectionName)
        {
            var iniSection = iniFile.GetSection(sectionName);
            ID = iniSection.GetIntValue("IDOverride", ID);
            Name = iniSection.GetStringValue(nameof(Name), Name);
            Description = iniSection.GetStringValue(nameof(Description), Description);
            OptionsSectionName = iniSection.GetStringValue(nameof(OptionsSectionName), OptionsSectionName);
            ParamDescription = iniSection.GetStringValue(nameof(ParamDescription), ParamDescription);
            if (Enum.TryParse(iniSection.GetStringValue(nameof(ParamType), "Unknown"), out TriggerParamType result))
            {
                ParamType = result;
            }

            var optionsSection = iniSection;
            string extraDesc = string.Empty;
            if (!string.IsNullOrEmpty(OptionsSectionName) && iniFile.SectionExists(OptionsSectionName))
            {
                optionsSection = iniFile.GetSection(OptionsSectionName);
                extraDesc = $"(Options section: {OptionsSectionName}) ";
            }

            int i = 0;
            while (true)
            {
                string key = "Option" + i;

                if (!optionsSection.KeyExists(key))
                    break;

                string value = optionsSection.GetStringValue(key, null);
                if (string.IsNullOrWhiteSpace(value))
                {
                    Logger.Log($"Invalid {key}= in ScriptAction {extraDesc}" + iniSection.SectionName);
                    break;
                }

                int commaIndex = value.IndexOf(',');
                if (commaIndex < 0)
                {
                    Logger.Log($"Invalid {key}= in ScriptAction {extraDesc}" + iniSection.SectionName);
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
