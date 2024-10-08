using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models.Enums;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes.HeightMutations
{
    public abstract class LowerGroundMutationBase : AlterElevationMutationBase
    {
        public LowerGroundMutationBase(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize) : base(mutationTarget, originCell, brushSize)
        {
        }

        protected void LowerGround()
        {
            var targetCell = Map.GetTile(OriginCell);

            if (targetCell == null || targetCell.Level < 1 || !IsCellMorphable(targetCell))
                return;

            int targetCellHeight = targetCell.Level;

            // If the brush size is 1, only process it if the target cell is a ramp.
            // If it is not a ramp, then we'd need to lower the cell's height,
            // which would always result in it affecting more than 1 cell,
            // which wouldn't be logical with the brush size.
            if (BrushSize.Width == 1 || BrushSize.Height == 1)
            {
                if (!RampTileSet.ContainsTile(targetCell.TileIndex))
                    return;
            }

            int xSize = BrushSize.Width;
            int ySize = BrushSize.Height;

            int beginY = OriginCell.Y - (ySize - 1) / 2;
            int endY = OriginCell.Y + ySize / 2;
            int beginX = OriginCell.X - (xSize - 1) / 2;
            int endX = OriginCell.X + xSize / 2;

            // For all other brush sizes we can have a generic implementation
            for (int y = beginY; y <= endY; y++)
            {
                for (int x = beginX; x <= endX; x++)
                {
                    var cellCoords = new Point2D(x, y);
                    targetCell = Map.GetTile(cellCoords);
                    if (targetCell == null || targetCell.Level < 1)
                        continue;

                    // Only lower ground that was on the same level with our original target cell,
                    // otherwise things get illogical
                    if (targetCell.Level != targetCellHeight)
                        continue;

                    // Lower this cell and check surrounding cells whether they need ramps
                    AddCellToUndoData(cellCoords);
                    targetCell.Level--;
                    targetCell.ChangeTileIndex(0, 0);
                    foreach (Point2D surroundingCellOffset in SurroundingTiles)
                    {
                        RegisterCell(cellCoords + surroundingCellOffset);
                    }

                    MarkCellAsProcessed(cellCoords);
                }
            }

            Process();

            if (MutationTarget.AutoLATEnabled)
                ApplyAutoLAT();
        }

        private void ApplyAutoLAT()
        {
            int minX = totalProcessedCells.Aggregate(int.MaxValue, (min, point) => point.X < min ? point.X : min) - 1;
            int minY = totalProcessedCells.Aggregate(int.MaxValue, (min, point) => point.Y < min ? point.Y : min) - 1;
            int maxX = totalProcessedCells.Aggregate(int.MinValue, (max, point) => point.X > max ? point.X : max) + 1;
            int maxY = totalProcessedCells.Aggregate(int.MinValue, (max, point) => point.Y > max ? point.Y : max) + 1;

            ApplyGenericAutoLAT(minX, minY, maxX, maxY);
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
                        cell.Level = (byte)(cell.Level + applyingTransition.HeightChange);

                        // If we fixed this flaw from one cell, also check the surrounding ones
                        foreach (var surroundingCell in SurroundingTiles)
                        {
                            if (!newCells.Contains(cc + surroundingCell))
                                newCells.Add(cc + surroundingCell);
                        }
                    }
                });

                var cellsForNextRound = newCells.Where(c => !cellsToCheck.Contains(c));
                cellsToCheck.Clear();
                cellsToCheck.AddRange(cellsForNextRound);
                totalProcessedCells.AddRange(cellsForNextRound.Where(c => !totalProcessedCells.Contains(c)));
                newCells.Clear();
            }
        }

        protected override void ApplyRamps()
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

            foreach (var cellCoords in cellsToProcess)
            {
                var cell = Map.GetTile(cellCoords);
                if (cell == null)
                    continue;

                if (!IsCellMorphable(cell))
                    continue;

                var cellTmpImage = Map.TheaterInstance.GetTile(cell.TileIndex).GetSubTile(cell.SubTileIndex).TmpImage;
                LandType landType = (LandType)cellTmpImage.TerrainType;
                if (landType == LandType.Rock || landType == LandType.Water)
                    continue;

                foreach (var transitionRampInfo in GetTransitionRampInfos())
                {
                    if (transitionRampInfo.Matches(Map, cellCoords, cell.Level))
                    {
                        AddCellToUndoData(cell.CoordsToPoint());

                        if (transitionRampInfo.RampType == RampType.None)
                        {
                            if (cellTmpImage.RampType != RampType.None)
                                cell.ChangeTileIndex(0, 0);
                        }
                        else
                        {
                            cell.ChangeTileIndex(RampTileSet.StartTileIndex + ((int)transitionRampInfo.RampType - 1), 0);
                        }

                        if (transitionRampInfo.HeightChange != 0)
                        {
                            cell.Level = (byte)(cell.Level + transitionRampInfo.HeightChange);
                        }

                        break;
                    }
                }
            }
        }
    }
}
