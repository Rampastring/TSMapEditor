using System;
using TSMapEditor.GameMath;

namespace TSMapEditor.Rendering
{
    /// <summary>
    /// Interface for a full tile image (containing all sub-tiles).
    /// </summary>
    public interface ITileImage
    {
        /// <summary>
        /// Width of the tile in cells.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Height of the tile in cells.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// The index of the tile's tileset.
        /// </summary>
        int TileSetId { get; }

        /// <summary>
        /// The index of the tile within its tileset.
        /// </summary>
        int TileIndexInTileSet { get; }

        /// <summary>
        /// The unique ID of this tile within all tiles in the game.
        /// </summary>
        int TileID { get; }

        int SubTileCount { get; }

        ISubTileImage GetSubTile(int index);

        Point2D? GetSubTileCoordOffset(int index);
    }

    /// <summary>
    /// Contains graphics for a single full TMP (all sub-tiles / all cells).
    /// </summary>
    public class TileImage : ITileImage
    {
        public TileImage(int width, int height, int tileSetId, int tileIndex, int tileId, MGTMPImage[] tmpImages)
        {
            Width = width;
            Height = height;
            TileSetId = tileSetId;
            TileIndexInTileSet = tileIndex;
            TileID = tileId;
            TMPImages = tmpImages;
        }

        /// <summary>
        /// Width of the tile in cells.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Height of the tile in cells.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// The index of the tile set.
        /// </summary>
        public int TileSetId { get; set; }

        /// <summary>
        /// The index of the tile within its tileset.
        /// </summary>
        public int TileIndexInTileSet { get; set; }

        /// <summary>
        /// The unique ID of this tile within all tiles in the game.
        /// </summary>
        public int TileID { get; set; }

        public ISubTileImage GetSubTile(int index) => TMPImages[index];

        public Point2D? GetSubTileCoordOffset(int index)
        {
            if (TMPImages[index] == null)
                return null;

            int x = index % Width;
            int y = index / Width;
            return new Point2D(x, y);
        }

        public int SubTileCount => TMPImages.Length;

        public MGTMPImage[] TMPImages { get; set; }

        /// <summary>
        /// Calculates and returns the width of this full tile image.
        /// </summary>
        public int GetWidth(out int outMinX)
        {
            outMinX = 0;

            if (TMPImages == null)
                return 0;

            int maxX = int.MinValue;
            int minX = int.MaxValue;

            for (int i = 0; i < TMPImages.Length; i++)
            {
                if (TMPImages[i] == null)
                    continue;

                var tmpData = TMPImages[i].TmpImage;
                if (tmpData == null)
                    continue;

                if (tmpData.X < minX)
                    minX = tmpData.X;

                int cellRightXCoordinate = tmpData.X + Constants.CellSizeX;
                if (cellRightXCoordinate > maxX)
                    maxX = cellRightXCoordinate;

                if (TMPImages[i].ExtraTexture != null)
                {
                    int extraRightXCoordinate = tmpData.X + TMPImages[i].TmpImage.XExtra + TMPImages[i].ExtraTexture.Width;
                    if (extraRightXCoordinate > maxX)
                        maxX = extraRightXCoordinate;
                }
            }

            outMinX = minX;
            return maxX - minX;
        }

        /// <summary>
        /// Calculates and returns the height of this full tile image.
        /// </summary>
        public int GetHeight()
        {
            if (TMPImages == null)
                return 0;

            int top = int.MaxValue;
            int bottom = int.MinValue;

            for (int i = 0; i < TMPImages.Length; i++)
            {
                if (TMPImages[i] == null)
                    continue;

                var tmpData = TMPImages[i].TmpImage;
                if (tmpData == null)
                    continue;

                int heightOffset = Constants.CellHeight * tmpData.Height;

                int cellTop = tmpData.Y - heightOffset;
                int cellBottom = cellTop + Constants.CellSizeY;

                if (cellTop < top)
                    top = cellTop;

                if (cellBottom > bottom)
                    bottom = cellBottom;

                if (TMPImages[i].ExtraTexture != null)
                {
                    int extraCellTop = tmpData.YExtra - heightOffset;
                    int extraCellBottom = extraCellTop + TMPImages[i].ExtraTexture.Height;

                    if (extraCellTop < top)
                        top = extraCellTop;

                    if (extraCellBottom > bottom)
                        bottom = extraCellBottom;
                }
            }

            return bottom - top;
        }

        public int GetYOffset()
        {
            int height = GetHeight();

            // return 0;

            int yOffset = 0;

            int maxTopCoord = int.MaxValue;
            int maxBottomCoord = int.MinValue;

            for (int i = 0; i < TMPImages.Length; i++)
            {
                if (TMPImages[i] == null)
                    continue;

                var tmpData = TMPImages[i].TmpImage;
                if (tmpData == null)
                    continue;

                int heightOffset = Constants.CellHeight * tmpData.Height;
                int cellTopCoord = tmpData.Y - heightOffset;
                int cellBottomCoord = tmpData.Y + Constants.CellSizeY - heightOffset;

                if (cellTopCoord < maxTopCoord)
                    maxTopCoord = cellTopCoord;

                if (cellBottomCoord > maxBottomCoord)
                    maxBottomCoord = cellBottomCoord;
            }

            for (int i = 0; i < TMPImages.Length; i++)
            {
                if (TMPImages[i] == null)
                    continue;

                var tmpData = TMPImages[i].TmpImage;
                if (tmpData == null)
                    continue;

                if (TMPImages[i].ExtraTexture != null)
                {
                    int heightOffset = Constants.CellHeight * tmpData.Height;

                    int extraTopCoord = TMPImages[i].TmpImage.YExtra - heightOffset;
                    int extraBottomCoord = TMPImages[i].TmpImage.YExtra + TMPImages[i].ExtraTexture.Height - heightOffset;

                    if (extraTopCoord < maxTopCoord)
                        maxTopCoord = extraTopCoord;

                    if (extraBottomCoord > maxBottomCoord)
                        maxBottomCoord = extraBottomCoord;
                }
            }

            if (maxTopCoord < 0)
                yOffset = -maxTopCoord;
            else if (maxBottomCoord > height)
                yOffset = -(maxBottomCoord - height);

            return yOffset;
        }

        public void Dispose()
        {
            Array.ForEach(TMPImages, tmp =>
            {
                if (tmp != null)
                    tmp.Dispose();
            });
        }
    }
}
