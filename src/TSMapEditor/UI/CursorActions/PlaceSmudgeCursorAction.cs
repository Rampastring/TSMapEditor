using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Rendering;

namespace TSMapEditor.UI.CursorActions
{
    public class PlaceSmudgeCursorAction : CursorAction
    {
        public PlaceSmudgeCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
            previewSmudge = new Smudge();
        }

        public override string GetName() => "Place Smudge";

        private SmudgeType _smudgeType;
        public SmudgeType SmudgeType 
        {
            get => _smudgeType;
            set
            {
                if (value != _smudgeType)
                {
                    _smudgeType = value;
                    previewSmudge.SmudgeType = value;
                } 
            }
        }

        private Smudge cachedSmudge;
        private Smudge previewSmudge;

        public override void PreMapDraw(Point2D cellCoords)
        {
            base.PreMapDraw(cellCoords);
        
            var cell = CursorActionTarget.Map.GetTile(cellCoords);
            if (cell == null)
                return;
        
            previewSmudge.Position = cellCoords;
            cachedSmudge = cell.Smudge;
            if (SmudgeType != null)
                cell.Smudge = previewSmudge;
            else
                cell.Smudge = null;
        
            CursorActionTarget.AddRefreshPoint(cellCoords, 1);
        }
        
        public override void PostMapDraw(Point2D cellCoords)
        {
            base.PostMapDraw(cellCoords);
        
            var cell = CursorActionTarget.Map.GetTile(cellCoords);
            if (cell == null)
                return;
        
            cell.Smudge = cachedSmudge;
            CursorActionTarget.AddRefreshPoint(cellCoords, 1);
        }

        public override void LeftClick(Point2D cellCoords)
        {
            var cell = CursorActionTarget.Map.GetTile(cellCoords);
            if (cell == null)
                return;

            if (cell.Smudge != null && cell.Smudge.SmudgeType == SmudgeType)
            {
                // it's pointless to replace a smudge with another smudge of the same type
                return;
            }

            if (cell.Smudge == null && SmudgeType == null)
                return; // we're in deletion mode when SmudgeType == null, skip if there's nothing to delete

            CursorActionTarget.MutationManager.PerformMutation(new PlaceSmudgeMutation(CursorActionTarget.MutationTarget, SmudgeType, cellCoords));
        }

        public override void LeftDown(Point2D cellCoords) => LeftClick(cellCoords);
    }
}
