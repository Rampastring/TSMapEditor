using TSMapEditor.GameMath;
using TSMapEditor.Mutations.Classes.HeightMutations;

namespace TSMapEditor.UI.CursorActions.HeightActions
{
    /// <summary>
    /// Non-steep, "FinalSun-like" ground lowering cursor action.
    /// </summary>
    internal class FSLowerGroundCursorAction : CursorAction
    {
        public FSLowerGroundCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Lower Ground (Non-Steep Ramps)";

        public override void OnActionEnter()
        {
            CursorActionTarget.BrushSize = Map.EditorConfig.BrushSizes.Find(bs => bs.Width == 2 && bs.Height == 2) ?? Map.EditorConfig.BrushSizes[0];
        }

        public override bool DrawCellCursor => true;

        public override void LeftClick(Point2D cellCoords)
        {
            base.LeftClick(cellCoords);

            var mutation = new FSLowerGroundMutation(CursorActionTarget.MutationTarget, cellCoords, CursorActionTarget.BrushSize);
            CursorActionTarget.MutationManager.PerformMutation(mutation);
        }
    }
}
