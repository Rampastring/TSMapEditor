using Rampastring.Tools;
using System;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.CCEngine
{
    public class TriggerActionParam
    {
        public TriggerActionParam(TriggerParamType triggerParamType, string nameOverride)
        {
            TriggerParamType = triggerParamType;
            NameOverride = nameOverride;
        }

        public TriggerParamType TriggerParamType { get; }
        public string NameOverride { get; }
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
            Name = iniSection.GetStringValue(nameof(Name), string.Empty);
            Description = iniSection.GetStringValue(nameof(Description), string.Empty);

            for (int i = 0; i < Parameters.Length; i++)
            {
                string key = $"P{i + 1}Type";
                string nameOverrideKey = $"P{i + 1}Name";

                if (!iniSection.KeyExists(key))
                {
                    Parameters[i] = new TriggerActionParam(TriggerParamType.Unused, null);
                    continue;
                }

                var triggerParamType = (TriggerParamType)Enum.Parse(typeof(TriggerParamType), iniSection.GetStringValue(key, string.Empty));
                string nameOverride = iniSection.GetStringValue(nameOverrideKey, null);
                if (triggerParamType == TriggerParamType.WaypointZZ && string.IsNullOrWhiteSpace(nameOverride))
                    nameOverride = "Waypoint";

                Parameters[i] = new TriggerActionParam(triggerParamType, nameOverride);
            }
        }
    }
}
