using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
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

        public override void DrawNonRemap(Smudge gameObject, Point2D drawPoint)
        {
            var drawParams = GetDrawParams(gameObject);

            if (drawParams.ShapeImage == null)
                return;

            PositionedTexture frame = drawParams.ShapeImage.GetFrame(0);
            if (frame == null || frame.Texture == null)
                return;

            Rectangle drawingBounds = GetTextureDrawCoords(gameObject, frame, drawPoint);

            Vector4 lighting = Vector4.One;
            var mapCell = Map.GetTile(gameObject.Position);

            if (RenderDependencies.EditorState.IsLighting && mapCell != null)
            {
                if (RenderDependencies.EditorState.IsLighting && drawParams.ShapeImage.SubjectToLighting)
                {
                    lighting = mapCell.CellLighting.ToXNAVector4(0);
                }
                else if (!drawParams.ShapeImage.SubjectToLighting)
                {
                    lighting = mapCell.CellLighting.ToXNAVector4Ambient(0);
                }
            }

            Point2D southernmostCellCoords = gameObject.Position + new Point2D(gameObject.SmudgeType.Width - 1, gameObject.SmudgeType.Height - 1);

            float depth = CellMath.GetDepthForCell(southernmostCellCoords, Map) + GetDepthAddition(gameObject);

            Texture2D texture = frame.Texture;

            Color color = new Color(lighting.X / 2f,
                lighting.Y / 2f,
                lighting.Z / 2f, depth);

            Renderer.DrawTexture(texture, drawingBounds, null, color, 0f, Vector2.Zero, SpriteEffects.None, depth);
        }
    }
}
