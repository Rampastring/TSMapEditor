using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    public class DeleteTubeCursorAction : CursorAction
    {
        public DeleteTubeCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Delete Tube";

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            var tube = CursorActionTarget.Map.Tubes.Find(tb => tb.EntryPoint == cellCoords || tb.ExitPoint == cellCoords);
            Color color = tube == null ? Color.Gray : Color.Red;

            Func<Point2D, Map, Point2D> getCellCenterPoint = Is2DMode ? CellMath.CellCenterPointFromCellCoords : CellMath.CellCenterPointFromCellCoords_3D;
            Point2D cellPixelCoords = getCellCenterPoint(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint;
            cellPixelCoords = new Point2D(cellPixelCoords.X + Constants.CellSizeX / 2, cellPixelCoords.Y);
            cellPixelCoords = cellPixelCoords.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            const string text = "Delete Tunnel";
            var textDimensions = Renderer.GetTextDimensions(text, Constants.UIBoldFont);
            int x = cellPixelCoords.X - (int)(textDimensions.X / 2);

            Renderer.DrawStringWithShadow(text,
                Constants.UIBoldFont,
                new Vector2(x, cellPixelCoords.Y),
                color);
        }

        public override void LeftClick(Point2D cellCoords)
        {
            var tube = CursorActionTarget.Map.Tubes.Find(tb => tb.EntryPoint == cellCoords || tb.ExitPoint == cellCoords);
            if (tube == null)
                return;

            CursorActionTarget.MutationManager.PerformMutation(new DeleteTubeMutation(CursorActionTarget.MutationTarget, cellCoords));
        }

        public override void LeftDown(Point2D cellCoords) => LeftClick(cellCoords);
    }
}
