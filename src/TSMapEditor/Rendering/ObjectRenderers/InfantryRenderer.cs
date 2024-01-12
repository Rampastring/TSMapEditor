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

        protected override ICommonDrawParams GetDrawParams(Infantry gameObject)
        {
            var graphics = TheaterGraphics.InfantryTextures[gameObject.ObjectType.Index];
            string iniName = gameObject.ObjectType.ININame;

            return new ShapeDrawParams(graphics, iniName);
        }

        protected override void Render(Infantry gameObject, int yDrawPointWithoutCellHeight, Point2D drawPoint, ICommonDrawParams drawParams)
        {
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

            if (drawParams is not ShapeDrawParams shapeDrawParams)
                return;

            if (!gameObject.ObjectType.NoShadow)
                DrawShadow(gameObject, drawParams, drawPoint, yDrawPointWithoutCellHeight);

            DrawShapeImage(gameObject, shapeDrawParams, shapeDrawParams.Graphics, 
                gameObject.GetFrameIndex(shapeDrawParams.Graphics.GetFrameCount()), 
                Color.White, true, gameObject.GetRemapColor(), drawPoint, yDrawPointWithoutCellHeight);
        }
    }
}
