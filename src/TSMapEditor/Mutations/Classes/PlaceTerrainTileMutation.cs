using System;
using System.Collections.Generic;
using TSMapEditor.CCEngine;
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
                    }
                }
            });

            // Apply autoLAT if necessary
            if (MutationTarget.AutoLATEnabled)
            {
                ApplyAutoLAT();
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

        private void ApplyAutoLAT()
        {
            // Get potential base tilesets of the placed LAT (if we're placing LAT)
            // This allows placing certain LATs on top of other LATs (example: snowy dirt on snow, when snow is also placed on grass)
            TileSet baseTileSet = null;
            TileSet altBaseTileSet = null;
            var tileAutoLatGrounds = MutationTarget.Map.TheaterInstance.Theater.LATGrounds.FindAll(
                g => g.GroundTileSet.Index == tile.TileSetId || g.TransitionTileSet.Index == tile.TileSetId);

            for (int i = 0; i < tileAutoLatGrounds.Count; i++)
            {
                var tileAutoLatGround = tileAutoLatGrounds[i];

                if (tileAutoLatGround != null && tileAutoLatGround.BaseTileSet != null)
                {
                    int baseTileSetId = tileAutoLatGround.BaseTileSet.Index;
                    var baseLatGround = MutationTarget.Map.TheaterInstance.Theater.LATGrounds.Find(
                        g => g.GroundTileSet.Index == baseTileSetId || g.TransitionTileSet.Index == baseTileSetId);

                    if (baseLatGround != null)
                    {
                        baseTileSet = baseLatGround.GroundTileSet;
                        altBaseTileSet = baseLatGround.TransitionTileSet;
                        break;
                    }
                }
            }

            DoForArea(offset =>
            {
                var mapTile = MutationTarget.Map.GetTile(targetCellCoords + offset);
                if (mapTile == null)
                    return;

                int x = mapTile.X;
                int y = mapTile.Y;

                int tileSetIndex = MutationTarget.Map.TheaterInstance.GetTileSetId(mapTile.TileIndex);

                var latGrounds = MutationTarget.Map.TheaterInstance.Theater.LATGrounds;

                // If we're not on a tile can be auto-LAT'd in the first place, skip
                var ourLatGround = latGrounds.Find(lg => lg.GroundTileSet.Index == tileSetIndex || lg.TransitionTileSet.Index == tileSetIndex);
                if (ourLatGround == null)
                    return;

                // For Auto-LAT purposes, consider our current tile set not to be transitional if it is so
                var matchingLatGround = latGrounds.Find(lg => lg.TransitionTileSet.Index == tileSetIndex);
                if (matchingLatGround != null)
                    tileSetIndex = matchingLatGround.GroundTileSet.Index;

                // Don't auto-lat ground that is a base for our placed ground type
                if ((baseTileSet != null && tileSetIndex == baseTileSet.Index) ||
                (altBaseTileSet != null && tileSetIndex == altBaseTileSet.Index))
                    return;

                // Look at the surrounding tiles to figure out the base tile set ID we should use
                int baseTileSetId = -1;

                foreach (var otherTileOffset in surroundingTiles)
                {
                    var otherTile = MutationTarget.Map.GetTile(x + otherTileOffset.X, y + otherTileOffset.Y);
                    if (otherTile == null)
                        continue;

                    int otherTileSetId = MutationTarget.Map.TheaterInstance.GetTileSetId(otherTile.TileIndex);
                    if (otherTileSetId != tileSetIndex)
                    {
                        // Check that the other tile is not a transitional tile type
                        var otherLatGround = latGrounds.Find(lg => lg.TransitionTileSet.Index == otherTileSetId);

                        if (otherLatGround == null)
                        {
                            if (otherTileSetId == 0 || latGrounds.Exists(lg => lg.BaseTileSet.Index == otherTileSetId))
                            {
                                baseTileSetId = otherTileSetId;
                                break;
                            }
                            else if (otherTileSetId != 0 && !latGrounds.Exists(lg => lg.BaseTileSet.Index == otherTileSetId))
                            {
                                baseTileSetId = 0;
                                continue;
                            }
                        }
                        else
                        {
                            // If it is a transitional tile type, then take its base tile set for our base tile set
                            // .. UNLESS we can connect to the transition smoothly as indicated by the non-transition
                            // ground tileset of the other cell's LAT being our base tileset,
                            // then take the actual non-transition ground for our base
                            if (ourLatGround.BaseTileSet == otherLatGround.GroundTileSet)
                                baseTileSetId = otherLatGround.GroundTileSet.Index;
                            else
                                baseTileSetId = otherLatGround.BaseTileSet.Index;

                            break;
                        }
                    }
                }

                if (baseTileSetId == -1)
                {
                    // Based on the surrounding tiles, we shouldn't need to use any base tile set
                    mapTile.TileIndex = MutationTarget.Map.TheaterInstance.Theater.TileSets[tileSetIndex].StartTileIndex;
                    mapTile.SubTileIndex = 0;
                    mapTile.TileImage = null;
                    return;
                }

                var autoLatGround = latGrounds.Find(g => g.GroundTileSet.Index == tileSetIndex &&
                    g.TransitionTileSet.Index != baseTileSetId && g.BaseTileSet.Index == baseTileSetId);

                var tileSet = MutationTarget.Map.TheaterInstance.Theater.TileSets[tileSetIndex];

                // When applying auto-LAT to an alt. terrain tile set, don't apply a transition when we are
                // evaluating a base alt. terrain tile set next to ground that is supposed to be placed on that
                // alt. terrain
                // For example, ~~~Snow shouldn't be auto-LAT'd when it's next to a tile belonging to ~~~Straight Dirt Roads
                Func<TileSet, bool> miscChecker = null;
                if (tileSet.SetName.StartsWith("~~~") && latGrounds.Exists(g => g.BaseTileSet == tileSet))
                {
                    miscChecker = (ts) =>
                    {
                        // On its own line so it's possible to debug this
                        return ts.SetName.StartsWith("~~~") && !latGrounds.Exists(g => g.GroundTileSet == ts);
                    };
                }
                else if (autoLatGround != null && MutationTarget.Map.TheaterInstance.Theater.TileSets.Exists(tSet => autoLatGround.ConnectToTileSetIndices.Contains(tSet.Index)))
                {
                    // Some tilesets connect to LAT types, so transitions should not be applied with them either either.
                    miscChecker = (ts) =>
                    {
                        // On its own line so it's possible to debug this
                        return autoLatGround != null && autoLatGround.ConnectToTileSetIndices.Contains(ts.Index);
                    };
                }

                if (autoLatGround != null)
                {
                    int autoLatIndex = MutationTarget.Map.GetAutoLATIndex(mapTile, autoLatGround.GroundTileSet.Index, autoLatGround.TransitionTileSet.Index, miscChecker);

                    if (autoLatIndex == -1)
                    {
                        // There's an edge case where this code path is hit unintentionally
                        mapTile.TileIndex = autoLatGround.GroundTileSet.StartTileIndex;
                    }
                    else
                    {
                        mapTile.TileIndex = autoLatGround.TransitionTileSet.StartTileIndex + autoLatIndex;
                    }

                    mapTile.SubTileIndex = 0;
                    mapTile.TileImage = null;
                }
            }, true);
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
                }
            }

            MutationTarget.AddRefreshPoint(targetCellCoords);
        }
    }
}
