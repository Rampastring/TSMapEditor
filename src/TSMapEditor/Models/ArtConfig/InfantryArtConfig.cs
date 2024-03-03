using Rampastring.Tools;

namespace TSMapEditor.Models.ArtConfig
{
    public class InfantryArtConfig : IArtConfig
    {
        public string SequenceName { get; private set; }
        public InfantrySequence Sequence { get; set; }
        public string Image { get; set; }
        public bool Remapable => true;

        /// <summary>
        /// Palette override introduced in Red Alert 2.
        /// </summary>
        public string Palette { get; set; }

        public void ReadFromIniSection(IniSection iniSection)
        {
            SequenceName = iniSection.GetStringValue("Sequence", SequenceName);
            Image = iniSection.GetStringValue(nameof(Image), Image);
            Palette = iniSection.GetStringValue(nameof(Palette), Palette);
        }
    }
}
