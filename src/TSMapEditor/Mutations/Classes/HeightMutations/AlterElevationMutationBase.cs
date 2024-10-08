using System;
using System.Collections.Generic;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes.HeightMutations
{
    public abstract class AlterElevationMutationBase : Mutation
    {
        protected AlterElevationMutationBase(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize) : base(mutationTarget)
        {
            OriginCell = originCell;
            BrushSize = brushSize ?? throw new ArgumentNullException(nameof(brushSize));
            RampTileSet = Map.TheaterInstance.Theater.RampTileSet;
        }

        protected readonly Point2D OriginCell;
        protected readonly BrushSize BrushSize;
        protected readonly TileSet RampTileSet;

        protected List<Point2D> cellsToProcess = new List<Point2D>();
        protected List<Point2D> processedCellsThisIteration = new List<Point2D>();
        protected List<Point2D> totalProcessedCells = new List<Point2D>();
        protected List<AlterGroundElevationUndoData> undoData = new List<AlterGroundElevationUndoData>();

        protected static readonly Point2D[] SurroundingTiles = new Point2D[] { new Point2D(-1, 0), new Point2D(1, 0), new Point2D(0, -1), new Point2D(0, 1),
                                                                             new Point2D(-1, -1), new Point2D(-1, 1), new Point2D(1, -1), new Point2D(1, 1) };

        protected bool IsCellMorphable(MapTile cell)
        {
            return Map.TheaterInstance.Theater.TileSets[Map.TheaterInstance.GetTileSetId(cell.TileIndex)].Morphable;
        }

        /// <summary>
        /// Adds a cell's data to the undo data structure.
        /// Does nothing if the cell has already been added to the undo data structure.
        /// </summary>
        protected void AddCellToUndoData(Point2D cellCoords)
        {
            var cell = Map.GetTile(cellCoords);
            if (cell == null)
                return;

            if (undoData.Exists(u => u.CellCoords == cellCoords))
                return;

            undoData.Add(new AlterGroundElevationUndoData(cellCoords, cell.TileIndex, cell.SubTileIndex, cell.Level));
        }

        protected void RegisterCell(Point2D cellCoords)
        {
            if (processedCellsThisIteration.Contains(cellCoords) || cellsToProcess.Contains(cellCoords) || totalProcessedCells.Contains(cellCoords))
                return;

            cellsToProcess.Add(cellCoords);
        }

        protected void MarkCellAsProcessed(Point2D cellCoords)
        {
            processedCellsThisIteration.Add(cellCoords);
            cellsToProcess.Remove(cellCoords);

            if (!totalProcessedCells.Contains(cellCoords))
                totalProcessedCells.Add(cellCoords);
        }

        /// <summary>
        /// Main function for ground height level alteration.
        /// Prior to this, a derived class has already raised or lowered
        /// some target cells. Now we need to figure out what kinds of changes
        /// to the map are necessary for these altered height levels to look smooth.
        /// 
        /// Algorithm goes as follows:
        /// 
        /// 1) Check surrounding cells for height differences of 2 levels, if found then
        ///    raise the relevant cells to lower the difference to only 1 level.
        ///    Repeat this process recursively until there are no cells to change.
        /// 
        /// 2) Apply some miscellaneous cell height fixes, the game does not have ramps for
        ///    every potential case.
        /// 
        /// 3) Check the affected cells and their neighbours for necessary ramp changes.
        /// </summary>
        protected void Process()
        {
            ProcessCells();

            CellHeightFixes();

            ApplyRamps();

            RefreshLighting();

            MutationTarget.InvalidateMap();
        }

        protected void ProcessCells()
        {
            while (cellsToProcess.Count > 0)
            {
                var cellsCopy = new List<Point2D>(cellsToProcess);
                cellsToProcess.Clear();
                processedCellsThisIteration.Clear();

                foreach (var cell in cellsCopy)
                    CheckCell(cell);
            }
        }

        protected abstract void CheckCell(Point2D cellCoords);

        protected abstract TransitionRampInfo[] GetTransitionRampInfos();

        protected abstract TransitionRampInfo[] GetHeightFixers();

        protected abstract void CellHeightFixes();

        protected abstract void ApplyRamps();

        private void RefreshLighting()
        {
            for (int i = 0; i < totalProcessedCells.Count; i++)
            {
                var cellCoords = totalProcessedCells[i];
                var cell = Map.GetTile(cellCoords);
                cell?.RefreshLighting(Map.Lighting, MutationTarget.LightingPreviewState);
            }
        }

        public override void Undo()
        {
            foreach (var entry in undoData)
            {
                var cell = Map.GetTile(entry.CellCoords);
                cell.ChangeTileIndex(entry.TileIndex, (byte)entry.SubTileIndex);
                cell.Level = (byte)entry.HeightLevel;
                cell.RefreshLighting(Map.Lighting, MutationTarget.LightingPreviewState);
            }

            MutationTarget.InvalidateMap();
        }
    }
}
