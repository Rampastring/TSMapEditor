using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allows users to calculate the value of Tiberium on an area.
    /// </summary>
    public class CalculateTiberiumValueCursorAction : CursorAction
    {
        public CalculateTiberiumValueCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Calculate Resource Value";

        public override bool DrawCellCursor => true;

        public Point2D? StartCellCoords { get; set; } = null;

        public override void LeftClick(Point2D cellCoords)
        {
            StartCellCoords = cellCoords;
        }

        public override void OnActionEnter()
        {
            StartCellCoords = null;
        }

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            if (StartCellCoords == null)
            {
                DrawText(cellCoords, cameraTopLeftPoint, 60, -150, "Click to select starting cell to calculate from.", Color.Yellow);
                return;
            }

            Point2D startCellCoords = StartCellCoords.Value;
            int startY = Math.Min(cellCoords.Y, startCellCoords.Y);
            int endY = Math.Max(cellCoords.Y, startCellCoords.Y);
            int startX = Math.Min(cellCoords.X, startCellCoords.X);
            int endX = Math.Max(cellCoords.X, startCellCoords.X);

            Func<Point2D, Map, Point2D> getCellTopLeftPoint = Is2DMode ? CellMath.CellTopLeftPointFromCellCoords : CellMath.CellTopLeftPointFromCellCoords_3D;

            Point2D startPoint = getCellTopLeftPoint(new Point2D(startX, startY), CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX / 2, 0);
            Point2D endPoint = getCellTopLeftPoint(new Point2D(endX, endY), CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX / 2, Constants.CellSizeY);
            Point2D corner1 = getCellTopLeftPoint(new Point2D(startX, endY), CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(0, Constants.CellSizeY / 2);
            Point2D corner2 = getCellTopLeftPoint(new Point2D(endX, startY), CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX, Constants.CellSizeY / 2);

            startPoint = startPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
            endPoint = endPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
            corner1 = corner1.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
            corner2 = corner2.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            Color lineColor = Color.Red;
            int thickness = 2;
            Renderer.DrawLine(startPoint.ToXNAVector(), corner1.ToXNAVector(), lineColor, thickness);
            Renderer.DrawLine(startPoint.ToXNAVector(), corner2.ToXNAVector(), lineColor, thickness);
            Renderer.DrawLine(corner1.ToXNAVector(), endPoint.ToXNAVector(), lineColor, thickness);
            Renderer.DrawLine(corner2.ToXNAVector(), endPoint.ToXNAVector(), lineColor, thickness);

            string text = "Click to select starting cell to calculate from." + Environment.NewLine + Environment.NewLine +
                "Value in current area: " + GetTiberiumValue(startY, endY, startX, endX) + " credits";
            DrawText(cellCoords, cameraTopLeftPoint, 60, -150, text, Color.Yellow);
        }

        private int GetTiberiumValue(int startY, int endY, int startX, int endX)
        {
            int tiberiumValue = 0;

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    if (!Map.IsCoordWithinMap(x, y))
                        continue;

                    var cell = Map.GetTile(x, y);

                    if (cell.Overlay == null || cell.Overlay.OverlayType == null || !cell.Overlay.OverlayType.Tiberium)
                        continue;

                    TiberiumType tiberiumType = cell.Overlay.OverlayType.TiberiumType;
                    if (tiberiumType != null)
                    {
                        tiberiumValue += cell.Overlay.FrameIndex * tiberiumType.Value;
                    }
                }
            }

            return tiberiumValue;
        }
    }
}
