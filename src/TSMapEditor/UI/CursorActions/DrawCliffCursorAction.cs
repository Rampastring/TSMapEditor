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
    /// <summary>
    /// Cursor action for placing bridges.
    /// </summary>
    public class DrawCliffCursorAction : CursorAction
    {
        public DrawCliffCursorAction(ICursorActionTarget cursorActionTarget, CliffType cliffType) : base(cursorActionTarget)
        {
            this.cliffType = cliffType;
            ActionExited += UndoOnExit;
        }

        public override string GetName() => "Draw Connected Tiles";

        public override bool HandlesKeyboardInput => true;

        public override bool DrawCellCursor => true;

        private readonly CliffType cliffType;

        private List<Point2D> cliffPath;
        private CliffSide cliffSide = CliffSide.Front;
        private DrawCliffMutation previewMutation;
        private byte extraHeight = 0;

        private readonly int randomSeed = new Random().Next();

        public override void OnActionEnter()
        {
            cliffPath = new List<Point2D>();

            base.OnActionEnter();
        }

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            const string mainText = "Click on a cell to place a new vertex.\r\n\r\n" +
                "ENTER to confirm\r\n" +
                "Backspace to go back one step\r\n";

            const string tabText = "TAB to toggle between front and back sides\r\n";
            const string pageUpDownText = "PageUp to raise the tiles, PageDown to lower them\r\n";
            const string exitText = "Right-click or ESC to exit";

            string text = (Constants.IsFlatWorld, cliffType.FrontOnly) switch
            {
                (true, true) => mainText + exitText,
                (true, false) => mainText + tabText + exitText,
                (false, true) => mainText + pageUpDownText + exitText,
                (false, false) => mainText + tabText + pageUpDownText + exitText
            };

            DrawText(cellCoords, cameraTopLeftPoint, 60, -150, text, Color.Yellow);

            Func<Point2D, Map, Point2D> getCellCenterPoint = Is2DMode ? CellMath.CellCenterPointFromCellCoords : CellMath.CellCenterPointFromCellCoords_3D;

            if (cliffPath.Count > 0)
            {
                Point2D start = cliffPath[0];
                start = getCellCenterPoint(start, CursorActionTarget.Map) - cameraTopLeftPoint;
                start = start.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

                Color color = Color.Red;
                int precision = 8;
                int thickness = 3;
                Renderer.DrawCircle(start.ToXNAVector(), Constants.CellSizeY * 0.25f, color, precision, thickness);
            }

            // Draw cliff path
            for (int i = 0; i < cliffPath.Count - 1; i++)
            {
                Point2D start = cliffPath[i];
                start = getCellCenterPoint(start, CursorActionTarget.Map) - cameraTopLeftPoint;
                start = start.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

                Point2D end = cliffPath[i + 1];
                end = getCellCenterPoint(end, CursorActionTarget.Map) - cameraTopLeftPoint;
                end = end.ScaleBy(CursorActionTarget.Camera.ZoomLevel);


                Color color = Color.Goldenrod;
                int thickness = 3;

                Renderer.DrawLine(start.ToXNAVector(), end.ToXNAVector(), color, thickness);
            }
        }

        public override void OnKeyPressed(KeyPressEventArgs e)
        {
            if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Escape)
            {
                ExitAction();

                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Tab)
            {
                if (!cliffType.FrontOnly)
                {
                    cliffSide = cliffSide == CliffSide.Front ? CliffSide.Back : CliffSide.Front;
                    RedrawPreview();
                }

                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Back)
            {
                if (cliffPath.Count > 0)
                    cliffPath.RemoveAt(cliffPath.Count - 1);
                
                RedrawPreview();

                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.PageUp)
            {
                if (!Constants.IsFlatWorld)
                {
                    if (cliffPath.Count > 0)
                    {
                        if (MutationTarget.Map.GetTile(cliffPath[0]).Level + extraHeight + 1 <= Constants.MaxMapHeightLevel)
                            extraHeight++;
                    }

                    RedrawPreview();
                }

                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.PageDown)
            {
                if (!Constants.IsFlatWorld)
                {
                    if (cliffPath.Count > 0)
                    {
                        if (MutationTarget.Map.GetTile(cliffPath[0]).Level + extraHeight - 1 >= 0)
                            extraHeight--;
                    }

                    RedrawPreview();
                }

                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Enter && cliffPath.Count >= 2)
            {
                previewMutation?.Undo();
                CursorActionTarget.MutationManager.PerformMutation(new DrawCliffMutation(CursorActionTarget.MutationTarget, cliffPath, cliffType, cliffSide, randomSeed, extraHeight));

                ExitAction();

                e.Handled = true;
            }
        }

        public override void LeftClick(Point2D cellCoords)
        {
            cliffPath.Add(cellCoords);
            RedrawPreview();
        }

        private void RedrawPreview()
        {
            previewMutation?.Undo();

            if (cliffPath.Count >= 2)
            {
                previewMutation = new DrawCliffMutation(MutationTarget, cliffPath, cliffType, cliffSide, randomSeed, extraHeight);
                previewMutation.Perform();
            }
            else
            {
                previewMutation = null;
            }
        }

        private void UndoOnExit(object sender, EventArgs e)
        {
            previewMutation?.Undo();
        }
    }
}
