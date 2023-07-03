using Rampastring.Tools;

namespace TSMapEditor.Models.ArtConfig
{
    public class OverlayArtConfig : IArtConfig
    {
        public bool Theater { get; set; }
        public bool NewTheater { get; set; }
        public bool Remapable => false;
        public string Image { get; set; }

        public void ReadFromIniSection(IniSection iniSection)
        {
            Theater = iniSection.GetBooleanValue(nameof(Theater), Theater);
            NewTheater = iniSection.GetBooleanValue(nameof(NewTheater), NewTheater);
            Image = iniSection.GetStringValue(nameof(Image), Image);
        }
    }
}
