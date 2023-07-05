using Microsoft.Xna.Framework;
using TSMapEditor.GameMath;

namespace TSMapEditor.Models
{
    public interface IMovable : IPositioned
    {
        RTTIType WhatAmI();

        bool IsTechno();
    }

    /// <summary>
    /// A base class for game objects.
    /// Represents ObjectClass in the original game's class hierarchy.
    /// </summary>
    public abstract class GameObject : AbstractObject, IMovable
    {
        public virtual Point2D Position { get; set; }

        public ulong LastRefreshIndex;

        public virtual int GetYDrawOffset()
        {
            return 0;
        }

        public virtual int GetXDrawOffset()
        {
            return 0;
        }

        public virtual int GetFrameIndex(int frameCount)
        {
            return 0;
        }

        public virtual int GetShadowFrameIndex(int frameCount)
        {
            return (frameCount / 2);
        }

        public virtual int GetXPositionForDrawOrder()
        {
            return Position.X;
        }

        public virtual int GetYPositionForDrawOrder()
        {
            return Position.Y;
        }

        public override int GetHashCode()
        {
            return (int)WhatAmI() * 10000000 + Position.Y * 512 + Position.X;
        }

        public virtual bool Remapable() => false;

        public virtual Color GetRemapColor() => Color.White;
    }
}
