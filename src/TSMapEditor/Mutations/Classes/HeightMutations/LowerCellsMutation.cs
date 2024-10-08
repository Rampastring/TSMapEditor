using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes.HeightMutations
{
    /// <summary>
    /// A mutation for lowering cell height levels.
    /// </summary>
    public class LowerCellsMutation : Mutation
    {
        public LowerCellsMutation(IMutationTarget mutationTarget, Point2D targetCellCoords, BrushSize brushSize, bool applyOnArea) : base(mutationTarget)
        {
            this.targetCellCoords = targetCellCoords;
            this.brushSize = brushSize;
            this.applyOnArea = applyOnArea;
        }

        private Point2D targetCellCoords;
        private BrushSize brushSize;
        private bool applyOnArea;

        private List<Point2D> affectedCells = new List<Point2D>();

        public override void Perform()
        {
            if (!applyOnArea)
            {
                brushSize.DoForBrushSize(offset =>
                {
                    Point2D cellCoords = targetCellCoords + offset;

                    var cell = MutationTarget.Map.GetTile(cellCoords);
                    LowerCellLevel(cell);
                });
            }
            else
            {
                var targetTile = MutationTarget.Map.GetTile(targetCellCoords);
                var tilesToProcess = Helpers.GetFillAreaTiles(targetTile, MutationTarget.Map, MutationTarget.TheaterGraphics);

                // Process tiles
                foreach (Point2D cellCoords in tilesToProcess)
                {
                    var cell = MutationTarget.Map.GetTile(cellCoords);
                    LowerCellLevel(cell);
                }
            }

            MutationTarget.AddRefreshPoint(targetCellCoords);
        }

        private void LowerCellLevel(MapTile cell)
        {
            if (cell == null)
                return;

            if (cell.Level > 0)
            {
                cell.Level--;
                affectedCells.Add(cell.CoordsToPoint());
                cell.RefreshLighting(Map.Lighting, MutationTarget.LightingPreviewState);
            }
        }

        public override void Undo()
        {
            foreach (Point2D cellCoords in affectedCells)
            {
                var cell = MutationTarget.Map.GetTile(cellCoords);
                if (cell == null)
                    return;

                if (cell.Level < Constants.MaxMapHeight)
                {
                    cell.Level++;
                    cell.RefreshLighting(Map.Lighting, MutationTarget.LightingPreviewState);
                }
            }

            MutationTarget.AddRefreshPoint(targetCellCoords);
        }
    }
}
