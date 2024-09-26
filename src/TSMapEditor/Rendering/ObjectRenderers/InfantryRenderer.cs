using Microsoft.Xna.Framework;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public sealed class InfantryRenderer : ObjectRenderer<Infantry>
    {
        public InfantryRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => Color.Teal;

        protected override CommonDrawParams GetDrawParams(Infantry gameObject)
        {
            return new CommonDrawParams()
            {
                IniName = gameObject.ObjectType.ININame,
                ShapeImage = TheaterGraphics.InfantryTextures[gameObject.ObjectType.Index]
            };
        }

        public override Point2D GetDrawPoint(Infantry gameObject)
        {
            Point2D drawPoint = base.GetDrawPoint(gameObject);

            switch (gameObject.SubCell)
            {
                case SubCell.Top:
                    drawPoint += new Point2D(0, Constants.CellSizeY / -4);
                    break;
                case SubCell.Bottom:
                    drawPoint += new Point2D(0, Constants.CellSizeY / 4);
                    break;
                case SubCell.Left:
                    drawPoint += new Point2D(Constants.CellSizeX / -4, 0);
                    break;
                case SubCell.Right:
                    drawPoint += new Point2D(Constants.CellSizeX / 4, 0);
                    break;
                case SubCell.Center:
                default:
                    break;
            }

            return drawPoint;
        }

        protected override void Render(Infantry gameObject, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            if (!gameObject.ObjectType.NoShadow)
                DrawShadowDirect(gameObject);

            DrawShapeImage(gameObject, drawParams.ShapeImage, 
                gameObject.GetFrameIndex(drawParams.ShapeImage.GetFrameCount()), 
                Color.White, true, gameObject.GetRemapColor(),
                false, true, drawPoint);
        }
    }
}
