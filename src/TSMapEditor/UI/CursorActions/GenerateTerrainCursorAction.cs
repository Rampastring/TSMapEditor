using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allows the user to select an area for terrain generation.
    /// </summary>
    class GenerateTerrainCursorAction : CursorAction
    {
        public GenerateTerrainCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Generate Terrain";

        public TerrainGeneratorConfiguration TerrainGeneratorConfiguration { get; set; }

        public Point2D? StartCellCoords { get; set; } = null;

        public override void LeftClick(Point2D cellCoords)
        {
            if (StartCellCoords == null)
            {
                StartCellCoords = cellCoords;
                return;
            }

            Point2D startCellCoords = StartCellCoords.Value;
            int startY = Math.Min(cellCoords.Y, startCellCoords.Y);
            int endY = Math.Max(cellCoords.Y, startCellCoords.Y);
            int startX = Math.Min(cellCoords.X, startCellCoords.X);
            int endX = Math.Max(cellCoords.X, startCellCoords.X);

            var cells = new List<Point2D>();

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    if (CursorActionTarget.Map.GetTile(x, y) != null)
                        cells.Add(new Point2D(x, y));
                }
            }

            var mutation = new TerrainGenerationMutation(CursorActionTarget.MutationTarget, cells, TerrainGeneratorConfiguration);
            CursorActionTarget.MutationManager.PerformMutation(mutation);

            ExitAction();
        }

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            if (StartCellCoords == null)
            {
                return;
            }

            Point2D startCellCoords = StartCellCoords.Value;
            int startY = Math.Min(cellCoords.Y, startCellCoords.Y);
            int endY = Math.Max(cellCoords.Y, startCellCoords.Y);
            int startX = Math.Min(cellCoords.X, startCellCoords.X);
            int endX = Math.Max(cellCoords.X, startCellCoords.X);

            Func<Point2D, Map, Point2D> getCellCenterPoint = Is2DMode ? CellMath.CellTopLeftPointFromCellCoords : CellMath.CellTopLeftPointFromCellCoords_3D;

            Point2D startPoint = getCellCenterPoint(new Point2D(startX, startY), CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX / 2, 0);
            Point2D endPoint = getCellCenterPoint(new Point2D(endX, endY), CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX / 2, Constants.CellSizeY);
            Point2D corner1 = getCellCenterPoint(new Point2D(startX, endY), CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(0, Constants.CellSizeY / 2);
            Point2D corner2 = getCellCenterPoint(new Point2D(endX, startY), CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX, Constants.CellSizeY / 2);

            startPoint = startPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
            endPoint = endPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
            corner1 = corner1.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
            corner2 = corner2.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            Color lineColor = Color.Goldenrod;
            int thickness = 2;
            Renderer.DrawLine(startPoint.ToXNAVector(), corner1.ToXNAVector(), lineColor, thickness);
            Renderer.DrawLine(startPoint.ToXNAVector(), corner2.ToXNAVector(), lineColor, thickness);
            Renderer.DrawLine(corner1.ToXNAVector(), endPoint.ToXNAVector(), lineColor, thickness);
            Renderer.DrawLine(corner2.ToXNAVector(), endPoint.ToXNAVector(), lineColor, thickness);
        }
    }
}
