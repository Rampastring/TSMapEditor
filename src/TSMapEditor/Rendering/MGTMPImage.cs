using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;
using TSMapEditor.CCEngine;

namespace TSMapEditor.Rendering
{
    /// <summary>
    /// Interface for a single cell of a tile; sub-tile of a full TMP.
    /// </summary>
    public interface ISubTileImage
    {
        TmpImage TmpImage { get; }
    }

    /// <summary>
    /// A MonoGame-drawable TMP image.
    /// Contains graphics and information for a single cell (sub-tile of a full TMP).
    /// </summary>
    public class MGTMPImage : ISubTileImage
    {
        public MGTMPImage(GraphicsDevice gd, TmpImage tmpImage, XNAPalette palette, int tileSetId)
        {
            if (tmpImage != null)
            {
                TmpImage = tmpImage;
                Palette = palette;
                Texture = TextureFromTmpImage_Paletted(gd, tmpImage);

                if (tmpImage.ExtraGraphicsColorData != null && tmpImage.ExtraGraphicsColorData.Length > 0)
                {
                    ExtraTexture = TextureFromExtraTmpData_Paletted(gd, tmpImage);
                }
            }

            TileSetId = tileSetId;
        }

        public Texture2D Texture { get; }
        public Texture2D ExtraTexture { get; }

        public int TileSetId { get; }
        public TmpImage TmpImage { get; private set; }
        private XNAPalette Palette { get; set; }

        public void Dispose()
        {
            if (Texture == null && ExtraTexture == null)
                return;

            // Workaround for a bug in SharpDX where it can crash when freeing a texture
            try
            {
                Texture?.Dispose();
                ExtraTexture?.Dispose();
            }
            catch (InvalidOperationException)
            {
                Logger.Log($"Failed to free a TMP texture! TileSet: {TileSetId}");
            }
        }

        private Texture2D TextureFromTmpImage_Paletted(GraphicsDevice graphicsDevice, TmpImage image)
        {
            Texture2D texture = new Texture2D(graphicsDevice, Constants.CellSizeX, Constants.CellSizeY, false, SurfaceFormat.Alpha8);
            byte[] colorData = new byte[Constants.CellSizeX * Constants.CellSizeY];

            int tmpPixelIndex = 0;
            int w = 4;
            for (int i = 0; i < Constants.CellSizeY; i++)
            {
                int xPos = Constants.CellSizeY - (w / 2);
                for (int x = 0; x < w; x++)
                {
                    if (image.ColorData[tmpPixelIndex] > 0)
                    {
                        colorData[i * Constants.CellSizeX + xPos] = image.ColorData[tmpPixelIndex];
                    }

                    xPos++;
                    tmpPixelIndex++;
                }

                if (i < (Constants.CellSizeY / 2) - 1)
                    w += 4;
                else
                    w -= 4;
            }

            texture.SetData(colorData);
            return texture;
        }

        private Texture2D TextureFromExtraTmpData_Paletted(GraphicsDevice graphicsDevice, TmpImage image)
        {
            int width = (int)image.ExtraWidth;
            int height = (int)image.ExtraHeight;

            var texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Alpha8);
            byte[] colorData = new byte[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width) + x;

                    if (image.ExtraGraphicsColorData[index] > 0)
                    {
                        colorData[index] = image.ExtraGraphicsColorData[index];
                    }
                }
            }

            texture.SetData(colorData);
            return texture;
        }

        
        public Texture2D TextureFromTmpImage_RGBA(GraphicsDevice graphicsDevice)
        {
            Texture2D texture = new Texture2D(graphicsDevice, Constants.CellSizeX, Constants.CellSizeY, false, SurfaceFormat.Color);
            Color[] colorData = new Color[Constants.CellSizeX * Constants.CellSizeY];
            for (int i = 0; i < colorData.Length; i++)
            {
                colorData[i] = Color.Transparent;
            }

            int tmpPixelIndex = 0;
            int w = 4;
            for (int i = 0; i < Constants.CellSizeY; i++)
            {
                int xPos = Constants.CellSizeY - (w / 2);
                for (int x = 0; x < w; x++)
                {
                    if (TmpImage.ColorData[tmpPixelIndex] > 0)
                    {
                        colorData[i * Constants.CellSizeX + xPos] = Palette.Data[TmpImage.ColorData[tmpPixelIndex]].ToXnaColor();
                    }
                    
                    xPos++;
                    tmpPixelIndex++;
                }

                if (i < (Constants.CellSizeY / 2) - 1)
                    w += 4;
                else
                    w -= 4;
            }

            texture.SetData(colorData);
            return texture;
        }

        private Texture2D TextureFromExtraTmpData_RGBA(GraphicsDevice graphicsDevice, TmpImage image, Palette palette)
        {
            int width = (int)image.ExtraWidth;
            int height = (int)image.ExtraHeight;

            var texture = new Texture2D(graphicsDevice, width, height);
            Color[] colorData = new Color[width * height];
            for (int i = 0; i < colorData.Length; i++)
            {
                colorData[i] = Color.Transparent;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width) + x;

                    if (image.ExtraGraphicsColorData[index] > 0)
                    {
                        colorData[index] = palette.Data[image.ExtraGraphicsColorData[index]].ToXnaColor();
                    }
                }
            }

            texture.SetData(colorData);
            return texture;
        }

        public Texture2D GetPaletteTexture(bool useLighting)
        {
            return Palette.GetTexture(useLighting);
        }
    }
}
