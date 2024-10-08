using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.UI.CursorActions
{
    public enum MovementZone
    {
        Land = 1,
        Water = 2,
        LandAndWater = Land + Water,
        Air = LandAndWater + 4
    }

    public class CheckDistancePathfindingCursorAction : CursorAction
    {
        public CheckDistancePathfindingCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
            Map.MapResized += Map_MapResized;
        }

        private void Map_MapResized(object sender, EventArgs e)
        {
            pathfindingInitialized = false;
            targetCellCoords = Point2D.NegativeOne;
        }

        public override string GetName() => "Check Distance (Path)";

        private byte[][] landPathfindingCache;
        private byte[][] navalPathfindingCache;
        private byte[][] landAndWaterPathfindingCache;
        private byte[][] airPathfindingCache;

        private Point2D[][] pathfindingMostEfficient;
        private int[][] pathfindingScore;
        private int[][] pathfindingGoalScore;
        private bool[][] pathfindingClosedNodes;
        private bool[][] pathfindingOpenedNodes;
        private List<Point2D> openSet;

        private bool pathfindingInitialized = false;
        private bool isInfantry = false;
        private MovementZone movementZone = MovementZone.Land;

        public override bool DrawCellCursor => true;

        public override bool HandlesKeyboardInput => true;

        private Point2D? source;
        private List<Point2D> pathCellCoords = new List<Point2D>();

        private Point2D targetCellCoords;

        public override void OnActionEnter()
        {
            pathfindingInitialized = false;
            source = null;
        }

        public override void OnKeyPressed(KeyPressEventArgs e, Point2D cellCoords)
        {
            if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Escape)
            {
                e.Handled = true;
                ExitAction();
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.I)
            {
                isInfantry = !isInfantry;
                targetCellCoords = Point2D.NegativeOne;
                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.C)
            {
                switch (movementZone)
                {
                    case MovementZone.Land:
                        movementZone = MovementZone.Water;
                        break;
                    case MovementZone.Water:
                        movementZone = MovementZone.LandAndWater;
                        break;
                    default:
                        movementZone = MovementZone.Land;
                        break;
                }

                targetCellCoords = Point2D.NegativeOne;

                e.Handled = true;
            }

            base.OnKeyPressed(e, cellCoords);
        }

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            Color sourceColor = Color.LimeGreen;
            Color destinationColor = Color.Red;
            Color pathColor = Color.Yellow;

            if (source == null)
            {
                DrawText(cellCoords, cameraTopLeftPoint, "Click to select source coordinate, or right-click to exit", sourceColor);
                return;
            }

            string instruction = Environment.NewLine + Environment.NewLine +
                "Current mode: " + (isInfantry ? "Infantry" : "Vehicle") + " (switch by pressing I)" + Environment.NewLine + Environment.NewLine +
                "Current movement zone: " + movementZone + " (cycle between Land, Water and both by pressing C)";

            Func<Point2D, Map, Point2D> getCellCenterPoint = Is2DMode ? CellMath.CellCenterPointFromCellCoords : CellMath.CellCenterPointFromCellCoords_3D;

            Point2D sourceCenterPoint = getCellCenterPoint(source.Value, CursorActionTarget.Map) - cameraTopLeftPoint;
            sourceCenterPoint = sourceCenterPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
            Renderer.FillRectangle(GetDrawRectangleForMarker(sourceCenterPoint), sourceColor);

            Point2D destinationCenterPoint = getCellCenterPoint(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint;
            destinationCenterPoint = destinationCenterPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
            Renderer.FillRectangle(GetDrawRectangleForMarker(destinationCenterPoint), Color.Red);

            if (targetCellCoords != cellCoords)
            {
                pathCellCoords = PrecisePathfindingAStar(source.Value, cellCoords, movementZone, isInfantry ? AllowInfantry : AllowVehicle);
                targetCellCoords = cellCoords;
            }

            foreach (Point2D pathCell in pathCellCoords)
            {
                Point2D pathCellCenterPoint = getCellCenterPoint(pathCell, CursorActionTarget.Map) - cameraTopLeftPoint;
                pathCellCenterPoint = pathCellCenterPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

                Renderer.FillRectangle(GetDrawRectangleForMarker(pathCellCenterPoint), Color.Yellow);
            }

            string text;

            if (pathCellCoords.Count == 0)
                text = "No path found!\r\n\r\nClick to select new source coordinate, or right-click to exit" + instruction;
            else
                text = "Path Length In Cells: " + pathCellCoords.Count + "\r\n\r\nClick to select new source coordinate, or right-click to exit" + instruction;

            DrawText(cellCoords, cameraTopLeftPoint, text, pathColor);
        }

        private void DrawText(Point2D cellCoords, Point2D cameraTopLeftPoint, string text, Color textColor)
        {
            DrawText(cellCoords, cameraTopLeftPoint, 60, -250, text, textColor);
        }

        private Rectangle GetDrawRectangleForMarker(Point2D cellCenterPoint)
        {
            int size = (int)(10 * CursorActionTarget.Camera.ZoomLevel);
            return new Rectangle(cellCenterPoint.X - (size / 2), cellCenterPoint.Y - (size / 2), size, size);
        }

        public override void LeftClick(Point2D cellCoords)
        {
            source = cellCoords;
        }

        public void BuildPathfindingCache()
        {
            const int MaxMapCoord = 512;

            landPathfindingCache = new byte[MaxMapCoord][];
            navalPathfindingCache = new byte[MaxMapCoord][];
            landAndWaterPathfindingCache = new byte[MaxMapCoord][];
            airPathfindingCache = new byte[MaxMapCoord][];
            pathfindingMostEfficient = new Point2D[MaxMapCoord][];
            pathfindingScore = new int[MaxMapCoord][];
            pathfindingGoalScore = new int[MaxMapCoord][];
            pathfindingClosedNodes = new bool[MaxMapCoord][];
            pathfindingOpenedNodes = new bool[MaxMapCoord][];
            openSet = new List<Point2D>(1000);

            for (int y = 0; y < MaxMapCoord; y++)
            {
                landPathfindingCache[y] = new byte[MaxMapCoord];
                navalPathfindingCache[y] = new byte[MaxMapCoord];
                landAndWaterPathfindingCache[y] = new byte[MaxMapCoord];
                airPathfindingCache[y] = new byte[MaxMapCoord];
                pathfindingMostEfficient[y] = new Point2D[MaxMapCoord];
                pathfindingScore[y] = new int[MaxMapCoord];
                pathfindingGoalScore[y] = new int[MaxMapCoord];
                pathfindingClosedNodes[y] = new bool[MaxMapCoord];
                pathfindingOpenedNodes[y] = new bool[MaxMapCoord];

                for (int x = 0; x < MaxMapCoord; x++)
                {
                    UpdatePathfindingCacheForTile(new Point2D(x, y));
                }
            }

            pathfindingInitialized = true;
        }

        /// <summary>
        /// Updates the pathfinding cache for a tile on the specific location.
        /// </summary>
        /// <param name="location">The location.</param>
        public void UpdatePathfindingCacheForTile(Point2D location)
        {
            int y = location.Y;
            int x = location.X;

            var tile = Map.GetTile(location);
            if (tile == null)
                return;

            landPathfindingCache[y][x] = GetMovementCost(tile, MovementZone.Land);
            navalPathfindingCache[y][x] = GetMovementCost(tile, MovementZone.Water);
            landAndWaterPathfindingCache[y][x] = GetMovementCost(tile, MovementZone.LandAndWater);
            airPathfindingCache[y][x] = GetMovementCost(tile, MovementZone.Air);
        }

        private byte GetMovementCost(MapTile mapTile, MovementZone movementZone)
        {
            var subTile = Map.TheaterInstance.GetTile(mapTile.TileIndex).GetSubTile(mapTile.SubTileIndex);

            int terrainType = subTile.TmpImage.TerrainType;
            if (mapTile.Overlay != null)
                terrainType = Helpers.LandTypeToInt(mapTile.Overlay.OverlayType.Land);

            switch (movementZone)
            {
                case MovementZone.Air:
                    return 1;
                case MovementZone.Land:
                    return Helpers.IsLandTypeImpassable(terrainType, true) ? byte.MaxValue : (byte)1;
                case MovementZone.Water:
                    return Helpers.IsLandTypeImpassableForNavalUnits(terrainType) ? byte.MaxValue : (byte)1;
                case MovementZone.LandAndWater:
                    return Helpers.IsLandTypeImpassable(terrainType, false) ? byte.MaxValue : (byte)1;
                default:
                    return byte.MaxValue;
            }
        }

        // Defines the priority of surrounding tiles as the pathfinder checks for them
        private static readonly Point2D[] pfLocationOffsets = new Point2D[]
        {
            new Point2D(0, -1),
            new Point2D(0, 1),
            new Point2D(-1, 0),
            new Point2D(1, 0),
            new Point2D(-1, -1),
            new Point2D(1, 1),
            new Point2D(-1, 1),
            new Point2D(1, -1)
        };

        private bool AllowVehicle(Point2D source, Point2D target)
        {
            var targetCell = Map.GetTile(target);

            if ((targetCell.Structures.Count > 0 && targetCell.Structures.Exists(s => !s.ObjectType.FirestormWall)) ||
                targetCell.TerrainObject != null ||
                (targetCell.Overlay != null && Helpers.IsLandTypeImpassable(targetCell.Overlay.OverlayType.Land, true)))
                return false;

            return true;
        }

        private bool AllowInfantry(Point2D source, Point2D target)
        {
            var targetCell = Map.GetTile(target);

            if ((targetCell.Structures.Count > 0 && targetCell.Structures.Exists(s => !s.ObjectType.FirestormWall)) ||
                (targetCell.TerrainObject != null && targetCell.TerrainObject.TerrainType.TemperateOccupationBits > (TerrainOccupation)6) ||
                (targetCell.Overlay != null && Helpers.IsLandTypeImpassable(targetCell.Overlay.OverlayType.Land, true)))
                return false;

            return true;
        }

        /// <summary>
        /// The precise variant of the main pathfinding function. 
        /// Finds the most optimal path between two locations,
        /// depending on the movement zone.
        /// </summary>
        /// <param name="start">The start location.</param>
        /// <param name="end">The end location.</param>
        /// <param name="movementZone">The movement zone.</param>
        /// <param name="advancedAllowanceCheckerFunction">A function that can be used by
        /// the pathfinder to check if it's allowed to path through a specific location
        /// when approaching it from another specific location.</param>
        public List<Point2D> PrecisePathfindingAStar(Point2D start, Point2D end, MovementZone movementZone,
             Func<Point2D, Point2D, bool> advancedAllowanceCheckerFunction)
        {
            byte[][] accessibleCache = InitPathfinding(start, end, movementZone);
            if (accessibleCache == null)
                return new List<Point2D>(0);

            while (openSet.Count > 0)
            {
                Point2D current = Point2D.NegativeOne;
                int lowestScore = int.MaxValue;

                foreach (var location in openSet)
                {
                    int score = pathfindingScore[location.Y][location.X];
                    if (score < lowestScore)
                    {
                        current = location;
                        lowestScore = score;
                    }
                }

                if (current == end)
                {
                    return AStar_ReconstructPath(current);
                }

                pathfindingOpenedNodes[current.Y][current.X] = false;
                openSet.Remove(current);
                pathfindingClosedNodes[current.Y][current.X] = true;

                // Check surrounding 8 tiles from the current tile
                // in a way that we check the vertical and horizontal directions first (NSEW)
                for (int nearbyLocationIndex = 0; nearbyLocationIndex < pfLocationOffsets.Length; nearbyLocationIndex++)
                {
                    Point2D offset = pfLocationOffsets[nearbyLocationIndex];
                    Point2D neighbor = current + offset;

                    if (!Map.IsCoordWithinMap(neighbor))
                        continue;

                    if (pathfindingClosedNodes[neighbor.Y][neighbor.X])
                        continue;

                    if (accessibleCache[neighbor.Y][neighbor.X] == byte.MaxValue)
                    {
                        // The tile isn't accessible in this movement zone
                        pathfindingClosedNodes[neighbor.Y][neighbor.X] = true;
                        continue;
                    }

                    // Perform direction-relative checking if allowed.
                    // If not allowed, don't add the neighbor to either the closed set or the open set to
                    // leave the neighbor tile usable for other nodes.
                    if (advancedAllowanceCheckerFunction != null && !advancedAllowanceCheckerFunction(current, neighbor))
                        continue;

                    if (!pathfindingOpenedNodes[neighbor.Y][neighbor.X])
                    {
                        pathfindingOpenedNodes[neighbor.Y][neighbor.X] = true;
                        openSet.Add(neighbor);
                    }

                    int tentativeScore = pathfindingScore[current.Y][current.X] + accessibleCache[neighbor.Y][neighbor.X];
                    if (tentativeScore >= pathfindingScore[neighbor.Y][neighbor.X])
                        continue; // This is not a better path

                    pathfindingMostEfficient[neighbor.Y][neighbor.X] = current;
                    pathfindingScore[neighbor.Y][neighbor.X] = tentativeScore;
                    pathfindingGoalScore[neighbor.Y][neighbor.X] = tentativeScore + neighbor.DistanceTo(end);
                }
            }

            // Failure
            return new List<Point2D>(0);
        }

        private byte[][] InitPathfinding(Point2D start, Point2D end, MovementZone movementZone)
        {
            if (!pathfindingInitialized)
            {
                BuildPathfindingCache();
            }

            openSet.Clear();
            openSet.Add(start);

            for (int y = 0; y < pathfindingScore.Length; y++)
            {
                for (int x = 0; x < pathfindingScore[y].Length; x++)
                {
                    pathfindingScore[y][x] = short.MaxValue;
                    pathfindingGoalScore[y][x] = short.MaxValue;
                }

                Array.Clear(pathfindingOpenedNodes[y], 0, pathfindingScore[y].Length);
                Array.Clear(pathfindingClosedNodes[y], 0, pathfindingScore[y].Length);
            }

            pathfindingMostEfficient[start.Y][start.X] = Point2D.NegativeOne;

            pathfindingScore[start.Y][start.X] = 0;
            pathfindingGoalScore[start.Y][start.X] = start.DistanceTo(end);
            pathfindingOpenedNodes[start.Y][start.X] = true;

            switch (movementZone)
            {
                case MovementZone.Land:
                    return landPathfindingCache;
                case MovementZone.Water:
                    return navalPathfindingCache;
                case MovementZone.LandAndWater:
                    return landAndWaterPathfindingCache;
                case MovementZone.Air:
                    return airPathfindingCache;
                default:
                    return null;
            }
        }

        private List<Point2D> AStar_ReconstructPath(Point2D location)
        {
            Point2D current = location;
            List<Point2D> returnValue = new List<Point2D>();
            returnValue.Add(current);
            while (true)
            {
                current = pathfindingMostEfficient[current.Y][current.X];

                if (current == Point2D.NegativeOne)
                    break;

                returnValue.Add(current);
            }

            returnValue.RemoveAt(returnValue.Count - 1);
            returnValue.Reverse();
            return returnValue;
        }
    }
}
