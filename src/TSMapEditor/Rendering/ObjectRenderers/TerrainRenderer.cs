using Microsoft.Xna.Framework;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public sealed class TerrainRenderer : ObjectRenderer<TerrainObject>
    {
        public TerrainRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => Color.Green;

        protected override CommonDrawParams GetDrawParams(TerrainObject gameObject)
        {
            return new CommonDrawParams()
            {
                IniName = gameObject.TerrainType.ININame,
                ShapeImage = TheaterGraphics.TerrainObjectTextures[gameObject.TerrainType.Index]
            };
        }

        protected override void Render(TerrainObject gameObject, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            bool affectedByLighting = RenderDependencies.EditorState.IsLighting;

            DrawShadow(gameObject, drawParams, affectedByLighting, drawPoint, heightOffset);

            DrawShapeImage(gameObject, drawParams, drawParams.ShapeImage, 0, 
                Color.White, false, false, Color.White, affectedByLighting, drawPoint, heightOffset);
        }
    }
}
