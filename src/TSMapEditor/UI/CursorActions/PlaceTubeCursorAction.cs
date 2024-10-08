using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Rendering;

namespace TSMapEditor.UI.CursorActions
{
    public class PlaceTubeCursorAction : CursorAction
    {
        private const double DoubleClickTime = 0.2f;

        public PlaceTubeCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Place Tube";

        public override bool HandlesKeyboardInput => true;

        public override bool DrawCellCursor => true;

        private Tube tube;

        private List<Point2D> points = new List<Point2D>();
        private List<Point2D> tubeCells = new List<Point2D>();

        private bool pointAddedForPreview;

        private Point2D lastClickedCell;
        private DateTime lastClickedCellDateTime;

        public override void PreMapDraw(Point2D cellCoords)
        {
            if (!points.Contains(cellCoords) && !tubeCells.Contains(cellCoords))
            {
                points.Add(cellCoords);
                pointAddedForPreview = true;
                RefreshTube();
            }
            else
            {
                pointAddedForPreview = false;
            }

            if (tube != null)
                CursorActionTarget.Map.Tubes.Add(tube);

            CursorActionTarget.InvalidateMap();
        }

        public override void PostMapDraw(Point2D cellCoords)
        {
            if (tube != null)
                CursorActionTarget.Map.Tubes.RemoveAt(CursorActionTarget.Map.Tubes.Count - 1);

            if (pointAddedForPreview)
            {
                points.RemoveAt(points.Count - 1);
                RefreshTube();
            }

            CursorActionTarget.InvalidateMap();
        }

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            Point2D cellTopLeftPoint = CellMath.CellTopLeftPointFromCellCoords_3D(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint;

            cellTopLeftPoint = cellTopLeftPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            const string text = "Click on cells to draw a tunnel. Once ready, use one of the options below:\r\n\r\n" +
                "Double-click to confirm\r\n" +
                "Double-click while holding Shift to create bidirectional tunnel\r\n" +
                "Press ESC to clear\r\n" +
                "Press B to step back\r\n" +
                "Right-click to exit";
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
        }

        public override void OnKeyPressed(KeyPressEventArgs e, Point2D cellCoords)
        {
            if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Escape)
            {
                if (tube != null)
                {
                    TubeRefreshHelper.MapViewRefreshTube(tube, CursorActionTarget.MutationTarget);
                    points.Clear();
                    RefreshTube();
                }

                if (CursorActionTarget.WindowManager.Keyboard.IsShiftHeldDown())
                    ExitAction();

                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.C && tube != null && tube.Directions.Count > 0)
            {
                ConfirmTube();

                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.B && tube != null && tube.Directions.Count > 0)
            {
                TubeRefreshHelper.MapViewRefreshTube(tube, CursorActionTarget.MutationTarget);
                tube.Directions.RemoveAt(tube.Directions.Count - 1);

                if (points.Count > 0)
                    points.RemoveAt(points.Count - 1);

                RefreshTube();

                e.Handled = true;
            }
        }

        private void ConfirmTube()
        {
            if (tube != null && tube.Directions.Count > 0)
            {
                tube.Pending = false;
                tube.UnitInitialFacing = tube.Directions[0];
                CursorActionTarget.MutationManager.PerformMutation(new PlaceTubeMutation(CursorActionTarget.MutationTarget, tube));

                tube.Directions.Add(TubeDirection.None);

                if (CursorActionTarget.WindowManager.Keyboard.IsShiftHeldDown())
                {
                    Tube reversedTube = tube.GetReversedTube();
                    CursorActionTarget.MutationManager.PerformMutation(new PlaceTubeMutation(CursorActionTarget.MutationTarget, reversedTube));
                }

                points.Clear();
                RefreshTube();
            }
        }

        private void RefreshTube()
        {
            tubeCells.Clear();

            if (points.Count == 0)
            {
                tube = null;
                return;
            }

            if (tube == null)
                tube = new Tube();

            tube.Pending = true;

            tubeCells.Add(points[0]);

            tube.EntryPoint = points[0];
            tube.ExitPoint = points[^1];

            tube.Directions.Clear();

            for (int i = 0; i < points.Count - 1; i++)
            {
                Point2D current = points[i];
                Point2D next = points[i + 1];

                Point2D previousStep = current;

                while (previousStep != next)
                {
                    int x = previousStep.X;
                    int y = previousStep.Y;

                    if (x > next.X)
                        x--;
                    else if (x < next.X)
                        x++;

                    if (y > next.Y)
                        y--;
                    else if (y < next.Y)
                        y++;

                    Point2D newPoint = new Point2D(x, y);

                    // Fetch tube direction for this cell
                    // Check each tube direction to see if we find a fitting one

                    TubeDirection? nextTubeDirection = null;
                    for (int dir = (int)TubeDirection.First; dir <= (int)TubeDirection.Max; dir++)
                    {
                        TubeDirection tubeDirection = (TubeDirection)dir;
                        Point2D nextPointCandidate = previousStep.NextPointFromTubeDirection(tubeDirection);

                        if (nextPointCandidate == newPoint)
                        {
                            nextTubeDirection = tubeDirection;
                            break;
                        }
                    }

                    if (!nextTubeDirection.HasValue)
                        throw new ApplicationException("Unable to find tunnel tube direction! From: " + previousStep + " To: " + newPoint);

                    tube.Directions.Add(nextTubeDirection.Value);
                    tubeCells.Add(newPoint);

                    previousStep = newPoint;
                }
            }

            tube.UnitInitialFacing = tube.Directions.Count > 0 ? tube.Directions[0] : TubeDirection.None;
        }

        public override void LeftClick(Point2D cellCoords)
        {
            if (points.Count == 0)
            {
                // Make sure that a tube doesn't already exist on this cell
                if (CursorActionTarget.Map.Tubes.Exists(tb => tb.EntryPoint == cellCoords))
                    return;
            }

            if (!points.Contains(cellCoords) && !tubeCells.Contains(cellCoords))
            {
                points.Add(cellCoords);
                RefreshTube();
                TubeRefreshHelper.MapViewRefreshTube(tube, CursorActionTarget.MutationTarget);
                lastClickedCell = cellCoords;
                lastClickedCellDateTime = DateTime.Now;
            }
            else
            {
                if (points.Count > 1 && lastClickedCell == cellCoords && DateTime.Now - lastClickedCellDateTime < TimeSpan.FromSeconds(DoubleClickTime))
                    ConfirmTube();
            }
        }
    }
}
