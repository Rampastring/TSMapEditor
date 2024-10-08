using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that allows placing terrain objects on the map.
    /// </summary>
    public class PlaceTerrainObjectMutation : Mutation
    {
        public PlaceTerrainObjectMutation(IMutationTarget mutationTarget, TerrainType terrainType, Point2D cellCoords) : base(mutationTarget)
        {
            this.terrainType = terrainType;
            this.cellCoords = cellCoords;
        }

        private readonly TerrainType terrainType;
        private readonly Point2D cellCoords;

        public override void Perform()
        {
            var tile = MutationTarget.Map.GetTile(cellCoords);
            if (tile.TerrainObject != null)
                throw new InvalidOperationException("Cannot place a terrain object on a tile that already has a terrain object!");

            var terrainObject = new TerrainObject(terrainType, cellCoords);
            MutationTarget.Map.AddTerrainObject(terrainObject);
        }

        public override void Undo()
        {
            MutationTarget.Map.RemoveTerrainObject(cellCoords);
        }
    }
}
