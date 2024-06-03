using Microsoft.Xna.Framework;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public sealed class BuildingRenderer : ObjectRenderer<Structure>
    {
        public BuildingRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
            buildingAnimRenderer = new AnimRenderer(renderDependencies);
        }

        protected override Color ReplacementColor => Color.Yellow;

        private AnimRenderer buildingAnimRenderer;

        private void DrawFoundationLines(Structure gameObject)
        {
            int foundationX = gameObject.ObjectType.ArtConfig.Foundation.Width;
            int foundationY = gameObject.ObjectType.ArtConfig.Foundation.Height;

            Color foundationLineColor = gameObject.Owner.XNAColor;
            if (gameObject.IsBaseNodeDummy)
                foundationLineColor *= 0.25f;

            if (foundationX == 0 || foundationY == 0)
                return;

            var map = RenderDependencies.Map;

            int heightOffset = 0;

            var cell = map.GetTile(gameObject.Position);
            if (cell != null && !RenderDependencies.EditorState.Is2DMode)
                heightOffset = cell.Level * Constants.CellHeight;

            SetLighting(cell.CellLighting.ToXNAVector4Ambient());
            SetEffectParams_RGBADraw(0.0f, 0.0f, Vector2.Zero, Vector2.Zero, false);

            foreach (var edge in gameObject.ObjectType.ArtConfig.Foundation.Edges)
            {
                // Translate edge vertices from cell coordinate space to world coordinate space.
                var start = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position + edge[0], map);
                var end = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position + edge[1], map);
                // Height is an illusion, just move everything up or down.
                // Also offset X to match the top corner of an iso tile.
                start += new Point2D(Constants.CellSizeX / 2, -heightOffset);
                end += new Point2D(Constants.CellSizeX / 2, -heightOffset);
                // Draw edge.
                DrawLine(start.ToXNAVector(), end.ToXNAVector(), foundationLineColor, 1);
            }
        }

        protected override CommonDrawParams GetDrawParams(Structure gameObject)
        {
            string iniName = gameObject.ObjectType.ININame;

            return new CommonDrawParams()
            {
                IniName = iniName,
                ShapeImage = RenderDependencies.TheaterGraphics.BuildingTextures[gameObject.ObjectType.Index],
                TurretVoxel = RenderDependencies.TheaterGraphics.BuildingTurretModels[gameObject.ObjectType.Index],
                BarrelVoxel = RenderDependencies.TheaterGraphics.BuildingBarrelModels[gameObject.ObjectType.Index]
            };
        }

        protected override bool ShouldRenderReplacementText(Structure gameObject)
        {
            var bibGraphics = RenderDependencies.TheaterGraphics.BuildingBibTextures[gameObject.ObjectType.Index];

            if (bibGraphics != null)
                return false;

            if (gameObject.TurretAnim != null)
                return false;

            if (gameObject.Anims.Length > 0)
                return false;

            return base.ShouldRenderReplacementText(gameObject);
        }

        private void DrawBibGraphics(Structure gameObject, ShapeImage bibGraphics, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams, bool affectedByLighting)
        {
            DrawShapeImage(gameObject, bibGraphics, 0, Color.White, false, true, gameObject.GetRemapColor(),
                affectedByLighting, !drawParams.ShapeImage.SubjectToLighting, drawPoint, heightOffset);
        }

        protected override void Render(Structure gameObject, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            if (RenderDependencies.EditorState.RenderInvisibleInGameObjects)
                DrawFoundationLines(gameObject);

            bool affectedByLighting = RenderDependencies.EditorState.IsLighting && (drawParams.ShapeImage != null && drawParams.ShapeImage.SubjectToLighting);

            // Bib is on the ground, gets grawn first
            var bibGraphics = RenderDependencies.TheaterGraphics.BuildingBibTextures[gameObject.ObjectType.Index];
            if (bibGraphics != null)
                DrawBibGraphics(gameObject, bibGraphics, heightOffset, drawPoint, drawParams, affectedByLighting);

            Color nonRemapColor = gameObject.IsBaseNodeDummy ? new Color(150, 150, 255) * 0.5f : Color.White;

            // Form the anims list
            var animsList = gameObject.Anims.ToList();
            animsList.AddRange(gameObject.PowerUpAnims);
            if (gameObject.TurretAnim != null)
                animsList.Add(gameObject.TurretAnim);
            
            // Sort the anims according to their settings
            animsList.Sort((anim1, anim2) =>
                anim1.BuildingAnimDrawConfig.SortValue.CompareTo(anim2.BuildingAnimDrawConfig.SortValue));

            // The building itself has an offset of 0, so first draw all anims with sort values < 0
            foreach (var anim in animsList.Where(a => a.BuildingAnimDrawConfig.SortValue < 0))
                buildingAnimRenderer.Draw(anim, false);

            // Then the building itself
            if (!gameObject.ObjectType.NoShadow)
                DrawShadow(gameObject, drawParams, drawPoint, heightOffset);

            int frameCount = drawParams.ShapeImage == null ? 0 : drawParams.ShapeImage.GetFrameCount();
            bool affectedByAmbient = !affectedByLighting;

            DrawShapeImage(gameObject, drawParams.ShapeImage,
                gameObject.GetFrameIndex(frameCount),
                nonRemapColor, false, true, gameObject.GetRemapColor(),
                affectedByLighting, affectedByAmbient, drawPoint, heightOffset);

            // Then draw all anims with sort values >= 0
            foreach (var anim in animsList.Where(a => a.BuildingAnimDrawConfig.SortValue >= 0))
                buildingAnimRenderer.Draw(anim, false);

            DrawVoxelTurret(gameObject, heightOffset, drawPoint, drawParams, nonRemapColor, affectedByLighting);

            if (gameObject.ObjectType.HasSpotlight)
            {
                Point2D cellCenter = RenderDependencies.EditorState.Is2DMode ?
                    CellMath.CellTopLeftPointFromCellCoords(gameObject.Position, Map) :
                    CellMath.CellTopLeftPointFromCellCoords_3D(gameObject.Position, Map);

                DrawObjectFacingArrow(gameObject.Facing, cellCenter);
            }
        }

        private void DrawVoxelTurret(Structure gameObject, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams, Color nonRemapColor, bool affectedByLighting)
        {
            if (gameObject.ObjectType.Turret && gameObject.ObjectType.TurretAnimIsVoxel)
            {
                var turretOffset = new Point2D(gameObject.ObjectType.TurretAnimX, gameObject.ObjectType.TurretAnimY);
                var turretDrawPoint = drawPoint + turretOffset;

                const byte facingStartDrawAbove = (byte)Direction.E * 32;
                const byte facingEndDrawAbove = (byte)Direction.W * 32;

                if (gameObject.Facing is > facingStartDrawAbove and <= facingEndDrawAbove)
                {
                    DrawVoxelModel(gameObject, drawParams.TurretVoxel,
                        gameObject.Facing, RampType.None, nonRemapColor, true, gameObject.GetRemapColor(),
                        affectedByLighting, turretDrawPoint, heightOffset);

                    DrawVoxelModel(gameObject, drawParams.BarrelVoxel,
                        gameObject.Facing, RampType.None, nonRemapColor, true, gameObject.GetRemapColor(),
                        affectedByLighting, turretDrawPoint, heightOffset);
                }
                else
                {
                    DrawVoxelModel(gameObject, drawParams.BarrelVoxel,
                        gameObject.Facing, RampType.None, nonRemapColor, true, gameObject.GetRemapColor(),
                        affectedByLighting, turretDrawPoint, heightOffset);

                    DrawVoxelModel(gameObject, drawParams.TurretVoxel,
                        gameObject.Facing, RampType.None, nonRemapColor, true, gameObject.GetRemapColor(),
                        affectedByLighting, turretDrawPoint, heightOffset);
                }
            }
            else if (gameObject.ObjectType.Turret && !gameObject.ObjectType.TurretAnimIsVoxel &&
                     gameObject.ObjectType.BarrelAnimIsVoxel)
            {
                DrawVoxelModel(gameObject, drawParams.BarrelVoxel,
                    gameObject.Facing, RampType.None, nonRemapColor, true, gameObject.GetRemapColor(),
                    affectedByLighting, drawPoint, heightOffset);
            }
        }

        protected override void DrawObjectReplacementText(Structure gameObject, in CommonDrawParams drawParams, Point2D drawPoint)
        {
            DrawFoundationLines(gameObject);

            base.DrawObjectReplacementText(gameObject, drawParams, drawPoint);
        }
    }
}
