using System;
using System.Collections.Generic;

namespace TSMapEditor.Models
{
    public abstract class GameObjectType : AbstractObject, INIDefined
    {
        public GameObjectType(string iniName)
        {
            ININame = iniName;
        }

        [INI(false)]
        public string ININame { get; }

        [INI(false)]
        public int Index { get; set; }

        public string Name { get; set; }
        public string FSName { get; set; }
        public string EditorCategory { get; set; }
        public bool EditorVisible { get; set; } = true;

        public bool InvisibleInGame { get; set; }

        /// <summary>
        /// Specifies which theaters this terrain object type can be placed down in.
        /// If empty, it is considered valid for all theaters.
        /// </summary>
        public List<string> AllowedTheaters { get; set; }

        public bool IsValidForTheater(string theaterName)
        {
            if (AllowedTheaters == null || AllowedTheaters.Count == 0)
                return true;

            return AllowedTheaters.Exists(t => t.Equals(theaterName, StringComparison.OrdinalIgnoreCase));
        }

        public string GetEditorDisplayName()
        {
            string name;

            if (!string.IsNullOrWhiteSpace(FSName))
            {
                name = FSName;
            }
            else if (!string.IsNullOrWhiteSpace(Name))
            {
                name = Name;
            }
            else
            {
                return ININame;
            }

            if (ININame.StartsWith("AI") && !name.StartsWith("AI "))
                return "AI " + name;

            return name;
        }
    }
}
