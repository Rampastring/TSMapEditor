using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that places a terrain tile on the map.
    /// </summary>
    public class PlaceTerrainTileMutation : Mutation
    {
        public PlaceTerrainTileMutation(IMutationTarget mutationTarget, Point2D targetCellCoords, TileImage tile, int heightOffset) : base(mutationTarget)
        {
            this.targetCellCoords = targetCellCoords;
            this.tile = tile;
            this.heightOffset = heightOffset;
            this.brushSize = mutationTarget.BrushSize;
        }

        private readonly Point2D targetCellCoords;
        private readonly TileImage tile;
        private readonly int heightOffset;
        private readonly BrushSize brushSize;

        private List<OriginalTerrainData> undoData;

        private static readonly Point2D[] surroundingTiles = new Point2D[] { new Point2D(-1, 0), new Point2D(1, 0), new Point2D(0, -1), new Point2D(0, 1) };

        private void AddUndoDataForTile(Point2D brushOffset)
        {
            for (int i = 0; i < tile.TMPImages.Length; i++)
            {
                MGTMPImage image = tile.TMPImages[i];

                if (image.TmpImage == null)
                    continue;

                int cx = targetCellCoords.X + (brushOffset.X * tile.Width) + i % tile.Width;
                int cy = targetCellCoords.Y + (brushOffset.Y * tile.Height) + i / tile.Width;

                var mapTile = MutationTarget.Map.GetTile(cx, cy);
                if (mapTile != null && (!MutationTarget.OnlyPaintOnClearGround || mapTile.IsClearGround()) &&
                    !undoData.Exists(otd => otd.CellCoords.X == cx && otd.CellCoords.Y == cy))
                {
                    undoData.Add(new OriginalTerrainData(mapTile.TileIndex, mapTile.SubTileIndex, mapTile.Level, mapTile.CoordsToPoint()));
                }
            }
        }

        public override void Perform()
        {
            undoData = new List<OriginalTerrainData>(tile.TMPImages.Length * brushSize.Width * brushSize.Height);

            int totalWidth = tile.Width * brushSize.Width;
            int totalHeight = tile.Height * brushSize.Height;

            // Get un-do data
            DoForArea(AddUndoDataForTile, MutationTarget.AutoLATEnabled);

            MapTile originCell = MutationTarget.Map.GetTile(targetCellCoords);
            int originLevel = -1;

            // First, look up the lowest point within the tile area for origin level
            // Only use a 1x1 brush size for this (meaning no brush at all)
            // so users can use larger brush sizes to "paint height"
            for (int i = 0; i < tile.TMPImages.Length; i++)
            {
                MGTMPImage image = tile.TMPImages[i];

                if (image.TmpImage == null)
                    continue;

                int cx = targetCellCoords.X + i % tile.Width;
                int cy = targetCellCoords.Y + i / tile.Width;

                var mapTile = MutationTarget.Map.GetTile(cx, cy);

                if (mapTile != null)
                {
                    var existingTile = Map.TheaterInstance.GetTile(mapTile.TileIndex).GetSubTile(mapTile.SubTileIndex);

                    int cellLevel = mapTile.Level;

                    // Allow replacing back cliffs
                    if (existingTile.TmpImage.Height == image.TmpImage.Height)
                        cellLevel -= existingTile.TmpImage.Height;

                    if (originLevel < 0 || cellLevel < originLevel)
                        originLevel = cellLevel;
                }
            }

            originLevel += heightOffset;
            if (originLevel < 0)
                originLevel = 0;

            // Place the terrain
            brushSize.DoForBrushSize(offset =>
            {
                for (int i = 0; i < tile.TMPImages.Length; i++)
                {
                    MGTMPImage image = tile.TMPImages[i];

                    if (image.TmpImage == null)
                        continue;

                    int cx = targetCellCoords.X + (offset.X * tile.Width) + i % tile.Width;
                    int cy = targetCellCoords.Y + (offset.Y * tile.Height) + i / tile.Width;

                    var mapTile = MutationTarget.Map.GetTile(cx, cy);
                    if (mapTile != null && (!MutationTarget.OnlyPaintOnClearGround || mapTile.IsClearGround()))
                    {
                        mapTile.ChangeTileIndex(tile.TileID, (byte)i);
                        mapTile.Level = (byte)Math.Min(originLevel + image.TmpImage.Height, Constants.MaxMapHeightLevel);
                        mapTile.RefreshLighting(Map.Lighting, MutationTarget.LightingPreviewState);
                    }
                }
            });

            // Apply autoLAT if necessary
            if (MutationTarget.AutoLATEnabled)
            {
                ApplyAutoLATForTilePlacement(tile, brushSize, targetCellCoords);
            }

            MutationTarget.AddRefreshPoint(targetCellCoords, Math.Max(tile.Width, tile.Height) * Math.Max(brushSize.Width, brushSize.Height));
        }

        private void DoForArea(Action<Point2D> action, bool doForSurroundings)
        {
            int totalWidth = tile.Width * brushSize.Width;
            int totalHeight = tile.Height * brushSize.Height;

            int initX = doForSurroundings ? -1 : 0;
            int initY = doForSurroundings ? -1 : 0;

            if (doForSurroundings)
            {
                totalWidth++;
                totalHeight++;
            }

            for (int y = initY; y <= totalHeight; y++)
            {
                for (int x = initX; x <= totalWidth; x++)
                {
                    action(new Point2D(x, y));
                }
            }
        }

        public override void Undo()
        {
            for (int i = 0; i < undoData.Count; i++)
            {
                OriginalTerrainData originalTerrainData = undoData[i];

                var mapCell = MutationTarget.Map.GetTile(originalTerrainData.CellCoords);
                if (mapCell != null)
                {
                    mapCell.ChangeTileIndex(originalTerrainData.TileIndex, originalTerrainData.SubTileIndex);
                    mapCell.Level = originalTerrainData.Level;
                    mapCell.RefreshLighting(Map.Lighting, MutationTarget.LightingPreviewState);
                }
            }

            MutationTarget.AddRefreshPoint(targetCellCoords);
        }
    }
}
