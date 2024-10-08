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

        public float BuildingAnimDepthAddition { get; set; }

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

        public override Point2D GetDrawPoint(Animation gameObject)
        {
            Point2D position = gameObject.Position;

            if (gameObject.IsBuildingAnim && gameObject.ParentBuilding != null)
            {
                position = gameObject.ParentBuilding.Position;
            }

            Point2D drawPointWithoutCellHeight = CellMath.CellTopLeftPointFromCellCoords(position, RenderDependencies.Map);

            var mapCell = RenderDependencies.Map.GetTile(position);
            int heightOffset = RenderDependencies.EditorState.Is2DMode ? 0 : mapCell.Level * Constants.CellHeight;
            Point2D drawPoint = new Point2D(drawPointWithoutCellHeight.X, drawPointWithoutCellHeight.Y - heightOffset);

            return drawPoint;
        }

        protected override float GetDepthAddition(Animation gameObject)
        {
            return Constants.DepthEpsilon * ObjectDepthAdjustments.Animation;
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
                case 0:
                    break;
                default:
                    return; // TODO Renderer does not currently support transparency for anims
                // case 75:
                //     alpha = 0.1f;
                //     break;
                // case 50:
                //     alpha = 0.2f;
                //     break;
                // case 25:
                //     alpha = 0.5f;
                //     break;
            }

            bool affectedByLighting = RenderDependencies.EditorState.IsLighting;
            bool affectedByAmbient = !drawParams.ShapeImage.SubjectToLighting;

            float depthAddition = 0f;
            if (gameObject.IsBuildingAnim)
            {
                depthAddition = BuildingAnimDepthAddition;

                // Reduce depth addition by the size of the animation's texture
                if (drawParams.ShapeImage != null)
                {
                    var frame = drawParams.ShapeImage.GetFrame(gameObject.GetFrameIndex(drawParams.ShapeImage.GetFrameCount()));
                    if (frame != null && frame.Texture != null)
                        depthAddition -= frame.Texture.Height / (float)Map.HeightInPixelsWithCellHeight;
                }
            }

            DrawShadow(gameObject);
            DrawShapeImage(gameObject, drawParams.ShapeImage,
                frameIndex, Color.White * alpha,
                gameObject.IsBuildingAnim, gameObject.GetRemapColor() * alpha,
                affectedByLighting, affectedByAmbient, drawPoint, depthAddition);
        }

        public override void DrawShadow(Animation gameObject)
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

                    RenderDependencies.ObjectSpriteRecord.AddGraphicsEntry(new ObjectSpriteEntry(null, frame.Texture, drawingBounds, Color.White, false, true, GetDepthAddition(gameObject)));
                }
            }
        }
    }
}
