using Rampastring.Tools;
using System;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.CCEngine
{
    public class TriggerEventParam
    {
        public TriggerEventParam(TriggerParamType triggerParamType, string nameOverride)
        {
            TriggerParamType = triggerParamType;
            NameOverride = nameOverride;
        }

        public TriggerParamType TriggerParamType { get; }
        public string NameOverride { get; }
    }

    public class TriggerEventType
    {
        public const int MAX_PARAM_COUNT = 3;

        public TriggerEventType(int id)
        {
            ID = id;
        }

        public int ID { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public TriggerEventParam[] Parameters { get; } = new TriggerEventParam[MAX_PARAM_COUNT];
        public bool Available { get; set; } = true;

        public bool UsesP3 => Parameters[MAX_PARAM_COUNT - 1].TriggerParamType != TriggerParamType.Unused;

        public void ReadPropertiesFromIniSection(IniSection iniSection)
        {
            ID = iniSection.GetIntValue("IDOverride", ID);
            Name = iniSection.GetStringValue(nameof(Name), string.Empty);
            Description = iniSection.GetStringValue(nameof(Description), string.Empty);
            Available = iniSection.GetBooleanValue(nameof(Available), true);

            for (int i = 0; i < Parameters.Length; i++)
            {
                string key = $"P{i + 1}Type";
                string nameOverrideKey = $"P{i + 1}Name";

                if (!iniSection.KeyExists(key))
                {
                    Parameters[i] = new TriggerEventParam(TriggerParamType.Unused, null);
                    continue;
                }

                var triggerParamType = (TriggerParamType)Enum.Parse(typeof(TriggerParamType), iniSection.GetStringValue(key, string.Empty));
                string nameOverride = iniSection.GetStringValue(nameOverrideKey, null);
                if (triggerParamType == TriggerParamType.WaypointZZ && string.IsNullOrWhiteSpace(nameOverride))
                    nameOverride = "Waypoint";

                Parameters[i] = new TriggerEventParam(triggerParamType, nameOverride);
            }
        }
    }
}
