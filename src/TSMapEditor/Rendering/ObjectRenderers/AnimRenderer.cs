using Microsoft.Xna.Framework;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public sealed class AnimRenderer : ObjectRenderer<Animation>
    {
        public AnimRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => Color.Orange;

        protected override CommonDrawParams GetDrawParams(Animation gameObject)
        {
            return new CommonDrawParams(TheaterGraphics.AnimTextures[gameObject.AnimType.Index], gameObject.AnimType.ININame);
        }

        protected override void Render(Animation gameObject, int yDrawPointWithoutCellHeight, Point2D drawPoint, CommonDrawParams commonDrawParams)
        {
            int frameIndex = gameObject.AnimType.ArtConfig.Start;
            if (gameObject.IsTurretAnim)
            {
                byte facing = Constants.ReverseFacing ? (byte)(255 - gameObject.Facing - 31) : (byte)(gameObject.Facing - 31);
                frameIndex = facing / (512 / commonDrawParams.Graphics.Frames.Length);
            }

            DrawShadow(gameObject, commonDrawParams, drawPoint, yDrawPointWithoutCellHeight);

            DrawObjectImage(gameObject, commonDrawParams, commonDrawParams.Graphics,
                frameIndex, Color.White,
                gameObject.IsBuildingAnim, gameObject.GetRemapColor(),
                drawPoint, yDrawPointWithoutCellHeight);
        }

        protected override void DrawShadow(Animation gameObject, CommonDrawParams drawParams, Point2D drawPoint, int initialYDrawPointWithoutCellHeight)
        {
            int shadowFrameIndex = gameObject.GetShadowFrameIndex(drawParams.Graphics.Frames.Length);
            if (gameObject.IsTurretAnim)
            {
                byte facing = Constants.ReverseFacing ? (byte)(255 - gameObject.Facing - 31) : (byte)(gameObject.Facing - 31);
                shadowFrameIndex += facing / (512 / drawParams.Graphics.Frames.Length);
            }

            if (shadowFrameIndex > 0 && shadowFrameIndex < drawParams.Graphics.Frames.Length)
            {
                DrawObjectImage(gameObject, drawParams, drawParams.Graphics, shadowFrameIndex,
                    new Color(0, 0, 0, 128), false, Color.White, drawPoint, initialYDrawPointWithoutCellHeight);
            }
        }
    }
}
