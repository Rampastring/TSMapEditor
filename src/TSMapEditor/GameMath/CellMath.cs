using Microsoft.Xna.Framework;
using TSMapEditor.Models;

namespace TSMapEditor.GameMath
{
    /// <summary>
    /// Provides static methods for calculating math related to cells.
    /// </summary>
    public static class CellMath
    {
        public static Point2D CellTopLeftPointFromCellCoords_NoBaseline(Point2D cellCoords, Map map)
        {
            int cx = (cellCoords.X - 1) * (Constants.CellSizeX / 2);
            int cy = (cellCoords.X - 1) * (Constants.CellSizeY / 2);

            int diff = map.Size.X - cellCoords.Y;
            cx += diff * (Constants.CellSizeX / 2);
            cy -= diff * (Constants.CellSizeY / 2);

            return new Point2D(cx, cy);
        }

        public static Point2D CellTopLeftPointFromCellCoords(Point2D cellCoords, Map map)
        {
            Point2D noBaseline = CellTopLeftPointFromCellCoords_NoBaseline(cellCoords, map);

            // Include height baseline, this function is typically used for graphics
            return new Point2D(noBaseline.X, noBaseline.Y + Constants.MapYBaseline);
        }

        public static Point2D CellTopLeftPointFromCellCoords_3D_NoBaseline(Point2D cellCoords, Map map)
        {
            var cell = map.GetTile(cellCoords);

            if (cell == null)
                return CellTopLeftPointFromCellCoords_NoBaseline(cellCoords, map);

            Point2D preHeightCoords = CellTopLeftPointFromCellCoords_NoBaseline(cellCoords, map);
            return preHeightCoords - new Point2D(0, cell.Level * Constants.CellHeight);
        }

        public static Point2D CellTopLeftPointFromCellCoords_3D(Point2D cellCoords, Map map)
        {
            var cell = map.GetTile(cellCoords);

            if (cell == null)
                return CellTopLeftPointFromCellCoords(cellCoords, map);

            Point2D preHeightCoords = CellTopLeftPointFromCellCoords(cellCoords, map);
            return preHeightCoords - new Point2D(0, cell.Level * Constants.CellHeight);
        }

        public static Point2D CellCenterPointFromCellCoords_NoBaseline(Point2D cellCoords, Map map)
            => CellTopLeftPointFromCellCoords_NoBaseline(cellCoords, map) + new Point2D(Constants.CellSizeX / 2, Constants.CellSizeY / 2);

        public static Point2D CellCenterPointFromCellCoords(Point2D cellCoords, Map map)
            => CellTopLeftPointFromCellCoords(cellCoords, map) + new Point2D(Constants.CellSizeX / 2, Constants.CellSizeY / 2);

        public static Point2D CellCenterPointFromCellCoords_3D_NoBaseline(Point2D cellCoords, Map map)
        {
            var cell = map.GetTile(cellCoords);

            if (cell == null)
                return CellCenterPointFromCellCoords_NoBaseline(cellCoords, map);

            Point2D preHeightCoords = CellCenterPointFromCellCoords_NoBaseline(cellCoords, map);
            return preHeightCoords - new Point2D(0, cell.Level * Constants.CellHeight);
        }

        public static Point2D CellCenterPointFromCellCoords_3D(Point2D cellCoords, Map map)
        {
            var cell = map.GetTile(cellCoords);

            if (cell == null)
                return CellCenterPointFromCellCoords(cellCoords, map);

            Point2D preHeightCoords = CellCenterPointFromCellCoords(cellCoords, map);
            return preHeightCoords - new Point2D(0, cell.Level * Constants.CellHeight);
        }

        public static Point2D CellCoordsFromPixelCoords_2D(Point2D pixelCoords, Map map)
        {
            // It's likely possible to find a more efficient algorithm for this

            // Find initial cell
            int cx = 1;
            int cy = map.Size.X;

            int xm = pixelCoords.X / Constants.CellSizeX;
            cx += xm;
            cy -= xm;

            int ym = pixelCoords.Y / Constants.CellSizeY;
            cx += ym;
            cy += ym;

            int rx = pixelCoords.X % Constants.CellSizeX;
            int ry = pixelCoords.Y % Constants.CellSizeY;

            if (rx + ry * 2 < Constants.CellSizeY)
                cx--;
            else if (rx + 2 * (ry - (Constants.CellSizeY / 2)) > Constants.CellSizeX)
                cx++;
            else if (rx > Constants.CellSizeY + ry * 2)
                cy--;
            else if (rx < (ry - Constants.CellSizeY / 2) * 2)
                cy++;

            return new Point2D(cx, cy);
        }

