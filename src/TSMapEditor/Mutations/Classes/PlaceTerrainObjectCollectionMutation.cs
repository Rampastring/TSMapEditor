using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that allows placing terrain object collections.
    /// </summary>
    class PlaceTerrainObjectCollectionMutation : Mutation
    {
        public PlaceTerrainObjectCollectionMutation(IMutationTarget mutationTarget, TerrainObjectCollection terrainObjectCollection, Point2D cellCoords) : base(mutationTarget)
        {
            this.terrainObjectCollection = terrainObjectCollection;
            this.cellCoords = cellCoords;
        }

        private readonly TerrainObjectCollection terrainObjectCollection;
        private readonly Point2D cellCoords;

        public override void Perform()
        {
            var tile = MutationTarget.Map.GetTile(cellCoords);
            if (tile.TerrainObject != null)
                throw new InvalidOperationException("Cannot place a terrain object on a tile that already has a terrain object!");

            var collectionEntry = terrainObjectCollection.Entries[MutationTarget.Randomizer.GetRandomNumber(0, terrainObjectCollection.Entries.Length - 1)];
            var terrainObject = new TerrainObject(collectionEntry.TerrainType, cellCoords);
            MutationTarget.Map.AddTerrainObject(terrainObject);
        }

        public override void Undo()
        {
            MutationTarget.Map.RemoveTerrainObject(cellCoords);
        }
    }
}
