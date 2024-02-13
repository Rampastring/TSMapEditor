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
            CreateTexture(graphicsDevice);
        }

        public Texture2D Texture;

        private void CreateTexture(GraphicsDevice graphicsDevice)
        {
            Texture = new Texture2D(graphicsDevice, LENGTH, 1, false, SurfaceFormat.Color);

            Color[] colorData = new Color[LENGTH];
            colorData[0] = Color.Transparent;
            for (int i = 1; i < colorData.Length; i++)
            {
                colorData[i] = Data[i].ToXnaColor();
            }
            Texture.SetData(colorData);
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
