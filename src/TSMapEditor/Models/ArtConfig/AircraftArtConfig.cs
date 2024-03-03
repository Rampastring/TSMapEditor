using Rampastring.Tools;

namespace TSMapEditor.Models.ArtConfig
{
    public class AircraftArtConfig : IArtConfig
    {
        public bool Remapable => true;
        public int Facings { get; set; } = 8;

        /// <summary>
        /// Palette override introduced in Red Alert 2.
        /// </summary>
        public string Palette { get; set; }

        public void ReadFromIniSection(IniSection iniSection)
        {
            if (iniSection == null)
                return;

            Facings = iniSection.GetIntValue(nameof(Facings), Facings);
            Palette = iniSection.GetStringValue(nameof(Palette), Palette);
        }
    }
}