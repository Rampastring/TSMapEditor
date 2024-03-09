using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.Extensions;
using TSMapEditor.GameMath;
using TSMapEditor.Initialization;
using TSMapEditor.Misc;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering;

namespace TSMapEditor.Models
{
    public class HouseEventArgs : EventArgs
    {
        public HouseEventArgs(House house)
        {
            House = house;
        }

        public House House { get; }
    }

    public class Map : IMap
    {
        private const int MaxTubes = 127;
        public const int TileBufferSize = 600; // for now

        public event EventHandler HousesChanged;
        public event EventHandler<HouseEventArgs> HouseColorChanged;
        public event EventHandler LocalSizeChanged;
        public event EventHandler MapResized;
        public event EventHandler MapHeightChanged;
        public event EventHandler CellLightingModified;
        public event EventHandler MapManuallySaved;
        public event EventHandler MapAutoSaved;
        public event EventHandler PreSave;
        public event EventHandler PostSave;


        public IniFile LoadedINI { get; private set; }

        public bool ReloadINI()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(LoadedINI.FileName))
                    throw new InvalidOperationException("Cannot reload INI of a map file that has no INI file path specified!");

                if (!File.Exists(LoadedINI.FileName))
                {
                    Logger.Log("Map.ReloadINI: Skipping re-loading map INI because the map INI does not exist!");
                    return true;
                }

                LoadedINI = new IniFileEx(LoadedINI.FileName, ccFileManager);

                ReloadSections();

