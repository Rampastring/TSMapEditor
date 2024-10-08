using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that fills an area with terrain.
    /// </summary>
    public partial class FillTerrainAreaMutation : Mutation
    {
        public FillTerrainAreaMutation(IMutationTarget mutationTarget, MapTile target, TileImage tile) : base(mutationTarget)
        {
            if (tile.Width > 1 || tile.Height > 1)
                throw new InvalidOperationException("Only 1x1 tiles can be used to fill areas.");

            targetTile = target;
            this.tile = tile;
        }

        private MapTile targetTile;
        private TileImage tile;
        private OriginalCellTerrainData[] undoData;

        public override void Perform()
        {
            var originalData = new List<OriginalCellTerrainData>();
            var tilesToProcess = Helpers.GetFillAreaTiles(targetTile, MutationTarget.Map, MutationTarget.TheaterGraphics);

            // Process tiles
            foreach (Point2D cellCoords in tilesToProcess)
            {
                var cell = MutationTarget.Map.GetTile(cellCoords);
                originalData.Add(new OriginalCellTerrainData(cellCoords, cell.TileIndex, cell.SubTileIndex, cell.Level));

                cell.ChangeTileIndex(tile.TileID, 0);
            }

            undoData = originalData.ToArray();
            MutationTarget.InvalidateMap();
        }

        public override void Undo()
        {
            foreach (var originalTerrainData in undoData)
            {
                var cell = MutationTarget.Map.GetTile(originalTerrainData.CellCoords);
                cell.ChangeTileIndex(originalTerrainData.TileIndex, originalTerrainData.SubTileIndex);
            }

            MutationTarget.InvalidateMap();
        }
    }
}
