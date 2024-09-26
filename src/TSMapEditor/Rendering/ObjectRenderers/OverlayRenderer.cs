using Microsoft.Xna.Framework;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public class OverlayRenderer : ObjectRenderer<Overlay>
    {
        public OverlayRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => new Color(255, 0, 255);

        protected override CommonDrawParams GetDrawParams(Overlay gameObject)
        {
            return new CommonDrawParams()
            {
                IniName = gameObject.OverlayType.ININame,
                ShapeImage = TheaterGraphics.OverlayTextures[gameObject.OverlayType.Index]
            };
        }

        protected override float GetDepth(Overlay gameObject, int referenceDrawPointY)
        {
            if (gameObject.OverlayType.HighBridgeDirection == BridgeDirection.None)
            {
                return base.GetDepth(gameObject, referenceDrawPointY) - Constants.DepthEpsilon;
            }

            var tile = Map.GetTile(gameObject.Position);
            return (((float)referenceDrawPointY / RenderDependencies.Map.HeightInPixelsWithCellHeight) * Constants.DownwardsDepthRenderSpace) + ((tile.Level + 4) * Constants.DepthRenderStep);
        }

        protected override void Render(Overlay gameObject, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            Color remapColor = Color.White;
            if (gameObject.OverlayType.TiberiumType != null)
                remapColor = gameObject.OverlayType.TiberiumType.XNAColor;

            if (!RenderDependencies.EditorState.Is2DMode && gameObject.OverlayType.HighBridgeDirection != BridgeDirection.None)
            {
                if (gameObject.OverlayType.HighBridgeDirection == BridgeDirection.EastWest)
                {
                    drawPoint.Y -= Constants.CellHeight + 1;
                }
                else
                {
                    drawPoint.Y -= Constants.CellHeight * 2 + 1;
                }
            }

            bool affectedByLighting = drawParams.ShapeImage.SubjectToLighting;
            bool affectedByAmbient = !gameObject.OverlayType.Tiberium && !affectedByLighting;

            DrawShadowDirect(gameObject);
            DrawShapeImage(gameObject, drawParams.ShapeImage, gameObject.FrameIndex, Color.White,
                true, remapColor, affectedByLighting, affectedByAmbient, drawPoint);
        }
    }
}
