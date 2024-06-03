using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.UI;

namespace TSMapEditor.Models
{
    /// <summary>
    /// Contains and handles editor-related configuration options.
    /// </summary>
    public class EditorConfig
    {
        public EditorConfig() 
        {
            EditorRulesIni = new IniFile(Environment.CurrentDirectory + "/Config/EditorRules.ini");
        }
        
        public IniFile EditorRulesIni { get; }
        public List<OverlayCollection> OverlayCollections { get; } = new List<OverlayCollection>();
        public List<TerrainObjectCollection> TerrainObjectCollections { get; } = new List<TerrainObjectCollection>();
        public List<SmudgeCollection> SmudgeCollections { get; } = new List<SmudgeCollection>();
        public List<BrushSize> BrushSizes { get; } = new List<BrushSize>() { new BrushSize(1, 1) };
        public Dictionary<int, ScriptAction> ScriptActions { get; } = new Dictionary<int, ScriptAction>();
        public Dictionary<int, TriggerEventType> TriggerEventTypes { get; } = new Dictionary<int, TriggerEventType>();
        public Dictionary<int, TriggerActionType> TriggerActionTypes { get; } = new Dictionary<int, TriggerActionType>();
        public List<Theater> Theaters { get; } = new List<Theater>();
        public List<BridgeType> Bridges { get; } = new List<BridgeType>();
        public List<ConnectedOverlayType> ConnectedOverlays { get; } = new List<ConnectedOverlayType>();
        public List<CliffType> Cliffs { get; } = new List<CliffType>();
        public List<TeamTypeFlag> TeamTypeFlags { get; } = new List<TeamTypeFlag>();
        public EvaSpeeches Speeches { get; private set; }

        private static readonly Dictionary<string, (int StartIndex, int Count)> TiberiumDefaults = new()
        {
            {
                "Riparius",
                (102, 20)
            },
            {
                "Cruentus",
                (27, 12)
            },
            {
                "Vinifera",
                (127, 20)
            },
            {
                "Aboreus",
                (147, 20)
            }
        };

        public void EarlyInit()
        {
            ReadBrushSizes();
            ReadScriptActions();
            ReadTriggerEventTypes();
            ReadTriggerActionTypes();
            ReadTheaters();
            ReadTeamTypeFlags();
            ReadSpeeches();
            ReadCliffs();
        }

        public void RulesDependentInit(Rules rules)
        {
            ReadOverlayCollections(rules);
            ReadTerrainObjectCollections(rules);
            ReadSmudgeCollections(rules);
            ReadBridges(rules);
            ReadConnectedOverlays(rules);
            ReadTiberiumOverlays(rules);
        }

        private void ReadTheaters()
        {
            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/Theaters.ini");
            var section = iniFile.GetSection("Theaters");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                string theaterName = kvp.Value;
                IniSection theaterSection = iniFile.GetSection(theaterName);
                if (theaterSection == null)
                    continue;

                Theater theater = new Theater(theaterName);
                theater.ReadPropertiesFromIniSection(theaterSection);
                Theaters.Add(theater);
            }
        }

        private void ReadOverlayCollections(Rules rules)
        {
            OverlayCollections.Clear();

            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/OverlayCollections.ini");

            var keys = iniFile.GetSectionKeys("OverlayCollections");
            if (keys == null)
                return;

            foreach (var key in keys)
            {
                var value = iniFile.GetStringValue("OverlayCollections", key, string.Empty);
                var collectionSection = iniFile.GetSection(value);
                if (collectionSection == null)
                    continue;

                var overlayCollection = OverlayCollection.InitFromIniSection(collectionSection, rules.OverlayTypes);
                OverlayCollections.Add(overlayCollection);
            }
        }

        private void ReadTerrainObjectCollections(Rules rules)
        {
            TerrainObjectCollections.Clear();

            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/TerrainObjectCollections.ini");

            var keys = iniFile.GetSectionKeys("TerrainObjectCollections");
            if (keys == null)
                return;

            foreach (var key in keys)
            {
                var value = iniFile.GetStringValue("TerrainObjectCollections", key, string.Empty);
                var collectionSection = iniFile.GetSection(value);
                if (collectionSection == null)
                    continue;

                var terrainObjectCollection = TerrainObjectCollection.InitFromIniSection(collectionSection, rules.TerrainTypes);
                TerrainObjectCollections.Add(terrainObjectCollection);
            }
        }

