using System;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;

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
                    var mapTile = MutationTarget.Map.GetTile(x, y);
                    if (mapTile == null)
                        continue;

                    int tileSetIndex = MutationTarget.Map.TheaterInstance.GetTileSetId(mapTile.TileIndex);

                    var latGrounds = MutationTarget.Map.TheaterInstance.Theater.LATGrounds;

                    // If we're not on a tile can be auto-LAT'd in the first place, skip
                    var ourLatGround = latGrounds.Find(lg => lg.GroundTileSet.Index == tileSetIndex || lg.TransitionTileSet.Index == tileSetIndex);
                    if (ourLatGround == null)
                        continue;

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
                        continue;
                    }

                    var tileSet = MutationTarget.Map.TheaterInstance.Theater.TileSets[tileSetIndex];
                    // Don't auto-lat ground that is a base for our placed ground type
                    // if ((baseTileSet != null && tileSetIndex == baseTileSet.Index) ||
                    //     (altBaseTileSet != null && tileSetIndex == altBaseTileSet.Index))
                    //     return;

                    // MutationTarget.Map.TheaterInstance.Theater.TileSets[tileSetIndex].SetName.StartsWith("~~~")

                    // When applying auto-LAT to an alt. terrain tile set, don't apply a transition when we are
                    // evaluating a base alt. terrain tile set next to ground that is supposed on place on that
                    // alt. terrain
                    // For example, ~~~Snow shouldn't be auto-LAT'd when it's next to a tile belonging to ~~~Straight Dirt Roads

                    var autoLatGround = latGrounds.Find(g => (g.GroundTileSet.Index == tileSetIndex || g.TransitionTileSet.Index == tileSetIndex) &&
                    g.TransitionTileSet.Index != baseTileSetId && g.BaseTileSet.Index == baseTileSetId);

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
    }
}
