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

        protected override void Render(Overlay gameObject, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams)
        {
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

            DrawShadow(gameObject, drawParams, drawPoint, heightOffset);
            DrawShapeImage(gameObject, drawParams, drawParams.ShapeImage, gameObject.FrameIndex, Color.White, false, true, remapColor, drawPoint, heightOffset);
        }
    }
}
