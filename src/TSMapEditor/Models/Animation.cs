using TSMapEditor.GameMath;
using Microsoft.Xna.Framework;

namespace TSMapEditor.Models
{
    public class Animation: GameObject
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

        public AnimType AnimType { get; private set; }
        public House Owner { get; set; }

        public override int GetYDrawOffset()
        {
            return Constants.CellSizeY / -2 + AnimType.ArtConfig.YDrawOffset;
        }

        public override int GetXDrawOffset()
        {
            return AnimType.ArtConfig.XDrawOffset;
        }

        public override bool Remapable() => AnimType.ArtConfig.IsBuildingAnim;
        public override Color GetRemapColor() => Remapable() ? Owner.XNAColor : Color.White;
    }
}
