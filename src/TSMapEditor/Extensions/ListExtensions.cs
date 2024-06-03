using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.Initialization;
using TSMapEditor.Models;

namespace TSMapEditor.Extensions
{
    public static class ListExtensions
    {
        public static void ReadTaskForces(this List<TaskForce> taskForceList, IniFile iniFile, Rules rules, Action<string> errorLogger)
        {
            var section = iniFile.GetSection("TaskForces");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                var taskForce = TaskForce.ParseTaskForce(rules, iniFile.GetSection(kvp.Value));
                if (taskForce == null)
                {
                    errorLogger($"Failed to load TaskForce {kvp.Value}. It might be missing a section or be otherwise invalid.");

                    continue;
                }

                int existingIndex = taskForceList.FindIndex(tf => tf.ININame == kvp.Value);
                if (existingIndex > -1)
                {
                    taskForceList[existingIndex] = taskForce;
                }
                else
                {
                    taskForceList.Add(taskForce);
                }
            }
        }

        public static void ReadScripts(this List<Script> scriptList, IniFile iniFile, Action<string> errorLogger)
        {
            var section = iniFile.GetSection("ScriptTypes");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                var script = Script.ParseScript(kvp.Value, iniFile.GetSection(kvp.Value));

                if (script == null)
                {
                    errorLogger($"Failed to load Script {kvp.Value}. It might be missing a section or be otherwise invalid.");

                    continue;
                }

                int existingIndex = scriptList.FindIndex(tf => tf.ININame == kvp.Value);
                if (existingIndex > -1)
                {
                    scriptList[existingIndex] = script;
                }
                else
                {
                    scriptList.Add(script);
                }
            }
        }

        public static void ReadTeamTypes(
            this List<TeamType> teamTypeList,
            IniFile iniFile,
            Func<string, HouseType> houseTypeFinder,
            Func<string, Script> scriptFinder,
            Func<string, TaskForce> taskForceFinder,
            Func<string, Tag> tagFinder,
            List<TeamTypeFlag> teamTypeFlags,
            Action<string> errorLogger,
            bool isGlobal)
        {
            var teamTypeListSection = iniFile.GetSection("TeamTypes");
            if (teamTypeListSection == null)
                return;

            foreach (var kvp in teamTypeListSection.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                var teamTypeSection = iniFile.GetSection(kvp.Value);
                if (teamTypeSection == null)
                    continue;

                var teamType = new TeamType(kvp.Value);
                teamType.IsGlobalTeamType = isGlobal;
                teamType.ReadPropertiesFromIniSection(teamTypeSection);
                string houseTypeIniName = teamTypeSection.GetStringValue("House", string.Empty);
                string scriptId = teamTypeSection.GetStringValue("Script", string.Empty);
                string taskForceId = teamTypeSection.GetStringValue("TaskForce", string.Empty);
                string tagId = teamTypeSection.GetStringValue("Tag", string.Empty);

                if (!Helpers.IsStringNoneValue(houseTypeIniName))
                {
                    teamType.HouseType = houseTypeFinder(houseTypeIniName);

                    if (teamType.HouseType == null)
                    {
                        errorLogger($"TeamType {teamType.ININame} has an invalid owner ({houseTypeIniName}) specified!");
                    }
                }

                teamType.Script = scriptFinder(scriptId);
                teamType.TaskForce = taskForceFinder(taskForceId);

                if (teamType.Script == null)
                {
                    errorLogger($"TeamType {teamType.ININame} has an invalid script ({scriptId}) specified!");
                }

                if (teamType.TaskForce == null)
                {
                    errorLogger($"TeamType {teamType.ININame} has an invalid TaskForce ({taskForceId}) specified!");
                }

                if (tagFinder != null && !string.IsNullOrWhiteSpace(tagId))
                {
                    teamType.Tag = tagFinder(tagId);

                    if (teamType.Tag == null)
                    {
                        errorLogger($"TeamType {teamType.ININame} has an invalid tag ({tagId}) specified!");
                    }
                }

                teamTypeFlags.ForEach(ttflag =>
                {
                    if (teamTypeSection.GetBooleanValue(ttflag.Name, false))
                        teamType.EnableFlag(ttflag.Name);
                });

                teamTypeList.Add(teamType);
            }
        }
    }
}
