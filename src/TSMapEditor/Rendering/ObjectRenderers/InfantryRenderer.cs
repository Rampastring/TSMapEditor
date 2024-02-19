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

        protected override void Render(Infantry gameObject, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams)
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

            bool affectedByLighting = RenderDependencies.EditorState.IsLighting;

            if (!gameObject.ObjectType.NoShadow)
                DrawShadow(gameObject, drawParams, affectedByLighting, drawPoint, heightOffset);

            DrawShapeImage(gameObject, drawParams, drawParams.ShapeImage, 
                gameObject.GetFrameIndex(drawParams.ShapeImage.GetFrameCount()), 
                Color.White, false, true, gameObject.GetRemapColor(),
                affectedByLighting, drawPoint, heightOffset);
        }
    }
}
