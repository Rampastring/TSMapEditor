using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that allows placing smudge collections.
    /// </summary>
    class PlaceSmudgeCollectionMutation : Mutation
    {
        public PlaceSmudgeCollectionMutation(IMutationTarget mutationTarget, SmudgeCollection smudgeCollection, Point2D cellCoords) : base(mutationTarget)
        {
            this.smudgeCollection = smudgeCollection;
            this.cellCoords = cellCoords;
        }

        private Smudge oldSmudge;
        private readonly SmudgeCollection smudgeCollection;
        private readonly Point2D cellCoords;

        public override void Perform()
        {
            var cell = MutationTarget.Map.GetTile(cellCoords);
            if (cell.Smudge != null)
                oldSmudge = cell.Smudge;

            var collectionEntry = smudgeCollection.Entries[MutationTarget.Randomizer.GetRandomNumber(0, smudgeCollection.Entries.Length - 1)];
            cell.Smudge = new Smudge() { SmudgeType = collectionEntry.SmudgeType, Position = cellCoords };

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
