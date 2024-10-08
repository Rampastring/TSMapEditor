using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    public class ChangeAttachedTagCursorAction : CursorAction
    {
        public ChangeAttachedTagCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Change Attached Tag";

        public Tag TagToAttach { get; set; }

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            Point2D cellTopLeftPoint = CellMath.CellTopLeftPointFromCellCoords(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint;
            cellTopLeftPoint = cellTopLeftPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            var mapCell = CursorActionTarget.Map.GetTile(cellCoords);
            if (mapCell == null)
                return;

            TechnoBase cellTechno = mapCell.GetTechno();
            Color textColor = cellTechno == null || cellTechno.AttachedTag == TagToAttach ? Color.Gray : Color.HotPink;

            const string text = "Attach Tag";
            var textDimensions = Renderer.GetTextDimensions(text, Constants.UIBoldFont);
            int x = cellTopLeftPoint.X - (int)(textDimensions.X - Constants.CellSizeX) / 2;

            Renderer.DrawStringWithShadow(text,
                Constants.UIBoldFont,
                new Vector2(x, cellTopLeftPoint.Y),
                textColor);
        }

        public override void LeftClick(Point2D cellCoords)
        {
            var mapCell = CursorActionTarget.Map.GetTile(cellCoords);
            if (mapCell == null)
                return;

            var cellTechno = mapCell.GetTechno();
            if (cellTechno == null)
                return;

            if (cellTechno.AttachedTag == TagToAttach)
                return;

            CursorActionTarget.MutationManager.PerformMutation(new ChangeAttachedTagMutation(CursorActionTarget.MutationTarget, cellTechno, TagToAttach));
        }

        public override void LeftDown(Point2D cellCoords) => LeftClick(cellCoords);
    }
}
