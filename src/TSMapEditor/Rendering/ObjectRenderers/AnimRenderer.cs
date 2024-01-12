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

        protected override ICommonDrawParams GetDrawParams(Animation gameObject)
        {
            return new ShapeDrawParams(TheaterGraphics.AnimTextures[gameObject.AnimType.Index], gameObject.AnimType.ININame);
        }

        protected override bool ShouldRenderReplacementText(Animation gameObject)
        {
            // Never draw this for animations
            return false;
        }

        protected override void Render(Animation gameObject, int yDrawPointWithoutCellHeight, Point2D drawPoint, ICommonDrawParams drawParams)
        {
            if (drawParams is not ShapeDrawParams shapeDrawParams || shapeDrawParams.Graphics == null)
                return;

            int frameIndex = gameObject.AnimType.ArtConfig.Start;
            if (gameObject.IsTurretAnim)
            {
                // Turret anims have their facing frames reversed
                byte facing = (byte)(255 - gameObject.Facing - 31);
                frameIndex = facing / (512 / shapeDrawParams.Graphics.GetFrameCount());
            }

            float alpha = 1.0f;

            // Translucency values don't seem to directly map into MonoGame alpha values,
            // this will need some investigating into
            switch (gameObject.AnimType.ArtConfig.Translucency)
            {
                case 75:
                    alpha = 0.1f;
                    break;
                case 50:
                    alpha = 0.2f;
                    break;
                case 25:
                    alpha = 0.5f;
                    break;
            }

            DrawShadow(gameObject, shapeDrawParams, drawPoint, yDrawPointWithoutCellHeight);

            DrawShapeImage(gameObject, shapeDrawParams, shapeDrawParams.Graphics,
                frameIndex, Color.White * alpha,
                gameObject.IsBuildingAnim, gameObject.GetRemapColor() * alpha,
                drawPoint, yDrawPointWithoutCellHeight);
        }

        protected override void DrawShadow(Animation gameObject, ICommonDrawParams drawParams, Point2D drawPoint, int initialYDrawPointWithoutCellHeight)
        {
            if (!Constants.DrawBuildingAnimationShadows && gameObject.IsBuildingAnim)
                return;

            if (drawParams is not ShapeDrawParams shapeDrawParams)
                return;

            int shadowFrameIndex = gameObject.GetShadowFrameIndex(shapeDrawParams.Graphics.GetFrameCount());

            if (gameObject.IsTurretAnim)
            {
                // Turret anims have their facing frames reversed
                byte facing = (byte)(255 - gameObject.Facing - 31);
                shadowFrameIndex += facing / (512 / shapeDrawParams.Graphics.GetFrameCount());
            }

            if (shadowFrameIndex > 0 && shadowFrameIndex < shapeDrawParams.Graphics.GetFrameCount())
            {
                DrawShapeImage(gameObject, drawParams, shapeDrawParams.Graphics, shadowFrameIndex,
                    new Color(0, 0, 0, 128), false, Color.White, drawPoint, initialYDrawPointWithoutCellHeight);
            }
        }
    }
}
