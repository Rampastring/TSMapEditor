using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;

namespace TSMapEditor.UI
{
    /// <summary>
    /// Combines many overlays into a single entry.
    /// </summary>
    public class OverlayCollection : ObjectTypeCollection
    {
        public struct OverlayCollectionEntry
        {
            public OverlayType OverlayType;
            public int Frame;

            public OverlayCollectionEntry(OverlayType overlayType, int frame)
            {
                OverlayType = overlayType;
                Frame = frame;
            }
        }

        public OverlayCollectionEntry[] Entries;

        public static OverlayCollection InitFromIniSection(IniSection iniSection, List<OverlayType> overlayTypes)
        {
            var overlayCollection = new OverlayCollection();
            overlayCollection.Name = iniSection.GetStringValue("Name", "Unnamed Collection");
            overlayCollection.AllowedTheaters = iniSection.GetListValue("AllowedTheaters", ',', s => s);

            var entryList = new List<OverlayCollectionEntry>();

            int i = 0;
            while (true)
            {
                string value = iniSection.GetStringValue("OverlayType" + i, null);
                if (string.IsNullOrWhiteSpace(value))
                    break;

                string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                string overlayTypeName;
                int frame = 0;
                if (parts.Length == 1)
                {
                    overlayTypeName = value;
                }
                else
                {
                    overlayTypeName = parts[0];
                    frame = Conversions.IntFromString(parts[1], -1);
                }

                var overlayType = overlayTypes.Find(o => o.ININame == overlayTypeName);
                if (overlayType == null)
                {
                    throw new INIConfigException($"Overlay type \"{overlayTypeName}\" not found while initializing overlay collection \"{overlayCollection.Name}\"!");
                }

                if (frame < 0)
                {
                    throw new INIConfigException($"Frame below zero defined in entry #{i} in overlay collection \"{overlayCollection.Name}\"!");
                }

                entryList.Add(new OverlayCollectionEntry(overlayType, frame));

                i++;
            }

            overlayCollection.Entries = entryList.ToArray();
            return overlayCollection;
        }
    }
}
