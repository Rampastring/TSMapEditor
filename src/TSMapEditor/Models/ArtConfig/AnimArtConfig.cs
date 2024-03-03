using Rampastring.Tools;

namespace TSMapEditor.Models.ArtConfig
{
    public class AnimArtConfig : IArtConfig
    {
        public AnimArtConfig() { }

        public bool Remapable => IsBuildingAnim;
        public string Image { get; set; }
        public int YDrawOffset { get; set; }
        public int XDrawOffset { get; set; } // Phobos
        public bool NewTheater { get; set; }
        public bool Theater { get; set; }
        public bool AltPalette { get; set; }
        public string CustomPalette { get; set; } // Ares
        public bool ShouldUseCellDrawer { get; set; } = true; // Only used by building and tile animations
        public bool Shadow { get; set; }
        public int Start { get; set; }
        public int Translucency { get; set; }

        /// <summary>
        /// Not an INI entry. Temporarily set per-type instead of per instance until
        /// we have indexed color rendering.
        /// </summary>
        public BuildingType ParentBuildingType { get; set; }

        public bool IsBuildingAnim => ParentBuildingType != null;

        public void ReadFromIniSection(IniSection iniSection)
        {
            if (iniSection == null)
                return;

            Image = iniSection.GetStringValue(nameof(Image), Image);
            YDrawOffset = iniSection.GetIntValue(nameof(YDrawOffset), YDrawOffset);
            XDrawOffset = iniSection.GetIntValue(nameof(XDrawOffset), XDrawOffset);
            NewTheater = iniSection.GetBooleanValue(nameof(NewTheater), NewTheater);
            Theater = iniSection.GetBooleanValue(nameof(Theater), Theater);
            AltPalette = iniSection.GetBooleanValue(nameof(AltPalette), AltPalette);
            CustomPalette = iniSection.GetStringValue(nameof(CustomPalette), CustomPalette);
            ShouldUseCellDrawer = iniSection.GetBooleanValue(nameof(ShouldUseCellDrawer), ShouldUseCellDrawer);
            Shadow = iniSection.GetBooleanValue(nameof(Shadow), Shadow);
            Start = iniSection.GetIntValue(nameof(Start), Start);
            Translucency = iniSection.GetIntValue(nameof(Translucency), Translucency);
        }
    }
}
