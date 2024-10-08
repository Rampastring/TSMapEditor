using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;

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
        public Point2D[] FoundationCells { get; set; } = Array.Empty<Point2D>();

        /// <summary>
        /// Generated list of edges defining foundation outline.
        /// </summary>
        public Point2D[][] Edges { get; set; } = new Point2D[][] { Array.Empty<Point2D>() };

        public int Width { get; set; }
        public int Height { get; set; }

        public void ReadFromIniSection(IniSection iniSection)
        {
            string foundationString = iniSection.GetStringValue("Foundation", string.Empty);
            if (string.IsNullOrWhiteSpace(foundationString))
                return;

            if (foundationString == "Custom")
            {
                Width = iniSection.GetIntValue("Foundation.X", -1);
                Height = iniSection.GetIntValue("Foundation.Y", -1);

                if (Width < 0 || Height < 0)
                    throw new InvalidOperationException("Invalid custom Foundation specified in Art.ini section " + iniSection.SectionName);

                CellsFromINI(iniSection);
                Edges = Helpers.CreateEdges(Width, Height, FoundationCells);
            }
            else
            {
                string[] foundationParts = foundationString.ToLower().Split('x');
                if (foundationParts.Length != 2)
                    throw new InvalidOperationException("Invalid Foundation= specified in Art.ini section " + iniSection.SectionName);

                Width = Conversions.IntFromString(foundationParts[0], 0);
                Height = Conversions.IntFromString(foundationParts[1], 0);

                if (Width != 0 && Height != 0)
                {
                    CellsFromXY(Width, Height);
                    CreateRectangleEdges(Width, Height);
                }
            }
        }

        /// <summary>
        /// Populates foundation cells for a typical scenario - foundation is a rectangle.
        /// </summary>
        public void CellsFromXY(int width, int height)
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
        /// Reads foundation cells from an INI section.
        /// </summary>
        public void CellsFromINI(IniSection iniSection)
        {
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
        /// Generates outline edges for a typical rectangle foundation.
        /// </summary>
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

    public class BuildingAnimArtConfig
    {
        public void ReadFromIniSection(IniSection iniSection, string name)
        {
            ININame = iniSection.GetStringValue(name, ININame);
            X = iniSection.GetIntValue($"{name}X", X);
            Y = iniSection.GetIntValue($"{name}Y", Y);
            YSort = iniSection.GetIntValue($"{name}YSort", YSort);
            ZAdjust = iniSection.GetIntValue($"{name}ZAdjust", ZAdjust);
        }

        public string ININame { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int YSort { get; set; }
        public int ZAdjust { get; set; }
    }

    public class PowerUpAnimArtConfig
    {
        public void ReadFromIniSection(IniSection iniSection, int i)
        {
            ININame = iniSection.GetStringValue($"PowerUp{i}Anim", ININame);
            LocXX = iniSection.GetIntValue($"PowerUp{i}LocXX", LocXX);
            LocYY = iniSection.GetIntValue($"PowerUp{i}LocYY", LocYY);
            LocZZ = iniSection.GetIntValue($"PowerUp{i}LocZZ", LocZZ);
            YSort = iniSection.GetIntValue($"PowerUp{i}LocXX", YSort);
        }

        public string ININame { get; set; }
        public int LocXX { get; set; }
        public int LocYY { get; set; }
        public int LocZZ { get; set; }
        public int YSort { get; set; }
    }

    public class BuildingArtConfig : IArtConfig
    {
        public BuildingArtConfig() { }

        public Foundation Foundation { get; set; } = new();
        public int Height { get; set; }
        public bool Remapable => !TerrainPalette;
        public bool NewTheater { get; set; }
        public bool TerrainPalette { get; set; }
        public bool Theater { get; set; }
        public string Image { get; set; }
        public string BibShape { get; set; }
        public List<BuildingAnimArtConfig> BuildingAnimConfigs { get; set; } = new();
        public List<PowerUpAnimArtConfig> PowerUpAnimConfigs { get; set; } = new();
        public AnimType[] Anims { get; set; } = Array.Empty<AnimType>();
        public AnimType[] PowerUpAnims { get; set; } = Array.Empty<AnimType>();
        public AnimType TurretAnim { get; set; }

        /// <summary>
        /// Palette override introduced in Red Alert 2.
        /// </summary>
        public string Palette { get; set; }

        private static readonly List<(string Name, string[] Suffixes)> BuildingAnimClasses = new()
        {
            ("ActiveAnim", new [] { "", "Two", "Three", "Four" }),
            ("IdleAnim", new [] { "", "Two" }),
            ("SuperAnim", new [] { "" })
        };

        public void ReadFromIniSection(IniSection iniSection)
        {
            if (iniSection == null)
                return;

            Foundation.ReadFromIniSection(iniSection);
            Height = iniSection.GetIntValue(nameof(Height), Height);
            NewTheater = iniSection.GetBooleanValue(nameof(NewTheater), NewTheater);
            TerrainPalette = iniSection.GetBooleanValue(nameof(TerrainPalette), TerrainPalette);
            Theater = iniSection.GetBooleanValue(nameof(Theater), Theater);
            Image = iniSection.GetStringValue(nameof(Image), Image);
            BibShape = iniSection.GetStringValue(nameof(BibShape), BibShape);
            Palette = iniSection.GetStringValue(nameof(Palette), Palette);

            var anims = new List<BuildingAnimArtConfig>();

            foreach (var animClass in BuildingAnimClasses)
            {
                foreach (var suffix in animClass.Suffixes)
                {
                    string animTypeName = iniSection.GetStringValue(animClass.Name + suffix, null);
                    if (string.IsNullOrEmpty(animTypeName))
                        break;

                    var animConfig = new BuildingAnimArtConfig();
                    animConfig.ReadFromIniSection(iniSection, animClass.Name + suffix);
                    anims.Add(animConfig);
                }
            }

            BuildingAnimConfigs = anims;
        }

        public void ReadUpgradeAnims(int upgradeCount, IniSection iniSection)
        {
            if (upgradeCount > PowerUpAnimConfigs.Count)
            {
                for (int i = PowerUpAnimConfigs.Count; i < upgradeCount; i++)
                    PowerUpAnimConfigs.Add(new PowerUpAnimArtConfig());
            }

            for (int i = 0; i < upgradeCount; i++)
            {
                PowerUpAnimConfigs[i].ReadFromIniSection(iniSection, i + 1);
            }
        }

        /// <summary>
        /// Performs an action for all cells of the building's foundation.
        /// Does NOT do anything for buildings with 0x0 foundations.
        /// If the action is also desired for them, call
        /// <see cref="DoForFoundationCoordsOrOrigin(Action{Point2D})"/> instead.
        /// </summary>
        public void DoForFoundationCoords(Action<Point2D> action)
        {
            if (Foundation.FoundationCells == null)
                return;

            foreach (var cell in Foundation.FoundationCells)
                action(cell);
        }

        /// <summary>
        /// Performs an action for all cells of the building's foundation.
        /// If the building's foundation is 0x0, then performs the action
        /// for the building's origin cell (offet 0,0) only.
        /// </summary>
        public void DoForFoundationCoordsOrOrigin(Action<Point2D> action)
        {
            if (Foundation.Width == 0 || Foundation.Height == 0)
                action(new Point2D(0, 0));
            else
                DoForFoundationCoords(action);
        }
    }
}
