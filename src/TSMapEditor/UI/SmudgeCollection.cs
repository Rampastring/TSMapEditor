using Rampastring.Tools;
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

            public SmudgeCollectionEntry(SmudgeType smudgeType)
            {
                SmudgeType = smudgeType;
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
                string smudgeTypeName = iniSection.GetStringValue("SmudgeType" + i, null);
                if (string.IsNullOrWhiteSpace(smudgeTypeName))
                    break;

                var smudgeType = smudgeTypes.Find(o => o.ININame == smudgeTypeName);
                if (smudgeType == null)
                {
                    throw new INIConfigException($"Smudge type \"{smudgeTypeName}\" not found while initializing smudge collection \"{smudgeCollection.Name}\"!");
                }

                entryList.Add(new SmudgeCollectionEntry(smudgeType));

                i++;
            }

            smudgeCollection.Entries = entryList.ToArray();
            return smudgeCollection;
        }
    }
}
