using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allow setting the follower of a vehicle.
    /// </summary>
    public class SetFollowerCursorAction : CursorAction
    {
        public SetFollowerCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Select Follower";

        public sealed override bool DrawCellCursor => true;

        public sealed override bool HandlesKeyboardInput => true;

        public Unit UnitToFollow { get; set; }

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            Point2D cellTopLeftPoint = CellMath.CellTopLeftPointFromCellCoords_3D(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint;
            cellTopLeftPoint = cellTopLeftPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            const string text = "Left-click on a unit to set it as Follower\r\n\r\nPress ESC to clear follower";
            var textDimensions = Renderer.GetTextDimensions(text, Constants.UIBoldFont);
            int x = cellTopLeftPoint.X + (int)(Constants.CellSizeX - textDimensions.X) / 2;
            int y = cellTopLeftPoint.Y + (int)(Constants.CellSizeY - textDimensions.Y) / 2;

            y -= 120;

            Color color = Color.Gray;
            var cell = Map.GetTile(cellCoords);

            if (cell != null && cell.Vehicles.Count > 0 && cell.Vehicles[0] != UnitToFollow)
                color = Color.Yellow;

            var rect = new Rectangle(x - Constants.UIEmptySideSpace, 
                y - Constants.UIEmptyTopSpace, 
                (int)textDimensions.X + Constants.UIEmptySideSpace * 2, 
                (int)textDimensions.Y + Constants.UIEmptyTopSpace + Constants.UIEmptyBottomSpace);

            Renderer.FillRectangle(rect, UISettings.ActiveSettings.PanelBackgroundColor);
            Renderer.DrawRectangle(rect, UISettings.ActiveSettings.PanelBorderColor);

            Renderer.DrawStringWithShadow(text,
                Constants.UIBoldFont,
                new Vector2(x, y),
                color);
        }

        public override void OnKeyPressed(KeyPressEventArgs e, Point2D cellCoords)
        {
            if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Escape)
            {
                e.Handled = true;
                PerformMutation(new SetFollowerMutation(MutationTarget, UnitToFollow, null));
                ExitAction();
            }
        }

        public override void LeftClick(Point2D cellCoords)
        {
            var cell = Map.GetTile(cellCoords);
            if (cell == null)
                return;

            if (cell.Vehicles.Count == 0)
                return;

            if (cell.Vehicles[0] == UnitToFollow)
                return;

            PerformMutation(new SetFollowerMutation(MutationTarget, UnitToFollow, cell.Vehicles[0]));
            ExitAction();
        }

        public override void LeftDown(Point2D cellCoords) => LeftClick(cellCoords);
    }
}
