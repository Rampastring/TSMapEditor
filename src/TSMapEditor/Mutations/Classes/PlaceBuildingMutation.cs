using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{

    /// <summary>
    /// A mutation that allows placing a building on the map.
    /// </summary>
    public class PlaceBuildingMutation : Mutation
    {
        public PlaceBuildingMutation(IMutationTarget mutationTarget, BuildingType buildingType, Point2D cellCoords) : base(mutationTarget)
        {
            this.buildingType = buildingType;
            this.cellCoords = cellCoords;
        }

        private readonly BuildingType buildingType;
        private readonly Point2D cellCoords;

        private Structure placedBuilding;

        public override void Perform()
        {
            var cell = MutationTarget.Map.GetTileOrFail(cellCoords);

            var structure = new Structure(buildingType);
            structure.Owner = MutationTarget.ObjectOwner;
            structure.Position = cellCoords;
            MutationTarget.Map.PlaceBuilding(structure);
            MutationTarget.AddRefreshPoint(cellCoords);

            placedBuilding = structure;
            MutationTarget.AddRefreshPoint(cellCoords);
        }

        public override void Undo()
        {
            MutationTarget.Map.RemoveBuilding(placedBuilding);
            MutationTarget.AddRefreshPoint(cellCoords);
        }
    }
}
