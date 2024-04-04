using System;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;

namespace TSMapEditor.Scripts
{
    public class CR04Script
    {
        public static void Perform(Map map)
        {
            Point2D[] offsets = new Point2D[] { new Point2D(-1, 0), new Point2D(1, 0), new Point2D(0, -1), new Point2D(0, 1) };

            var snowPebblesTileSet = map.TheaterInstance.Theater.TileSets.Find(ts => ts.SetName == "~~~Pebbles");
            var snowTileSet = map.TheaterInstance.Theater.TileSets.Find(ts => ts.SetName == "~~~Snow");

            map.DoForAllValidTiles(mapCell =>
            {
                var tileIndex = mapCell.TileIndex;
                var tile = map.TheaterInstance.GetTile(tileIndex);
                var tileSet = map.TheaterInstance.Theater.TileSets[tile.TileSetId];

                // If we a non-snowy pebble tile or clear tile with only snow-to-clear LAT around it, then replace it with a snowy pebble tile or a snowy clear tile
                if (tileSet.SetName == "Pebbles" || tileSet.SetName == "Clear")
                {
                    bool isValid = true;

                    foreach (var offset in offsets)
                    {
                        var otherCell = map.GetTile(mapCell.CoordsToPoint() + offset);
                        if (otherCell == null)
                            continue;

                        var otherTileIndex = otherCell.TileIndex;
                        var otherTileSet = map.TheaterInstance.Theater.TileSets[map.TheaterInstance.GetTile(otherTileIndex).TileSetId];
                        if (otherTileSet.SetName != "~~~Snow/Clear LAT")
                        {
                            isValid = false;
                            break;
                        }
                    }

                    if (isValid)
                    {
                        if (tileSet.SetName == "Pebbles")
                            mapCell.ChangeTileIndex(snowPebblesTileSet.StartTileIndex, 0);
                        else
                            mapCell.ChangeTileIndex(snowTileSet.StartTileIndex, 0);

                        foreach (var offset in offsets)
                        {
                            var otherCell = map.GetTile(mapCell.CoordsToPoint() + offset);
                            if (otherCell == null)
                                continue;

                            otherCell.ChangeTileIndex(snowTileSet.StartTileIndex, 0);
                        }
                    }
                }
            });

            map.DoForAllValidTiles(mapCell => ApplyAutoLAT(map, mapCell.X, mapCell.Y));
        }


        private static void ApplyAutoLAT(Map map, int x, int y)
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

            var mapTile = map.GetTile(x, y);
            if (mapTile == null)
                return;

            int tileSetIndex = map.TheaterInstance.GetTileSetId(mapTile.TileIndex);
            var tileSet = map.TheaterInstance.Theater.TileSets[tileSetIndex];
            // Don't auto-lat ground that is a base for our placed ground type
            // if ((baseTileSet != null && tileSetIndex == baseTileSet.Index) ||
            //     (altBaseTileSet != null && tileSetIndex == altBaseTileSet.Index))
            //     return;

            // MutationTarget.Map.TheaterInstance.Theater.TileSets[tileSetIndex].SetName.StartsWith("~~~")

            var latGrounds = map.TheaterInstance.Theater.LATGrounds;
            var autoLatGround = map.TheaterInstance.Theater.LATGrounds.Find(g => g.GroundTileSet.Index == tileSetIndex || g.TransitionTileSet.Index == tileSetIndex);

            Func<TileSet, bool> miscChecker = null;
            if (tileSet.SetName.StartsWith("~~~"))
            {
                miscChecker = (ts) =>
                {
                    // On its own line so it's possible to debug this
                    return ts.SetName.StartsWith("~~~") && !latGrounds.Exists(g => g.GroundTileSet == ts);
                };
            }
            else if (autoLatGround != null && map.TheaterInstance.Theater.TileSets.Exists(tSet => autoLatGround.ConnectToTileSetIndices.Contains(tSet.Index)))
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
                int autoLatIndex = map.GetAutoLATIndex(mapTile, autoLatGround.GroundTileSet.Index, autoLatGround.TransitionTileSet.Index, miscChecker);
                if (autoLatIndex == -1)
                {
                    mapTile.TileIndex = autoLatGround.GroundTileSet.StartTileIndex;
                }
                else
                {
                    mapTile.TileIndex = autoLatGround.TransitionTileSet.StartTileIndex + autoLatIndex;
                }

                mapTile.SubTileIndex = 0;
                mapTile.TileImage = null;
            }
        }
    }
}
