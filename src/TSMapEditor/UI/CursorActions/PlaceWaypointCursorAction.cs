using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI.Windows;

namespace TSMapEditor.UI.CursorActions
{
    public class PlaceWaypointCursorAction : CursorAction
    {
        public PlaceWaypointCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Place Waypoint";

        public PlaceWaypointWindow PlaceWaypointWindow { get; set; }

        public override void LeftClick(Point2D cellCoords)
        {
            PlaceWaypointWindow.Open(cellCoords);
        }

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            Func<Point2D, Map, Point2D> getCellTopLeftPoint = Is2DMode ? CellMath.CellTopLeftPointFromCellCoords : CellMath.CellTopLeftPointFromCellCoords_3D;
            Point2D cellTopLeftPoint = getCellTopLeftPoint(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint;
            cellTopLeftPoint = cellTopLeftPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            Renderer.DrawTexture(CursorActionTarget.EditorGraphics.GenericTileWithBorderTexture,
                new Rectangle(cellTopLeftPoint.X, cellTopLeftPoint.Y,
                CursorActionTarget.Camera.ScaleIntWithZoom(Constants.CellSizeX),
                CursorActionTarget.Camera.ScaleIntWithZoom(Constants.CellSizeY)),
                Color.LimeGreen * 0.5f);
        }
    }
}
