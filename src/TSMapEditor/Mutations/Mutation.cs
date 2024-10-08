using System;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations
{
    /// <summary>
    /// A base class for all mutations.
    /// A mutation modifies something in the map in a way that makes the effect
    /// un-doable and re-doable through the Undo/Redo system.
    /// </summary>
    public abstract class Mutation
    {
        public Mutation(IMutationTarget mutationTarget)
        {
            MutationTarget = mutationTarget;
        }

        protected IMutationTarget MutationTarget { get; }

        protected Map Map => MutationTarget.Map;

        public abstract void Perform();

        public abstract void Undo();


        private static readonly Point2D[] surroundingTiles = new Point2D[] { new Point2D(-1, 0), new Point2D(1, 0), new Point2D(0, -1), new Point2D(0, 1) };

        protected void ApplyGenericAutoLAT(int minX, int minY, int maxX, int maxY)
        {
            // Get potential base tilesets of the placed LAT (if we're placing LAT)
            // This allows placing certain LATs on top of other LATs (example: snowy dirt on snow, when snow is also placed on grass)
            // TileSet baseTileSet = null;
            // TileSet altBaseTileSet = null;
            // var tileAutoLatGround = map.TheaterInstance.Theater.LATGrounds.Find(
            //     g => g.GroundTileSet.Index == tileSetId || g.TransitionTileSet.Index == tileSetId);
            // 
            // if (tileAutoLatGround != null && tileAutoLatGround.BaseTileSet != null)
            // {
            //     int baseTileSetId = tileAutoLatGround.BaseTileSet.Index;
            //     var baseLatGround = map.TheaterInstance.Theater.LATGrounds.Find(
            //         g => g.GroundTileSet.Index == baseTileSetId || g.TransitionTileSet.Index == baseTileSetId);
            // 
            //     if (baseLatGround != null)
            //     {
            //         baseTileSet = baseLatGround.GroundTileSet;
            //         altBaseTileSet = baseLatGround.TransitionTileSet;
            //     }
            // }

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Point2D cellCoords = new Point2D(x, y);
                    int tileIndex = GetAutoLATTileIndexForCell(Map, cellCoords, null, null, false);

                    if (tileIndex > -1)
                    {
                        Map.GetTile(cellCoords).ChangeTileIndex(tileIndex, 0);
                    }
                }
            }
        }

        public static int GetAutoLATTileIndexForCell(Map map, Point2D targetCellCoords, TileSet baseTileSet, TileSet altBaseTileSet, bool usePreview)
        {
            int tileIndex = -1;

            var mapTile = map.GetTile(targetCellCoords);
            if (mapTile == null)
                return tileIndex;

            int x = mapTile.X;
            int y = mapTile.Y;

            int tileSetIndex = map.TheaterInstance.GetTileSetId((usePreview && mapTile.PreviewTileImage != null) ? mapTile.PreviewTileImage.TileID : mapTile.TileIndex);

            var latGrounds = map.TheaterInstance.Theater.LATGrounds;

            // If we're not on a tile can be auto-LAT'd in the first place, skip
            var ourLatGround = latGrounds.Find(lg => lg.GroundTileSet.Index == tileSetIndex || lg.TransitionTileSet.Index == tileSetIndex);
            if (ourLatGround == null)
                return tileIndex;

            // For Auto-LAT purposes, consider our current tile set not to be transitional if it is so
            var matchingLatGround = latGrounds.Find(lg => lg.TransitionTileSet.Index == tileSetIndex);
            if (matchingLatGround != null)
                tileSetIndex = matchingLatGround.GroundTileSet.Index;

            // Don't auto-lat ground that is a base for our placed ground type
            if ((baseTileSet != null && tileSetIndex == baseTileSet.Index) ||
                (altBaseTileSet != null && tileSetIndex == altBaseTileSet.Index))
                return tileIndex;

            // Look at the surrounding tiles to figure out the base tile set ID we should use
            int baseTileSetId = -1;

            foreach (var otherTileOffset in surroundingTiles)
            {
                var otherTile = map.GetTile(x + otherTileOffset.X, y + otherTileOffset.Y);
                if (otherTile == null)
                    continue;

                int otherTileSetId = map.TheaterInstance.GetTileSetId((usePreview && otherTile.PreviewTileImage != null) ? 
                    otherTile.PreviewTileImage.TileID : otherTile.TileIndex);

                if (otherTileSetId != tileSetIndex)
                {
                    // Check that the other tile is not a transitional tile type
                    var otherLatGround = latGrounds.Find(lg => lg.TransitionTileSet.Index == otherTileSetId);

                    if (otherLatGround == null)
                    {
                        if (otherTileSetId == 0 || latGrounds.Exists(lg => lg.BaseTileSet.Index == otherTileSetId && lg.GroundTileSet.Index == tileSetIndex))
                        {
                            baseTileSetId = otherTileSetId;
                            break;
                        }
                        else /*if (otherTileSetId != 0 && !latGrounds.Exists(lg => lg.BaseTileSet.Index == otherTileSetId))*/
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
                tileIndex = map.TheaterInstance.Theater.TileSets[tileSetIndex].StartTileIndex;
                return tileIndex;
            }

            var autoLatGround = latGrounds.Find(g => g.GroundTileSet.Index == tileSetIndex &&
                g.TransitionTileSet.Index != baseTileSetId && g.BaseTileSet.Index == baseTileSetId);

            var tileSet = map.TheaterInstance.Theater.TileSets[tileSetIndex];

            Func<TileSet, bool> miscChecker = null;

            // Some tilesets connect to LAT types, so transitions should not be applied with them
            if (autoLatGround != null && map.TheaterInstance.Theater.TileSets.Exists(tSet => autoLatGround.ConnectToTileSetIndices.Contains(tSet.Index)))
            {
                miscChecker = (ts) =>
                {
                    // On its own line so it's possible to debug this
                    return autoLatGround != null && autoLatGround.ConnectToTileSetIndices.Contains(ts.Index);
                };
            }

            if (autoLatGround != null)
            {
                int autoLatIndex = map.GetAutoLATIndex(mapTile, autoLatGround.GroundTileSet.Index, autoLatGround.TransitionTileSet.Index, usePreview, miscChecker);

                if (autoLatIndex == -1)
                {
                    tileIndex = autoLatGround.GroundTileSet.StartTileIndex;
                }
                else
                {
                    tileIndex = autoLatGround.TransitionTileSet.StartTileIndex + autoLatIndex;
                }
            }

            return tileIndex;
        }

        public static (TileSet baseTileSet, TileSet altBaseTileSet) GetBaseTileSetsForTileSet(ITheater theater, int tileSetId)
        {
            TileSet baseTileSet = null;
            TileSet altBaseTileSet = null;
            var tileAutoLatGrounds = theater.Theater.LATGrounds.FindAll(
                g => g.GroundTileSet.Index == tileSetId || g.TransitionTileSet.Index == tileSetId);

            for (int i = 0; i < tileAutoLatGrounds.Count; i++)
            {
                var tileAutoLatGround = tileAutoLatGrounds[i];

                if (tileAutoLatGround != null && tileAutoLatGround.BaseTileSet != null)
                {
                    int baseTileSetId = tileAutoLatGround.BaseTileSet.Index;
                    var baseLatGround = theater.Theater.LATGrounds.Find(
                        g => g.GroundTileSet.Index == baseTileSetId || g.TransitionTileSet.Index == baseTileSetId);

                    if (baseLatGround != null)
                    {
                        baseTileSet = baseLatGround.GroundTileSet;
                        altBaseTileSet = baseLatGround.TransitionTileSet;
                        break;
                    }
                }
            }

            return (baseTileSet, altBaseTileSet);
        }

        protected void ApplyAutoLATForTilePlacement(TileImage tile, BrushSize brushSize, Point2D targetCellCoords)
        {
            // Get potential base tilesets of the placed LAT (if we're placing LAT)
            // This allows placing certain LATs on top of other LATs (example: snowy dirt on snow, when snow is also placed on grass)
            (var baseTileSet, var altBaseTileSet) = GetBaseTileSetsForTileSet(Map.TheaterInstance, tile.TileSetId);

            int totalWidth = tile.Width * brushSize.Width;
            int totalHeight = tile.Height * brushSize.Height;
            totalWidth++;
            totalHeight++;

            ApplyAutoLATForTileSetPlacement(tile.TileSetId,
                targetCellCoords.X - 1,
                targetCellCoords.Y - 1,
                targetCellCoords.X + totalWidth,
                targetCellCoords.Y + totalHeight);
        }

        protected void ApplyAutoLATForTileSetPlacement(int tileSetIndex, int minX, int minY, int maxX, int maxY)
        {
            (var baseTileSet, var altBaseTileSet) = GetBaseTileSetsForTileSet(Map.TheaterInstance, tileSetIndex);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Point2D cellCoords = new Point2D(x, y);
                    int tileIndex = GetAutoLATTileIndexForCell(Map, cellCoords, baseTileSet, altBaseTileSet, false);

                    if (tileIndex > -1)
                    {
                        Map.GetTile(cellCoords).ChangeTileIndex(tileIndex, 0);
                    }
                }
            }
        }
    }
}
