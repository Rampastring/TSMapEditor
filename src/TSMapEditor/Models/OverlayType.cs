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

        public int GetTiberiumIndex(bool useYROrder)
        {
            var tiberium = getTiberiumTypeEnum();
            return (tiberium, useYROrder) switch
            {
                (TiberiumTypeEnum.Vinifera, true) => (int)TiberiumTypeEnum.Cruentus,
                (TiberiumTypeEnum.Cruentus, true) => (int)TiberiumTypeEnum.Vinifera,
                (null, _) => -1,
                _ => (int)tiberium,
            };
        }

        private TiberiumTypeEnum? getTiberiumTypeEnum()
        {
            switch (Index)
            {
                case 27:
                case 28:
                case 29:
                case 30:
                case 31:
                case 32:
                case 33:
                case 34:
                case 35:
                case 36:
                case 37:
                case 38:
                    return TiberiumTypeEnum.Vinifera;
                case 102:
                case 103:
                case 104:
                case 105:
                case 106:
                case 107:
                case 108:
                case 109:
                case 110:
                case 111:
                case 112:
                case 113:
                case 114:
                case 115:
                case 116:
                case 117:
                case 118:
                case 119:
                case 120:
                case 121:
                    return TiberiumTypeEnum.Riparius;
                case 127:
                case 128:
                case 129:
                case 130:
                case 131:
                case 132:
                case 133:
                case 134:
                case 135:
                case 136:
                case 137:
                case 138:
                case 139:
                case 140:
                case 141:
                case 142:
                case 143:
                case 144:
                case 145:
                case 146:
                    return TiberiumTypeEnum.Cruentus;
                case 147:
                case 148:
                case 149:
                case 150:
                case 151:
                case 152:
                case 153:
                case 154:
                case 155:
                case 156:
                case 157:
                case 158:
                case 159:
                case 160:
                case 161:
                case 162:
                case 163:
                case 164:
                case 165:
                case 166:
                    return TiberiumTypeEnum.Aboreus;
                default:
                    return null;
            }
        }
    }
}