        /// <summary>
        /// Calculates and returns the game-logical coordinates of a cell at given pixel-based coordinates in the world.
        /// </summary>
        /// <param name="pixelCoords">The pixel-based location.</param>
        /// <param name="map">The map.</param>
        /// <param name="seethrough">Whether we should "see through" walls in the game world, such as cliffs,
        /// allowing us to reach cells that are behind cliffs.</param>
        /// <param name="includeHeightBaseline">Should the "height padding area" at the top of the map be taken into account?</param>
        public static Point2D CellCoordsFromPixelCoords(Point2D pixelCoords, Map map, bool seethrough = true)
        {
            Point2D coords2D = CellCoordsFromPixelCoords_2D(pixelCoords, map);

            if (Constants.IsFlatWorld)
                return coords2D;

            Point2D nearestCenterCoords = new Point2D(-1, -1);
            float nearestDistance = float.MaxValue;

            const int threshold = 1;
            const int scanSize = 3;


            // Take isometric perspective into account
            Vector2 worldCoordsNonIsometric = new Point2D(pixelCoords.X / 2, pixelCoords.Y).ToXNAVector();

            // Scan all cells that are deemed to be "close enough" to the initial cell
            // to see if our cursor is on the cell
            for (int y = -scanSize; y <= scanSize; y++)
            {
                for (int x = -scanSize; x <= scanSize; x++)
                {
                    // traverse height
                    for (int i = 0; i <= Constants.MaxMapHeightLevel; i++)
                    {
                        var otherCellCoords = coords2D + new Point2D(x, y) + new Point2D(i, i);
                        var otherCell = map.GetTile(otherCellCoords);

                        if (otherCell != null)
                        {
                            var centerCoords = CellCenterPointFromCellCoords_3D_NoBaseline(otherCellCoords, map);

                            // Take isometric perspective into account
                            centerCoords = new Point2D(centerCoords.X / 2, centerCoords.Y);

                            float distance = Vector2.Distance(centerCoords.ToXNAVector(), worldCoordsNonIsometric);
                            if (distance <= nearestDistance)
                            {
                                bool acceptCell = true;

                                if (!seethrough)
                                {
                                    // If seethrough is disabled and the "currently nearest" cell is below the evaluated cell in 2D coords,
                                    // only accept the evaluated cell as nearest if it is significantly closer than the nearest cell.
                                    if (otherCellCoords.X + otherCellCoords.Y < nearestCenterCoords.X + nearestCenterCoords.Y)
                                    {
                                        if (nearestDistance - distance <= threshold)
                                        {
                                            acceptCell = false;
                                        }
                                    }
                                }

                                if (acceptCell)
                                {
                                    nearestDistance = distance;
                                    nearestCenterCoords = otherCellCoords;
                                }
                            }

                            // If seethrough is disabled, we need to additionally check if the evaluated cell could be the closest cell at
                            // any potential height level, if it'd be below the current nearest cell in 2D mode
                            if (!seethrough && otherCellCoords.X + otherCellCoords.Y > nearestCenterCoords.X + nearestCenterCoords.Y)
                            {
                                for (int h = 0; h < otherCell.Level; h++)
                                {
                                    centerCoords = CellCenterPointFromCellCoords_NoBaseline(otherCellCoords, map);
                                    centerCoords = new Point2D(centerCoords.X / 2, centerCoords.Y - Constants.CellHeight * h);

                                    distance = Vector2.Distance(centerCoords.ToXNAVector(), worldCoordsNonIsometric);

                                    if (distance <= nearestDistance)
                                    {
                                        nearestDistance = distance;
                                        nearestCenterCoords = otherCellCoords;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return nearestCenterCoords;
        }

        public static Point2D GetSubCellOffset(SubCell subcell)
        {
            switch (subcell)
            {
                case SubCell.Top:
                    return new Point2D(0, Constants.CellSizeY / -4);

                case SubCell.Bottom:
                    return new Point2D(0, Constants.CellSizeY / 4);

                case SubCell.Left:
                    return new Point2D(Constants.CellSizeX / -4, 0);

                case SubCell.Right:
                    return new Point2D(Constants.CellSizeX / 4, 0);

                default:
                    return Point2D.Zero;
            }
        }
    }
}
