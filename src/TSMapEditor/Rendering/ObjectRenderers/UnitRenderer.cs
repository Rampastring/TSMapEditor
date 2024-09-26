using System;
using Microsoft.Xna.Framework;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public sealed class UnitRenderer : ObjectRenderer<Unit>
    {
        public UnitRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => Color.Red;

        protected override CommonDrawParams GetDrawParams(Unit gameObject)
        {
            return new CommonDrawParams()
            {
                IniName = gameObject.ObjectType.ININame,
                ShapeImage = TheaterGraphics.UnitTextures[gameObject.ObjectType.Index],
                MainVoxel = TheaterGraphics.UnitModels[gameObject.ObjectType.Index],
                TurretVoxel = TheaterGraphics.UnitTurretModels[gameObject.ObjectType.Index],
                BarrelVoxel = TheaterGraphics.UnitBarrelModels[gameObject.ObjectType.Index]
            };
        }

        protected override void Render(Unit gameObject, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            bool affectedByLighting = RenderDependencies.EditorState.IsLighting;

            // We need to calculate depth earlier so it can also be used for potential turrets
            float depthOverride = -1f;

            if (gameObject.UnitType.ArtConfig.Voxel)
            {
                var frame = FrameFromVoxel(gameObject, drawParams.MainVoxel);
                if (frame != null && frame.Texture != null)
                {
                    var textureDrawCoords = GetTextureDrawCoords(gameObject, frame, drawPoint);
                    depthOverride = GetDepth(gameObject, textureDrawCoords.Bottom);
                }

                RenderVoxelModel(gameObject, drawPoint, drawParams.MainVoxel, affectedByLighting, depthOverride);
            }
            else
            {
                if (drawParams.ShapeImage != null)
                {
                    var frame = drawParams.ShapeImage.GetFrame(gameObject.GetFrameIndex(drawParams.ShapeImage.GetFrameCount()));
                    if (frame != null && frame.Texture != null)
                    {
                        var textureDrawCoords = GetTextureDrawCoords(gameObject, frame, drawPoint);
                        depthOverride = GetDepth(gameObject, textureDrawCoords.Bottom);
                    }
                }

                RenderMainShape(gameObject, drawPoint, drawParams, depthOverride);
            }

            if (gameObject.UnitType.Turret)
            {
                const byte facingStartDrawAbove = (byte)Direction.E * 32;
                const byte facingEndDrawAbove = (byte)Direction.W * 32;

                byte facing = Convert.ToByte(Math.Clamp(
                    Math.Round((float)gameObject.Facing / 8, MidpointRounding.AwayFromZero) * 8,
                    byte.MinValue,
                    byte.MaxValue));

                float rotationFromFacing = 2 * (float)Math.PI * ((float)facing / Constants.FacingMax);

                Vector2 leptonTurretOffset = new Vector2(0, -gameObject.UnitType.ArtConfig.TurretOffset);
                leptonTurretOffset = Vector2.Transform(leptonTurretOffset, Matrix.CreateRotationZ(rotationFromFacing));

                Point2D turretOffset = Helpers.ScreenCoordsFromWorldLeptons(leptonTurretOffset);

                if (gameObject.Facing is > facingStartDrawAbove and <= facingEndDrawAbove)
                {
                    if (gameObject.UnitType.ArtConfig.Voxel)
                        RenderVoxelModel(gameObject, drawPoint + turretOffset, drawParams.TurretVoxel, affectedByLighting, depthOverride + Constants.DepthEpsilon);
                    else
                        RenderTurretShape(gameObject, drawPoint, drawParams, depthOverride + Constants.DepthEpsilon);
                    
                    RenderVoxelModel(gameObject, drawPoint + turretOffset, drawParams.BarrelVoxel, affectedByLighting, depthOverride + (Constants.DepthEpsilon * 2));
                }
                else
                {
                    RenderVoxelModel(gameObject, drawPoint + turretOffset, drawParams.BarrelVoxel, affectedByLighting, depthOverride + Constants.DepthEpsilon);

                    if (gameObject.UnitType.ArtConfig.Voxel)
                        RenderVoxelModel(gameObject, drawPoint + turretOffset, drawParams.TurretVoxel, affectedByLighting, depthOverride + (Constants.DepthEpsilon * 2));
                    else
                        RenderTurretShape(gameObject,  drawPoint, drawParams, depthOverride + (Constants.DepthEpsilon * 2));
                }
            }
        }

        private void RenderMainShape(Unit gameObject, Point2D drawPoint, CommonDrawParams drawParams, float depthOverride)
        {
            if (!gameObject.ObjectType.NoShadow)
                DrawShadowDirect(gameObject);

            DrawShapeImage(gameObject, drawParams.ShapeImage, 
                gameObject.GetFrameIndex(drawParams.ShapeImage.GetFrameCount()),
                Color.White, true, gameObject.GetRemapColor(),
                false, true, drawPoint, depthOverride);
        }

        private void RenderTurretShape(Unit gameObject, Point2D drawPoint,
            CommonDrawParams drawParams, float depthOverride)
        {
            int turretFrameIndex = gameObject.GetTurretFrameIndex();

            if (turretFrameIndex > -1 && turretFrameIndex < drawParams.ShapeImage.GetFrameCount())
            {
                PositionedTexture frame = drawParams.ShapeImage.GetFrame(turretFrameIndex);

                if (frame == null)
                    return;

                DrawShapeImage(gameObject, drawParams.ShapeImage,
                    turretFrameIndex, Color.White, true, gameObject.GetRemapColor(),
                    false, true, drawPoint, depthOverride);
            }
        }

        private PositionedTexture FrameFromVoxel(Unit gameObject, VoxelModel voxel)
        {
            var unitTile = RenderDependencies.Map.GetTile(gameObject.Position.X, gameObject.Position.Y);

            if (unitTile == null)
                return null;

            ITileImage tile = RenderDependencies.Map.TheaterInstance.GetTile(unitTile.TileIndex);
            ISubTileImage subTile = tile.GetSubTile(unitTile.SubTileIndex);
            RampType ramp = subTile.TmpImage.RampType;
            return voxel.GetFrame(gameObject.Facing, ramp, RenderDependencies.EditorState.IsLighting);
        }

        private void RenderVoxelModel(Unit gameObject, Point2D drawPoint, 
            VoxelModel model, bool affectedByLighting, float depthOverride)
        {
            var unitTile = RenderDependencies.Map.GetTile(gameObject.Position.X, gameObject.Position.Y);

            if (unitTile == null)
                return;

            ITileImage tile = RenderDependencies.Map.TheaterInstance.GetTile(unitTile.TileIndex);
            ISubTileImage subTile = tile.GetSubTile(unitTile.SubTileIndex);
            RampType ramp = subTile.TmpImage.RampType;

            DrawVoxelModel(gameObject, model,
                gameObject.Facing, ramp, Color.White, true, gameObject.GetRemapColor(),
                affectedByLighting, drawPoint, depthOverride);
        }
    }
}
