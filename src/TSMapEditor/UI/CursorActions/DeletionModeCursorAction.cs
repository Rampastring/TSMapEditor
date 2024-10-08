using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using TSMapEditor.GameMath;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allows the user to delete anything.
    /// </summary>
    public class DeletionModeCursorAction : CursorAction
    {
        public DeletionModeCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Delete Object";

        public override bool DrawCellCursor => true;

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            Point2D cellCenterPoint;

            if (CursorActionTarget.Is2DMode)
                cellCenterPoint = CellMath.CellCenterPointFromCellCoords(cellCoords, Map) - cameraTopLeftPoint;
            else
                cellCenterPoint = CellMath.CellCenterPointFromCellCoords_3D(cellCoords, Map) - cameraTopLeftPoint;

            cellCenterPoint = cellCenterPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            const string text = "Delete";
            var textDimensions = Renderer.GetTextDimensions(text, Constants.UIBoldFont);
            int x = cellCenterPoint.X - (int)(textDimensions.X / 2);
            int y = cellCenterPoint.Y - (int)(textDimensions.Y / 2);

            Renderer.DrawStringWithShadow(text,
                Constants.UIBoldFont,
                new Vector2(x, y),
                Color.Red);
        }

        public override void LeftClick(Point2D cellCoords)
        {
            if (Map.HasObjectToDelete(cellCoords, CursorActionTarget.DeletionMode))
            {
                PerformMutation(new DeleteObjectMutation(MutationTarget, cellCoords, CursorActionTarget.DeletionMode));
                CursorActionTarget.AddRefreshPoint(cellCoords);
            }
        }

        public override void LeftDown(Point2D cellCoords) => LeftClick(cellCoords);
    }
}
