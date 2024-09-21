using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allows copying terrain tiles.
    /// </summary>
    public class CopyCustomShapedTerrainCursorAction : CopyTerrainCursorActionBase
    {
        public CopyCustomShapedTerrainCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Copy Terrain (Custom Shape)";


        public override bool HandlesKeyboardInput => true;

        private HashSet<Point2D> cellsToCopy { get; set; } = new HashSet<Point2D>();
        private List<Point2D> cellsToCopyList { get; set; } = new List<Point2D>();

        private Point2D[][] edges { get; set; } = new Point2D[][] { Array.Empty<Point2D>() };

        private bool modified;
        private Point2D startPoint;

        public override void OnActionEnter() => modified = true;

        public override void LeftDown(Point2D cellCoords)
        {
            CursorActionTarget.BrushSize.DoForBrushSize(offset =>
            {
                Point2D coords = cellCoords + offset;
                if (!cellsToCopy.Contains(coords) && Map.IsCoordWithinMap(coords)) 
                {
                    cellsToCopy.Add(coords);
                    cellsToCopyList.Add(coords);
                    modified = true;
                }

                if (Keyboard.IsShiftHeldDown() && cellsToCopy.Contains(coords))
                {
                    cellsToCopy.Remove(coords);
                    cellsToCopyList.Remove(coords);
                    modified = true;
                }
            });
        }

        public override void LeftClick(Point2D cellCoords) => LeftDown(cellCoords);

        public override void OnKeyPressed(KeyPressEventArgs e, Point2D cellCoords)
        {
            base.OnKeyPressed(e, cellCoords);

            if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Enter)
            {
                CopyFromCells(cellsToCopyList);
                cellsToCopy.Clear();
                cellsToCopyList.Clear();

                ExitAction();

                e.Handled = true;
            }
        }

        private void RegenerateGraphics()
        {
            int startX = int.MaxValue;
            int startY = int.MaxValue;
            int endX = int.MinValue;
            int endY = int.MinValue;

            // Figure out start cell coords so we can calculate offsets.
            // Also figure out end cell coords for rectangular preview.
            for (int i = 0; i < cellsToCopy.Count; i++)
            {
                var cellCoords = cellsToCopyList[i];

                if (cellCoords.X < startX)
                    startX = cellCoords.X;

                if (cellCoords.Y < startY)
                    startY = cellCoords.Y;

                if (cellCoords.X > endX)
                    endX = cellCoords.X;

                if (cellCoords.Y > endY)
                    endY = cellCoords.Y;
            }

            startPoint = new Point2D(startX, startY);
            var foundationList = cellsToCopyList.Select(cc => new Point2D(cc.X - startX, cc.Y - startY)).ToList();
            edges = Helpers.CreateEdges((endX - startX) + 2, (endY - startY) + 2, foundationList);

            modified = false;
        }

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            if (cellsToCopyList.Count > 0)
            {
                if (modified)
                    RegenerateGraphics();

                foreach (var edge in edges)
                {
                    Point2D edgeCell0 = startPoint + edge[0];
                    Point2D edgeCell1 = startPoint + edge[1];
                    int heightOffset0 = 0;
                    int heightOffset1 = 0;

                    if (!CursorActionTarget.Is2DMode)
                    {
                        var cell = Map.GetTile(edgeCell0);
                        if (cell != null)
                            heightOffset0 = Constants.CellHeight * cell.Level;

                        cell = Map.GetTile(edgeCell1);
                        if (cell != null)
                            heightOffset1 = Constants.CellHeight * cell.Level;
                    }

                    // Translate edge vertices from cell coordinate space to world coordinate space.
                    var start = CellMath.CellTopLeftPointFromCellCoords(edgeCell0, Map) - cameraTopLeftPoint;
                    var end = CellMath.CellTopLeftPointFromCellCoords(edgeCell1, Map) - cameraTopLeftPoint;
                    // Height is an illusion, just move everything up or down.
                    // Also offset X to match the top corner of an iso tile.
                    start += new Point2D(Constants.CellSizeX / 2, -heightOffset0);
                    end += new Point2D(Constants.CellSizeX / 2, -heightOffset1);

                    start = start.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
                    end = end.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

                    // Draw edge.
                    Renderer.DrawLine(start.ToXNAVector(), end.ToXNAVector(), Color.Red, 2);
                }
            }

            string text = "Press left click on cells to mark them to be copied." + Environment.NewLine + Environment.NewLine +
                    "Hold SHIFT while pressing to remove cells." + Environment.NewLine + Environment.NewLine +
                    "Press ENTER when ready to copy the cells to the clipboard.";

            DrawText(cellCoords, cameraTopLeftPoint, 90, -200, text, Color.Yellow);
        }
    }
}
