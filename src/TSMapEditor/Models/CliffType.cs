using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using TSMapEditor.GameMath;
using TSMapEditor.UI;

namespace TSMapEditor.Models
{
    public enum CliffSide
    {
        Front,
        Back
    }

    public readonly struct CliffConnectionPoint
    {
        /// <summary>
        /// Index of the connection point, 0 or 1
        /// </summary>
        public int Index { get; init; }

        /// <summary>
        /// Offset of this connection point relative to the tile's (0,0) point
        /// </summary>
        public Point2D CoordinateOffset { get; init; }

        /// <summary>
        /// Mask of bits determining which way the connection point "faces".
        /// Ordered in the same way as the Directions enum
        /// </summary>
        public byte ConnectionMask { get; init; }

        /// <summary>
        /// Connection mask with its first and last half swapped to bitwise and it with the opposing cliff's mask
        /// </summary>
        public byte ReversedConnectionMask => (byte)((ConnectionMask >> 4) + (0b11110000 & (ConnectionMask << 4)));

        /// <summary>
        /// List of tiles this connection point must connect to. RequiredTiles take priority over ForbiddenTiles
        /// </summary>
        public int[] RequiredTiles { get; init; }

        /// <summary>
        /// List of tiles this connection point cannot connect to. RequiredTiles take priority over ForbiddenTiles
        /// </summary>
        public int[] ForbiddenTiles { get; init; }

        /// <summary>
        /// Whether the connection point faces "backwards" or "forwards"
        /// </summary>
        public CliffSide Side { get; init; }
    }

    public class CliffAStarNode
    {
        private CliffAStarNode() {}

        public CliffAStarNode(CliffAStarNode parent, CliffConnectionPoint exit, Point2D location, CliffTile tile)
        {
            Location = location;
            Tile = tile;

            Parent = parent;
            Exit = exit;
            Destination = Parent.Destination;

            OccupiedCells = new HashSet<Point2D>(parent.OccupiedCells);
            OccupiedCells.UnionWith(tile.Foundation.Select(coordinate => coordinate + Location));
        }

        /// <summary>
        /// Absolute world coordinates of the node's tile
        /// </summary>
        public Point2D Location;

        /// <summary>
        /// Absolute world coordinates of the node's tile's exit
        /// </summary>
        public Point2D ExitCoords => Location + Exit.CoordinateOffset;

        /// <summary>
        /// Tile data
        /// </summary>
        public CliffTile Tile;

        ///// A* Stuff

        /// <summary>
        /// A* end point
        /// </summary>
        public Point2D Destination;

        /// <summary>
        /// Where this node connects to the next node
        /// </summary>
        public CliffConnectionPoint Exit;

        /// <summary>
        /// Distance from starting node
        /// </summary>
        public float GScore => Parent == null ? 0 : Parent.GScore + Vector2.Distance(Parent.ExitCoords.ToXNAVector(), ExitCoords.ToXNAVector());

        /// <summary>
        /// Distance to end node
        /// </summary>
        public float HScore => Vector2.Distance(Destination.ToXNAVector(), ExitCoords.ToXNAVector());
        public float FScore => GScore * 0.7f + HScore + (Tile?.DistanceModifier ?? 0);

        /// <summary>
        /// Previous node
        /// </summary>
        public CliffAStarNode Parent;

        /// <summary>
        /// Accumulated set of all cell coordinates occupied up to this node
        /// </summary>
        public HashSet<Point2D> OccupiedCells = new HashSet<Point2D>();

        public static CliffAStarNode MakeStartNode(Point2D location, Point2D destination, CliffSide startingSide)
        {
            CliffConnectionPoint connectionPoint = new CliffConnectionPoint
            {
                Index = 0,
                ConnectionMask = 0b11111111,
                CoordinateOffset = Point2D.Zero,
                Side = startingSide,
                RequiredTiles = Array.Empty<int>(),
                ForbiddenTiles = Array.Empty<int>()
            };

            var startNode = new CliffAStarNode()
            {
                Location = location,
                Tile = null,

                Parent = null,
                Exit = connectionPoint,
                Destination = destination
            };

            return startNode;
        }

