using Rampastring.Tools;
using System.Collections.Generic;
using System.Collections.Immutable;
using TSMapEditor.Extensions;
using TSMapEditor.Misc;

namespace TSMapEditor.Models
{
    public class Theme : INIDefineable
    {
        public Theme(string iniName, int index)
        {
            ININame = iniName;
            Index = index;
        }

        [INI(false)]
        public string ININame { get; }

        [INI(false)]
        public int Index { get; }

        public string Name { get; set; } = string.Empty;
        public double Length { get; set; }
        public bool Normal { get; set; }
        public int Scenario { get; set; }
        public int Side { get; set; }
        public bool Repeat { get; set; }

        public override string ToString()
        {
            return $"{Index} {Name}";
        }

        public void ReadFromSection(IniSection iniSection)
        {
            ReadPropertiesFromIniSection(iniSection);
        }
    }

    public class Themes
    {
        public Themes(IniFileEx themeIni)
        {
            Initialize(themeIni);
        }

        public ImmutableList<Theme> List { get; private set; }

        public Theme Get(int index)
        {
            return List.GetElementIfInRange(index);
        }

        public Theme Get(string name)
        {
            return List.Find(theme => theme.Name == name);
        }

        private void Initialize(IniFileEx themeIni)
        {
            var themes = new List<Theme>();

            const string definitionsSectionName = "Themes";

            var themeDefinitions = themeIni.GetSectionKeys(definitionsSectionName);
            if (themeDefinitions == null)
            {
                Logger.Log($"Failed to find [{definitionsSectionName}] section from {Constants.ThemeIniPath} - skipping loading themes");
                return;
            }

            foreach (string key in themeDefinitions)
            {
                string themeIniName = themeIni.GetStringValue(definitionsSectionName, key, string.Empty);
                if (!string.IsNullOrWhiteSpace(themeIniName))
                    themes.Add(new Theme(themeIniName, themes.Count));
            }
            
            foreach (var theme in themes)
            {
                var themeSection = themeIni.GetSection(theme.ININame);
                if (themeSection != null)
                    theme.ReadFromSection(themeSection);
            }

            List = ImmutableList.Create(themes.ToArray());
        }
    }
}
