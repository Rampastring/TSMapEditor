using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TSMapEditor.CCEngine
{
    /// <summary>
    /// Layer on top of the game palette to enable rendering paletted textures with it.
    /// </summary>
    public class XNAPalette : Palette
    {
        public XNAPalette(string name, byte[] buffer, GraphicsDevice graphicsDevice) : base(name, buffer)
        {
            PaletteWithLight = new(name, buffer);
            Texture = CreateTexture(graphicsDevice, this);
            TextureWithLight = CreateTexture(graphicsDevice, PaletteWithLight);
        }

        private Texture2D Texture;
        private Texture2D TextureWithLight;
        private Palette PaletteWithLight;

        public Texture2D GetTexture(bool subjectToLighting)
        {
            return subjectToLighting ? TextureWithLight : Texture;
        }

        public Palette GetPalette(bool subjectToLighting)
        {
            return subjectToLighting ? PaletteWithLight : this;
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

        public void ApplyLighting(Color color)
        {
            Color[] colorData = new Color[LENGTH];
            for (int i = 1; i < LENGTH - 8; i++)
            {
                RGBColor newColor = new
                (
                    (byte)((Data[i].R * color.R) / 255),
                    (byte)((Data[i].G * color.G) / 255),
                    (byte)((Data[i].B * color.B) / 255)
                );
                PaletteWithLight.Data[i] = newColor;
                colorData[i] = newColor.ToXnaColor();
            }

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
