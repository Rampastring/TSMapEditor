using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// Cursor action for placing bridges.
    /// </summary>
    public class PlaceBridgeCursorAction : CursorAction
    {
        public PlaceBridgeCursorAction(ICursorActionTarget cursorActionTarget, BridgeType bridgeType) : base(cursorActionTarget)
        {
            this.bridgeType = bridgeType;
        }

        public override string GetName() => "Draw Bridge";

        public override bool HandlesKeyboardInput => true;

        public override bool DrawCellCursor => true;

        private readonly BridgeType bridgeType;

        private Point2D startPoint;
        private Point2D endPoint;

        public override void OnActionEnter()
        {
            startPoint = Point2D.NegativeOne;
            endPoint = Point2D.NegativeOne;

            base.OnActionEnter();
        }

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            Point2D cellTopLeftPoint = CellMath.CellTopLeftPointFromCellCoords(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint;

            cellTopLeftPoint = cellTopLeftPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            const string text = "Hold left click to draw bridge.\r\n\r\nENTER to confirm\r\nBackspace to clear\r\nRight-click or ESC to exit";
            var textDimensions = Renderer.GetTextDimensions(text, Constants.UIBoldFont);
            int x = cellTopLeftPoint.X - (int)(textDimensions.X - Constants.CellSizeX) / 2;

            Vector2 textPosition = new Vector2(x + 60, cellTopLeftPoint.Y - 150);

            Rectangle textBackgroundRectangle = new Rectangle((int)textPosition.X - Constants.UIEmptySideSpace,
                (int)textPosition.Y - Constants.UIEmptyTopSpace,
                (int)textDimensions.X + Constants.UIEmptySideSpace * 2,
                (int)textDimensions.Y + Constants.UIEmptyBottomSpace + Constants.UIEmptyTopSpace);

            Renderer.FillRectangle(textBackgroundRectangle, UISettings.ActiveSettings.PanelBackgroundColor);
            Renderer.DrawRectangle(textBackgroundRectangle, UISettings.ActiveSettings.PanelBorderColor);

            Renderer.DrawStringWithShadow(text, Constants.UIBoldFont, textPosition, Color.Yellow);

            Func<Point2D, Map, Point2D> getCellCenterPoint = Is2DMode ? CellMath.CellCenterPointFromCellCoords : CellMath.CellCenterPointFromCellCoords_3D;

            // Draw bridge lines
            if (startPoint != Point2D.NegativeOne && endPoint != Point2D.NegativeOne && endPoint != startPoint)
            {
                var bridgeDirection = GetBridgeDirection();

                Point2D cellEndPoint;

                Point2D corner1;
                Point2D corner2;
                Point2D corner3;
                Point2D corner4;

                // Time for math!
                if (bridgeDirection == BridgeDirection.EastWest)
                {
                    Point2D actualStartPoint = startPoint;
                    Point2D actualEndPoint = new Point2D(endPoint.X, startPoint.Y);

                    if (startPoint.X > endPoint.X)
                    {
                        actualStartPoint = actualEndPoint;
                        actualEndPoint = startPoint;
                    }

                    cellEndPoint = new Point2D(actualEndPoint.X, actualStartPoint.Y);

                    corner1 = actualStartPoint + new Point2D(0, 1);
                    corner1 = getCellCenterPoint(corner1, CursorActionTarget.Map) - cameraTopLeftPoint;
                    corner1 += new Point2D(Constants.CellSizeX / -2, 0);

                    corner2 = actualStartPoint + new Point2D(0, -1);
                    corner2 = getCellCenterPoint(corner2, CursorActionTarget.Map) - cameraTopLeftPoint;
                    corner2 += new Point2D(0, Constants.CellSizeY / -2);

                    corner3 = cellEndPoint + new Point2D(0, 1);
                    corner3 = getCellCenterPoint(corner3, CursorActionTarget.Map) - cameraTopLeftPoint;
                    corner3 += new Point2D(0, Constants.CellSizeY / 2);

                    corner4 = cellEndPoint + new Point2D(0, -1);
                    corner4 = getCellCenterPoint(corner4, CursorActionTarget.Map) - cameraTopLeftPoint;
                    corner4 += new Point2D(Constants.CellSizeX / 2, 0);
                }
                else
                {
                    Point2D actualStartPoint = startPoint;
                    Point2D actualEndPoint = new Point2D(startPoint.X, endPoint.Y);

                    if (startPoint.Y > endPoint.Y)
                    {
                        actualStartPoint = actualEndPoint;
                        actualEndPoint = startPoint;
                    }

                    cellEndPoint = new Point2D(actualStartPoint.X, actualEndPoint.Y);

                    corner1 = actualStartPoint + new Point2D(1, 0);
                    corner1 = getCellCenterPoint(corner1, CursorActionTarget.Map) - cameraTopLeftPoint;
                    corner1 += new Point2D(Constants.CellSizeX / 2, 0);
                    
                    corner2 = actualStartPoint + new Point2D(-1, 0);
                    corner2 = getCellCenterPoint(corner2, CursorActionTarget.Map) - cameraTopLeftPoint;
                    corner2 += new Point2D(0, Constants.CellSizeY / -2);
                    
                    corner3 = cellEndPoint + new Point2D(1, 0);
                    corner3 = getCellCenterPoint(corner3, CursorActionTarget.Map) - cameraTopLeftPoint;
                    corner3 += new Point2D(0, Constants.CellSizeY / 2);
                    
                    corner4 = cellEndPoint + new Point2D(-1, 0);
                    corner4 = getCellCenterPoint(corner4, CursorActionTarget.Map) - cameraTopLeftPoint;
                    corner4 += new Point2D(Constants.CellSizeX / -2, 0);
                }

                corner1 = corner1.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
                corner2 = corner2.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
                corner3 = corner3.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
                corner4 = corner4.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

                Color color = Color.Goldenrod;
                int thickness = 3;

                Renderer.DrawLine(corner1.ToXNAVector(), corner2.ToXNAVector(), color, thickness);
                Renderer.DrawLine(corner1.ToXNAVector(), corner3.ToXNAVector(), color, thickness);
                Renderer.DrawLine(corner2.ToXNAVector(), corner4.ToXNAVector(), color, thickness);
                Renderer.DrawLine(corner3.ToXNAVector(), corner4.ToXNAVector(), color, thickness);
            }
        }

        public override void OnKeyPressed(KeyPressEventArgs e, Point2D cellCoords)
        {
            if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Escape)
            {
                ExitAction();

                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Back)
            {
                startPoint = Point2D.NegativeOne;
                endPoint = Point2D.NegativeOne;

                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Enter && 
                startPoint != Point2D.NegativeOne && endPoint != Point2D.NegativeOne && endPoint != startPoint)
            {
                var bridgeDirection = GetBridgeDirection();

                Point2D bridgeEndPoint = bridgeDirection == BridgeDirection.EastWest ? new Point2D(endPoint.X, startPoint.Y) : new Point2D(startPoint.X, endPoint.Y);

                CursorActionTarget.MutationManager.PerformMutation(new PlaceBridgeMutation(CursorActionTarget.MutationTarget, startPoint, bridgeEndPoint, bridgeType));

                ExitAction();

                e.Handled = true;
            }
        }

        private BridgeDirection GetBridgeDirection()
        {
            int diffX = Math.Abs(startPoint.X - endPoint.X);
            int diffY = Math.Abs(startPoint.Y - endPoint.Y);

            return diffX > diffY ? BridgeDirection.EastWest : BridgeDirection.NorthSouth;
        }

        public override void LeftClick(Point2D cellCoords)
        {
            if (startPoint == Point2D.NegativeOne)
            {
                startPoint = cellCoords;
            }
            else
            {
                endPoint = cellCoords;
            }
        }

        public override void LeftDown(Point2D cellCoords) => LeftClick(cellCoords);
    }
}
