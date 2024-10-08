using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that allows placing a CellTag on the map.
    /// </summary>
    public class PlaceCellTagMutation : Mutation
    {
        public PlaceCellTagMutation(IMutationTarget mutationTarget, Point2D cellCoords, Tag tag) : base(mutationTarget)
        {
            this.cellCoords = cellCoords;
            this.tag = tag;
        }

        private readonly Point2D cellCoords;
        private readonly Tag tag;

        public override void Perform()
        {
            MutationTarget.Map.AddCellTag(new CellTag(cellCoords, tag));
            MutationTarget.AddRefreshPoint(cellCoords, 1);
        }

        public override void Undo()
        {
            MutationTarget.Map.RemoveCellTagFrom(cellCoords);
            MutationTarget.AddRefreshPoint(cellCoords, 1);
        }
    }
}
