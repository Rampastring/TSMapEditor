using Rampastring.Tools;
using System.Collections.Generic;
using TSMapEditor.Extensions;

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

        private List<Theme> themes;

        public List<Theme> GetThemes() => new List<Theme>(themes);

        public Theme GetByIndex(int index) => (index < 0 || index >= themes.Count) ? null : themes[index];

        private void Initialize(IniFileEx themeIni)
        {
            themes = new List<Theme>();

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
        }
    }
}
