using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that places a smudge on the map.
    /// </summary>
    public class PlaceSmudgeMutation : Mutation
    {
        public PlaceSmudgeMutation(IMutationTarget mutationTarget, SmudgeType smudgeType, Point2D cellCoords) : base(mutationTarget)
        {
            this.smudgeType = smudgeType;
            this.cellCoords = cellCoords;
        }

        private Smudge oldSmudge;
        private SmudgeType smudgeType;
        private Point2D cellCoords;

        public override void Perform()
        {
            var cell = MutationTarget.Map.GetTile(cellCoords);
            if (cell.Smudge != null)
                oldSmudge = cell.Smudge;

            if (smudgeType != null)
                cell.Smudge = new Smudge() { SmudgeType = smudgeType, Position = cellCoords };
            else
                cell.Smudge = null; // delete an existing smudge if smudge type is null

            MutationTarget.AddRefreshPoint(cellCoords, 1);
        }

        public override void Undo()
        {
            var cell = MutationTarget.Map.GetTile(cellCoords);

            // if oldSmudge is null, then this has the same effect as cell.Smudge = null
            // otherwise the smudge is replaced with the old smudge
            // iow. we don't need to handle the null case separately
            cell.Smudge = oldSmudge;
            MutationTarget.AddRefreshPoint(cellCoords, 1);
        }
    }
}
