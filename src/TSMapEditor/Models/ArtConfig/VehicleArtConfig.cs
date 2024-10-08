using Rampastring.Tools;

namespace TSMapEditor.Models.ArtConfig
{
    public class VehicleArtConfig : IArtConfig
    {
        public bool Voxel { get; set; }
        public int TurretOffset { get; set; }
        public bool Remapable => true;
        public int StartStandFrame { get; set; } = -1;
        public int StandingFrames { get; set; }
        public int StartWalkFrame { get; set; } = -1;
        public int WalkFrames { get; set; } = 15;
        public int Facings { get; set; } = 8;

        public int FiringFrames { get; set; } = -1;

        /// <summary>
        /// Vinifera addition, override for start turret frame.
        /// If not specified, it's negative.
        /// </summary>
        public int StartTurretFrame { get; set; } = -1;

        /// <summary>
        /// Palette override introduced in Red Alert 2.
        /// Not actually used by game for vehicles without Phobos.
        /// </summary>
        public string Palette { get; set; }

        public void ReadFromIniSection(IniSection iniSection)
        {
            if (iniSection == null)
                return;

            Voxel = iniSection.GetBooleanValue(nameof(Voxel), Voxel);
            TurretOffset = iniSection.GetIntValue(nameof(TurretOffset), TurretOffset);
            StartStandFrame = iniSection.GetIntValue(nameof(StartStandFrame), StartStandFrame);
            StartWalkFrame = iniSection.GetIntValue(nameof(StartWalkFrame), StartWalkFrame);

            // Hackity hackity hack hack
            // In DTA WalkFrames defaults to 1 instead of 15
            if (Constants.ExpectedClientExecutableName == "DTA.exe")
                WalkFrames = 1;

            WalkFrames = iniSection.GetIntValue(nameof(WalkFrames), WalkFrames);
            StandingFrames = iniSection.GetIntValue(nameof(StandingFrames), WalkFrames); // intentionally defaults to walkframes, the game does that too
            Facings = iniSection.GetIntValue(nameof(Facings), Facings);
            StartTurretFrame = iniSection.GetIntValue(nameof(StartTurretFrame), StartTurretFrame);

            Palette = iniSection.GetStringValue(nameof(Palette), Palette);
        }
    }
}