        private void ReadSmudgeCollections(Rules rules)
        {
            SmudgeCollections.Clear();

            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/SmudgeCollections.ini");

            var keys = iniFile.GetSectionKeys("SmudgeCollections");
            if (keys == null)
                return;

            foreach (var key in keys)
            {
                var value = iniFile.GetStringValue("SmudgeCollections", key, string.Empty);
                var collectionSection = iniFile.GetSection(value);
                if (collectionSection == null)
                    continue;

                var smudgeCollection = SmudgeCollection.InitFromIniSection(collectionSection, rules.SmudgeTypes);
                SmudgeCollections.Add(smudgeCollection);
            }
        }

        private void ReadBrushSizes()
        {
            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/BrushSizes.ini");
            var section = iniFile.GetSection("BrushSizes");
            if (section == null)
                return;

            BrushSizes.Clear();

            foreach (var kvp in section.Keys)
            {
                string[] parts = kvp.Value.Split('x');
                if (parts.Length != 2)
                    continue;

                int[] sizes = Array.ConvertAll(parts, s => Conversions.IntFromString(s, 0));
                if (sizes[0] < 1 || sizes[1] < 1)
                    continue;

                BrushSizes.Add(new BrushSize(sizes[0], sizes[1]));
            }
        }

        private void ReadScriptActions()
        {
            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/ScriptActions.ini");
            List<string> sections = iniFile.GetSections();

            for (int i = 0; i < sections.Count; i++)
            {
                var scriptAction = new ScriptAction(i);
                var scriptSection = iniFile.GetSection(sections[i]);
                scriptAction.ReadIniSection(scriptSection);

                if (ScriptActions.ContainsKey(scriptAction.ID))
                {
                    throw new INIConfigException($"Error while adding Script Action {scriptAction.Name}: " + 
                                                 $"a Script Action with ID {scriptAction.ID} already exists!");
                }

                ScriptActions.Add(scriptAction.ID, scriptAction);
            }
        }

        private void ReadTriggerEventTypes()
        {
            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/Events.ini");
            List<string> sections = iniFile.GetSections();

            for (int i = 0; i < sections.Count; i++)
            {
                var triggerEventType = new TriggerEventType(i);
                var section = iniFile.GetSection(sections[i]);
                triggerEventType.ReadPropertiesFromIniSection(section);

                if (TriggerEventTypes.ContainsKey(triggerEventType.ID))
                {
                    throw new INIConfigException($"Error while adding Trigger Event {triggerEventType.Name}: " + 
                                                 $"a Trigger Event with ID {triggerEventType.ID} already exists!");
                }

                TriggerEventTypes.Add(triggerEventType.ID, triggerEventType);
            }
        }

        private void ReadTriggerActionTypes()
        {
            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/Actions.ini");
            List<string> sections = iniFile.GetSections();

            for (int i = 0; i < sections.Count; i++)
            {
                var triggerActionType = new TriggerActionType(i);
                var section = iniFile.GetSection(sections[i]);
                triggerActionType.ReadPropertiesFromIniSection(section);

                if (TriggerActionTypes.ContainsKey(triggerActionType.ID))
                {
                    throw new INIConfigException($"Error while adding Trigger Action {triggerActionType.Name}: " +
                                                 $"a Trigger Action with ID {triggerActionType.ID} already exists!");
                }

                TriggerActionTypes.Add(triggerActionType.ID, triggerActionType);
            }
        }

