using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Rendering;

namespace TSMapEditor.UI.CursorActions
{
    public class PlaceSmudgeCollectionCursorAction : CursorAction
    {
        public PlaceSmudgeCollectionCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Place Smudge Collection";

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
            if (cell.Smudge == null)
            {
                smudge.Position = cell.CoordsToPoint();
                cell.Smudge = smudge;
            }

            CursorActionTarget.AddRefreshPoint(cellCoords);
        }

        public override void PostMapDraw(Point2D cellCoords)
        {
            var cell = CursorActionTarget.Map.GetTile(cellCoords);
            if (cell.Smudge == smudge)
            {
                cell.Smudge = null;
            }

            CursorActionTarget.AddRefreshPoint(cellCoords);
        }

        public override void LeftDown(Point2D cellCoords)
        {
            var cell = CursorActionTarget.Map.GetTile(cellCoords);
            if (cell.Smudge != null)
                return;

            var mutation = new PlaceSmudgeCollectionMutation(CursorActionTarget.MutationTarget, SmudgeCollection, cellCoords);
            CursorActionTarget.MutationManager.PerformMutation(mutation);
        }

        public override void LeftClick(Point2D cellCoords) => LeftDown(cellCoords);
    }
}