        public List<CliffAStarNode> GetNextNodes(CliffTile tile)
        {
            List<(CliffConnectionPoint, List<Direction>)> possibleNeighbors = new();

            foreach (CliffConnectionPoint cp in tile.ConnectionPoints)
            {
                if (Tile != null)
                {
                    if ((cp.RequiredTiles?.Length > 0 && !cp.RequiredTiles.Contains(Tile.Index)) ||
                        (cp.ForbiddenTiles?.Length > 0 && cp.ForbiddenTiles.Contains(Tile.Index)))
                        continue;
                }
                
                var possibleDirections = GetDirectionsInMask((byte)(cp.ReversedConnectionMask & Exit.ConnectionMask));
                if (possibleDirections.Count == 0)
                    continue;

                possibleNeighbors.Add((cp, possibleDirections));
            }

            var neighbors = new List<CliffAStarNode>();
            
            foreach (var (connectionPoint, directions) in possibleNeighbors)
            {
                if (connectionPoint.Side != Exit.Side)
                    continue;

                foreach (Direction dir in directions)
                {
                    Point2D placementOffset = Helpers.VisualDirectionToPoint(dir) - connectionPoint.CoordinateOffset;
                    Point2D placementCoords = ExitCoords + placementOffset;

                    var exit = tile.GetExit(connectionPoint.Index);
                    var newNode = new CliffAStarNode(this, exit, placementCoords, tile);

                    // Make sure that the new node doesn't overlap anything
                    if (newNode.OccupiedCells.Count - OccupiedCells.Count == newNode.Tile.Foundation.Count)
                        neighbors.Add(newNode);
                }
            }
            
            return neighbors;
        }

        public List<CliffAStarNode> GetNextNodes(List<CliffTile> tiles)
        {
            return tiles.SelectMany(GetNextNodes).ToList();
        }

        private List<Direction> GetDirectionsInMask(byte mask)
        {
            List<Direction> directions = new List<Direction>();

            for (int direction = 0; direction < (int)Direction.Count; direction++)
            {
                if ((mask & (byte)(0b10000000 >> direction)) > 0)
                    directions.Add((Direction)direction);
            }

            return directions;
        }
    }

    public class CliffTile
    {
        public CliffTile(IniSection iniSection, int index)
        {
            Index = index;

            string indicesString = iniSection.GetStringValue("TileIndices", null);
            if (indicesString == null || !Regex.IsMatch(indicesString, "^((?:\\d+?,)*(?:\\d+?))$"))
                throw new INIConfigException($"Connected Tile {iniSection.SectionName} has invalid TileIndices list: {indicesString}!");


            string tileSet = iniSection.GetStringValue("TileSet", null);
            if (string.IsNullOrWhiteSpace(tileSet))
                throw new INIConfigException($"Connected Tile {iniSection.SectionName} has no TileSet!");

            TileSetName = tileSet;

            IndicesInTileSet = indicesString.Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToList();

            ConnectionPoints = new CliffConnectionPoint[2];

            for (int i = 0; i < ConnectionPoints.Length; i++)
            {
                string coordsString = iniSection.GetStringValue($"ConnectionPoint{i}", null);
                if (coordsString == null || !Regex.IsMatch(coordsString, "^\\d+?,\\d+?$"))
                    throw new INIConfigException($"Connected Tile {iniSection.SectionName} has invalid ConnectionPoint{i} value: {coordsString}!");

                Point2D coords = Point2D.FromString(coordsString);

                string directionsString = iniSection.GetStringValue($"ConnectionPoint{i}.Directions", null);
                if (directionsString == null || directionsString.Length != (int)Direction.Count || Regex.IsMatch(directionsString, "[^01]"))
                    throw new INIConfigException($"Connected Tile {iniSection.SectionName} has invalid ConnectionPoint{i}.Directions value: {directionsString}!");

                byte directions = Convert.ToByte(directionsString, 2);

                string sideString = iniSection.GetStringValue($"ConnectionPoint{i}.Side", string.Empty);
                CliffSide side = sideString.ToLower() switch
                {
                    "front" => CliffSide.Front,
                    "back" => CliffSide.Back,
                    "" => CliffSide.Front,
                    _ => throw new INIConfigException($"Connected Tile {iniSection.SectionName} has an invalid ConnectionPoint{i}.Side value: {sideString}!")
                };

                int[] requiredTiles, forbiddenTiles;

                var requiredTilesList =
                    iniSection.GetListValue($"ConnectionPoint{i}.RequiredTiles", ',', int.Parse);

                if (requiredTilesList.Count > 0)
                {
                    requiredTiles = requiredTilesList.ToArray();
                    forbiddenTiles = Array.Empty<int>();
                }
                else
                {
                    var forbiddenTilesList =
                        iniSection.GetListValue($"ConnectionPoint{i}.ForbiddenTiles", ',', int.Parse);

                    forbiddenTiles = forbiddenTilesList.ToArray();
                    requiredTiles = Array.Empty<int>();
                }

                ConnectionPoints[i] = new CliffConnectionPoint
                {
                    Index = i,
                    ConnectionMask = directions,
                    CoordinateOffset = coords,
                    Side = side,
                    RequiredTiles = requiredTiles,
                    ForbiddenTiles = forbiddenTiles
                };
            }

            if (iniSection.KeyExists("Foundation"))
            {
                string foundationString = iniSection.GetStringValue("Foundation", string.Empty);
                if (!Regex.IsMatch(foundationString, "^((?:\\d+?,\\d+?\\|)*(?:\\d+?,\\d+?))$"))
                    throw new INIConfigException($"Connected Tile {iniSection.SectionName} has an invalid Foundation: {foundationString}!");

                Foundation = foundationString.Split("|").Select(Point2D.FromString).ToHashSet();
            }

            ExtraPriority = -iniSection.GetIntValue("ExtraPriority", 0); // negated because sorting is in ascending order by default, but it's more intuitive to have larger numbers be more important
            DistanceModifier = iniSection.GetIntValue("DistanceModifier", 0);
        }

