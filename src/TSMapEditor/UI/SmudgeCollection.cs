using Rampastring.Tools;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;

namespace TSMapEditor.UI
{
    /// <summary>
    /// Combines many smudges into a single entry.
    /// </summary>
    public class SmudgeCollection
    {
        public struct SmudgeCollectionEntry
        {
            public SmudgeType SmudgeType;
            public int Frame;

            public SmudgeCollectionEntry(SmudgeType smudgeType, int frame)
            {
                SmudgeType = smudgeType;
                Frame = frame;
            }
        }

        public string Name { get; set; }
        public SmudgeCollectionEntry[] Entries;

        public static SmudgeCollection InitFromIniSection(IniSection iniSection, List<SmudgeType> smudgeTypes)
        {
            var smudgeCollection = new SmudgeCollection();
            smudgeCollection.Name = iniSection.GetStringValue("Name", "Unnamed Collection");

            var entryList = new List<SmudgeCollectionEntry>();

            int i = 0;
            while (true)
            {
                string value = iniSection.GetStringValue("SmudgeType" + i, null);
                if (string.IsNullOrWhiteSpace(value))
                    break;

                string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                string smudgeTypeName;
                int frame = 0;
                if (parts.Length == 1)
                {
                    smudgeTypeName = value;
                }
                else
                {
                    smudgeTypeName = parts[0];
                    frame = Conversions.IntFromString(parts[1], -1);
                }

                var smudgeType = smudgeTypes.Find(o => o.ININame == smudgeTypeName);
                if (smudgeType == null)
                {
                    throw new INIConfigException($"Smudge type \"{smudgeTypeName}\" not found while initializing smudge collection \"{smudgeCollection.Name}\"!");
                }

                if (frame < 0)
                {
                    throw new INIConfigException($"Frame below zero defined in entry #{i} in smudge collection \"{smudgeCollection.Name}\"!");
                }

                entryList.Add(new SmudgeCollectionEntry(smudgeType, frame));

                i++;
            }

            smudgeCollection.Entries = entryList.ToArray();
            return smudgeCollection;
        }
    }
}
