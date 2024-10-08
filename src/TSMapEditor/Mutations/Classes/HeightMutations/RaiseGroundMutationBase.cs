using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models.Enums;
using TSMapEditor.UI;
using HCT = TSMapEditor.Mutations.Classes.HeightMutations.HeightComparisonType;

namespace TSMapEditor.Mutations.Classes.HeightMutations
{
    public abstract class RaiseGroundMutationBase : AlterElevationMutationBase
    {
        protected RaiseGroundMutationBase(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize) : base(mutationTarget, originCell, brushSize)
        {
        }

        private TransitionRampInfo flatGroundCheck = new TransitionRampInfo(RampType.None, new() { HCT.Equal, HCT.Equal, HCT.Equal, HCT.Equal, HCT.Equal, HCT.Equal, HCT.Equal, HCT.Equal });


        /// <summary>
        /// Entry point for raising ground.
        /// </summary>
        protected void RaiseGround()
        {
            var targetCell = Map.GetTile(OriginCell);

            if (targetCell == null || targetCell.Level >= Constants.MaxMapHeightLevel || !IsCellMorphable(targetCell))
                return;

            int targetCellHeight = targetCell.Level;

            int xSize = BrushSize.Width - 2;
            int ySize = BrushSize.Height - 2;

            // Special case for 2x2 brush.
            // Check if we can create a 2x2 "hill". If yes, then do so.
            // Otherwise, process it as 1x1.
            if (BrushSize.Width == 2 && BrushSize.Height == 2)
            {
                bool canCreateSmallHill = true;
                int height = targetCell.Level;
                BrushSize.DoForBrushSize(offset =>
                {
                    if (!canCreateSmallHill)
                        return;

                    var otherCellCoords = OriginCell + offset;
                    var otherCell = Map.GetTile(otherCellCoords);
                    if (otherCell == null)
                    {
                        canCreateSmallHill = false;
                        return;
                    }

                    var subTile = Map.TheaterInstance.GetTile(otherCell.TileIndex).GetSubTile(otherCell.SubTileIndex);
                    if (!IsCellMorphable(otherCell) || otherCell.Level != height || subTile.TmpImage.RampType != RampType.None)
                        canCreateSmallHill = false;
                });

                if (canCreateSmallHill)
                {
                    CreateSmallHill(OriginCell);
                    return;
                }
            }

            // If the brush size is 1, only process it if the target cell is a ramp.
            // If it is not a ramp, then we'd need to raise the cell's height,
            // which would always result in it affecting more than 1 cell,
            // which wouldn't be logical with the brush size.
            if (BrushSize.Width == 1 || BrushSize.Height == 1)
            {
                if (!RampTileSet.ContainsTile(targetCell.TileIndex))
                    return;
            }

            int beginY = OriginCell.Y - ySize / 2;
            int endY = OriginCell.Y + ySize / 2;
            int beginX = OriginCell.X - xSize / 2;
            int endX = OriginCell.X + xSize / 2;

            // For all other brush sizes we can have a generic implementation
            for (int y = beginY; y <= endY; y++)
            {
                for (int x = beginX; x <= endX; x++)
                {
                    var cellCoords = new Point2D(x, y);
                    targetCell = Map.GetTile(cellCoords);
                    if (targetCell == null || targetCell.Level >= Constants.MaxMapHeightLevel)
                        continue;

                    // Only raise ground that was on the same level with our original target cell,
                    // otherwise things get illogical
                    if (targetCell.Level != targetCellHeight)
                        continue;

                    // Raise this cell and check surrounding cells whether they need ramps
                    AddCellToUndoData(cellCoords);
                    targetCell.Level++;
                    targetCell.ChangeTileIndex(0, 0);
                    foreach (Point2D surroundingCellOffset in SurroundingTiles)
                    {
                        RegisterCell(cellCoords + surroundingCellOffset);
                    }

                    MarkCellAsProcessed(cellCoords);
                }
            }

            Process();
        }