        /// <summary>
        /// Tile's in-editor index
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The name of the tile's Tile Set
        /// </summary>
        public string TileSetName { get; set; }

        /// <summary>
        /// Indices of tiles relative to the Tile Set
        /// </summary>
        public List<int> IndicesInTileSet { get; set; }

        /// <summary>
        /// Places this tile connects to other tiles
        /// </summary>
        public CliffConnectionPoint[] ConnectionPoints { get; set; }

        /// <summary>
        /// Set of all relative cell coordinates this tile occupies
        /// </summary>
        public HashSet<Point2D> Foundation { get; set; }

        /// <summary>
        /// Extra priority to be used as a secondary key when sorting tiles
        /// </summary>
        public int ExtraPriority { get; set; }

        /// <summary>
        /// A distance modifier added directly to the FScore. Use with caution!
        /// </summary>
        public int DistanceModifier { get; set; }

        public CliffConnectionPoint GetExit(int entryIndex)
        {
            return ConnectionPoints[0].Index == entryIndex ? ConnectionPoints[1] : ConnectionPoints[0];
        }
    }

    public class CliffType
    {
        public static CliffType FromIniSection(IniFile iniFile, string sectionName)
        {
            IniSection cliffSection = iniFile.GetSection(sectionName);
            if (cliffSection == null)
                return null;

            string cliffName = cliffSection.GetStringValue("Name", null);

            if (string.IsNullOrEmpty(cliffName))
                return null;

            var allowedTheaters = cliffSection.GetListValue("AllowedTheaters", ',', s => s);

            bool frontOnly = cliffSection.GetBooleanValue("FrontOnly", false);

            return new CliffType(iniFile, sectionName, cliffName, frontOnly, allowedTheaters);
        }

        private CliffType(IniFile iniFile, string iniName, string name, bool frontOnly, List<string> allowedTheaters)
        {
            IniName = iniName;
            Name = name;
            AllowedTheaters = allowedTheaters;
            FrontOnly = frontOnly;

            Tiles = new List<CliffTile>();

            foreach (var sectionName in iniFile.GetSections())
            {
                var parts = sectionName.Split('.');
                if (parts.Length != 2 || parts[0] != IniName || !int.TryParse(parts[1], out int index))
                    continue;

                if (Tiles.Exists(tile => tile.Index == index))
                {
                    throw new INIConfigException(
                        $"Connected Tile {iniName} has multiple tiles with the same index {index}!");
                }

                Tiles.Add(new CliffTile(iniFile.GetSection(sectionName), index));
            }
        }

        public string IniName { get; }
        public string Name { get; }
        public bool FrontOnly { get; }
        public List<string> AllowedTheaters { get; set; }
        public List<CliffTile> Tiles { get; }
    }
}
