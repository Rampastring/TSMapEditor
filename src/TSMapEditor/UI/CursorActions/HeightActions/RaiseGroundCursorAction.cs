using TSMapEditor.GameMath;
using TSMapEditor.Mutations.Classes.HeightMutations;

namespace TSMapEditor.UI.CursorActions.HeightActions
{
    public class RaiseGroundCursorAction : CursorAction
    {
        public RaiseGroundCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Raise Ground (Steep Ramps)";

        public override void OnActionEnter()
        {
            CursorActionTarget.BrushSize = Map.EditorConfig.BrushSizes.Find(bs => bs.Width == 3 && bs.Height == 3) ?? Map.EditorConfig.BrushSizes[0];
        }

        public override bool DrawCellCursor => true;

        public override void LeftClick(Point2D cellCoords)
        {
            base.LeftClick(cellCoords);

            var mutation = new RaiseGroundMutation(CursorActionTarget.MutationTarget, cellCoords, CursorActionTarget.BrushSize);
            CursorActionTarget.MutationManager.PerformMutation(mutation);
        }
    }
}