        private void ReadBridges(Rules rules)
        {
            Bridges.Clear();

            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/Bridges.ini");
            var section = iniFile.GetSection("Bridges");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                string bridgeName = kvp.Value;
                IniSection bridgeSection = iniFile.GetSection(bridgeName);
                if (bridgeSection == null)
                    continue;

                BridgeType bridgeType = new BridgeType(bridgeSection, rules);
                Bridges.Add(bridgeType);
            }
        }

        private void ReadConnectedOverlays(Rules rules)
        {
            ConnectedOverlays.Clear();

            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/ConnectedOverlays.ini");
            var section = iniFile.GetSection("ConnectedOverlays");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                string overlayName = kvp.Value;
                IniSection overlaySection = iniFile.GetSection(overlayName);
                if (overlaySection == null)
                    continue;

                ConnectedOverlayType overlayType = new ConnectedOverlayType(overlaySection, rules);
                ConnectedOverlays.Add(overlayType);
            }

            foreach (var connectedOverlay in ConnectedOverlays)
            {
                IniSection overlaySection = iniFile.GetSection(connectedOverlay.Name);
                connectedOverlay.InitializeRelatedOverlays(overlaySection, ConnectedOverlays);
            }
        }

        private void ReadTiberiumOverlays(Rules rules)
        {
            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/Tiberiums.ini");
            const string sectionName = "Tiberiums";

            foreach (var tiberiumType in rules.TiberiumTypes)
            {
                tiberiumType.Overlays = new List<OverlayType>();
                string tibName = tiberiumType.ININame;

                string overlaysString = iniFile.GetStringValue(sectionName, tibName, null);

                if (overlaysString == null)
                {
                    if (!TiberiumDefaults.ContainsKey(tibName))
                        continue;

                    var defaultOverlays = rules.OverlayTypes.Slice(TiberiumDefaults[tibName].StartIndex, TiberiumDefaults[tibName].Count);
                    tiberiumType.Overlays.AddRange(defaultOverlays);
                    defaultOverlays.ForEach(ot =>
                    {
                        if (ot.TiberiumType == null)
                        {
                            ot.TiberiumType = tiberiumType;
                        }
                        else
                        {
                            throw new INIConfigException(
                                $"OverlayType {ot.ININame} is already associated with Tiberium {ot.TiberiumType.Index} ({ot.TiberiumType.ININame}), " +
                                $"but it is also set to be associated with Tiberium {tiberiumType.Index} ({tiberiumType.ININame})!");
                        }
                    });

                    continue;
                }

                var overlayNames = overlaysString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var overlays = overlayNames.Select(name => (name, rules.FindOverlayType(name))).ToList();

                if (overlays.Any(ot => ot.Item2 == null))
                {
                    throw new INIConfigException($"Tiberium {tibName} has invalid overlay type(s) specified: " +
                                                 $"{string.Join(", ", overlays.Where(ot => ot.Item2 == null).Select(ot => ot.name))}!");
                }

                tiberiumType.Overlays.AddRange(overlays.Select(ot => ot.Item2));
                overlays.ForEach(ot =>
                {
                    if (ot.Item2.TiberiumType == null)
                    {
                        ot.Item2.TiberiumType = tiberiumType;
                    }
                    else
                    {
                        throw new INIConfigException(
                            $"OverlayType {ot.Item2.ININame} is already associated with Tiberium {ot.Item2.TiberiumType.Index} ({ot.Item2.TiberiumType.ININame}), " +
                            $"but it is also set to be associated with Tiberium {tiberiumType.Index} ({tiberiumType.ININame})!");
                    }
                });
            }
        }

        private void ReadTeamTypeFlags()
        {
            TeamTypeFlags.Clear();

            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/TeamTypeFlags.ini");
            const string sectionName = "TeamTypeFlags";

            var keys = iniFile.GetSectionKeys(sectionName);
            if (keys == null)
                return;

            foreach (var key in keys)
            {
                string value = iniFile.GetStringValue(sectionName, key, string.Empty);
                var teamTypeFlag = new TeamTypeFlag(key, Conversions.BooleanFromString(value, false));
                TeamTypeFlags.Add(teamTypeFlag);
            }
        }

        private void ReadSpeeches()
        {
            // Don't load speeches from the config if we're in YR mode
            if (Constants.IsRA2YR)
                return;

            var speeches = new List<EvaSpeech>();

            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/Speeches.ini");
            const string sectionName = "Speeches";

            foreach (var kvp in iniFile.GetSection(sectionName).Keys)
            {
                if (string.IsNullOrEmpty(kvp.Key))
                    continue;

                speeches.Add(new EvaSpeech(speeches.Count, kvp.Value, string.Empty));
            }

            Speeches = new EvaSpeeches(speeches.ToArray());
        }

        private void ReadCliffs()
        {
            Cliffs.Clear();

            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/ConnectedTileDrawer.ini");
            var section = iniFile.GetSection("ConnectedTiles");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                string cliffIniName = kvp.Value;

                CliffType cliffType = CliffType.FromIniSection(iniFile, cliffIniName);
                if (cliffType != null)
                    Cliffs.Add(cliffType);
            }
        }
    }
}
