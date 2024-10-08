using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSMapEditor.Models;

namespace TSMapEditor.CCEngine
{
    /// <summary>
    /// Layer on top of the game palette to enable rendering paletted textures with it.
    /// </summary>
    public class XNAPalette : Palette
    {
        public XNAPalette(string name, byte[] buffer, GraphicsDevice graphicsDevice, bool hasFullyBrightColors) : base(name, buffer)
        {
            PaletteWithLight = new(name, buffer);
            Texture = CreateTexture(graphicsDevice, this);
            TextureWithLight = CreateTexture(graphicsDevice, PaletteWithLight);
            HasFullyBrightColors = hasFullyBrightColors;
        }

        private Texture2D Texture;
        private Texture2D TextureWithLight;
        private Palette PaletteWithLight;
        private bool HasFullyBrightColors;

        public Texture2D GetTexture()
        {
            return Texture;
        }

        public Palette GetPalette()
        {
            return this;
        }

        private Texture2D CreateTexture(GraphicsDevice graphicsDevice, Palette palette)
        {
            Texture2D texture = new(graphicsDevice, LENGTH, 1, false, SurfaceFormat.Color);

            Color[] colorData = new Color[LENGTH];
            colorData[0] = Color.Transparent;
            for (int i = 1; i < colorData.Length; i++)
            {
                colorData[i] = palette.Data[i].ToXnaColor();
            }
            texture.SetData(colorData);

            return texture;
        }

        private void AdjustColor(int i, Color[] colorData, MapColor color)
        {
            RGBColor newColor = Data[i] * color;
            PaletteWithLight.Data[i] = newColor;
            colorData[i] = newColor.ToXnaColor();
        }

        public void ApplyLighting(MapColor color)
        {
            Color[] colorData = new Color[LENGTH];
            int last = HasFullyBrightColors ? LENGTH - 16 : 255;

            for (int i = 1; i < last; i++)
                AdjustColor(i, colorData, color);

            AdjustColor(255, colorData, color);

            TextureWithLight.SetData(colorData);
        }
    }

    /// <summary>
    /// A C&C Tiberian Sun or Red Alert 2 palette.
    /// </summary>
    public class Palette
    {
        protected const int LENGTH = 256;

        public Palette(string name, byte[] buffer)
        {
            Name = name;
            Data = new RGBColor[LENGTH];
            Parse(buffer);
        }

        public readonly string Name;

        public RGBColor[] Data;

        public void Parse(byte[] buffer)
        {
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = new RGBColor(buffer, i * 3, 2);
            }
        }
    }
}
