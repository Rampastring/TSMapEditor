using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    public class PlaceSmudgeCollectionCursorAction : CursorAction
    {
        public PlaceSmudgeCollectionCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Place Smudge Collection";

        private Smudge oldSmudge;
        private Smudge smudge;

        private SmudgeCollection _smudgeCollection;
        public SmudgeCollection SmudgeCollection
        {
            get => _smudgeCollection;
            set
            {
                if (value.Entries.Length == 0)
                {
                    throw new InvalidOperationException($"Smudge collection {value.Name} has no smudge entries!");
                }

                _smudgeCollection = value;
                smudge = new Smudge { SmudgeType = _smudgeCollection.Entries[0].SmudgeType };
            }
        }

        public override void PreMapDraw(Point2D cellCoords)
        {
            var cell = CursorActionTarget.Map.GetTile(cellCoords);

            // "Randomize" the smudge image, it makes it clearer that we're placing down one from a collection.
            // If we used actual RNG here we'd need to avoid doing it every frame to avoid a constantly
            // changing smudge even when the cursor is still. Using cell numbers gives the intended
            // effect without pointless flickering.
            int cellnum = cellCoords.X + cellCoords.Y;
            int smudgeNumber = cellnum % SmudgeCollection.Entries.Length;
            smudge.SmudgeType = SmudgeCollection.Entries[smudgeNumber].SmudgeType;

            oldSmudge = cell.Smudge;

            smudge.Position = cell.CoordsToPoint();
            cell.Smudge = smudge;

            CursorActionTarget.AddRefreshPoint(cellCoords);
        }

        public override void PostMapDraw(Point2D cellCoords)
        {
            var cell = CursorActionTarget.Map.GetTile(cellCoords);
            if (cell.Smudge == smudge)
            {
                cell.Smudge = oldSmudge;
            }

            CursorActionTarget.AddRefreshPoint(cellCoords);
        }

        public override void LeftDown(Point2D cellCoords)
        {
            var mutation = new PlaceSmudgeCollectionMutation(CursorActionTarget.MutationTarget, SmudgeCollection, cellCoords);
            CursorActionTarget.MutationManager.PerformMutation(mutation);
        }

        public override void LeftClick(Point2D cellCoords) => LeftDown(cellCoords);
    }
}
