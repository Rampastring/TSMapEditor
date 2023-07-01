using Rampastring.Tools;
using System;
using System.Collections.Generic;
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
        public List<ScriptAction> ScriptActions { get; } = new List<ScriptAction>();
        public List<TriggerEventType> TriggerEventTypes { get; } = new List<TriggerEventType>();
        public List<TriggerActionType> TriggerActionTypes { get; } = new List<TriggerActionType>();
        public List<Theater> Theaters { get; } = new List<Theater>();

        public void Init(Rules rules)
        {
            ReadTheaters();
            ReadOverlayCollections(rules);
            ReadTerrainObjectCollections(rules);
            ReadSmudgeCollections(rules);
            ReadBrushSizes();
            ReadScriptActions();
            ReadTriggerEventTypes();
            ReadTriggerActionTypes();
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

                ScriptActions.Add(scriptAction);
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

                TriggerEventTypes.Add(triggerEventType);
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

                TriggerActionTypes.Add(triggerActionType);
            }
        }
    }
}
