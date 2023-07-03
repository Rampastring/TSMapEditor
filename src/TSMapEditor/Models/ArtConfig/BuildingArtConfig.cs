using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.UI;

namespace TSMapEditor.Models.ArtConfig
{
    /// <summary>
    /// Represents a building foundation and contains its outline edges.
    /// </summary>
    public class Foundation
    {
        /// <summary>
        /// Ares Foundation.N entries, list of cell grid coordinates starting at 0,0 (top left).
        /// </summary>
        public Point2D[] FoundationCells { get; set; }

        /// <summary>
        /// Generated list of edges defining foundation outline.
        /// </summary>
        public Point2D[][] Edges { get; set; } = new Point2D[][] { Array.Empty<Point2D>() };

        public void ReadFromIniSection(IniSection iniSection)
        {
            if (iniSection == null)
                return;

            var foundationCells = new List<Point2D>();

            int i = 0;
            while (true)
            {
                // The dot is intentional, Ares expects Foundation.N=X,Y syntax
                string value = iniSection.GetStringValue("Foundation." + i, null);
                if (string.IsNullOrEmpty(value))
                    break;

                string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    throw new INIConfigException($"Building type \"{iniSection.SectionName}\" has invalid custom \"{"Foundation." + i}\"");

                foundationCells.Add(new Point2D(Conversions.IntFromString(parts[0], -1), Conversions.IntFromString(parts[1], -1)));
                i++;
            }

            FoundationCells = foundationCells.ToArray();
        }

        /// <summary>
        /// Populates foundation cells for a typical scenario - foundation is a rectangle.
        /// </summary>
        public void FromXY(int width, int height)
        {
            var foundationCells = new List<Point2D>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    foundationCells.Add(new Point2D(x, y));
                }
            }

