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

        public Point2D GetBuildingCenterPoint(Structure structure)
        {
            Point2D topPoint = CellMath.CellCenterPointFromCellCoords(structure.Position, Map);
            var foundation = structure.ObjectType.ArtConfig.Foundation;
            Point2D bottomPoint = CellMath.CellCenterPointFromCellCoords(structure.Position + new Point2D(foundation.Width - 1, foundation.Height - 1), Map);
            return topPoint + new Point2D((bottomPoint.X - topPoint.X) / 2, (bottomPoint.Y - topPoint.Y) / 2);
        }

        public void DrawFoundationLines(Structure gameObject)
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

            // Cell lighting ranges from 0.0 to 2.0, XNA colors from 0.0 to 1.0. Thus division by 2
            foundationLineColor = new Color((foundationLineColor.R / 255.0f) * (float)cell.CellLighting.R / 2.0f,
                (foundationLineColor.G / 255.0f) * (float)cell.CellLighting.G / 2.0f,
                (foundationLineColor.B / 255.0f) * (float)cell.CellLighting.B / 2.0f,
                1.0f);

            foreach (var edge in gameObject.ObjectType.ArtConfig.Foundation.Edges)
            {
                // Translate edge vertices from cell coordinate space to world coordinate space.
                var start = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position + edge[0], map);
                var end = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position + edge[1], map);

                float depth = GetFoundationLineDepth(gameObject, start, end);
                // Height is an illusion, just move everything up or down.
                // Also offset X to match the top corner of an iso tile.
                start += new Point2D(Constants.CellSizeX / 2, -heightOffset);
                end += new Point2D(Constants.CellSizeX / 2, -heightOffset);
                // Draw edge.
                RenderDependencies.ObjectSpriteRecord.AddLineEntry(new LineEntry(start.ToXNAVector(), end.ToXNAVector(), foundationLineColor, 1, depth));
            }
        }

        private float GetFoundationLineDepth(Structure gameObject, Point2D startPoint, Point2D endPoint)
        {
            Point2D lowerPoint = startPoint.Y > endPoint.Y ? startPoint : endPoint;
            return base.GetDepth(gameObject, lowerPoint.Y) - (Constants.DepthRenderStep);
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

        protected override float GetDepth(Structure gameObject, int referenceDrawPointY)
        {
            // The 100.0 divisor is just an arbitrary number here. It appeared to give me the best result on SOV01UMD.MAP.
            // Before we implement a shader-based replacement for BUILDINGZ.SHP, we probably can't do better.
            return base.GetDepth(gameObject, referenceDrawPointY) + ((gameObject.ObjectType.ArtConfig.Height * Constants.DepthRenderStep) / 100.0f);
        }

        private void DrawBibGraphics(Structure gameObject, ShapeImage bibGraphics, Point2D drawPoint, in CommonDrawParams drawParams, bool affectedByLighting)
        {
            DrawShapeImage(gameObject, bibGraphics, 0, Color.White, true, gameObject.GetRemapColor(),
                affectedByLighting, !drawParams.ShapeImage.SubjectToLighting, drawPoint);
        }

        protected override void Render(Structure gameObject, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            if (RenderDependencies.EditorState.RenderInvisibleInGameObjects)
                DrawFoundationLines(gameObject);

            bool affectedByLighting = RenderDependencies.EditorState.IsLighting && (drawParams.ShapeImage != null && drawParams.ShapeImage.SubjectToLighting);

            // Bib is on the ground, gets grawn first
            var bibGraphics = RenderDependencies.TheaterGraphics.BuildingBibTextures[gameObject.ObjectType.Index];
            if (bibGraphics != null)
                DrawBibGraphics(gameObject, bibGraphics, drawPoint, drawParams, affectedByLighting);

            Color nonRemapColor = gameObject.IsBaseNodeDummy ? new Color(150, 150, 255) * 0.5f : Color.White;

            int frameCount = drawParams.ShapeImage == null ? 0 : drawParams.ShapeImage.GetFrameCount();
            int frameIndex = gameObject.GetFrameIndex(frameCount);

            // We need to calculate depth earlier so it can also be used for the building's animations
            // Animations are typically higher than the building itself, but they still need a greater z-value,
            // which isn't achieved by default because z grows downwards
            float depthOverride = -1f;
            if (drawParams.ShapeImage != null)
            {
                var frame = drawParams.ShapeImage.GetFrame(frameIndex);
                if (frame != null && frame.Texture != null)
                {
                    var textureDrawCoords = GetTextureDrawCoords(gameObject, frame, drawPoint);
                    depthOverride = GetDepth(gameObject, textureDrawCoords.Bottom);
                }
            }

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
            {
                buildingAnimRenderer.BuildingAnimDepth = depthOverride - Constants.DepthEpsilon;
                buildingAnimRenderer.Draw(anim, false);
            }

            // Then the building itself
            if (!gameObject.ObjectType.NoShadow)
                DrawShadowDirect(gameObject);

            bool affectedByAmbient = !affectedByLighting;

            DrawShapeImage(gameObject, drawParams.ShapeImage,
                gameObject.GetFrameIndex(frameCount),
                nonRemapColor, true, gameObject.GetRemapColor(),
                affectedByLighting, affectedByAmbient, drawPoint, depthOverride);

            // Then draw all anims with sort values >= 0
            foreach (var anim in animsList.Where(a => a.BuildingAnimDrawConfig.SortValue >= 0))
            {
                buildingAnimRenderer.BuildingAnimDepth = depthOverride + Constants.DepthEpsilon;
                buildingAnimRenderer.Draw(anim, false);
            }

            DrawVoxelTurret(gameObject, drawPoint, drawParams, nonRemapColor, affectedByLighting, depthOverride);

            if (gameObject.ObjectType.HasSpotlight)
            {
                Point2D cellCenter = RenderDependencies.EditorState.Is2DMode ?
                    CellMath.CellTopLeftPointFromCellCoords(gameObject.Position, Map) :
                    CellMath.CellTopLeftPointFromCellCoords_3D(gameObject.Position, Map);

                DrawObjectFacingArrow(gameObject.Facing, cellCenter);
            }
        }

        private void DrawVoxelTurret(Structure gameObject, Point2D drawPoint, in CommonDrawParams drawParams, Color nonRemapColor, bool affectedByLighting, float depthOverride)
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
                        affectedByLighting, turretDrawPoint, depthOverride + Constants.DepthEpsilon);

                    DrawVoxelModel(gameObject, drawParams.BarrelVoxel,
                        gameObject.Facing, RampType.None, nonRemapColor, true, gameObject.GetRemapColor(),
                        affectedByLighting, turretDrawPoint, depthOverride + (Constants.DepthEpsilon * 2));
                }
                else
                {
                    DrawVoxelModel(gameObject, drawParams.BarrelVoxel,
                        gameObject.Facing, RampType.None, nonRemapColor, true, gameObject.GetRemapColor(),
                        affectedByLighting, turretDrawPoint, depthOverride - Constants.DepthEpsilon);

                    DrawVoxelModel(gameObject, drawParams.TurretVoxel,
                        gameObject.Facing, RampType.None, nonRemapColor, true, gameObject.GetRemapColor(),
                        affectedByLighting, turretDrawPoint, depthOverride + Constants.DepthEpsilon); // Turret is always drawn above building
                }
            }
            else if (gameObject.ObjectType.Turret && !gameObject.ObjectType.TurretAnimIsVoxel &&
                     gameObject.ObjectType.BarrelAnimIsVoxel)
            {
                DrawVoxelModel(gameObject, drawParams.BarrelVoxel,
                    gameObject.Facing, RampType.None, nonRemapColor, true, gameObject.GetRemapColor(),
                    affectedByLighting, drawPoint, depthOverride + Constants.DepthEpsilon);
            }
        }

        protected override void DrawObjectReplacementText(Structure gameObject, string text, Point2D drawPoint)
        {
            DrawFoundationLines(gameObject);

            base.DrawObjectReplacementText(gameObject, text, drawPoint);
        }
    }
}
