using Microsoft.Xna.Framework;
using TSMapEditor.GameMath;

namespace TSMapEditor.Models
{
    public class Animation : GameObject
    {
        public Animation(AnimType animType)
        {
            AnimType = animType;
        }

        public Animation(AnimType animType, Point2D position) : this(animType)
        {
            Position = position;
        }

        public override RTTIType WhatAmI() => RTTIType.Anim;

        public override GameObjectType GetObjectType() => AnimType;

        public AnimType AnimType { get; private set; }
        public House Owner { get; set; }
        public byte Facing { get; set; }
        public bool IsBuildingAnim { get; set; }
        public Structure ParentBuilding { get; set; }
        public bool IsTurretAnim { get; set; }
        public BuildingAnimDrawConfig BuildingAnimDrawConfig { get; set; } // Contains offsets loaded from the parent building

        public override int GetYDrawOffset()
        {
            return Constants.CellSizeY / -2 + AnimType.ArtConfig.YDrawOffset +
                   (IsBuildingAnim ? BuildingAnimDrawConfig.Y : 0);
        }

        public override int GetXDrawOffset()
        {
            return AnimType.ArtConfig.XDrawOffset +
                   (IsBuildingAnim ? BuildingAnimDrawConfig.X : 0);
        }

        public override int GetFrameIndex(int frameCount)
        {
            if (IsBuildingAnim && ParentBuilding != null)
            {
                if (frameCount > 1 && ParentBuilding.HP < Constants.ConditionYellowHP)
                    return frameCount / 4;
            }

            return 0;
        }

        public override int GetShadowFrameIndex(int frameCount)
        {
            if (IsBuildingAnim && ParentBuilding != null)
            {
                if (ParentBuilding.HP < Constants.ConditionYellowHP)
                    return frameCount / 4 * 3;
            }

            return frameCount / 2;
        }

        public override bool Remapable() => IsBuildingAnim;
        public override Color GetRemapColor() => Remapable() ? Owner.XNAColor : Color.White;
    }

    public struct BuildingAnimDrawConfig
    {
        private const int ZAdjustMult = 5; // Random value to make z-sorting work
        public int Y { get; set; }
        public int X { get; set; }
        public int YSort { get; set; }
        public int ZAdjust { get; set; }
        public readonly int SortValue => YSort - ZAdjust * ZAdjustMult;
    }
}