            FoundationCells = foundationCells.ToArray();
        }

        /// <summary>
        /// Generates outline edges for own foundation in two runs, creating horizontal
        /// and vertical edges separately.
        /// </summary>
        /// <param name="maxWidth">Maximum possible length of a horizontal edge in cells.</param>
        /// <param name="maxHeight">Maximum possible length of a vertical edge in cells.</param>
        public void CreateCustomEdges(int maxWidth, int maxHeight)
        {
            // Create a padded map of every cell occupied by building foundation.
            var occupyCells = new int[maxWidth + 2, maxHeight + 2];
            foreach (var cell in FoundationCells)
                occupyCells[cell.X + 1, cell.Y + 1] = 1;

            var edges = new List<Point2D[]>();

            // Horizontal edge scanning.
            for (int y = 0; y < maxHeight + 1; y++)
            {
                int startX = -1;
                int endX = -1;
                for (int x = 0; x < maxWidth + 2; x++)
                {
                    // If we found an edge...
                    if (occupyCells[x, y] + occupyCells[x, y + 1] == 1)
                    {
                        // ...and we're not continuing an existing edge, then start a new one.
                        if (startX == -1)
                        {
                            startX = x - 1;
                            endX = x;
                        }
                        // ... and we're continuing an existing edge, then make it longer.
                        else
                        {
                            endX++;
                        }
                    }
                    // ...otherwise end and save the current edge if there's one.
                    else if (startX != -1)
                    {
                        edges.Add(new Point2D[] { new Point2D(startX, y), new Point2D(endX, y) });
                        startX = -1;
                    }
                }
            }

            // Vertical edge scanning, the same idea as with horizontal edges.
            for (int x = 0; x < maxWidth + 1; x++)
            {
                int startY = -1;
                int endY = -1;
                for (int y = 0; y < maxHeight + 2; y++)
                {
                    // If we found an edge...
                    if (occupyCells[x, y] + occupyCells[x + 1, y] == 1)
                    {
                        // ...and we're not continuing an existing edge, then start a new one.
                        if (startY == -1)
                        {
                            startY = y - 1;
                            endY = y;
                        }
                        // ... and we're continuing an existing edge, then make it longer.
                        else
                        {
                            endY++;
                        }
                    }
                    // ...otherwise end and save the current edge if there's one.
                    else if (startY != -1)
                    {
                        edges.Add(new Point2D[] { new Point2D(x, startY), new Point2D(x, endY) });
                        startY = -1;
                    }
                }
            }

            Edges = edges.ToArray();
        }

        /// <summary>
        /// Generates outline edges for a typical rectangle foundation.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void CreateRectangleEdges(int width, int height)
        {
            Edges = new Point2D[][] {
                new Point2D[] { new Point2D(0, 0), new Point2D(width, 0) },
                new Point2D[] { new Point2D(0, 0), new Point2D(0, height) },
                new Point2D[] { new Point2D(width, 0), new Point2D(width, height) },
                new Point2D[] { new Point2D(0, height), new Point2D(width, height) }
            };
        }
    }

    public class BuildingArtConfig : IArtConfig
    {
        public BuildingArtConfig() { }

        public int FoundationX { get; set; }
        public int FoundationY { get; set; }
        public Foundation Foundation { get; set; }
        public int Height { get; set; }
        public bool Remapable { get; set; }
        public bool NewTheater { get; set; }
        public bool TerrainPalette { get; set; }
        public bool Theater { get; set; }
        public string Image { get; set; }
        public string BibShape { get; set; }

        /// <summary>
        /// Palette override introduced in Red Alert 2.
        /// </summary>
        public string Palette { get; set; }

        public void ReadFromIniSection(IniSection iniSection)
        {
            if (iniSection == null)
                return;

            string foundationString = iniSection.GetStringValue("Foundation", string.Empty);
            if (!string.IsNullOrWhiteSpace(foundationString))
            {
                if (foundationString == "Custom")
                {
                    FoundationX = iniSection.GetIntValue("Foundation.X", -1);
                    FoundationY = iniSection.GetIntValue("Foundation.Y", -1);

                    if (FoundationX < 0 || FoundationY < 0)
                    {
                        throw new InvalidOperationException("Invalid custom Foundation specified in Art.ini section " + iniSection.SectionName);
                    }

                    Foundation = new();
                    Foundation.ReadFromIniSection(iniSection);
                    Foundation.CreateCustomEdges(FoundationX, FoundationY);
                }
                else
                {

                    string[] foundationParts = foundationString.ToLower().Split('x');
                    if (foundationParts.Length != 2)
                    {
                        throw new InvalidOperationException("Invalid Foundation= specified in Art.ini section " + iniSection.SectionName);
                    }

                    FoundationX = Conversions.IntFromString(foundationParts[0], 0);
                    FoundationY = Conversions.IntFromString(foundationParts[1], 0);

                    Foundation = new();
                    if (FoundationX != 0 && FoundationY != 0)
                    {
                        Foundation.FromXY(FoundationX, FoundationY);
                        Foundation.CreateRectangleEdges(FoundationX, FoundationY);
                    }
                }
            }

            Height = iniSection.GetIntValue(nameof(Height), Height);
            Remapable = iniSection.GetBooleanValue(nameof(Remapable), Remapable);
            NewTheater = iniSection.GetBooleanValue(nameof(NewTheater), NewTheater);
            TerrainPalette = iniSection.GetBooleanValue(nameof(TerrainPalette), TerrainPalette);
            Theater = iniSection.GetBooleanValue(nameof(Theater), Theater);
            Image = iniSection.GetStringValue(nameof(Image), Image);
            BibShape = iniSection.GetStringValue(nameof(BibShape), BibShape);
            Palette = iniSection.GetStringValue(nameof(Palette), Palette);
        }

        public void DoForFoundationCoords(Action<Point2D> action)
        {
            foreach (var cell in Foundation.FoundationCells)
                action(cell);
        }

        public void DoForFoundationCoordsOrOrigin(Action<Point2D> action)
        {
            if (FoundationY == 0 || FoundationX == 0)
                action(new Point2D(0, 0));
            else
                DoForFoundationCoords(action);
        }
    }
}
