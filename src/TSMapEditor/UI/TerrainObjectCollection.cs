using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;

namespace TSMapEditor.UI
{
    /// <summary>
    /// Combines many terrain objects into a single entry.
    /// </summary>
    public class TerrainObjectCollection
    {
        public struct TerrainObjectCollectionEntry
        {
            public TerrainType TerrainType;
            public int Frame;

            public TerrainObjectCollectionEntry(TerrainType terrainType, int frame)
            {
                TerrainType = terrainType;
                Frame = frame;
            }
        }

        public string Name { get; set; }
        public TerrainObjectCollectionEntry[] Entries;

        public static TerrainObjectCollection InitFromIniSection(IniSection iniSection, List<TerrainType> terrainTypes)
        {
            var terrainObjectCollection = new TerrainObjectCollection();
            terrainObjectCollection.Name = iniSection.GetStringValue("Name", "Unnamed Collection");

            var entryList = new List<TerrainObjectCollectionEntry>();

            int i = 0;
            while (true)
            {
                string value = iniSection.GetStringValue("TerrainObjectType" + i, null);
                if (string.IsNullOrWhiteSpace(value))
                    break;

                string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                string terrainTypeName;
                int frame = 0;
                if (parts.Length == 1)
                {
                    terrainTypeName = value;
                }
                else
                {
                    terrainTypeName = parts[0];
                    frame = Conversions.IntFromString(parts[1], -1);
                }

                var terrainType = terrainTypes.Find(o => o.ININame == terrainTypeName);
                if (terrainType == null)
                {
                    throw new INIConfigException($"Terrain object type \"{terrainTypeName}\" not found while initializing terrain object collection \"{terrainObjectCollection.Name}\"!");
                }

                if (frame < 0)
                {
                    throw new INIConfigException($"Frame below zero defined in entry #{i} in terrain object collection \"{terrainObjectCollection.Name}\"!");
                }

                entryList.Add(new TerrainObjectCollectionEntry(terrainType, frame));

                i++;
            }

            terrainObjectCollection.Entries = entryList.ToArray();
            return terrainObjectCollection;
        }
    }
}
