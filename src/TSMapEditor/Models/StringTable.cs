using System.Collections.Generic;
using TSMapEditor.CCEngine;

namespace TSMapEditor.Models
{
    public class StringTable
    {
        /// <summary>
        /// Map of all CSF label/string pairs that have been parsed.
        /// </summary>
        private Dictionary<string, CsfString> map = new();

        public StringTable(List<CsfFile> csfFiles)
        {
            foreach (var csfFile in csfFiles)
            {
                foreach (var csfString in csfFile.Strings)
                    map[csfString.ID] = csfString;
            }
        }

        public IEnumerable<CsfString> GetStringEnumerator()
        {
            return map.Values;
        }

        public string LookUpValue(string label)
        {
            return map.TryGetValue(label, out var result) ? result.Value : null;
        }

        public CsfString LookUpString(string label)
        {
            return map.TryGetValue(label, out var result) ? result : null;
        }
    }
}