        private void CreateSmallHill(Point2D cellCoords)
        {
            AddCellToUndoData(cellCoords);
            AddCellToUndoData(cellCoords + new Point2D(1, 0));
            AddCellToUndoData(cellCoords + new Point2D(0, 1));
            AddCellToUndoData(cellCoords + new Point2D(1, 1));

            Map.GetTile(cellCoords).ChangeTileIndex(RampTileSet.StartTileIndex + ((int)RampType.CornerNW - 1), 0);
            Map.GetTile(cellCoords + new Point2D(1, 0)).ChangeTileIndex(RampTileSet.StartTileIndex + ((int)RampType.CornerNE - 1), 0);
            Map.GetTile(cellCoords + new Point2D(0, 1)).ChangeTileIndex(RampTileSet.StartTileIndex + ((int)RampType.CornerSW - 1), 0);
            Map.GetTile(cellCoords + new Point2D(1, 1)).ChangeTileIndex(RampTileSet.StartTileIndex + ((int)RampType.CornerSE - 1), 0);

            MutationTarget.InvalidateMap();
        }

        /// <summary>
        /// Applies miscellaneous fixes to height data of processed cells.
        /// </summary>
        protected override void CellHeightFixes()
        {
            // We process one set of cells at a time, starting from the cells
            // processed so far.
            // During the processing, we might get new cells to process.
            // We repeat the process until no new cells to process have been added to the list.
            List<Point2D> cellsToCheck = new(totalProcessedCells);
            List<Point2D> newCells = new();

            while (cellsToCheck.Count > 0)
            {
                // If a cell has higher cells on both of its sides in a straight diagonal line,
                // we need to normalize its height to be on the same level with the diagonal sides,
                // because no "one cell" depression exists
                cellsToCheck.ForEach(cc => CellHeightFix_CheckStraightDiagonalLine(cc, newCells, true));
                cellsToCheck.ForEach(cc => CellHeightFix_CheckStraightDiagonalLine(cc, newCells, false));

                // If a cell has a two-levels-higher cell next to it on a horizontal or vertical line,
                // and a one-level-higher cell next to it on the opposite side,
                // we need to increase its level by 1
                totalProcessedCells.ForEach(cc => CellHeightFix_DifferentHeightDiffsOnStraightLine(cc, new Point2D(1, -1), new Point2D(-1, 1)));
                totalProcessedCells.ForEach(cc => CellHeightFix_DifferentHeightDiffsOnStraightLine(cc, new Point2D(-1, -1), new Point2D(1, 1)));

                // Special height fixes
                totalProcessedCells.ForEach(cc =>
                {
                    var cell = Map.GetTile(cc);
                    if (cell == null)
                        return;

                    if (!IsCellMorphable(cell))
                        return;

                    var applyingTransition = Array.Find(GetHeightFixers(), tri => tri.Matches(Map, cc, cell.Level));
                    if (applyingTransition != null)
                    {
                        AddCellToUndoData(cell.CoordsToPoint());
                        cell.ChangeTileIndex(0, 0);
                        cell.Level += (byte)applyingTransition.HeightChange;

                        // If we fixed this flaw from one cell, also check the surrounding ones
                        foreach (var surroundingCell in SurroundingTiles)
                        {
                            if (!newCells.Contains(cc + surroundingCell))
                                newCells.Add(cc + surroundingCell);
                        }
                    }
                });

                var cellsForNextRound = newCells; //.Where(c => !cellsToCheck.Contains(c));
                cellsToCheck.Clear();
                cellsToCheck.AddRange(cellsForNextRound);
                totalProcessedCells.AddRange(cellsForNextRound.Where(c => !totalProcessedCells.Contains(c)));
                newCells.Clear();
            }
        }

        private void CellHeightFix_CheckStraightDiagonalLine(Point2D cellCoords, List<Point2D> newCells, bool isXAxis)
        {
            var cell = Map.GetTile(cellCoords);
            if (cell == null)
                return;

            if (!IsCellMorphable(cell))
                return;

            Point2D otherCellCoords = cellCoords + new Point2D(isXAxis ? 1 : 0, isXAxis ? 0 : 1);
            var otherCell = Map.GetTile(otherCellCoords);
            if (otherCell == null || otherCell.Level <= cell.Level)
                return;

            int otherLevel = otherCell.Level;

            otherCellCoords = cellCoords + new Point2D(isXAxis ? -1 : 0, isXAxis ? 0 : -1);
            otherCell = Map.GetTile(otherCellCoords);
            if (otherCell == null)
                return;

            if (otherCell.Level == otherLevel)
            {
                AddCellToUndoData(cell.CoordsToPoint());
                cell.ChangeTileIndex(0, 0);
                cell.Level = otherCell.Level;

                // If we fixed this flaw from one cell, also check the surrounding ones
                foreach (var surroundingCell in SurroundingTiles)
                {
                    if (!newCells.Contains(cellCoords + surroundingCell))
                        newCells.Add(cellCoords + surroundingCell);
                }
            }
        }

