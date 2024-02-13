using Microsoft.Xna.Framework;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public class SmudgeRenderer : ObjectRenderer<Smudge>
    {
        public SmudgeRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => Color.Cyan;

        protected override CommonDrawParams GetDrawParams(Smudge gameObject)
        {
            return new CommonDrawParams()
            {
                IniName = gameObject.SmudgeType.ININame,
                ShapeImage = TheaterGraphics.SmudgeTextures[gameObject.SmudgeType.Index]
            };
        }

        protected override void Render(Smudge gameObject, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            DrawShapeImage(gameObject, drawParams, drawParams.ShapeImage, 0, Color.White, false, false, Color.White, drawPoint, heightOffset);
        }
    }
}
