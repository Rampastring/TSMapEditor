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

        public float BuildingAnimDepth { get; set; }

        protected override Color ReplacementColor => Color.Orange;

        protected override CommonDrawParams GetDrawParams(Animation gameObject)
        {
            return new CommonDrawParams()
            {
                IniName = gameObject.AnimType.ININame,
                ShapeImage = TheaterGraphics.AnimTextures[gameObject.AnimType.Index]
            };
        }

        protected override bool ShouldRenderReplacementText(Animation gameObject)
        {
            // Never draw this for animations
            return false;
        }

        protected override void Render(Animation gameObject, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            if (drawParams.ShapeImage == null)
                return;

            int frameIndex = gameObject.GetFrameIndex(drawParams.ShapeImage.GetFrameCount());
            if (gameObject.IsTurretAnim)
            {
                // Turret anims have their facing frames reversed
                // Turret anims also only have 32 facings
                byte facing = (byte)(255 - gameObject.Facing - 31);
                frameIndex = facing / (256 / 32);
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

            bool affectedByLighting = RenderDependencies.EditorState.IsLighting;
            bool affectedByAmbient = !drawParams.ShapeImage.SubjectToLighting;

            float depthOverride = -1;
            if (gameObject.IsBuildingAnim)
            {
                depthOverride = BuildingAnimDepth;
            }

            DrawShadowDirect(gameObject);
            DrawShapeImage(gameObject, drawParams.ShapeImage,
                frameIndex, Color.White * alpha,
                gameObject.IsBuildingAnim, gameObject.GetRemapColor() * alpha,
                affectedByLighting, affectedByAmbient, drawPoint, depthOverride);
        }

        public override void DrawShadowDirect(Animation gameObject)
        {
            if (!Constants.DrawBuildingAnimationShadows && gameObject.IsBuildingAnim)
                return;

            var drawParams = GetDrawParams(gameObject);
            var drawPoint = GetDrawPoint(gameObject);

            int shadowFrameIndex = gameObject.GetShadowFrameIndex(drawParams.ShapeImage.GetFrameCount());

            if (gameObject.IsTurretAnim)
            {
                // Turret anims have their facing frames reversed
                byte facing = (byte)(255 - gameObject.Facing - 31);
                shadowFrameIndex += facing / (512 / drawParams.ShapeImage.GetFrameCount());
            }

            if (shadowFrameIndex > 0 && shadowFrameIndex < drawParams.ShapeImage.GetFrameCount())
            {
                var frame = drawParams.ShapeImage.GetFrame(shadowFrameIndex);
                if (frame != null && frame.Texture != null)
                {
                    Rectangle drawingBounds = GetTextureDrawCoords(gameObject, frame, drawPoint);
                    float depth = GetDepth(gameObject, drawPoint.Y + drawingBounds.Height);

                    RenderDependencies.ObjectSpriteRecord.AddGraphicsEntry(new ObjectSpriteEntry(null, frame.Texture, drawingBounds, Color.White, false, true, depth));
                }
            }
        }
    }
}
