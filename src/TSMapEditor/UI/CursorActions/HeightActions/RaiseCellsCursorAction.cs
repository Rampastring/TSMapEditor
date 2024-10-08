using TSMapEditor.GameMath;
using TSMapEditor.Mutations.Classes.HeightMutations;

namespace TSMapEditor.UI.CursorActions.HeightActions
{
    internal class RaiseCellsCursorAction : CursorAction
    {
        public RaiseCellsCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Raise Cells";

        public override bool DrawCellCursor => true;

        public override void LeftClick(Point2D cellCoords)
        {
            base.LeftClick(cellCoords);

            Point2D targetCellCoords = cellCoords - new Point2D(CursorActionTarget.BrushSize.Width / 2, CursorActionTarget.BrushSize.Height / 2);
            bool applyOnArea = KeyboardCommands.Instance.FillTerrain.AreKeysOrModifiersDown(CursorActionTarget.WindowManager.Keyboard);

            var mutation = new RaiseCellsMutation(CursorActionTarget.MutationTarget, targetCellCoords, CursorActionTarget.BrushSize, applyOnArea);
            CursorActionTarget.MutationManager.PerformMutation(mutation);
        }
    }
}
