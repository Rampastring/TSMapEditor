﻿using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.Scripts;

namespace TSMapEditor.UI.CursorActions
{
    class GenerateForestCursorAction : CursorAction
    {
        public GenerateForestCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public Point2D? StartCellCoords { get; set; } = null;

        public override void LeftClick(Point2D cellCoords)
        {
            if (StartCellCoords == null)
            {
                StartCellCoords = cellCoords;
                return;
            }

            CursorActionTarget.CopiedTerrainData.Clear();

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

            var treeGroupTerrainTypes = CursorActionTarget.Map.Rules.TerrainTypes.FindAll(tt => tt.ININame.StartsWith("TC0"));

            new ForestGenerator().Generate(CursorActionTarget.Map, cells, treeGroupTerrainTypes, 0.15, 0.65, 0.1);
            CursorActionTarget.InvalidateMap();
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

            Point2D startPoint = CellMath.CellTopLeftPointFromCellCoords(new Point2D(startX, startY), CursorActionTarget.Map.Size.X) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX / 2, 0);
            Point2D endPoint = CellMath.CellTopLeftPointFromCellCoords(new Point2D(endX, endY), CursorActionTarget.Map.Size.X) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX / 2, Constants.CellSizeY);
            Point2D corner1 = CellMath.CellTopLeftPointFromCellCoords(new Point2D(startX, endY), CursorActionTarget.Map.Size.X) - cameraTopLeftPoint + new Point2D(0, Constants.CellSizeY / 2);
            Point2D corner2 = CellMath.CellTopLeftPointFromCellCoords(new Point2D(endX, startY), CursorActionTarget.Map.Size.X) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX, Constants.CellSizeY / 2);

            Color lineColor = Color.Goldenrod;
            int thickness = 2;
            Renderer.DrawLine(startPoint.ToXNAVector(), corner1.ToXNAVector(), lineColor, thickness);
            Renderer.DrawLine(startPoint.ToXNAVector(), corner2.ToXNAVector(), lineColor, thickness);
            Renderer.DrawLine(corner1.ToXNAVector(), endPoint.ToXNAVector(), lineColor, thickness);
            Renderer.DrawLine(corner2.ToXNAVector(), endPoint.ToXNAVector(), lineColor, thickness);
        }
    }
}
