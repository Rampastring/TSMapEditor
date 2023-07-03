using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;

namespace TSMapEditor.Rendering
{
    /// <summary>
    /// Editor-only assets.
    /// </summary>
    public class EditorGraphics
    {
        public Texture2D GenericTileTexture { get; private set; }
        public Texture2D ImpassableCellHighlightTexture { get; private set; }
        public Texture2D IceGrowthHighlightTexture { get; private set; }
        public Texture2D RangeIndicatorTexture { get; private set; }

        public EditorGraphics()
        {
            GenericTileTexture = AssetLoader.LoadTexture("generictile.png");
            ImpassableCellHighlightTexture = AssetLoader.LoadTexture("impassablehighlight.png");
            IceGrowthHighlightTexture = AssetLoader.LoadTexture("icehighlight.png");
            RangeIndicatorTexture = AssetLoader.LoadTexture("rangeindicator.png");
        }

        public void DisposeAll()
        {
            GenericTileTexture.Dispose();
            ImpassableCellHighlightTexture.Dispose();
            IceGrowthHighlightTexture.Dispose();
            RangeIndicatorTexture.Dispose();
        }
    }
}
