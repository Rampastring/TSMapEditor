namespace TSMapEditor.Models
{
    public class Overlay : GameObject
    {
        public override GameObjectType GetObjectType() => OverlayType;

        public override RTTIType WhatAmI() => RTTIType.Overlay;

        public OverlayType OverlayType { get; set; }
        public int FrameIndex { get; set; }


        public override int GetFrameIndex(int frameCount)
        {
            return FrameIndex;
        }

        public override int GetShadowFrameIndex(int frameCount)
        {
            return 100;
        }

        public override bool HasShadow() => true;

        public override bool IsInvisibleInGame() => OverlayType.InvisibleInGame;

        public override int GetYDrawOffset()
        {
            if (OverlayType.IsVeinholeMonster)
                return Constants.CellSizeY * -2;

            if (OverlayType.Tiberium || OverlayType.Wall || OverlayType.IsVeins)
                return Constants.CellSizeY / -2;

            return 0;
        }
    }
}