                return true;
            }
            catch (IOException ex)
            {
                // Sometimes the file might be used by some external software, making us unable
                // to access it
                Logger.Log("IOException when attempting to reload the map INI: " + ex.Message);
                return false;
            }
        }

        public Rules Rules { get; private set; }
        public EditorConfig EditorConfig { get; private set; }

        public BasicSection Basic { get; private set; } = new BasicSection();

        public MapTile[][] Tiles { get; private set; }
        public MapTile GetTile(int x, int y)
        {
            if (y < 0 || y >= Tiles.Length)
                return null;

            if (x < 0 || x >= Tiles[y].Length)
                return null;

            return Tiles[y][x];
        }

        public MapTile GetTile(Point2D cellCoords) => GetTile(cellCoords.X, cellCoords.Y);
        public MapTile GetTileOrFail(Point2D cellCoords) => GetTile(cellCoords.X, cellCoords.Y) ?? throw new InvalidOperationException("Invalid cell coords: " + cellCoords);
        public List<Aircraft> Aircraft { get; private set; } = new List<Aircraft>();
        public List<Infantry> Infantry { get; private set; } = new List<Infantry>();
        public List<Unit> Units { get; private set; } = new List<Unit>();
        public List<Structure> Structures { get; private set; } = new List<Structure>();

        /// <summary>
        /// The list of standard house types loaded from EditorRules.ini, or Rules.ini as a fallback.
        /// Relevant when the map itself has no house types specified.
        /// New house types might be added to this list if the map has
        /// objects whose owner does not exist in the map's list of house types
        /// or in the Rules.ini standard house type list.
        /// </summary>
        public List<HouseType> StandardHouseTypes { get; set; }
        public List<House> StandardHouses { get; set; }

        /// <summary>
        /// The list of house types defined in the map itself.
        /// For Tiberian Sun, this is the full house type list (if it has entries at all).
        /// For Yuri's Revenge, it only contains "bonus" house types defined in the map itself,
        /// which need to be appended into the Rules house type list for most use cases.
        ///
        /// In Tiberian Sun maps, each House has one and exactly one HouseType associated with it.
        /// In Yuri's Revenge this is not the case, but instead, multiple Houses can use one
        /// HouseType, and there can also be completely unused HouseTypes.
        /// </summary>
        public List<HouseType> HouseTypes { get; protected set; } = new List<HouseType>();
        public List<HouseType> GetHouseTypes()
        {
            if (Constants.UseCountries)
            {
                if (HouseTypes.Count > 0)
                    return Rules.RulesHouseTypes.Concat(HouseTypes).ToList();
                else
                    return StandardHouseTypes;
            }
            else
            {
                return HouseTypes.Count > 0 ? HouseTypes : StandardHouseTypes;
            }
        }

        public List<House> Houses { get; protected set; } = new List<House>();
        public List<House> GetHouses() => Houses.Count > 0 ? Houses : StandardHouses;

        public List<TerrainObject> TerrainObjects { get; private set; } = new List<TerrainObject>();
        public List<Waypoint> Waypoints { get; private set; } = new List<Waypoint>();

        public List<TaskForce> TaskForces { get; protected set; } = new List<TaskForce>();
        public List<Trigger> Triggers { get; protected set; } = new List<Trigger>();
        public List<Tag> Tags { get; protected set; } = new List<Tag>();
        public List<CellTag> CellTags { get; private set; } = new List<CellTag>();
        public List<Script> Scripts { get; protected set; } = new List<Script>();
        public List<TeamType> TeamTypes { get; protected set; } = new List<TeamType>();
        public List<AITriggerType> AITriggerTypes { get; protected set; } = new List<AITriggerType>();
        public List<LocalVariable> LocalVariables { get; protected set; } = new List<LocalVariable>();
        public List<Tube> Tubes { get; private set; } = new List<Tube>();

        public Lighting Lighting { get; } = new Lighting();

        public List<GraphicalBaseNode> GraphicalBaseNodes { get; protected set; } = new List<GraphicalBaseNode>();

        public Point2D Size { get; set; }

        private Rectangle _localSize;
        public Rectangle LocalSize 
        {
            get => _localSize;
            set
            {
                _localSize = value;
                LocalSizeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int WidthInPixels => Size.X * Constants.CellSizeX;

        public int HeightInPixels => Size.Y * Constants.CellSizeY;

        public string TheaterName { get; set; }
        public ITheater TheaterInstance { get; set; }
        public string LoadedTheaterName => TheaterInstance.Theater.UIName;

        public StringTable StringTable { get; private set; }

        private readonly Initializer initializer;

        private readonly CCFileManager ccFileManager;

        public Map()
        {
            InitCells();

            initializer = new Initializer(this);
        }

        public Map(CCFileManager ccFileManager)
        {
            InitCells();

            initializer = new Initializer(this);

            this.ccFileManager = ccFileManager;
        }

        private void InitEditorConfig()
        {
            EditorConfig = new EditorConfig();
            EditorConfig.EarlyInit();
        }

        public void InitNew(GameConfigINIFiles gameConfigINIFiles, string theaterName, Point2D size, byte startingLevel)
        {
            const int marginY = 6;
            const int marginX = 4;

            InitEditorConfig();
            InitializeRules(gameConfigINIFiles);
            LoadedINI = new IniFileEx();
            var baseMap = new IniFileEx(Environment.CurrentDirectory + "/Config/BaseMap.ini", ccFileManager);
            baseMap.FileName = string.Empty;
            baseMap.SetStringValue("Map", "Theater", theaterName);
            baseMap.SetStringValue("Map", "Size", $"0,0,{size.X},{size.Y}");
            baseMap.SetStringValue("Map", "LocalSize", $"{marginX},{marginY},{size.X - (marginX * 2)},{size.Y - (marginY * 2)}");
            LoadExisting(gameConfigINIFiles, baseMap);
            SetTileData(null, defaultLevel: startingLevel, overrideExisting: true);
        }

        public void LoadExisting(GameConfigINIFiles gameConfigINIFiles, IniFile mapIni)
        {
            InitEditorConfig();
            InitializeRules(gameConfigINIFiles);

            LoadedINI = mapIni ?? throw new ArgumentNullException(nameof(mapIni));
            Rules.InitFromINI(mapIni, initializer, true);
            EditorConfig.RulesDependentInit(Rules);

            PostInitializeRules_ReinitializeArt(gameConfigINIFiles.ArtIni, gameConfigINIFiles.ArtFSIni);
            Rules.SolveDependencies();

            MapLoader.MapLoadErrors.Clear();

            MapLoader.ReadBasicSection(this, mapIni);
            MapLoader.ReadMapSection(this, mapIni);
            MapLoader.ReadIsoMapPack(this, mapIni);

            MapLoader.ReadHouseTypes(this, mapIni);
            MapLoader.ReadHouses(this, mapIni);

            MapLoader.ReadSmudges(this, mapIni);
            MapLoader.ReadOverlays(this, mapIni);
            MapLoader.ReadTerrainObjects(this, mapIni);
            MapLoader.ReadTubes(this, mapIni);

            MapLoader.ReadWaypoints(this, mapIni);
            MapLoader.ReadTaskForces(this, mapIni);
            MapLoader.ReadTriggers(this, mapIni);
            MapLoader.ReadTags(this, mapIni);
            MapLoader.ReadCellTags(this, mapIni);
            MapLoader.ReadScripts(this, mapIni);
            MapLoader.ReadTeamTypes(this, mapIni, EditorConfig.TeamTypeFlags);
            MapLoader.ReadAITriggerTypes(this, mapIni);
            MapLoader.ReadLocalVariables(this, mapIni);

            MapLoader.ReadBuildings(this, mapIni);
            MapLoader.ReadAircraft(this, mapIni);
            MapLoader.ReadUnits(this, mapIni);
            MapLoader.ReadInfantry(this, mapIni);

            CreateGraphicalNodesFromBaseNodes();

            Lighting.ReadFromIniFile(mapIni);

            StringTable = new(ccFileManager.CsfFiles);
        }

        private void CreateGraphicalNodesFromBaseNodes()
        {
            // Check base nodes and create graphical base node instances from them
            if (Houses.Count > 0)
            {
                foreach (var house in Houses)
                {
                    for (int i = 0; i < house.BaseNodes.Count; i++)
                    {
                        var baseNode = house.BaseNodes[i];

                        BuildingType buildingType = Rules.BuildingTypes.Find(bt => bt.ININame == baseNode.StructureTypeName);
                        bool remove = false;
                        if (buildingType == null)
                        {
                            Logger.Log($"Building type {baseNode.StructureTypeName} not found for base node for house {house.ININame}! Removing the node.");
                            remove = true;
                        }

                        var cell = GetTile(baseNode.Position);
                        if (cell == null)
                        {
                            Logger.Log($"Base node for building type {baseNode.StructureTypeName} for house {house.ININame} is outside of the map! Coords: {baseNode.Position}. Removing the node.");
                            remove = true;
                        }

                        if (remove)
                        {
                            house.BaseNodes.RemoveAt(i);
                            i--;
                            continue;
                        }

                        var graphicalBaseNode = new GraphicalBaseNode(baseNode, buildingType, house);
                        GraphicalBaseNodes.Add(graphicalBaseNode);
                    }
                }
            }
        }

        private void ReloadSections()
        {
            MapLoader.ReadBasicSection(this, LoadedINI);
            Lighting.ReadFromIniFile(LoadedINI);
        }

        public void Save()
        {
            Write(null);
            MapManuallySaved?.Invoke(this, EventArgs.Empty);
        }

        public void AutoSave(string filePath)
        {
            Write(filePath);
            MapAutoSaved?.Invoke(this, EventArgs.Empty);
        }

        private void Write(string filePath = null)
        {
            PreSave?.Invoke(this, EventArgs.Empty);

            LoadedINI.Comment = "Written by the World-Altering Editor (WAE)\r\n; all comments have been truncated\r\n; github.com/Rampastring/WorldAlteringEditor\r\n; if you wish to support the editor, you can subscribe at patreon.com/rampastring\r\n; or buy me a coffee at ko-fi.com/rampastring";

            MapWriter.WriteMapSection(this, LoadedINI);
            MapWriter.WriteBasicSection(this, LoadedINI);
            MapWriter.WriteAITriggerTypes(this, LoadedINI);
            MapWriter.WriteIsoMapPack5(this, LoadedINI);

            Lighting.WriteToIniFile(LoadedINI);

            MapWriter.WriteHouseTypes(this, LoadedINI);
            MapWriter.WriteHouses(this, LoadedINI);

            MapWriter.WriteSmudges(this, LoadedINI);
            MapWriter.WriteOverlays(this, LoadedINI);
            MapWriter.WriteTerrainObjects(this, LoadedINI);
            MapWriter.WriteTubes(this, LoadedINI);

            MapWriter.WriteWaypoints(this, LoadedINI);
            MapWriter.WriteTaskForces(this, LoadedINI);
            MapWriter.WriteTriggers(this, LoadedINI);
            MapWriter.WriteTags(this, LoadedINI);
            MapWriter.WriteCellTags(this, LoadedINI);
            MapWriter.WriteScripts(this, LoadedINI);
            MapWriter.WriteTeamTypes(this, LoadedINI, EditorConfig.TeamTypeFlags);

            MapWriter.WriteLocalVariables(this, LoadedINI);

            MapWriter.WriteAircraft(this, LoadedINI);
            MapWriter.WriteUnits(this, LoadedINI);
            MapWriter.WriteInfantry(this, LoadedINI);
            MapWriter.WriteBuildings(this, LoadedINI);

            MapWriter.WriteDummyPreview(this, LoadedINI);

            string savePath = filePath ?? LoadedINI.FileName;

            LoadedINI.WriteIniFile(savePath);

            PostSave?.Invoke(this, EventArgs.Empty);
        }

        public void WritePreview(Texture2D texture) => MapWriter.WriteActualPreview(texture, LoadedINI);

        public HouseType FindHouseType(string houseTypeName)
        {
            return GetHouseTypes().Find(ht => ht.ININame == houseTypeName);
        }

        public HouseType FindHouseType(int index)
        {
            return GetHouseTypes().Find(ht => ht.Index == index);
        }

        /// <summary>
        /// Finds a house with the given name from the map's or the game's house lists.
        /// If no house is found, creates one and adds it to the standard house list.
        /// Returns the house that was found or created.
        /// </summary>
        /// <param name="houseName">The name of the house to find.</param>
        public House FindOrMakeHouse(string houseName)
        {
            var house = Houses.Find(h => h.ININame == houseName);
            if (house != null)
                return house;

            house = StandardHouses.Find(h => h.ININame == houseName);
            if (house != null)
                return house;

            // Try to find a matching standard house type for this house.
            // If we can't find one, create one.
            HouseType houseType = Rules.RulesHouseTypes.Find(ht => houseName.StartsWith(ht.ININame));
            if (houseType == null)
                houseType = Rules.RulesHouseTypes.Find(ht => ht.ININame == "Neutral");

            if (houseType == null)
            {
                houseType = new HouseType(houseName);
                StandardHouseTypes.Add(houseType);
            }
            
            house = new House(houseName, houseType);
            StandardHouses.Add(house);
            return house;
        }

        /// <summary>
        /// Finds a house with the given name from the map's or the game's house lists.
        /// Returns null if no house is found.
        /// </summary>
        /// <param name="houseName">The name of the house to find.</param>
        public House FindHouse(string houseName)
        {
            var house = Houses.Find(h => h.ININame == houseName);
            if (house != null)
                return house;

            return StandardHouses.Find(h => h.ININame == houseName);
        }

        public bool IsCoordWithinMap(int x, int y) => IsCoordWithinMap(new Point2D(x, y));

        public bool IsCoordWithinMap(Point2D coord)
        {
            if (coord.X <= 0 || coord.Y <= 0)
                return false;

            // Filter out cells that would be above (to the north of) the map area
            if (coord.X + coord.Y < Size.X + 1)
                return false;

            // Filter out cells that would be to the right (east) of the map area
            if (coord.X - coord.Y > Size.X - 1)
                return false;

            // Filter out cells that would be to the left (west) of the map area
            if (coord.Y - coord.X > Size.X - 1)
                return false;

            // Filter out cells that would be below (to the south of) the map area
            if (coord.Y + coord.X > Size.Y * 2 + Size.X)
                return false;

            return true;
        }

        public void SetTileData(List<MapTile> tiles, byte defaultLevel = 0, bool overrideExisting = false)
        {
            if (tiles != null)
            {
                foreach (var tile in tiles)
                {
                    if (!IsCoordWithinMap(tile.CoordsToPoint()))
                    {
                        Logger.Log("Dropping cell " + tile.CoordsToPoint() + " that would be outside of the allowed map area.");
                        continue;
                    }

                    Tiles[tile.Y][tile.X] = tile;
                }
            }

            // Check for uninitialized tiles within the map bounds
            // Begin from the top-left corner and proceed row by row
            int ox = 1;
            int oy = Size.X;
            while (ox <= Size.Y)
            {
                int tx = ox;
                int ty = oy;
                while (tx < Size.X + ox)
                {
                    if (Tiles[ty][tx] == null || overrideExisting)
                    {
                        Tiles[ty][tx] = new MapTile() { X = (short)tx, Y = (short)ty, Level = defaultLevel };
                    }

                    if (tx < Size.X + ox - 1 && (Tiles[ty][tx + 1] == null || overrideExisting))
                    {
                        Tiles[ty][tx + 1] = new MapTile() { X = (short)(tx + 1), Y = (short)ty, Level = defaultLevel };
                    }

                    tx++;
                    ty--;
                }

                ox++;
                oy++;
            }
        }

        public int GetCellCount()
        {
            int cellCount = 0;

            int ox = 1;
            int oy = Size.X;
            while (ox <= Size.Y)
            {
                int tx = ox;
                int ty = oy;
                while (tx < Size.X + ox)
                {
                    cellCount += 2;

                    tx++;
                    ty--;
                }

                ox++;
                oy++;
            }

            return cellCount;
        }

        /// <summary>
        /// Resizes the map. Handles moving map elements (objects, waypoints etc.) as required.
        /// Deletes elements that would end up outside of the new map borders.
        /// </summary>
        /// <param name="newSize">The new size of the map.</param>
        /// <param name="eastShift">Defines how many coords existing cells and 
        /// objects should be moved to the east.</param>
        /// <param name="southShift">Defines by how many coords existing cells and 
        /// objects should be moved to the south.</param>
        public void Resize(Point2D newSize, int eastShift, int southShift)
        {
            // Copy current cell list to preserve it
            MapTile[][] cells = Tiles;

            // Combine all cells into one single-dimensional list
            List<MapTile> allCellsInList = cells.Aggregate(new List<MapTile>(), (totalCellList, rowCellList) =>
            {
                var nonNullValues = rowCellList.Where(mapcell => mapcell != null);
                return totalCellList.Concat(nonNullValues).ToList();
            });

            // Handle east-shift
            // We can shift a cell one point towards the east by 
            // adding 1 to its X coordinate and subtracting 1 from its Y coordinate
            allCellsInList.ForEach(mapCell => { mapCell.ShiftPosition(eastShift, -eastShift); });

            // Handle south-shift
            // We can shift a cell one point towards the south by 
            // adding 1 to both its X and Y coordinates
            allCellsInList.ForEach(mapCell => { mapCell.ShiftPosition(southShift, southShift); });


            // Then the "fun" part. Shift every object, waypoint, celltag etc. similarly!
            ShiftObjectsInList(Aircraft, eastShift, southShift);
            ShiftObjectsInList(Infantry, eastShift, southShift);
            ShiftObjectsInList(Units, eastShift, southShift);
            ShiftObjectsInList(Structures, eastShift, southShift);
            ShiftObjectsInList(TerrainObjects, eastShift, southShift);
            ShiftObjectsInList(Waypoints, eastShift, southShift);
            ShiftObjectsInList(CellTags, eastShift, southShift);

            // Iterate all houses to shift base nodes
            foreach (House house in GetHouses())
            {
                ShiftObjectsInList(house.BaseNodes, eastShift, southShift);
            }

            // Tubes are slightly more complicated...
            Tubes.ForEach(tube =>
            {
                tube.ShiftPosition(eastShift, -eastShift);
                tube.ShiftPosition(southShift, southShift);
            });


            // Now let's apply our changes and remove stuff that would end up outside of the map

            Size = newSize;

            // Re-init cell list
            // This will automatically get rid of cells that would end up outside of the map
            InitCells();
            SetTileData(allCellsInList);

            // Objects we have to check manually
            // Luckily functional programming and our design makes this relatively painless!
            Aircraft = Aircraft.Where(a => IsCoordWithinMap(a.Position)).ToList();
            Infantry = Infantry.Where(i => IsCoordWithinMap(i.Position)).ToList();
            Units = Units.Where(u => IsCoordWithinMap(u.Position)).ToList();
            Structures = Structures.Where(s => IsCoordWithinMap(s.Position)).ToList();
            TerrainObjects = TerrainObjects.Where(t => IsCoordWithinMap(t.Position)).ToList();
            Waypoints = Waypoints.Where(wp => IsCoordWithinMap(wp.Position)).ToList();
            CellTags = CellTags.Where(ct => IsCoordWithinMap(ct.Position)).ToList();
            Tubes = Tubes.Where(tube => IsCoordWithinMap(tube.EntryPoint) && IsCoordWithinMap(tube.ExitPoint)).ToList();

            // Refresh base nodes
            GraphicalBaseNodes.Clear();

            foreach (House house in GetHouses())
            {
                var nodesWithinMap = house.BaseNodes.Where(bn => IsCoordWithinMap(bn.Position)).ToList();
                house.BaseNodes.Clear();
                house.BaseNodes.AddRange(nodesWithinMap);
                house.BaseNodes.ForEach(bn => RegisterBaseNode(house, bn));
            }

            // We're done!
            MapResized?.Invoke(this, EventArgs.Empty);
        }

        private void ShiftObjectsInList<T>(List<T> list, int eastShift, int southShift) where T : IPositioned
        {
            list.ForEach(element => ShiftObject(element, eastShift, southShift));
        }

        private void ShiftObject(IPositioned movableObject, int eastShift, int southShift)
        {
            int x = movableObject.Position.X;
            int y = movableObject.Position.Y;

            x += eastShift;
            y -= eastShift;

            x += southShift;
            y += southShift;

            movableObject.Position = new Point2D(x, y);
        }

        private void InitCells()
        {
            Tiles = new MapTile[TileBufferSize][];
            for (int i = 0; i < Tiles.Length; i++)
            {
                Tiles[i] = new MapTile[TileBufferSize];
            }
        }

        /// <summary>
        /// Changes the height of the entire map by the given value.
        /// </summary>
        /// <param name="height">The number of height levels to add to the height of each cell on the map. Can be negative.</param>
        public void ChangeHeight(int height)
        {
            if (height == 0)
                return;

            DoForAllValidTiles(cell =>
            {
                if (height > 0)
                {
                    cell.Level = (byte)Math.Min(cell.Level + height, Constants.MaxMapHeightLevel);
                }
                else
                {
                    if (Math.Abs(height) > cell.Level)
                        cell.Level = 0;
                    else
                        cell.Level = (byte)(cell.Level - Math.Abs(height));
                }
            });

            MapHeightChanged?.Invoke(this, EventArgs.Empty);
        }

        public void PlaceTerrainTileAt(ITileImage tile, Point2D cellCoords)
        {
            for (int i = 0; i < tile.SubTileCount; i++)
            {
                var subTile = tile.GetSubTile(i);
                if (subTile.TmpImage == null)
                    continue;

                Point2D offset = tile.GetSubTileCoordOffset(i).Value;

                var mapTile = GetTile(cellCoords + offset);
                if (mapTile == null)
                    continue;

                mapTile.TileImage = null;
                mapTile.TileIndex = tile.TileID;
                mapTile.SubTileIndex = (byte)i;
            }
        }

        public void AddWaypoint(Waypoint waypoint)
        {
            Waypoints.Add(waypoint);
            var tile = GetTile(waypoint.Position.X, waypoint.Position.Y);
            if (tile.Waypoints.Count > 0)
            {
                Logger.Log($"NOTE: Waypoint {waypoint.Identifier} exists in the cell at {waypoint.Position} that already contains other waypoints: {string.Join(", ", tile.Waypoints.Select(s => s.Identifier))}");
            }

            tile.Waypoints.Add(waypoint);
        }

        public void RemoveWaypoint(Waypoint waypoint)
        {
            var tile = GetTile(waypoint.Position);
            if (tile.Waypoints.Contains(waypoint))
            {
                Waypoints.Remove(waypoint);
                tile.Waypoints.Remove(waypoint);
            }
        }

        public void RemoveWaypointsFrom(Point2D cellCoords)
        {
            var tile = GetTile(cellCoords);
            if (tile.Waypoints.Count > 0)
            {
                foreach (var waypoint in tile.Waypoints)
                {
                    Waypoints.Remove(waypoint);
                }
                tile.Waypoints.Clear();
            }
        }

        public void AddTaskForce(TaskForce taskForce)
        {
            TaskForces.Add(taskForce);
        }

        public void RemoveTaskForce(TaskForce taskForce)
        {
            TaskForces.Remove(taskForce);
            TeamTypes.FindAll(tt => tt.TaskForce == taskForce).ForEach(tt => tt.TaskForce = null);
            LoadedINI.RemoveSection(taskForce.ININame);
        }

        public void ClearTaskForces()
        {
            var taskforcesCopy = new List<TaskForce>(TaskForces);
            taskforcesCopy.ForEach(tf => RemoveTaskForce(tf));
        }

        public void AddTrigger(Trigger trigger)
        {
            Triggers.Add(trigger);
        }

        public void AddTag(Tag tag)
        {
            Tags.Add(tag);
        }

        public void AddCellTag(CellTag cellTag)
        {
            var tile = GetTile(cellTag.Position);
            if (tile.CellTag != null)
            {
                Logger.Log("Tile already has a celltag, skipping placing of celltag at " + cellTag.Position);
                return;
            }

            CellTags.Add(cellTag);
            tile.CellTag = cellTag;
        }

        public void RemoveCellTagFrom(Point2D cellCoords)
        {
            var tile = GetTile(cellCoords);
            if (tile.CellTag != null)
            {
                CellTags.Remove(tile.CellTag);
                tile.CellTag = null;
            }
        }

        public void AddScript(Script script)
        {
            Scripts.Add(script);
        }

        public void RemoveScript(Script script)
        {
            Scripts.Remove(script);
            TeamTypes.FindAll(tt => tt.Script == script).ForEach(tt => tt.Script = null);
            LoadedINI.RemoveSection(script.ININame);
        }

        public void AddTeamType(TeamType teamType)
        {
            TeamTypes.Add(teamType);
        }

        public void RemoveTeamType(TeamType teamType)
        {
            TeamTypes.Remove(teamType);
            LoadedINI.RemoveSection(teamType.ININame);
        }

        public void AddHouseType(HouseType houseType)
        {
            HouseTypes.Add(houseType);
        }

        public void AddHouses(List<House> houses)
        {
            if (houses.Count > 0)
            {
                Houses.AddRange(houses);
                HousesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void AddHouse(House house)
        {
            Houses.Add(house);
            HousesChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool DeleteHouse(House house)
        {
            if (Houses.Remove(house))
            {
                for (int i = 0; i < Houses.Count; i++)
                    Houses[i].ID = i;

                LoadedINI.RemoveSection(house.ININame);
                HousesChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }

        public bool DeleteHouseType(HouseType houseType)
        {
            if (HouseTypes.Remove(houseType))
            {
                for (int i = 0; i < HouseTypes.Count; i++)
                    HouseTypes[i].Index = i + (Constants.UseCountries ? Rules.RulesHouseTypes.Count : 0);

                return true;
            }

            return false;
        }

        public void RegisterBaseNode(House house, BaseNode baseNode)
        {
            var buildingType = Rules.BuildingTypes.Find(bt => bt.ININame == baseNode.StructureTypeName) ??
                throw new KeyNotFoundException("Building type not found while adding base node: " + baseNode.StructureTypeName);

            GraphicalBaseNodes.Add(new GraphicalBaseNode(baseNode, buildingType, house));
        }

        public void UnregisterBaseNode(BaseNode baseNode)
        {
            int index = GraphicalBaseNodes.FindIndex(gbn => gbn.BaseNode == baseNode);
            if (index > -1)
                GraphicalBaseNodes.RemoveAt(index);
        }

        public void HouseColorUpdated(House house)
        {
            HouseColorChanged?.Invoke(this, new HouseEventArgs(house));
        }

        public void PlaceBuilding(Structure structure)
        {
            structure.ObjectType.ArtConfig.DoForFoundationCoords(offset =>
            {
                var cell = GetTile(structure.Position + offset);
                if (cell == null)
                    return;

                cell.Structures.Add(structure);
            });

            if (structure.ObjectType.ArtConfig.Foundation.Width == 0 && structure.ObjectType.ArtConfig.Foundation.Height == 0)
            {
                GetTile(structure.Position).Structures.Add(structure);
            }
            
            Structures.Add(structure);
            CheckForLightingChange(structure);
        }

        public void RemoveBuildingsFrom(Point2D cellCoords)
        {
            var cell = GetTile(cellCoords);

            bool refreshLighting = false;

            while (cell.Structures.Count > 0)
            {
                refreshLighting = cell.Structures[0].ObjectType.LightVisibility > 0;
                RemoveBuilding(cell.Structures[0], false);
            }

            if (refreshLighting)
                CheckForLightingChange(null);
        }

        public void RemoveBuilding(Structure structure, bool updateLighting = true)
        {
            structure.ObjectType.ArtConfig.DoForFoundationCoords(offset =>
            {
                var cell = GetTile(structure.Position + offset);
                if (cell == null)
                    return;

                if (cell.Structures.Contains(structure))
                    cell.Structures.Remove(structure);
            });

            if (structure.ObjectType.ArtConfig.Foundation.Width == 0 && structure.ObjectType.ArtConfig.Foundation.Height == 0)
            {
                GetTile(structure.Position).Structures.Remove(structure);
            }

            Structures.Remove(structure);

            if (updateLighting)
                CheckForLightingChange(structure);
        }

        public void MoveBuilding(Structure structure, Point2D newCoords)
        {
            RemoveBuilding(structure);
            structure.Position = newCoords;
            PlaceBuilding(structure);
        }

        private void CheckForLightingChange(GameObject gameObject)
        {
            if (gameObject == null)
            {
                CellLightingModified?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (gameObject.WhatAmI() == RTTIType.Building)
            {
                var structure = (Structure)gameObject;
                if (structure.ObjectType.LightVisibility > 0)
                {
                    CellLightingModified?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void PlaceUnit(Unit unit)
        {
            var cell = GetTile(unit.Position);

            cell.Vehicles.Add(unit);
            Units.Add(unit);
        }

        public void RemoveUnit(Unit unit)
        {
            var cell = GetTile(unit.Position);
            Units.Remove(unit);
            cell.Vehicles.Remove(unit);
        }

        public void RemoveUnitsFrom(Point2D cellCoords)
        {
            var cell = GetTile(cellCoords);
            cell.DoForAllVehicles(unit => Units.Remove(unit));

            cell.Vehicles.Clear();
        }

        public void MoveUnit(Unit unit, Point2D newCoords)
        {
            RemoveUnit(unit);
            unit.Position = newCoords;
            PlaceUnit(unit);
        }

        public void PlaceInfantry(Infantry infantry)
        {
            var cell = GetTile(infantry.Position);
            if (cell.Infantry[(int)infantry.SubCell] != null)
                throw new InvalidOperationException("Cannot place infantry on an occupied sub-cell spot!");

            cell.Infantry[(int)infantry.SubCell] = infantry;
            Infantry.Add(infantry);
        }

        public void RemoveInfantry(Infantry infantry)
        {
            var cell = GetTile(infantry.Position);
            cell.Infantry[(int)infantry.SubCell] = null;
            Infantry.Remove(infantry);
        }

        public void RemoveInfantry(Point2D cellCoords, SubCell subCell)
        {
            var cell = GetTile(cellCoords);
            var infantry = cell.Infantry[(int)subCell];
            Infantry.Remove(infantry);
            cell.Infantry[(int)subCell] = null;
        }

        public void MoveInfantry(Infantry infantry, Point2D newCoords)
        {
            var newCell = GetTile(newCoords);
            SubCell freeSubCell = newCell.GetFreeSubCellSpot();
            RemoveInfantry(infantry);
            infantry.Position = newCoords;
            infantry.SubCell = freeSubCell;
            PlaceInfantry(infantry);
        }

        public void PlaceAircraft(Aircraft aircraft)
        {
            var cell = GetTile(aircraft.Position);

            cell.Aircraft.Add(aircraft);
            Aircraft.Add(aircraft);
        }

        public void RemoveAircraft(Aircraft aircraft)
        {
            var cell = GetTile(aircraft.Position);
            cell.Aircraft.Remove(aircraft);
            Aircraft.Remove(aircraft);
        }

        public void RemoveAircraftFrom(Point2D cellCoords)
        {
            var cell = GetTile(cellCoords);
            cell.DoForAllAircraft(aircraft => Aircraft.Remove(aircraft));

            cell.Aircraft.Clear();
        }

        public void MoveAircraft(Aircraft aircraft, Point2D newCoords)
        {
            RemoveAircraft(aircraft);
            aircraft.Position = newCoords;
            PlaceAircraft(aircraft);
        }

        public void AddTerrainObject(TerrainObject terrainObject)
        {
            var cell = GetTile(terrainObject.Position);
            if (cell.TerrainObject != null)
                throw new InvalidOperationException("Cannot place a terrain object on a cell that already has a terrain object!");

            cell.TerrainObject = terrainObject;
            TerrainObjects.Add(terrainObject);
        }

        public void RemoveTerrainObject(TerrainObject terrainObject)
        {
            RemoveTerrainObject(terrainObject.Position);
        }

        public void RemoveTerrainObject(Point2D cellCoords)
        {
            var cell = GetTile(cellCoords);
            TerrainObjects.Remove(cell.TerrainObject);
            cell.TerrainObject = null;
        }

        public void MoveTerrainObject(TerrainObject terrainObject, Point2D newCoords)
        {
            RemoveTerrainObject(terrainObject.Position);
            terrainObject.Position = newCoords;
            AddTerrainObject(terrainObject);
        }

        public void MoveWaypoint(Waypoint waypoint, Point2D newCoords)
        {
            RemoveWaypoint(waypoint);
            waypoint.Position = newCoords;
            AddWaypoint(waypoint);
        }

        /// <summary>
        /// Determines whether an object can be moved to a specific location.
        /// </summary>
        /// <param name="movable">The object to move.</param>
        /// <param name="newCoords">The new coordinates of the object.</param>
        /// <param name="blocksSelf">Determines whether the object itself can be considered as blocking placement of the object.</param>
        /// <param name="overlapObjects">Determines whether multiple objects of the same type should be allowed to exist in the same cell.</param>
        /// <returns>True if the object can be moved, otherwise false.</returns>
        public bool CanPlaceObjectAt(IMovable movable, Point2D newCoords, bool blocksSelf, bool overlapObjects)
        {
            if (movable.WhatAmI() == RTTIType.Waypoint)
                return true;

            if (movable.WhatAmI() == RTTIType.Building)
            {
                bool canPlace = true;

                ((Structure)movable).ObjectType.ArtConfig.DoForFoundationCoords(offset =>
                {
                    MapTile foundationCell = GetTile(newCoords + offset);
                    if (foundationCell == null)
                        return;

                    if (!foundationCell.CanAddObject((GameObject)movable, blocksSelf, overlapObjects))
                        canPlace = false;
                });

                return canPlace;
            }

            MapTile cell = GetTile(newCoords);
            return cell.CanAddObject((GameObject)movable, blocksSelf, overlapObjects);
        }

        public AbstractObject DeleteObjectFromCell(Point2D cellCoords, DeletionMode deletionMode)
        {
            var tile = GetTile(cellCoords.X, cellCoords.Y);
            if (tile == null)
                return null;

            AbstractObject returnValue = null;

            if (deletionMode.HasFlag(DeletionMode.CellTags) && tile.CellTag != null)
            {
                returnValue = tile.CellTag;
                RemoveCellTagFrom(tile.CoordsToPoint());
            }
            else if (deletionMode.HasFlag(DeletionMode.Waypoints) && tile.Waypoints.Count > 0)
            {
                returnValue = tile.Waypoints[0];
                RemoveWaypoint(tile.Waypoints[0]);
            }
            else if (deletionMode.HasFlag(DeletionMode.Infantry) && tile.HasInfantry())
            {
                for (int i = 0; i < tile.Infantry.Length; i++)
                {
                    if (tile.Infantry[i] != null)
                    {
                        returnValue = tile.Infantry[i];
                        RemoveInfantry(tile.Infantry[i]);
                    }
                }
            }
            else if (deletionMode.HasFlag(DeletionMode.Aircraft) && tile.Aircraft.Count > 0)
            {
                returnValue = tile.Aircraft[0];
                RemoveAircraft(tile.Aircraft[0]);
            }
            else if (deletionMode.HasFlag(DeletionMode.Vehicles) && tile.Vehicles.Count > 0)
            {
                returnValue = tile.Vehicles[0];
                RemoveUnit(tile.Vehicles[0]);
            }
            else if (deletionMode.HasFlag(DeletionMode.Structures) && tile.Structures.Count > 0)
            {
                returnValue = tile.Structures[0];
                RemoveBuilding(tile.Structures[0]);
            }
            else if (deletionMode.HasFlag(DeletionMode.TerrainObjects) && tile.TerrainObject != null)
            {
                returnValue = tile.TerrainObject;
                RemoveTerrainObject(tile.CoordsToPoint());
            }

            return returnValue;
        }

        public bool HasObjectToDelete(Point2D cellCoords, DeletionMode deletionMode)
        {
            var tile = GetTile(cellCoords.X, cellCoords.Y);
            if (tile == null)
                return false;

            if (deletionMode.HasFlag(DeletionMode.CellTags) && tile.CellTag != null)
            {
                return true;
            }
            else if (deletionMode.HasFlag(DeletionMode.Waypoints) && tile.Waypoints.Count > 0)
            {
                return true;
            }
            else if (deletionMode.HasFlag(DeletionMode.Infantry) && tile.HasInfantry())
            {
                return true;
            }
            else if (deletionMode.HasFlag(DeletionMode.Aircraft) && tile.Aircraft.Count > 0)
            {
                return true;
            }
            else if (deletionMode.HasFlag(DeletionMode.Vehicles) && tile.Vehicles.Count > 0)
            {
                return true;
            }
            else if (deletionMode.HasFlag(DeletionMode.Structures) && tile.Structures.Count > 0)
            {
                return true;
            }
            else if (deletionMode.HasFlag(DeletionMode.TerrainObjects) && tile.TerrainObject != null)
            {
                return true;
            }

            return false;
        }

        public List<TechnoType> GetAllTechnoTypes()
        {
            List<TechnoType> technoTypes = new List<TechnoType>();
            technoTypes.AddRange(Rules.BuildingTypes);
            technoTypes.AddRange(Rules.UnitTypes);
            technoTypes.AddRange(Rules.InfantryTypes);
            technoTypes.AddRange(Rules.AircraftTypes);

            return technoTypes;
        }

        public int GetOverlayFrameIndex(Point2D cellCoords)
        {
            var cell = GetTile(cellCoords);
            if (cell.Overlay == null || cell.Overlay.OverlayType == null)
                return Constants.NO_OVERLAY;

            if (!cell.Overlay.OverlayType.Tiberium)
                return cell.Overlay.FrameIndex;

            // Smooth out tiberium

            int[] frameIndexesForEachAdjacentTiberiumCell = { 0, 1, 3, 4, 6, 7, 8, 10, 11 };
            int adjTiberiumCount = 0;

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (y == 0 && x == 0)
                        continue;

                    var otherTile = GetTile(cellCoords + new Point2D(x, y));
                    if (otherTile != null && otherTile.Overlay != null)
                    {
                        if (otherTile.Overlay.OverlayType.Tiberium)
                            adjTiberiumCount++;
                    }
                }
            }

            return frameIndexesForEachAdjacentTiberiumCell[adjTiberiumCount];
        }

        public void DoForAllValidTiles(Action<MapTile> action)
        {
            for (int y = 0; y < Tiles.Length; y++)
            {
                for (int x = 0; x < Tiles[y].Length; x++)
                {
                    MapTile tile = Tiles[y][x];

                    if (tile == null)
                        continue;

                    action(tile);
                }
            }
        }

        public void DoForRectangle(int startX, int startY, int endX, int endY, Action<MapTile> action, bool callForNullTiles)
        {
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    MapTile tile = GetTile(x, y);

                    if (tile == null && !callForNullTiles)
                        continue;

                    action(tile);
                }
            }
        }

        public void DoForAllTechnos(Action<TechnoBase> action)
        {
            var aircraftCopy = new List<Aircraft>(Aircraft);
            var infantryCopy = new List<Infantry>(Infantry);
            var unitsCopy = new List<Unit>(Units);
            var structuresCopy = new List<Structure>(Structures);

            aircraftCopy.ForEach(a => action(a));
            infantryCopy.ForEach(i => action(i));
            unitsCopy.ForEach(u => action(u));
            structuresCopy.ForEach(s => action(s));
        }

        public void DoForAllTerrainObjects(Action<TerrainObject> action)
        {
            var terrainObjectsCopy = new List<TerrainObject>(TerrainObjects);
            terrainObjectsCopy.ForEach(t => action(t));
        }

        public void SortWaypoints() => Waypoints = Waypoints.OrderBy(wp => wp.Identifier).ToList();

        public int GetAutoLATIndex(MapTile mapTile, int baseLATTileSetIndex, int transitionLATTileSetIndex, Func<TileSet, bool> miscChecker)
        {
            foreach (var autoLatData in AutoLATType.AutoLATData)
            {
                if (TransitionArrayDataMatches(autoLatData.TransitionMatchArray, mapTile, baseLATTileSetIndex, transitionLATTileSetIndex, miscChecker))
                {
                    return autoLatData.TransitionTypeIndex;
                }
            }

            return -1;
        }

        public void RefreshCellLighting(LightingPreviewMode lightingPreviewMode)
        {
            const double LeptonsPerCell = 256.0;

            var lightSources = Structures.FindAll(s => s.ObjectType.LightVisibility > 0);

            MapColor globalLightingColor = Lighting.MapColorFromPreviewMode(lightingPreviewMode);

            DoForAllValidTiles(cell =>
            {
                double currentR = globalLightingColor.R;
                double currentG = globalLightingColor.G;
                double currentB = globalLightingColor.B;

                Point2D cellCoords = cell.CoordsToPoint();

                // Check all the light sources and how they affect this light
                foreach (var building in lightSources)
                {
                    int xDifference = cellCoords.X - building.Position.X;
                    int yDifference = cellCoords.Y - building.Position.Y;

                    double distanceInCells = Math.Sqrt(xDifference * xDifference + yDifference * yDifference);
                    double distanceInLeptons = distanceInCells * LeptonsPerCell;

                    if (distanceInLeptons > building.ObjectType.LightVisibility)
                        continue;

                    double distanceRatio = 1.0 - (distanceInLeptons / building.ObjectType.LightVisibility);

                    double intensity = distanceRatio * building.ObjectType.LightIntensity * 10.0;

                    // Calculate tint
                    double highestComponent = Math.Max(building.ObjectType.LightRedTint, Math.Max(building.ObjectType.LightGreenTint, building.ObjectType.LightBlueTint));
                    double redStrength = building.ObjectType.LightRedTint / highestComponent;
                    double greenStrength = building.ObjectType.LightGreenTint / highestComponent;
                    double blueStrength = building.ObjectType.LightBlueTint / highestComponent;

                    redStrength *= intensity;
                    greenStrength *= intensity;
                    blueStrength *= intensity;

                    currentR += redStrength;
                    currentG += greenStrength;
                    currentB += blueStrength;
                }

                // TODO cap lighting values to 2.0?
                cell.CellLighting = new MapColor(currentR, currentG, currentB);
            });
        }

        /// <summary>
        /// Convenience structure for <see cref="TransitionArrayDataMatches(int[], MapTile, int, int)"/>.
        /// </summary>
        struct NearbyTileData
        {
            public int XOffset;
            public int YOffset;
            public int DirectionIndex;

            public NearbyTileData(int xOffset, int yOffset, int directionIndex)
            {
                XOffset = xOffset;
                YOffset = yOffset;
                DirectionIndex = directionIndex;
            }
        }

        /// <summary>
        /// Checks if specific transition data matches for a tile.
        /// If it does, then the tile should use the LAT transition index related to the data.
        /// </summary>
        private bool TransitionArrayDataMatches(int[] transitionData, MapTile mapTile, int desiredTileSetId1, int desiredTileSetId2, Func<TileSet, bool> miscChecker)
        {
            var nearbyTiles = new NearbyTileData[]
            {
                new NearbyTileData(0, -1, AutoLATType.NE_INDEX),
                new NearbyTileData(-1, 0, AutoLATType.NW_INDEX),
                new NearbyTileData(0, 0, AutoLATType.CENTER_INDEX),
                new NearbyTileData(1, 0, AutoLATType.SE_INDEX),
                new NearbyTileData(0, 1, AutoLATType.SW_INDEX)
            };

            foreach (var nearbyTile in nearbyTiles)
            {
                if (!TileSetMatchesExpected(mapTile.X + nearbyTile.XOffset, mapTile.Y + nearbyTile.YOffset,
                    transitionData, nearbyTile.DirectionIndex, desiredTileSetId1, desiredTileSetId2, miscChecker))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TileSetMatchesExpected(int x, int y, int[] transitionData, int transitionDataIndex, int desiredTileSetId1, int desiredTileSetId2, Func<TileSet, bool> miscChecker)
        {
            var tile = GetTile(x, y);

            if (tile == null)
                return true;

            bool shouldMatch = transitionData[transitionDataIndex] > 0;

            int tileSetId = TheaterInstance.GetTileSetId(tile.TileIndex);
            var tileSet = TheaterInstance.Theater.TileSets[tileSetId];
            if (shouldMatch && (tileSetId != desiredTileSetId1 && tileSetId != desiredTileSetId2 && (miscChecker == null || !miscChecker(tileSet))))
                return false;

            if (!shouldMatch && (tileSetId == desiredTileSetId1 || tileSetId == desiredTileSetId2 || (miscChecker != null && miscChecker(tileSet))))
                return false;

            return true;
        }

        /// <summary>
        /// Generates an unique internal ID.
        /// Used for new TaskForces, Scripts, TeamTypes and Triggers.
        /// </summary>
        public string GetNewUniqueInternalId()
        {
            int id = 1000000;
            string idString = string.Empty;

            while (true)
            {
                idString = "0" + id.ToString(CultureInfo.InvariantCulture);

                if (TaskForces.Exists(tf => tf.ININame == idString) || 
                    Scripts.Exists(s => s.ININame == idString) || 
                    TeamTypes.Exists(tt => tt.ININame == idString) ||
                    Triggers.Exists(t => t.ID == idString) ||
                    Tags.Exists(t => t.ID == idString) ||
                    AITriggerTypes.Exists(t => t.ININame == idString) ||
                    LoadedINI.SectionExists(idString))
                {
                    id++;
                    continue;
                }

                break;
            }

            return idString;
        }

        /// <summary>
        /// Re-generates internal IDs for all editor-generated scripting elements of the map.
        /// </summary>
        public void RegenerateInternalIds()
        {
            string idBeginning = "0100";
            List<IIDContainer> scriptElements = new List<IIDContainer>();

            Func<IIDContainer, bool> f = (scriptElement) => scriptElement.GetInternalID().StartsWith(idBeginning);

            var tfs = TaskForces.FindAll(tf => f(tf));
            var scripts = Scripts.FindAll(s => f(s));
            var teamtypes = TeamTypes.FindAll(tt => f(tt));
            var tags = Tags.FindAll(t => f(t));
            var triggers = Triggers.FindAll(t => f(t));

            scriptElements.AddRange(TaskForces.FindAll(tf => f(tf)));
            scriptElements.AddRange(Scripts.FindAll(s => f(s)));
            scriptElements.AddRange(TeamTypes.FindAll(tt => f(tt)));
            scriptElements.AddRange(Triggers.FindAll(t => f(t)));
            scriptElements.AddRange(Tags.FindAll(t => f(t)));
            scriptElements = scriptElements.OrderBy(se => se.GetInternalID()).ToList();

            tfs.ForEach(tf => LoadedINI.RemoveSection(tf.ININame));
            scripts.ForEach(s => LoadedINI.RemoveSection(s.ININame));
            teamtypes.ForEach(tt => LoadedINI.RemoveSection(tt.ININame));
            // Our map writing system takes care of the tags and triggers, no need to manually remove their INI entries
            // (they don't have sections)

            // TODO implement old-ID-to-new-ID lookup table, necessary for fixing up trigger references

            // Zero out the internal IDs
            scriptElements.ForEach(se => se.SetInternalID(string.Empty));

            // Generate new internal IDs
            scriptElements.ForEach(se => se.SetInternalID(GetNewUniqueInternalId()));
        }

        public void InitializeRules(GameConfigINIFiles gameConfigINIFiles)
        {
            if (gameConfigINIFiles == null)
                throw new ArgumentNullException(nameof(gameConfigINIFiles));

            Rules = new Rules();
            Rules.InitFromINI(gameConfigINIFiles.RulesIni, initializer);

            Rules.InitArt(gameConfigINIFiles.ArtIni, initializer);

            if (gameConfigINIFiles.FirestormIni != null)
            {
                Rules.InitFromINI(gameConfigINIFiles.FirestormIni, initializer);

                if (gameConfigINIFiles.ArtFSIni != null)
                    Rules.InitArt(gameConfigINIFiles.ArtFSIni, initializer);
            }

            var editorRulesIni = new IniFile(Environment.CurrentDirectory + "/Config/EditorRules.ini");
            Rules.InitEditorOverrides(editorRulesIni);

            Rules.InitFromINI(editorRulesIni, initializer, false);

            InitStandardHouseTypesAndHouses(editorRulesIni, gameConfigINIFiles.RulesIni, gameConfigINIFiles.FirestormIni);

            if (gameConfigINIFiles.AIIni != null)
                Rules.InitAI(gameConfigINIFiles.AIIni, EditorConfig.TeamTypeFlags);

            if (gameConfigINIFiles.AIFSIni != null)
                Rules.InitAI(gameConfigINIFiles.AIFSIni, EditorConfig.TeamTypeFlags);

            // Load impassable cell information for terrain types
            var impassableTerrainObjectsIni = new IniFile(Environment.CurrentDirectory + "/Config/TerrainTypeImpassability.ini");

            Rules.TerrainTypes.ForEach(tt =>
            {
                string value = impassableTerrainObjectsIni.GetStringValue(tt.ININame, "ImpassableCells", null);
                if (string.IsNullOrWhiteSpace(value))
                    return;

                string[] cellInfos = value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var cellInfo in cellInfos)
                {
                    Point2D point = Point2D.FromString(cellInfo);
                    if (tt.ImpassableCells == null)
                        tt.ImpassableCells = new List<Point2D>(2);

                    tt.ImpassableCells.Add(point);
                }
            });
        }

        private void InitStandardHouseTypesAndHouses(IniFile editorRulesIni, IniFile rulesIni, IniFile firestormIni)
        {
            StandardHouseTypes = Rules.GetStandardHouseTypes(editorRulesIni);
            if (StandardHouseTypes.Count == 0)
                StandardHouseTypes = Rules.GetStandardHouseTypes(rulesIni);
            if (StandardHouseTypes.Count == 0 && firestormIni != null)
                StandardHouseTypes = Rules.GetStandardHouseTypes(firestormIni);

            StandardHouses = StandardHouseTypes.Select(ht => HouseFromHouseType(ht)).ToList();
        }

        public House HouseFromHouseType(HouseType houseType)
        {
            var house = new House(houseType.ININame, houseType);
            house.XNAColor = houseType.XNAColor;

            if (!Constants.UseCountries)
                house.ActsLike = houseType.Index;
            else
                house.Country = houseType.ININame;

            return house;
        }

        /// <summary>
        /// Re-initializes art.
        /// Call after initializing rules overrides from map.
        /// The game seems to initialize rules and art in multiple phases,
        /// where it loads Art configurations each time after
        /// it has loaded a Rules configuration.
        /// Loading them in only one phase can cause objects using Image=
        /// overrides in both rules and the map to bug out.
        /// </summary>
        public void PostInitializeRules_ReinitializeArt(IniFile artIni, IniFile artFirestormIni)
        {
            Rules.InitArt(artIni, initializer);

            if (artFirestormIni != null)
            {
                Rules.InitArt(artFirestormIni, initializer);
            }
        }

        /// <summary>
        /// Checks the map for issues.
        /// Returns a list of issues found.
        /// TODO split up into multiple functions, preferably within a separate file/class
        /// </summary>
        public List<string> CheckForIssues()
        {
            var issueList = new List<string>();

            DoForAllValidTiles(cell =>
            {
                if (cell.HasTiberium())
                {
                    ITileImage tile = TheaterInstance.GetTile(cell.TileIndex);
                    ISubTileImage subTile = tile.GetSubTile(cell.SubTileIndex);

                    // Check whether the cell has tiberium on an impassable terrain type
                    if (Helpers.IsLandTypeImpassable(subTile.TmpImage.TerrainType, true))
                    {
                        issueList.Add($"Cell at {cell.CoordsToPoint()} has Tiberium on an otherwise impassable cell. This can cause harvesters to get stuck.");
                    }

                    // Check for tiberium on ramps that don't support tiberium on them
                    if (subTile.TmpImage.RampType > RampType.South)
                    {
                        issueList.Add($"Cell at {cell.CoordsToPoint()} has Tiberium on a ramp that does not allow Tiberium on it. This can crash the game!");
                    }
                }
            });

            // Check for teamtypes having no taskforce or script
            TeamTypes.ForEach(tt =>
            {
                if (tt.TaskForce == null)
                    issueList.Add($"TeamType \"{tt.Name}\" has no TaskForce set!");

                if (tt.Script == null)
                    issueList.Add($"TeamType \"{tt.Name}\" has no Script set!");
            });

            const int EnableTriggerActionIndex = 53;
            const int TriggerParamIndex = 1;
            const int DisableTriggerActionIndex = 54;

            // Check for triggers that are disabled and are never enabled by any other triggers
            Triggers.ForEach(trigger =>
            {
                if (!trigger.Disabled)
                    return;

                const int RevealAllMapActionIndex = 16;

                // If this trigger has a "reveal all map" action, don't create an issue - those are usually only for debugging
                if (trigger.Actions.Exists(a => a.ActionIndex == RevealAllMapActionIndex))
                    return;

                // Allow the user to skip this warning by including "DEBUG" or "OBSOLETE" in the trigger's name
                if (trigger.Name.ToUpperInvariant().Contains("DEBUG") || trigger.Name.ToUpperInvariant().Contains("OBSOLETE"))
                    return;

                // Is this trigger enabled by another trigger?
                if (Triggers.Exists(otherTrigger => otherTrigger != trigger && otherTrigger.Actions.Exists(a => a.ActionIndex == EnableTriggerActionIndex && a.Parameters[TriggerParamIndex] == trigger.ID)))
                    return;

                // If it's not enabled by another trigger, add an issue
                issueList.Add($"Trigger \"{trigger.Name}\" ({trigger.ID}) is disabled and never enabled by another trigger." + Environment.NewLine +
                    "Did you forget to enable it? If the trigger exists for debugging purposes, add DEBUG or OBSOLETE to its name to skip this warning.");
            });

            // Check for triggers that are enabled by other triggers, but never disabled - enabling them is
            // useless in that case and likely means that there's a scripting issue
            Triggers.ForEach(trigger =>
            {
                if (trigger.Disabled)
                    return;

                // If this trigger is never enabled by another trigger, skip
                if (!Triggers.Exists(otherTrigger => otherTrigger != trigger && otherTrigger.Actions.Exists(a => a.ActionIndex == EnableTriggerActionIndex && a.Parameters[TriggerParamIndex] == trigger.ID)))
                    return;

                // If this trigger is disabled by itself or some other trigger, skip
                if (Triggers.Exists(otherTrigger => otherTrigger.Actions.Exists(a => a.ActionIndex == DisableTriggerActionIndex && a.Parameters[TriggerParamIndex] == trigger.ID)))
                    return;

                // This trigger is never disabled, but it is enabled by at least 1 other trigger - add an issue
                issueList.Add($"Trigger \"{trigger.Name}\" ({trigger.ID}) is enabled by another trigger, but it is never in a disabled state" + Environment.NewLine +
                    "(it is neither disabled by default nor disabled by other triggers). Did you forget to disable it?");
            });

            // Check for triggers that enable themselves, there's no need to ever do this -> either redundant action or a scripting error
            Triggers.ForEach(trigger =>
            {
                if (!trigger.Actions.Exists(a => a.ActionIndex == EnableTriggerActionIndex && a.Parameters[TriggerParamIndex] == trigger.ID))
                    return;

                issueList.Add($"Trigger \"{trigger.Name}\" ({trigger.ID}) has an action for enabling itself. Is it supposed to enable something else instead?");
            });

            // Check that the primary player house has "Player Control" enabled in case [Basic] Player= is specified
            // (iow. this is a singleplayer mission)
            if (!string.IsNullOrWhiteSpace(Basic.Player) && !Helpers.IsStringNoneValue(Basic.Player))
            {
                House matchingHouse = GetHouses().Find(h => h.ININame == Basic.Player);
                if (matchingHouse == null)
                    issueList.Add("A nonexistent house has been specified in [Basic] Player= .");
                else if (!matchingHouse.PlayerControl)
                    issueList.Add("The human player's house does not have the \"Player-Controlled\" flag checked.");
            }

            // Check for more than 127 tunnel tubes
            if (Tubes.Count > MaxTubes)
            {
                issueList.Add($"The map has more than {MaxTubes} tunnel tubes. This might cause issues when units cross the tunnels.");
            }

            // Check for vehicles sharing the same follows index and for vehicles following themselves
            List<Unit> followedUnits = new List<Unit>();
            for (int i = 0; i < Units.Count; i++)
            {
                var unit = Units[i];
                int followsId = unit.FollowerUnit == null ? -1 : Units.IndexOf(unit.FollowerUnit);
                if (followsId == -1)
                    continue;

                if (followedUnits.Contains(unit.FollowerUnit))
                {
                    issueList.Add($"Multiple units are configured to make unit {unit.FollowerUnit.UnitType.ININame} at {unit.FollowerUnit.Position} to follow them! " + Environment.NewLine +
                        $"This can cause strange behaviour in the game. {unit.UnitType.ININame} at {unit.Position} is one of the followed units.");
                }
                else
                {
                    followedUnits.Add(unit.FollowerUnit);
                }

                if (followsId < -1)
                    issueList.Add($"Unit {unit.UnitType.ININame} at {unit.Position} has a follower ID below -1. It is unknown how the game reacts to this.");
                else if (followsId == i)
                    issueList.Add($"Unit {unit.UnitType.ININame} at {unit.Position} follows itself! This can cause the game to crash or freeze!");
            }

            var reportedTeams = new List<TeamType>();

            // Check for AI Trigger linked TeamTypes having Max=0
            foreach (var aiTrigger in AITriggerTypes)
            {
                CheckForAITriggerTeamWithMaxZeroIssue(aiTrigger, aiTrigger.PrimaryTeam, reportedTeams, issueList);
                CheckForAITriggerTeamWithMaxZeroIssue(aiTrigger, aiTrigger.SecondaryTeam, reportedTeams, issueList);
            }

            // Check for triggers having 0 events or actions
            foreach (var trigger in Triggers)
            {
                if (trigger.Conditions.Count == 0)
                {
                    issueList.Add($"Trigger '{trigger.Name}' has 0 events specified. It will never be fired. Did you forget to give it events?");
                }

                if (trigger.Actions.Count == 0)
                {
                    issueList.Add($"Trigger '{trigger.Name}' has 0 actions specified. It will not do anything. Did you forget to give it actions?");
                }
            }

            // Check for triggers using the "Entered by" event without being attached to anything
            const int EnteredByConditionIndex = 1;
            foreach (var trigger in Triggers)
            {
                if (!trigger.Conditions.Exists(c => c.ConditionIndex == EnteredByConditionIndex))
                    continue;

                var tag = Tags.Find(t => t.Trigger == trigger);
                if (tag == null)
                    continue;

                if (!Structures.Exists(s => s.AttachedTag == tag) &&
                    !Infantry.Exists(i => i.AttachedTag == tag) &&
                    !Units.Exists(u => u.AttachedTag == tag) &&
                    !Aircraft.Exists(a => a.AttachedTag == tag) &&
                    !TeamTypes.Exists(tt => tt.Tag == tag) &&
                    !CellTags.Exists(ct => ct.Tag == tag) &&
                    !Triggers.Exists(otherTrigger => otherTrigger.LinkedTrigger == trigger))
                {
                    issueList.Add($"Trigger '{trigger.Name}' is using the \"Entered by...\" event without being attached to any object, cell, or team. Did you forget to attach it?");
                }
            }

            // Check for triggers using the "Bridge destroyed" event without being linked to any cell
            const int BridgeDestroyedConditionIndex = 31;
            foreach (var trigger in Triggers)
            {
                if (!trigger.Conditions.Exists(c => c.ConditionIndex == BridgeDestroyedConditionIndex))
                    continue;

                var tag = Tags.Find(t => t.Trigger == trigger);
                if (tag == null)
                    continue;

                if (!CellTags.Exists(ct => ct.Tag == tag))
                {
                    issueList.Add($"Trigger '{trigger.Name}' is using the \"Bridge destroyed\" event, but it is not attached to any CellTag. Did you forget to place a celltag for it?");
                }
            }

            // Check for triggers using an object-specific event (like "destroyed" or "damaged") without
            // being linked to any object
            var objectSpecificEventIndexes = new List<int>() {
                6,  // Attacked by any house
                7,  // Destroyed by any house
                33, // Selected by player
                34, // Comes near waypoint
                38, // First damaged (combat only)
                39, // Half health (combat only)
                40, // Quarter health (combat only)
                41, // First damaged (any source)
                42, // Half health (any source)
                43, // Quarter health (any source)
                44, // Attacked by (house)
                48  // Destroyed by anything
                /*55 Limpet Attached - need to think how to handle YR*/
            };

            foreach (var trigger in Triggers)
            {
                int indexInList = objectSpecificEventIndexes.FindIndex(eventIndex => trigger.Conditions.Exists(c => c.ConditionIndex == eventIndex));
                if (indexInList == -1)
                    continue;

                int usedEventIndex = objectSpecificEventIndexes[indexInList];
                var triggerEventType = EditorConfig.TriggerEventTypes.GetValueOrDefault(usedEventIndex);

                if (triggerEventType == null)
                    continue;

                var tag = Tags.Find(t => t.Trigger == trigger);
                if (tag == null)
                    continue;

                if (!Structures.Exists(s => s.AttachedTag == tag) &&
                    !Infantry.Exists(i => i.AttachedTag == tag) &&
                    !Units.Exists(u => u.AttachedTag == tag) &&
                    !Aircraft.Exists(a => a.AttachedTag == tag) &&
                    !TeamTypes.Exists(tt => tt.Tag == tag) &&
                    !Triggers.Exists(otherTrigger => otherTrigger.LinkedTrigger == trigger))
                {
                    string eventName = triggerEventType.Name;

                    issueList.Add($"Trigger '{trigger.Name}' is using the {eventName} event without being attached to any object or team. Did you forget to attach it?");
                }
            }

            // Check for AITriggerTypes that have a non-existing condition object
            foreach (var aiTrigger in AITriggerTypes)
            {
                if (!string.IsNullOrWhiteSpace(aiTrigger.ConditionObjectString) &&
                    !Helpers.IsStringNoneValue(aiTrigger.ConditionObjectString) &&
                    Rules.FindTechnoType(aiTrigger.ConditionObjectString) == null)
                {
                    issueList.Add($"AITrigger '{aiTrigger.Name}' has a condition object '{aiTrigger.ConditionObjectString}' that does not exist in Rules!");
                }
            }

            // Check for triggers being attached to themselves (potentially recursively)
            foreach (var trigger in Triggers)
            {
                if (trigger.LinkedTrigger == null)
                    continue;

                Trigger linkedTrigger = trigger.LinkedTrigger;
                while (linkedTrigger != null)
                {
                    if (linkedTrigger == trigger)
                    {
                        issueList.Add($"Trigger '{trigger.Name}' is attached to itself (potentially through other triggers). This will cause the game to crash!");
                        break;
                    }

                    linkedTrigger = linkedTrigger.LinkedTrigger;
                }
            }

            // Check for invalid TeamType parameter values in triggers
            foreach (var trigger in Triggers)
            {
                foreach (var action in trigger.Actions)
                {
                    if (!EditorConfig.TriggerActionTypes.TryGetValue(action.ActionIndex, out var triggerActionType))
                        continue;

                    for (int i = 0; i < triggerActionType.Parameters.Length; i++)
                    {
                        if (triggerActionType.Parameters[i].TriggerParamType == TriggerParamType.TeamType)
                        {
                            if (!TeamTypes.Exists(tt => tt.ININame == action.Parameters[i]))
                            {
                                issueList.Add($"Trigger '{trigger.Name}' has a nonexistent TeamType specified as a parameter for one or more of its actions.");
                                break;
                            }
                        }
                    }
                }
            }

            // Check for triggers having too many actions. This can cause a crash because the game's buffer for parsing trigger actions
            // is limited (to 512 chars according to ModEnc)
            if (Constants.WarnOfTooManyTriggerActions)
            {
                const int maxActionCount = 18;
                foreach (var trigger in Triggers)
                {
                    if (trigger.Actions.Count > maxActionCount)
                    {
                        issueList.Add($"Trigger '{trigger.Name}' has more than {maxActionCount} actions! This can cause the game to crash! Consider splitting it up to multiple triggers.");
                    }
                }
            }

            return issueList;
        }

        private void CheckForAITriggerTeamWithMaxZeroIssue(AITriggerType aiTrigger, TeamType team, List<TeamType> reportedTeams, List<string> issueList)
        {
            if (reportedTeams.Contains(team))
                return;

            if (team == null)
                return;

            if (TeamTypes.Contains(team) && team.Max == 0)
            {
                issueList.Add($"Team '{team.Name}', linked to AITrigger '{aiTrigger.Name}', has Max=0. This prevents the AI from building the team.");
                reportedTeams.Add(team);
            }
        }

        public void Clear()
        {
            LoadedINI = null;

            TheaterInstance = null;

            for (int y = 0; y < Tiles.Length; y++)
            {
                for (int x = 0; x < Tiles[y].Length; x++)
                {
                    Tiles[y][x] = null;
                }
            }

            Tiles = null;

            Rules = null;
            EditorConfig = null;
            Basic = null;

            Aircraft.Clear();
            Infantry.Clear();
            Units.Clear();
            Structures.Clear();
            StandardHouses.Clear();
            Houses.Clear();
            TerrainObjects.Clear();
            Waypoints.Clear();
            TaskForces.Clear();
            Triggers.Clear();
            Tags.Clear();
            CellTags.Clear();
            Scripts.Clear();
            TeamTypes.Clear();
            AITriggerTypes.Clear();
            LocalVariables.Clear();
            Tubes.Clear();
            GraphicalBaseNodes.Clear();

            Aircraft = null;
            Infantry = null;
            Units = null;
            Structures = null;
            StandardHouses = null;
            Houses = null;
            TerrainObjects = null;
            Waypoints = null;
            TaskForces = null;
            Triggers = null;
            Tags = null;
            CellTags = null;
            Scripts = null;
            TeamTypes = null;
            AITriggerTypes = null;
            LocalVariables = null;
            Tubes = null;
            GraphicalBaseNodes = null;
        }
    }
}
