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

        protected override void Render(Unit gameObject, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            bool affectedByLighting = RenderDependencies.EditorState.IsLighting;

            if (gameObject.UnitType.ArtConfig.Voxel)
            {
                RenderVoxelModel(gameObject, heightOffset, drawPoint, drawParams, drawParams.MainVoxel, affectedByLighting);
            }
            else
            {
                RenderMainShape(gameObject, heightOffset, drawPoint, drawParams, affectedByLighting);
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
                        RenderVoxelModel(gameObject, heightOffset, drawPoint + turretOffset, drawParams, drawParams.TurretVoxel, affectedByLighting);
                    else
                        RenderTurretShape(gameObject, heightOffset, drawPoint, drawParams, affectedByLighting);
                    
                    RenderVoxelModel(gameObject, heightOffset, drawPoint + turretOffset, drawParams, drawParams.BarrelVoxel, affectedByLighting);
                }
                else
                {
                    RenderVoxelModel(gameObject, heightOffset, drawPoint + turretOffset, drawParams, drawParams.BarrelVoxel, affectedByLighting);

                    if (gameObject.UnitType.ArtConfig.Voxel)
                        RenderVoxelModel(gameObject, heightOffset, drawPoint + turretOffset, drawParams, drawParams.TurretVoxel, affectedByLighting);
                    else
                        RenderTurretShape(gameObject, heightOffset, drawPoint, drawParams, affectedByLighting);
                }
            }
        }

        private void RenderMainShape(Unit gameObject, int heightOffset, Point2D drawPoint,
            CommonDrawParams drawParams, bool affectedByLighting)
        {
            if (!gameObject.ObjectType.NoShadow)
                DrawShadow(gameObject, drawParams, affectedByLighting, drawPoint, heightOffset);

            DrawShapeImage(gameObject, drawParams, drawParams.ShapeImage, 
                gameObject.GetFrameIndex(drawParams.ShapeImage.GetFrameCount()),
                Color.White, false, true, gameObject.GetRemapColor(),
                affectedByLighting, drawPoint, heightOffset);
        }

        private void RenderTurretShape(Unit gameObject, int heightOffset, Point2D drawPoint,
            CommonDrawParams drawParams, bool affectedByLighting)
        {
            int turretFrameIndex = gameObject.GetTurretFrameIndex();

            if (turretFrameIndex > -1 && turretFrameIndex < drawParams.ShapeImage.GetFrameCount())
            {
                PositionedTexture frame = drawParams.ShapeImage.GetFrame(turretFrameIndex);

                if (frame == null)
                    return;

                DrawShapeImage(gameObject, drawParams, drawParams.ShapeImage,
                    turretFrameIndex, Color.White, false, true, gameObject.GetRemapColor(),
                    affectedByLighting, drawPoint, heightOffset);
            }
        }

        private void RenderVoxelModel(Unit gameObject, int heightOffset, Point2D drawPoint,
            in CommonDrawParams drawParams, VoxelModel model, bool affectedByLighting)
        {
            var unitTile = RenderDependencies.Map.GetTile(gameObject.Position.X, gameObject.Position.Y);

            if (unitTile == null)
                return;

            ITileImage tile = RenderDependencies.Map.TheaterInstance.GetTile(unitTile.TileIndex);
            ISubTileImage subTile = tile.GetSubTile(unitTile.SubTileIndex);
            RampType ramp = subTile.TmpImage.RampType;

            DrawVoxelModel(gameObject, drawParams, model,
                gameObject.Facing, ramp, Color.White, true, gameObject.GetRemapColor(),
                affectedByLighting, drawPoint, heightOffset);
        }
    }
}
