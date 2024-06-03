using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace TSMapEditor.Models
{
    public class TeamType : AbstractObject, IIDContainer, IHintable
    {
        public TeamType(string iniName)
        {
            ININame = iniName;
        }

        public string GetInternalID() => ININame;
        public void SetInternalID(string id) => ININame = id;

        public string ININame { get; private set; }

        public string Name { get; set; }
        public int Group { get; set; } = -1;
        public HouseType HouseType { get; set; }
        public Script Script { get; set; }
        public TaskForce TaskForce { get; set; }
        public Tag Tag { get; set; }
        public int Max { get; set; }
        public int Priority { get; set; }
        public string Waypoint { get; set; }
        public string TransportWaypoint { get; set; }
        public int? MindControlDecision { get; set; }
        public int TechLevel { get; set; }
        public int VeteranLevel { get; set; } = 1;

        [INI(false)]
        public List<string> EnabledTeamTypeFlags { get; private set; } = new List<string>();

        [INI(false)]
        public bool IsGlobalTeamType { get; set; }

        public bool IsFlagEnabled(string flagName) => EnabledTeamTypeFlags.Contains(flagName);

        public string GetDisplayName() => IsGlobalTeamType ? "(global) " + Name : Name;

        public void EnableFlag(string flagName)
        {
            if (IsFlagEnabled(flagName))
                return;

            EnabledTeamTypeFlags.Add(flagName);
        }

        public void DisableFlag(string flagName)
        {
            EnabledTeamTypeFlags.Remove(flagName);
        }

        public string GetHeaderText() => Name;

        public string GetHintText()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("Owner: " + (HouseType == null ? Constants.NoneValue2 : HouseType.ININame));
            stringBuilder.Append(Environment.NewLine + Environment.NewLine);
            stringBuilder.Append("Script: " + (Script == null ? Constants.NoneValue2 : Script.Name));
            stringBuilder.Append(Environment.NewLine + Environment.NewLine);

            if (Tag != null)
            {
                stringBuilder.Append("Tag: " + Tag.Name);
                stringBuilder.Append(Environment.NewLine + Environment.NewLine);
            }

            stringBuilder.Append("Waypoint: " + (string.IsNullOrWhiteSpace(Waypoint) ? Constants.NoneValue2 : Helpers.GetWaypointNumberFromAlphabeticalString(Waypoint)));

            if (Constants.IsRA2YR)
            {
                stringBuilder.Append("TransportWaypoint: " + (string.IsNullOrWhiteSpace(TransportWaypoint) ? Constants.NoneValue2 : Helpers.GetWaypointNumberFromAlphabeticalString(TransportWaypoint)));
            }

            stringBuilder.Append(Environment.NewLine + Environment.NewLine);

            if (VeteranLevel > 1)
            {
                stringBuilder.Append("Veteran Level: " + (VeteranLevel > 2 ? "Elite" : "Veteran"));
                stringBuilder.Append(Environment.NewLine + Environment.NewLine);
            }

            if (TaskForce == null)
            {
                stringBuilder.Append("No TaskForce set");
            }
            else
            {
                stringBuilder.Append(TaskForce.GetHintText());
            }

            stringBuilder.Append(Environment.NewLine + Environment.NewLine);
            AppendFlag(stringBuilder, nameof(Max), Max);
            EnabledTeamTypeFlags.ForEach(s => AppendFlag(stringBuilder, s, true));

            return stringBuilder.ToString();
        }

        private void AppendFlag<T>(StringBuilder stringBuilder, string name, T flagValue, T defValue = default) where T : struct
        {
            if (!flagValue.Equals(defValue))
            {
                if (flagValue.GetType() == typeof(bool))
                {
                    // Bool fields are only listed if their value is 'true', so just telling their name is enough
                    stringBuilder.Append(name);
                    stringBuilder.Append(Environment.NewLine);
                }
                else
                {
                    stringBuilder.Append(name + ": " + flagValue.ToString());
                    stringBuilder.Append(Environment.NewLine);
                }
            }
        }

        /// <summary>
        /// Creates and returns a clone of this TeamType.
        /// </summary>
        /// <param name="iniName">The INI name of the created TeamType.</param>
        public TeamType Clone(string iniName)
        {
            var clone = MemberwiseClone() as TeamType;
            clone.ININame = iniName;
            clone.Name = Name + " (Clone)";

            clone.EnabledTeamTypeFlags = new List<string>(EnabledTeamTypeFlags);

            return clone;
        }

        public void WriteToIniSection(IniSection iniSection, List<TeamTypeFlag> teamTypeFlags)
        {
            // This cuts it for all properties of standard types
            WritePropertiesToIniSection(iniSection);

            if (HouseType != null)
                iniSection.SetStringValue("House", HouseType.ININame);
            else
                iniSection.SetStringValue("House", Constants.NoneValue1);

            if (Script != null)
                iniSection.SetStringValue("Script", Script.ININame);

            if (TaskForce != null)
                iniSection.SetStringValue("TaskForce", TaskForce.ININame);

            if (Tag != null)
                iniSection.SetStringValue("Tag", Tag.ID);

            teamTypeFlags.ForEach(flag =>
            {
                iniSection.SetBooleanValue(flag.Name, IsFlagEnabled(flag.Name), BooleanStringStyle.YESNO_LOWERCASE);
            });
        }

        public override RTTIType WhatAmI()
        {
            return RTTIType.TeamType;
        }

        public Color GetXNAColor() => Helpers.GetHouseTypeUITextColor(HouseType);
    }
}