        private void CellHeightFix_DifferentHeightDiffsOnStraightLine(Point2D cellCoords, Point2D offset1, Point2D offset2)
        {
            var cell = Map.GetTile(cellCoords);
            if (cell == null)
                return;

            if (!IsCellMorphable(cell))
                return;

            int totalLevelDifference = 0;

            Point2D otherCellCoords = cellCoords + offset1;
            var otherCell = Map.GetTile(otherCellCoords);
            if (otherCell != null)
                totalLevelDifference += otherCell.Level - cell.Level;

            otherCellCoords = cellCoords + offset2;
            otherCell = Map.GetTile(otherCellCoords);
            if (otherCell != null)
                totalLevelDifference += otherCell.Level - cell.Level;

            if (totalLevelDifference >= 3)
            {
                AddCellToUndoData(cell.CoordsToPoint());
                cell.ChangeTileIndex(0, 0);
                cell.Level++;
            }
        }

        protected override void ApplyRamps()
        {
            foreach (var cellCoords in totalProcessedCells)
            {
                var cell = Map.GetTile(cellCoords);
                if (cell == null)
                    continue;

                if (!IsCellMorphable(cell))
                    continue;

                LandType landType = (LandType)Map.TheaterInstance.GetTile(cell.TileIndex).GetSubTile(cell.SubTileIndex).TmpImage.TerrainType;
                if (landType == LandType.Rock || landType == LandType.Water)
                    continue;

                foreach (var transitionRampInfo in GetTransitionRampInfos())
                {
                    if (transitionRampInfo.Matches(Map, cellCoords, cell.Level))
                    {
                        AddCellToUndoData(cell.CoordsToPoint());

                        if (transitionRampInfo.RampType == RampType.None)
                        {
                            cell.ChangeTileIndex(0, 0);
                        }
                        else
                        {
                            cell.ChangeTileIndex(RampTileSet.StartTileIndex + ((int)transitionRampInfo.RampType - 1), 0);
                        }

                        if (transitionRampInfo.HeightChange != 0)
                        {
                            cell.Level += (byte)transitionRampInfo.HeightChange;
                        }

                        break;
                    }
                }
            }

            CheckForRampsOnFlatGround();
        }

        /// <summary>
        /// Checks for leftover ramps on ground surrouding the altered area.
        /// </summary>
        private void CheckForRampsOnFlatGround()
        {
            var cellsToProcess = new List<Point2D>(totalProcessedCells);

            // Go through all processed cells and add their neighbours to be processed too
            totalProcessedCells.ForEach(cc =>
            {
                for (int i = 0; i < (int)Direction.Count; i++)
                {
                    var otherCellCoords = cc + Helpers.VisualDirectionToPoint((Direction)i);

                    if (!cellsToProcess.Contains(otherCellCoords))
                        cellsToProcess.Add(otherCellCoords);
                }
            });

            for (int i = totalProcessedCells.Count; i < cellsToProcess.Count; i++)
            {
                var cellCoords = cellsToProcess[i];

                var cell = Map.GetTile(cellCoords);
                if (cell == null)
                    continue;

                if (!IsCellMorphable(cell))
                    continue;

                var subTile = Map.TheaterInstance.GetTile(cell.TileIndex).GetSubTile(cell.SubTileIndex);
                LandType landType = (LandType)subTile.TmpImage.TerrainType;
                if (landType == LandType.Rock || landType == LandType.Water)
                    continue;

                if (flatGroundCheck.Matches(Map, cellCoords, cell.Level) && subTile.TmpImage.RampType != RampType.None)
                {
                    cell.ChangeTileIndex(0, 0);

                    // Add surroundings of the cell to check as well
                    for (int dir = 0; dir < (int)Direction.Count; dir++)
                    {
                        var otherCellCoords = cellCoords + Helpers.VisualDirectionToPoint((Direction)dir);

                        if (!cellsToProcess.Contains(otherCellCoords))
                            cellsToProcess.Add(otherCellCoords);
                    }
                }
            }
        }
    }
}
