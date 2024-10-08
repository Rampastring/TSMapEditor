using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.CCEngine
{
    public class TriggerActionParam
    {
        public TriggerActionParam(TriggerParamType triggerParamType, string nameOverride, List<string> presetOptions = null)
        {
            TriggerParamType = triggerParamType;
            NameOverride = nameOverride;
            PresetOptions = presetOptions;
        }

        public TriggerParamType TriggerParamType { get; }
        public string NameOverride { get; }
        public List<string> PresetOptions { get; }

        public bool HasPresetOptions() => PresetOptions != null && PresetOptions.Count > 0;
    }

    public class TriggerActionType
    {
        public const int MAX_PARAM_COUNT = 7;

        public TriggerActionType(int id)
        {
            ID = id;
        }


        public int ID { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public TriggerActionParam[] Parameters { get; } = new TriggerActionParam[MAX_PARAM_COUNT];

        public void ReadPropertiesFromIniSection(IniSection iniSection)
        {
            ID = iniSection.GetIntValue("IDOverride", ID);
            Name = iniSection.GetStringValue(nameof(Name), string.Empty);
            Description = iniSection.GetStringValue(nameof(Description), string.Empty);

            for (int i = 0; i < Parameters.Length; i++)
            {
                string key = $"P{i + 1}Type";
                string nameOverrideKey = $"P{i + 1}Name";
                string presetOptionsKey = $"P{i + 1}PresetOptions";

                if (!iniSection.KeyExists(key))
                {
                    Parameters[i] = new TriggerActionParam(TriggerParamType.Unused, null);
                    continue;
                }

                var triggerParamType = (TriggerParamType)Enum.Parse(typeof(TriggerParamType), iniSection.GetStringValue(key, string.Empty));
                string nameOverride = iniSection.GetStringValue(nameOverrideKey, null);
                if (triggerParamType == TriggerParamType.WaypointZZ && string.IsNullOrWhiteSpace(nameOverride))
                    nameOverride = "Waypoint";

                List<string> presetOptions = null;
                string presetOptionsString = iniSection.GetStringValue(presetOptionsKey, null);
                if (!string.IsNullOrWhiteSpace(presetOptionsString))
                {
                    presetOptions = new List<string>(presetOptionsString.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries));
                }

                Parameters[i] = new TriggerActionParam(triggerParamType, nameOverride, presetOptions);
            }
        }
    }
}
