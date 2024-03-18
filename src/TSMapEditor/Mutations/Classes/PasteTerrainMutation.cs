using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// Defines types that can be copied from the map.
    /// </summary>
    [Flags]
    public enum CopiedEntryType
    {
        Invalid = 0,
        Terrain = 1,
        Overlay = 2,
        Smudge = 4,
        TerrainObject = 8,
        Structure = 16,
        Vehicle = 32,
        Infantry = 64
    }

    public class CopiedMapDataSerializationException : Exception
    {
        public CopiedMapDataSerializationException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Base class for all objects that can be copied from a map.
    /// </summary>
    public abstract class CopiedMapEntry
    {
        public Point2D Offset { get; protected set; }
        public abstract CopiedEntryType EntryType { get; }

        private byte[] buffer;

        protected CopiedMapEntry()
        {
        }

        protected CopiedMapEntry(Point2D offset)
        {
            Offset = offset;
        }

        protected int ReadInt(Stream stream)
        {
            if (stream.Read(buffer, 0, 4) != 4)
                throw new CopiedMapDataSerializationException("Failed to read integer from stream: end of stream");

            return BitConverter.ToInt32(buffer, 0);
        }

        protected long ReadLong(Stream stream)
        {
            if (stream.Read(buffer, 0, 8) != 8)
                throw new CopiedMapDataSerializationException("Failed to read integer from stream: end of stream");

            return BitConverter.ToInt64(buffer, 0);
        }

        protected string ReadASCIIString(Stream stream)
        {
            int length = ReadInt(stream);
            byte[] stringBuffer = new byte[length];
            if (stream.Read(stringBuffer, 0, length) != length)
                throw new CopiedMapDataSerializationException("Failed to read string from stream: end of stream");

            string result = Encoding.ASCII.GetString(stringBuffer);
            return result;
        }

        protected byte[] ASCIIStringToBytes(string str)
        {
            byte[] buffer = new byte[sizeof(int) + str.Length];
            Array.Copy(BitConverter.GetBytes(str.Length), buffer, sizeof(int));
            byte[] stringBytes = Encoding.ASCII.GetBytes(str);
            Array.Copy(stringBytes, 0, buffer, sizeof(int), stringBytes.Length);
            return buffer;
        }

        /// <summary>
        /// Reads all of the map entry's data from a stream.
        /// </summary>
        /// <param name="stream">The stream to read the data from.</param>
        public void ReadData(Stream stream)
        {
            buffer = new byte[8];

            int x = ReadInt(stream);
            int y = ReadInt(stream);
            Offset = new Point2D(x, y);
            ReadCustomData(stream);
            buffer = null; // Free memory
        }

        /// <summary>
        /// When overriden in a derived class, reads and applies this instance's custom data from a stream.
        /// </summary>
        /// <param name="stream">The stream to read the custom data from.</param>
        protected abstract void ReadCustomData(Stream stream);

        /// <summary>
        /// Writes all of the map entry's data to a stream.
        /// </summary>
        /// <param name="stream">The stream to write the data to.</param>
        public void WriteData(Stream stream)
        {
            stream.WriteByte((byte)EntryType);
            byte[] offsetData = Offset.GetData();
            stream.Write(offsetData);

            byte[] data = GetCustomData();
            stream.Write(data);
        }

        /// <summary>
        /// When overriden in a derived class, returns this instance's custom data serialized to an array of bytes.
        /// </summary>
        protected abstract byte[] GetCustomData();
    }

    public class CopiedTerrainEntry : CopiedMapEntry
    {
        public int TileIndex;
        public byte SubTileIndex;
        public byte HeightOffset;

        public CopiedTerrainEntry()
        {
        }

        public CopiedTerrainEntry(Point2D offset, int tileIndex, byte subTileIndex, byte heightOffset) : base(offset)
        {
            TileIndex = tileIndex;
            SubTileIndex = subTileIndex;
            HeightOffset = heightOffset;
        }

        public override CopiedEntryType EntryType => CopiedEntryType.Terrain;

        protected override byte[] GetCustomData()
        {
            byte[] returnValue = new byte[6];
            Array.Copy(BitConverter.GetBytes(TileIndex), returnValue, 4);
            returnValue[4] = SubTileIndex;
            returnValue[5] = HeightOffset;

            return returnValue;
        }

        protected override void ReadCustomData(Stream stream)
        {
            TileIndex = ReadInt(stream);
            SubTileIndex = (byte)stream.ReadByte();
            HeightOffset = (byte)stream.ReadByte();
        }
    }

    public class CopiedOverlayEntry : CopiedMapEntry
    {
        public string OverlayTypeName;
        public int FrameIndex;

        public CopiedOverlayEntry()
        {
        }

        public CopiedOverlayEntry(Point2D offset, string overlayTypeName, int frameIndex) : base(offset)
        {
            OverlayTypeName = overlayTypeName;
            FrameIndex = frameIndex;
        }

        public override CopiedEntryType EntryType => CopiedEntryType.Overlay;

        protected override byte[] GetCustomData()
        {
            byte[] nameBytes = ASCIIStringToBytes(OverlayTypeName);
            byte[] buffer = new byte[ nameBytes.Length + sizeof(int)];
            Array.Copy(nameBytes, buffer, nameBytes.Length);
            Array.Copy(BitConverter.GetBytes(FrameIndex), 0, buffer, nameBytes.Length, sizeof(int));
            return buffer;
        }

        protected override void ReadCustomData(Stream stream)
        {
            OverlayTypeName = ReadASCIIString(stream);
            FrameIndex = ReadInt(stream);
        }
    }

    public class CopiedSmudgeEntry : CopiedMapEntry
    {
        public string SmudgeTypeName;

        public CopiedSmudgeEntry() 
        {
        }

        public CopiedSmudgeEntry(Point2D offset, string smudgeTypeName) : base(offset)
        {
            SmudgeTypeName = smudgeTypeName;
        }

        public override CopiedEntryType EntryType => CopiedEntryType.Smudge;

        protected override byte[] GetCustomData()
        {
            byte[] nameBytes = ASCIIStringToBytes(SmudgeTypeName);
            return nameBytes;
        }

        protected override void ReadCustomData(Stream stream)
        {
            SmudgeTypeName = ReadASCIIString(stream);
        }
    }

    public class CopiedObjectEntry : CopiedMapEntry
    {
        public string ObjectTypeName;

        public CopiedObjectEntry() { }

        public CopiedObjectEntry(Point2D offset, string objectTypeName) : base(offset)
        {
            ObjectTypeName = objectTypeName;
        }

        public override CopiedEntryType EntryType => throw new NotImplementedException();

        protected override byte[] GetCustomData()
        {
            return ASCIIStringToBytes(ObjectTypeName);
        }

        protected override void ReadCustomData(Stream stream)
        {
            ObjectTypeName = ReadASCIIString(stream);
        }
    }

    public class CopiedTerrainObjectEntry : CopiedObjectEntry
    {
        public CopiedTerrainObjectEntry()
        {
        }

        public CopiedTerrainObjectEntry(Point2D offset, string terrainObjectTypeName) : base(offset, terrainObjectTypeName)
        {
        }

        public override CopiedEntryType EntryType => CopiedEntryType.TerrainObject;
    }

    public class CopiedTechnoEntry : CopiedObjectEntry
    {
        public CopiedTechnoEntry()
        {
        }

        public CopiedTechnoEntry(Point2D offset, string objectTypeName, string ownerName, int hp, int veterancy, byte facing, string mission) : base(offset, objectTypeName)
        {
            HP = hp;
            Veterancy = veterancy;
            Facing = facing;
            OwnerHouseName = ownerName;
            Mission = mission;
        }

        public int HP;
        public int Veterancy;
        public byte Facing;
        public string OwnerHouseName;
        public string Mission;

        protected override byte[] GetCustomData()
        {
            byte[] objectTypeBuffer = ASCIIStringToBytes(ObjectTypeName);
            byte[] ownerBuffer = ASCIIStringToBytes(OwnerHouseName);
            byte[] missionBuffer = ASCIIStringToBytes(Mission);
            byte[] result = new byte[sizeof(int) + sizeof(int) + 1 + objectTypeBuffer.Length + ownerBuffer.Length + missionBuffer.Length];
            Array.Copy(BitConverter.GetBytes(HP), 0, result, 0, sizeof(int));
            Array.Copy(BitConverter.GetBytes(Veterancy), 0, result, sizeof(int), sizeof(int));
            result[sizeof(int) * 2] = Facing;
            Array.Copy(objectTypeBuffer, 0, result, sizeof(int) + sizeof(int) + 1, objectTypeBuffer.Length);
            Array.Copy(ownerBuffer, 0, result, sizeof(int) + sizeof(int) + 1 + objectTypeBuffer.Length, ownerBuffer.Length);
            Array.Copy(missionBuffer, 0, result, sizeof(int) + sizeof(int) + 1 + objectTypeBuffer.Length + ownerBuffer.Length, missionBuffer.Length);
            return result;
        }

        protected override void ReadCustomData(Stream stream)
        {
            HP = ReadInt(stream);
            Veterancy = ReadInt(stream);
            Facing = (byte)stream.ReadByte();
            ObjectTypeName = ReadASCIIString(stream);
            OwnerHouseName = ReadASCIIString(stream);
            Mission = ReadASCIIString(stream);
        }
    }

    public class CopiedVehicleEntry : CopiedTechnoEntry
    {
        public CopiedVehicleEntry()
        {
        }

        public CopiedVehicleEntry(Point2D offset, string vehicleTypeName, string ownerName, int hp, int veterancy, byte facing, string mission) : base(offset, vehicleTypeName, ownerName, hp, veterancy, facing, mission)
        {
        }

        public override CopiedEntryType EntryType => CopiedEntryType.Vehicle;
    }

    public class CopiedStructureEntry : CopiedTechnoEntry
    {
        public CopiedStructureEntry()
        {
        }

        public CopiedStructureEntry(Point2D offset, string structureTypeName, string ownerName, int hp, int veterancy, byte facing, string mission) : base(offset, structureTypeName, ownerName, hp, veterancy, facing, mission)
        {
        }

        public override CopiedEntryType EntryType => CopiedEntryType.Structure;
    }

    public class CopiedInfantryEntry : CopiedTechnoEntry
    {
        public CopiedInfantryEntry()
        {
        }

        public CopiedInfantryEntry(Point2D offset, string infantryTypeName, string ownerName, int hp, int veterancy, byte facing, string mission, SubCell subCell) : base(offset, infantryTypeName, ownerName, hp, veterancy, facing, mission)
        {
            SubCell = subCell;
        }

        public SubCell SubCell { get; set; }

        public override CopiedEntryType EntryType => CopiedEntryType.Infantry;

        protected override byte[] GetCustomData()
        {
            byte[] baseData = base.GetCustomData();
            byte[] infantryData = new byte[baseData.Length + sizeof(SubCell)];
            Array.Copy(baseData, 0, infantryData, 0, baseData.Length);
            Array.Copy(BitConverter.GetBytes((int)SubCell), 0, infantryData, infantryData.Length - 4, sizeof(int));
            return infantryData;
        }

        protected override void ReadCustomData(Stream stream)
        {
            base.ReadCustomData(stream);
            SubCell = (SubCell)ReadInt(stream);
        }
    }


    public class CopiedMapData
    {
        public List<CopiedMapEntry> CopiedMapEntries { get; set; } = new List<CopiedMapEntry>();
        public ushort Width { get; set; }
        public ushort Height { get; set; }

        public byte[] Serialize()
        {
            byte[] bytes;

            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Write(BitConverter.GetBytes(Width));
                memoryStream.Write(BitConverter.GetBytes(Height));

                // Write entry count
                memoryStream.Write(BitConverter.GetBytes(CopiedMapEntries.Count));

                // Write entries
                foreach (var entry in CopiedMapEntries)
                {
                    entry.WriteData(memoryStream);
                }

                bytes = memoryStream.ToArray();
            }

            return bytes;
        }

        public void Deserialize(byte[] bytes)
        {
            if (bytes.Length < 8)
            {
                Logger.Log("Failed to deserialize copied map data: provided array is less than 8 bytes in size.");
                return;
            }

            Width = BitConverter.ToUInt16(bytes, 0);
            Height = BitConverter.ToUInt16(bytes, 2);
            int entryCount = BitConverter.ToInt32(bytes, 4);

            using (var memoryStream = new MemoryStream(bytes))
            {
                memoryStream.Position = 8;

                for (int i = 0; i < entryCount; i++)
                {
                    CopiedEntryType entryType = (CopiedEntryType)memoryStream.ReadByte();

                    CopiedMapEntry entry;

                    switch (entryType)
                    {
                        case CopiedEntryType.Terrain:
                            entry = new CopiedTerrainEntry();
                            break;
                        case CopiedEntryType.Overlay:
                            entry = new CopiedOverlayEntry();
                            break;
                        case CopiedEntryType.Smudge:
                            entry = new CopiedSmudgeEntry();
                            break;
                        case CopiedEntryType.TerrainObject:
                            entry = new CopiedTerrainObjectEntry();
                            break;
                        case CopiedEntryType.Vehicle:
                            entry = new CopiedVehicleEntry();
                            break;
                        case CopiedEntryType.Structure:
                            entry = new CopiedStructureEntry();
                            break;
                        case CopiedEntryType.Infantry:
                            entry = new CopiedInfantryEntry();
                            break;
                        default:
                        case CopiedEntryType.Invalid:
                            throw new CopiedMapDataSerializationException("Invalid map data entry type " + entryType);
                    }

                    entry.ReadData(memoryStream);
                    CopiedMapEntries.Add(entry);
                }
            }
        }
    }

    /// <summary>
    /// A mutation that allows pasting terrain on the map.
    /// </summary>
    public class PasteTerrainMutation : Mutation
    {
        public PasteTerrainMutation(IMutationTarget mutationTarget, CopiedMapData copiedMapData, Point2D origin, bool allowOverlap, int heightOffset) : base(mutationTarget)
        {
            this.copiedMapData = copiedMapData;
            this.origin = origin;
            this.allowOverlap = allowOverlap;
            this.heightOffset = heightOffset;
        }

        private struct PlacedInfantryInfo
        {
            public Point2D Coords;
            public SubCell SubCell;

            public PlacedInfantryInfo(Point2D coords, SubCell subCell)
            {
                Coords = coords;
                SubCell = subCell;
            }
        }

        private readonly CopiedMapData copiedMapData;
        private readonly Point2D origin;
        private readonly bool allowOverlap;
        private readonly int heightOffset;

        private OriginalCellTerrainData[] terrainUndoData;
        private OriginalOverlayInfo[] overlayUndoData;
        private OriginalSmudgeInfo[] smudgeUndoData;
        private Point2D[] terrainObjectCells;
        private Point2D[] vehicleCells;
        private Point2D[] structureCells;
        private PlacedInfantryInfo[] infantryLocations;

        private List<TechnoBase> placedObjects = new List<TechnoBase>();

        private void AddRefresh()
        {
            if (copiedMapData.CopiedMapEntries.Count > 10)
                MutationTarget.InvalidateMap();
            else
                MutationTarget.AddRefreshPoint(origin);
        }

        public override void Perform()
        {
            // *******
            // Terrain
            // *******

            var terrainUndoData = new List<OriginalCellTerrainData>();

            MapTile originCell = MutationTarget.Map.GetTile(origin);
            int originLevel = originCell?.Level ?? -1;

            foreach (var entry in copiedMapData.CopiedMapEntries.FindAll(e => e.EntryType == CopiedEntryType.Terrain))
            {
                var copiedTerrainData = entry as CopiedTerrainEntry;
                Point2D cellCoords = origin + copiedTerrainData.Offset;
                MapTile cell = MutationTarget.Map.GetTile(cellCoords);

                if (cell == null)
                    continue;

                terrainUndoData.Add(new OriginalCellTerrainData(cellCoords, cell.TileIndex, cell.SubTileIndex, cell.Level));

                cell.TileImage = null;
                cell.TileIndex = copiedTerrainData.TileIndex;
                cell.SubTileIndex = copiedTerrainData.SubTileIndex;
                cell.Level = (byte)Math.Max(0, Math.Min(Constants.MaxMapHeightLevel, originLevel + copiedTerrainData.HeightOffset + heightOffset));
            }

            this.terrainUndoData = terrainUndoData.ToArray();

            // *******
            // Overlay
            // *******

            var overlayUndoData = new List<OriginalOverlayInfo>();

            foreach (var entry in copiedMapData.CopiedMapEntries.FindAll(e => e.EntryType == CopiedEntryType.Overlay))
            {
                var copiedOverlayData = entry as CopiedOverlayEntry;
                Point2D cellCoords = origin + copiedOverlayData.Offset;
                MapTile cell = MutationTarget.Map.GetTile(cellCoords);

                if (cell == null)
                    continue;

                var overlayType = MutationTarget.Map.Rules.OverlayTypes.Find(ovt => ovt.ININame == copiedOverlayData.OverlayTypeName);
                if (overlayType == null)
                    continue;

                if (cell.Overlay == null)
                    overlayUndoData.Add(new OriginalOverlayInfo(-1, 0, cellCoords));
                else
                    overlayUndoData.Add(new OriginalOverlayInfo(cell.Overlay.OverlayType.Index, cell.Overlay.FrameIndex, cellCoords));

                cell.Overlay = new Overlay()
                {
                    FrameIndex = copiedOverlayData.FrameIndex,
                    OverlayType = overlayType,
                    Position = cellCoords
                };
            }

            this.overlayUndoData = overlayUndoData.ToArray();

            // *******
            // Smudges
            // *******

            var smudgeUndoData = new List<OriginalSmudgeInfo>();

            foreach (var entry in copiedMapData.CopiedMapEntries.FindAll(e => e.EntryType == CopiedEntryType.Smudge))
            {
                var copiedSmudgeEntry = entry as CopiedSmudgeEntry;
                Point2D cellCoords = origin + copiedSmudgeEntry.Offset;
                MapTile cell = MutationTarget.Map.GetTile(cellCoords);

                if (cell == null)
                    continue;

                var smudgeType = MutationTarget.Map.Rules.SmudgeTypes.Find(st => st.ININame == copiedSmudgeEntry.SmudgeTypeName);
                if (smudgeType == null)
                    continue;

                if (cell.Smudge == null)
                {
                    smudgeUndoData.Add(new OriginalSmudgeInfo(-1, cellCoords));
                    cell.Smudge = new Smudge() { Position = cellCoords, SmudgeType = smudgeType };
                }
                else
                {
                    smudgeUndoData.Add(new OriginalSmudgeInfo(cell.Smudge.SmudgeType.Index, cellCoords));
                    cell.Smudge.SmudgeType = smudgeType;
                }
            }

            this.smudgeUndoData = smudgeUndoData.ToArray();

            // ***************
            // Terrain Objects
            // ***************

            var terrainObjectCells = new List<Point2D>();

            foreach (var entry in copiedMapData.CopiedMapEntries.FindAll(e => e.EntryType == CopiedEntryType.TerrainObject))
            {
                var copiedTerrainObjectEntry = entry as CopiedTerrainObjectEntry;
                Point2D cellCoords = origin + copiedTerrainObjectEntry.Offset;
                MapTile cell = MutationTarget.Map.GetTile(cellCoords);

                if (cell == null)
                    continue;

                if (cell.TerrainObject != null)
                    continue;

                var terrainType = MutationTarget.Map.Rules.TerrainTypes.Find(tt => tt.ININame == copiedTerrainObjectEntry.ObjectTypeName);
                if (terrainType == null)
                    continue;

                terrainObjectCells.Add(cellCoords);
                MutationTarget.Map.AddTerrainObject(new TerrainObject(terrainType, cellCoords));
            }

            this.terrainObjectCells = terrainObjectCells.ToArray();

            // ********
            // Vehicles
            // ********

            var vehicleCells = new List<Point2D>();

            foreach (var entry in copiedMapData.CopiedMapEntries.FindAll(e => e.EntryType == CopiedEntryType.Vehicle))
            {
                var copiedVehicleEntry = entry as CopiedVehicleEntry;
                Point2D cellCoords = origin + copiedVehicleEntry.Offset;
                MapTile cell = MutationTarget.Map.GetTile(cellCoords);

                if (cell == null)
                    continue;

                if (cell.Vehicles.Count > 0 && !allowOverlap)
                {
                    bool foreignUnitExists = false;
                    foreach (var vehicle in cell.Vehicles)
                    {
                        if (!placedObjects.Contains(vehicle))
                        {
                            foreignUnitExists = true;
                            break;
                        }
                    }

                    if (foreignUnitExists)
                        continue;
                }

                var unitType = MutationTarget.Map.Rules.UnitTypes.Find(tt => tt.ININame == copiedVehicleEntry.ObjectTypeName);
                if (unitType == null)
                    continue;

                House owner = MutationTarget.Map.GetHouses().Find(h => h.ININame == copiedVehicleEntry.OwnerHouseName);
                if (owner == null)
                    continue;

                var unit = new Unit(unitType)
                {
                    Position = cellCoords,
                    Owner = owner,
                    HP = copiedVehicleEntry.HP,
                    Veterancy = copiedVehicleEntry.Veterancy,
                    Facing = copiedVehicleEntry.Facing,
                    Mission = copiedVehicleEntry.Mission
                };

                MutationTarget.Map.PlaceUnit(unit);
                placedObjects.Add(unit);

                vehicleCells.Add(cellCoords);
            }

            this.vehicleCells = vehicleCells.ToArray();

            // **********
            // Structures
            // **********

            var structureCells = new List<Point2D>();

            foreach (var entry in copiedMapData.CopiedMapEntries.FindAll(e => e.EntryType == CopiedEntryType.Structure))
            {
                var copiedStructureEntry = entry as CopiedStructureEntry;
                Point2D cellCoords = origin + copiedStructureEntry.Offset;
                MapTile cell = MutationTarget.Map.GetTile(cellCoords);

                if (cell == null)
                    continue;

                var buildingType = MutationTarget.Map.Rules.BuildingTypes.Find(tt => tt.ININame == copiedStructureEntry.ObjectTypeName);
                if (buildingType == null)
                    continue;

                if (cell.Structures.Count > 0 && !allowOverlap)
                {
                    bool isFoundationClear = true;
                    buildingType.ArtConfig.DoForFoundationCoordsOrOrigin(offset =>
                    {
                        Point2D foundationCellCoords = offset + cellCoords;
                        MapTile foundationCell = MutationTarget.Map.GetTile(foundationCellCoords);

                        foreach (var building in foundationCell.Structures)
                        {
                            if (!placedObjects.Contains(building))
                            {
                                isFoundationClear = false;
                                break;
                            }
                        }
                    });

                    if (!isFoundationClear)
                        continue;
                }
                
                House owner = MutationTarget.Map.GetHouses().Find(h => h.ININame == copiedStructureEntry.OwnerHouseName);
                if (owner == null)
                    continue;

                var building = new Structure(buildingType)
                {
                    Position = cellCoords, Owner = owner, HP = copiedStructureEntry.HP,
                    Facing = copiedStructureEntry.Facing
                };

                MutationTarget.Map.PlaceBuilding(building);
                placedObjects.Add(building);

                structureCells.Add(cellCoords);
            }

            this.structureCells = structureCells.ToArray();

            // ********
            // Infantry
            // ********

            var infantryLocations = new List<PlacedInfantryInfo>();

            foreach (var entry in copiedMapData.CopiedMapEntries.FindAll(e => e.EntryType == CopiedEntryType.Infantry))
            {
                var copiedInfantryEntry = entry as CopiedInfantryEntry;
                Point2D cellCoords = origin + copiedInfantryEntry.Offset;
                MapTile cell = MutationTarget.Map.GetTile(cellCoords);

                if (cell == null)
                    continue;

                if (cell.GetInfantryFromSubCellSpot(copiedInfantryEntry.SubCell) != null)
                    continue;

                var infantryType = MutationTarget.Map.Rules.InfantryTypes.Find(tt => tt.ININame == copiedInfantryEntry.ObjectTypeName);
                if (infantryType == null)
                    continue;

                House owner = MutationTarget.Map.GetHouses().Find(h => h.ININame == copiedInfantryEntry.OwnerHouseName);
                if (owner == null)
                    continue;

                MutationTarget.Map.PlaceInfantry(new Infantry(infantryType) 
                { 
                    Position = cellCoords, 
                    Owner = owner, 
                    HP = copiedInfantryEntry.HP, 
                    Veterancy = copiedInfantryEntry.Veterancy, 
                    Facing = copiedInfantryEntry.Facing, 
                    Mission = copiedInfantryEntry.Mission, 
                    SubCell = copiedInfantryEntry.SubCell 
                });

                infantryLocations.Add(new PlacedInfantryInfo(cellCoords, copiedInfantryEntry.SubCell));
            }

            this.infantryLocations = infantryLocations.ToArray();

            AddRefresh();
        }

        public override void Undo()
        {
            foreach (var originalTerrainData in terrainUndoData)
            {
                var cell = MutationTarget.Map.GetTile(originalTerrainData.CellCoords);
                cell.ChangeTileIndex(originalTerrainData.TileIndex, originalTerrainData.SubTileIndex);
                cell.Level = originalTerrainData.HeightLevel;
            }

            foreach (OriginalOverlayInfo info in overlayUndoData)
            {
                var cell = MutationTarget.Map.GetTile(info.CellCoords);
                if (info.OverlayTypeIndex < 0)
                {
                    cell.Overlay = null;
                    continue;
                }

                cell.Overlay = new Overlay()
                {
                    OverlayType = MutationTarget.Map.Rules.OverlayTypes[info.OverlayTypeIndex],
                    Position = info.CellCoords,
                    FrameIndex = info.FrameIndex
                };
            }

            foreach (OriginalSmudgeInfo info in smudgeUndoData)
            {
                var cell = MutationTarget.Map.GetTile(info.CellCoords);
                if (info.SmudgeTypeIndex < 0)
                {
                    cell.Smudge = null;
                    continue;
                }

                cell.Smudge.SmudgeType = Map.Rules.SmudgeTypes[info.SmudgeTypeIndex];
            }

            foreach (Point2D cellCoords in terrainObjectCells)
            {
                MutationTarget.Map.RemoveTerrainObject(cellCoords);
            }

            foreach (Point2D cellCoords in vehicleCells)
            {
                MutationTarget.Map.RemoveUnitsFrom(cellCoords);
            }

            foreach (Point2D cellCoords in structureCells)
            {
                MutationTarget.Map.RemoveBuildingsFrom(cellCoords);
            }

            foreach (var infantryInfo in infantryLocations)
            {
                MutationTarget.Map.RemoveInfantry(infantryInfo.Coords, infantryInfo.SubCell);
            }

            AddRefresh();
        }
    }
}
