using Rampastring.Tools;
using System;
using System.Text;

namespace TSMapEditor.Models
{
    /// <summary>
    /// A single unit entry in a TaskForce. Contains information on the unit and its quantity.
    /// </summary>
    public class TaskForceTechnoEntry
    {
        public TechnoType TechnoType { get; set; }
        public int Count { get; set; }

        public TaskForceTechnoEntry Clone()
        {
            return (TaskForceTechnoEntry)MemberwiseClone();
        }

        public static TaskForceTechnoEntry CreateEntry(Rules rules, string data)
        {
            string[] parts = data.Split(',');
            if (parts.Length != 2)
                return null;

            int count = Conversions.IntFromString(parts[0], -1);
            if (count < 1)
                return null;

            string objectININame = parts[1];
            TechnoType technoType = null;
            technoType = rules.AircraftTypes.Find(a => a.ININame == objectININame);

            if (technoType == null)
                technoType = rules.UnitTypes.Find(u => u.ININame == objectININame);

            if (technoType == null)
                technoType = rules.InfantryTypes.Find(i => i.ININame == objectININame);

            if (technoType == null)
                return null;

            return new TaskForceTechnoEntry()
            {
                Count = count,
                TechnoType = technoType
            };
        }
    }

    /// <summary>
    /// A taskforce. A group of unit types that can be used in team types.
    /// </summary>
    public class TaskForce : IIDContainer, IHintable
    {
        public const int MaxTechnoCount = 6;

        public TaskForce(string iniName)
        {
            ININame = iniName;
        }

        public string GetInternalID() => ININame;
        public void SetInternalID(string id) => ININame = id;

        public string ININame { get; private set; }

        public string Name { get; set; }

        public TaskForceTechnoEntry[] TechnoTypes = new TaskForceTechnoEntry[MaxTechnoCount];

        public int Group { get; set; } = -1;

        public void AddTechnoEntry(TaskForceTechnoEntry taskForceTechnoEntry)
        {
            for (int i = 0; i < MaxTechnoCount; i++)
            {
                if (TechnoTypes[i] == null)
                {
                    TechnoTypes[i] = taskForceTechnoEntry;
                    break;
                }
            }
        }

        public bool HasFreeTechnoSlot()
        {
            for (int i = 0; i < MaxTechnoCount; i++)
            {
                if (TechnoTypes[i] == null)
                    return true;
            }

            return false;
        }

        public void InsertTechnoEntry(int index, TaskForceTechnoEntry taskForceTechnoEntry)
        {
            if (index >= MaxTechnoCount)
                throw new ArgumentException($"{nameof(InsertTechnoEntry)}: {nameof(index)} cannot be higher than {nameof(MaxTechnoCount)}.");

            if (!HasFreeTechnoSlot())
                throw new InvalidOperationException("Cannot insert a Techno entry when the TaskForce has no free techno slots!");

            for (int i = MaxTechnoCount - 2; i >= index; i--)
            {
                TechnoTypes[i + 1] = TechnoTypes[i];
            }

            TechnoTypes[index] = taskForceTechnoEntry;
        }

        public void RemoveTechnoEntry(int technoEntryIndex)
        {
            TechnoTypes[technoEntryIndex] = null;
            for (int i = technoEntryIndex + 1; i < MaxTechnoCount; i++)
            {
                TechnoTypes[i - 1] = TechnoTypes[i];
            }

            // Set the last item to null, necessary so deletion of
            // techno entries works properly for TaskForces with MaxTechnoCount techno entries

            TechnoTypes[MaxTechnoCount - 1] = null;
        }

        public string GetHeaderText() => Name;

        public string GetHintText()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Contains:");

            foreach (var entry in TechnoTypes)
            {
                if (entry == null)
                    break;

                sb.Append(Environment.NewLine);
                sb.Append("    ");
                sb.Append(entry.Count);
                sb.Append("x ");
                sb.Append(entry.TechnoType.GetEditorDisplayName());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Creates and returns a deep clone of this task force.
        /// </summary>
        /// <param name="iniName">The INI name of the new task force.</param>
        public TaskForce Clone(string iniName)
        {
            var newTaskForce = new TaskForce(iniName);
            newTaskForce.Name = Name + " (Clone)";
            newTaskForce.Group = Group;

            for (int i = 0; i < TechnoTypes.Length; i++)
            {
                if (TechnoTypes[i] != null)
                {
                    newTaskForce.TechnoTypes[i] = new TaskForceTechnoEntry()
                    {
                        TechnoType = TechnoTypes[i].TechnoType,
                        Count = TechnoTypes[i].Count
                    };
                }
            }

            return newTaskForce;
        }

        public void Write(IniSection section)
        {
            for (int i = 0; i < TechnoTypes.Length; i++)
            {
                if (TechnoTypes[i] == null)
                    continue;

                section.SetStringValue(i.ToString(), $"{TechnoTypes[i].Count},{TechnoTypes[i].TechnoType.ININame}");
            }

            section.SetStringValue(nameof(Name), Name);
            section.SetIntValue(nameof(Group), Group);
        }

        public static TaskForce ParseTaskForce(Rules rules, IniSection taskforceSection)
        {
            if (taskforceSection == null)
                return null;

            var taskForce = new TaskForce(taskforceSection.SectionName);
            taskForce.Name = taskforceSection.GetStringValue(nameof(Name), string.Empty);
            taskForce.Group = taskforceSection.GetIntValue(nameof(Group), taskForce.Group);

            for (int i = 0; i < MaxTechnoCount; i++)
            {
                if (!taskforceSection.KeyExists(i.ToString()))
                    break;

                string value = taskforceSection.GetStringValue(i.ToString(), null);
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                var entry = TaskForceTechnoEntry.CreateEntry(rules, value);
                if (entry == null)
                    break;

                taskForce.TechnoTypes[i] = entry;
            }

            return taskForce;
        }
    }
}
