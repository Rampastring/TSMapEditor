using TSMapEditor.Models.ArtConfig;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.Models
{
    public class OverlayType : GameObjectType, IArtConfigContainer
    {
        public OverlayType(string iniName) : base(iniName)
        {
        }

        public override RTTIType WhatAmI() => RTTIType.OverlayType;

        // We might not need all of these properties at least immediately,
        // but I listed them all for possible future convenience

        public LandType Land { get; set; }
        public string Image { get; set; }
        public OverlayArtConfig ArtConfig { get; } = new OverlayArtConfig();
        public IArtConfig GetArtConfig() => ArtConfig;
        public bool WaterBound { get; set; }
        public bool Wall { get; set; }
        public bool RadarInvisible { get; set; }
        public bool Crushable { get; set; }
        public bool DrawFlat { get; set; } = true;
        public bool NoUseTileLandType { get; set; }
        public bool IsARock { get; set; }
        public bool Tiberium { get; set; }
        public bool IsVeins { get; set; }
        public bool IsVeinholeMonster { get; set; }
        public TiberiumType TiberiumType { get; set; }

        [INI(false)]
        public BridgeDirection HighBridgeDirection { get; set; }
    }
}
