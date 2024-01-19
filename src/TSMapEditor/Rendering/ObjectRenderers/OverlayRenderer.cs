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

        protected override ICommonDrawParams GetDrawParams(Overlay gameObject)
        {
            return new ShapeDrawParams(TheaterGraphics.OverlayTextures[gameObject.OverlayType.Index], gameObject.OverlayType.ININame);
        }

        protected override void Render(Overlay gameObject, int yDrawPointWithoutCellHeight, Point2D drawPoint, ICommonDrawParams drawParams)
        {
            if (drawParams is not ShapeDrawParams shapeDrawParams)
                return;

            int tiberiumIndex = gameObject.OverlayType.GetTiberiumIndex(Constants.UseCountries);

            Color remapColor = Color.White;
            if (tiberiumIndex > -1 && tiberiumIndex < Map.Rules.TiberiumTypes.Count)
                remapColor = Map.Rules.TiberiumTypes[tiberiumIndex].XNAColor;

            int overlayIndex = gameObject.OverlayType.Index;

            if (!RenderDependencies.EditorState.Is2DMode)
            {
                foreach (var bridge in Map.EditorConfig.Bridges)
                {
                    if (bridge.Kind == BridgeKind.High)
                    {
                        if (bridge.EastWest.Pieces.Contains(overlayIndex))
                        {
                            drawPoint.Y -= Constants.CellHeight + 1;
                            break;
                        }

                        if (bridge.NorthSouth.Pieces.Contains(overlayIndex))
                        {
                            drawPoint.Y -= Constants.CellHeight * 2 + 1;
                            break;
                        }
                    }
                }
            }

            DrawShadow(gameObject, shapeDrawParams, drawPoint, yDrawPointWithoutCellHeight);
            DrawShapeImage(gameObject, shapeDrawParams, shapeDrawParams.Graphics, gameObject.FrameIndex, Color.White, true, remapColor, drawPoint, yDrawPointWithoutCellHeight);
        }
    }
}
