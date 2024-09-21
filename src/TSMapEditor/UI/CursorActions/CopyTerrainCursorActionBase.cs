using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Rendering;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allows copying terrain tiles.
    /// </summary>
    public abstract class CopyTerrainCursorActionBase : CursorAction
    {
        public CopyTerrainCursorActionBase(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public CopiedEntryType EntryTypes { get; set; }

        protected void CopyFromCells(List<Point2D> cellsToCopy)
        {
            var copiedMapData = new CopiedMapData();

            byte lowestHeight = byte.MaxValue;

            int startX = int.MaxValue;
            int startY = int.MaxValue;
            int endX = int.MinValue;
            int endY = int.MinValue;

            // Figure out start cell coords so we can calculate offsets.
            // Also figure out end cell coords for rectangular preview.
            cellsToCopy.ForEach(cellCoords =>
            {
                if (Map.IsCoordWithinMap(cellCoords))
                {
                    if (cellCoords.X < startX)
                        startX = cellCoords.X;

                    if (cellCoords.Y < startY)
                        startY = cellCoords.Y;

                    if (cellCoords.X > endX)
                        endX = cellCoords.X;

                    if (cellCoords.Y > endY)
                        endY = cellCoords.Y;
                }
            });

            copiedMapData.Width = (ushort)(endX - startX);
            copiedMapData.Height = (ushort)(endY - startY);

            // To handle height, we first look up the lowest height level of the copied
            // area. Any cell higher than that gets assigned an offset for its height.
            if ((EntryTypes & CopiedEntryType.Terrain) == CopiedEntryType.Terrain)
            {
                cellsToCopy.ForEach(cellCoords =>
                {
                    var cell = Map.GetTile(cellCoords);
                    if (cell != null && cell.Level < lowestHeight)
                        lowestHeight = cell.Level;
                });
            }

            foreach (Point2D cellCoords in cellsToCopy)
            {
                MapTile cell = Map.GetTile(cellCoords);
                if (cell == null)
                    continue;

                var offset = new Point2D(cellCoords.X - startX, cellCoords.Y - startY);

                if ((EntryTypes & CopiedEntryType.Terrain) == CopiedEntryType.Terrain)
                {
                    copiedMapData.CopiedMapEntries.Add(new CopiedTerrainEntry(offset, cell.TileIndex, cell.SubTileIndex, (byte)(cell.Level - lowestHeight)));
                }

                if ((EntryTypes & CopiedEntryType.Overlay) == CopiedEntryType.Overlay)
                {
                    if (cell.Overlay != null)
                        copiedMapData.CopiedMapEntries.Add(new CopiedOverlayEntry(offset, cell.Overlay.OverlayType.ININame, cell.Overlay.FrameIndex));
                }

                if ((EntryTypes & CopiedEntryType.Smudge) == CopiedEntryType.Smudge)
                {
                    if (cell.Smudge != null)
                        copiedMapData.CopiedMapEntries.Add(new CopiedSmudgeEntry(offset, cell.Smudge.SmudgeType.ININame));
                }

                if ((EntryTypes & CopiedEntryType.TerrainObject) == CopiedEntryType.TerrainObject)
                {
                    if (cell.TerrainObject != null)
                        copiedMapData.CopiedMapEntries.Add(new CopiedTerrainObjectEntry(offset, cell.TerrainObject.TerrainType.ININame));
                }

                if ((EntryTypes & CopiedEntryType.Vehicle) == CopiedEntryType.Vehicle)
                {
                    cell.DoForAllVehicles(unit => copiedMapData.CopiedMapEntries.Add(new CopiedVehicleEntry(offset, unit.ObjectType.ININame, unit.Owner.ININame, unit.HP, unit.Veterancy, unit.Facing, unit.Mission)));
                }

                if ((EntryTypes & CopiedEntryType.Structure) == CopiedEntryType.Structure)
                {
                    cell.DoForAllBuildings(structure =>
                    {
                        if (structure.Position == cellCoords)
                            copiedMapData.CopiedMapEntries.Add(new CopiedStructureEntry(offset, structure.ObjectType.ININame, structure.Owner.ININame, structure.HP, 0, structure.Facing, string.Empty));
                    });
                }

                if ((EntryTypes & CopiedEntryType.Infantry) == CopiedEntryType.Infantry)
                {
                    cell.DoForAllInfantry(inf => copiedMapData.CopiedMapEntries.Add(new CopiedInfantryEntry(offset, inf.ObjectType.ININame, inf.Owner.ININame, inf.HP, inf.Veterancy, inf.Facing, inf.Mission, inf.SubCell)));
                }
            }

            System.Windows.Forms.Clipboard.SetData(Constants.ClipboardMapDataFormatValue, copiedMapData.Serialize());
        }
    }
}
