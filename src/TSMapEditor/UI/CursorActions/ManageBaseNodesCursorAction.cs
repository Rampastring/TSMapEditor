using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Misc;
using TSMapEditor.Models;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allows adding and removing base nodes.
    /// </summary>
    public class ManageBaseNodesCursorAction : CursorAction
    {
        public ManageBaseNodesCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Manage Base Nodes";

        public override bool DrawCellCursor => true;

        public override bool HandlesKeyboardInput => true;

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            string text = "Placement actions:" + Environment.NewLine +
                "Click on building to place a base node." + Environment.NewLine +
                "Hold SHIFT while clicking to also delete the building." + Environment.NewLine +
                "Hold CTRL while clicking to erase a base node." + Environment.NewLine + Environment.NewLine +
                "Ordering actions:" + Environment.NewLine +
                "Press E while hovering over a base node to shift it to be built earlier." + Environment.NewLine +
                "Press D while hovering over a base node to shift it to be built later.";

            DrawText(cellCoords, cameraTopLeftPoint, text, UISettings.ActiveSettings.AltColor);
        }

        private void DrawText(Point2D cellCoords, Point2D cameraTopLeftPoint, string text, Color textColor)
        {
            Point2D cellTopLeftPoint = CellMath.CellTopLeftPointFromCellCoords(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint;
            cellTopLeftPoint = cellTopLeftPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            var textDimensions = Renderer.GetTextDimensions(text, Constants.UIBoldFont);
            int x = cellTopLeftPoint.X - (int)(textDimensions.X - Constants.CellSizeX) / 2;

            Vector2 textPosition = new Vector2(x + 60, cellTopLeftPoint.Y - 200);
            Rectangle textBackgroundRectangle = new Rectangle((int)textPosition.X - Constants.UIEmptySideSpace,
                (int)textPosition.Y - Constants.UIEmptyTopSpace,
                (int)textDimensions.X + Constants.UIEmptySideSpace * 2,
                (int)textDimensions.Y + Constants.UIEmptyBottomSpace + Constants.UIEmptyTopSpace);

            Renderer.FillRectangle(textBackgroundRectangle, UISettings.ActiveSettings.PanelBackgroundColor);
            Renderer.DrawRectangle(textBackgroundRectangle, UISettings.ActiveSettings.PanelBorderColor);

            Renderer.DrawStringWithShadow(text, Constants.UIBoldFont, textPosition, textColor);
        }

        public override void OnKeyPressed(KeyPressEventArgs e, Point2D cellCoords)
        {
            if (cellCoords == Point2D.NegativeOne)
                return;

            if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.D)
            {
                ShiftBaseNodeLater(cellCoords);
                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.E)
            {
                ShiftBaseNodeEarlier(cellCoords);
                e.Handled = true;
            }
        }

        public override void LeftClick(Point2D cellCoords)
        {
            if (Keyboard.IsCtrlHeldDown())
            {
                DeleteBaseNode(cellCoords);
            }
            else
            {
                CreateBaseNode(cellCoords);
            }

            base.LeftClick(cellCoords);
        }

        // TODO implement all these manipulations as mutations so they go through the undo/redo system
        private void CreateBaseNode(Point2D cellCoords)
        {
            var mapCell = Map.GetTile(cellCoords);

            if (mapCell.Structures.Count == 0)
                return;

            var structureType = mapCell.Structures[0].ObjectType;
            var cellCoordsToCheck = new List<Point2D>();
            if (structureType.ArtConfig.Foundation.Width == 0 || structureType.ArtConfig.Foundation.Height == 0)
                cellCoordsToCheck.Add(cellCoords);

            for (int y = 0; y < structureType.ArtConfig.Foundation.Height; y++)
            {
                for (int x = 0; x < structureType.ArtConfig.Foundation.Width; x++)
                {
                    cellCoordsToCheck.Add(mapCell.Structures[0].Position + new Point2D(x, y));
                }
            }

            House owner = mapCell.Structures[0].Owner;

            bool overlappingNodes = false;

            // Make sure that a node doesn't already exist in this location for the same house
            foreach (Point2D structureFoundationPoint in cellCoordsToCheck)
            {
                foreach (BaseNode baseNode in owner.BaseNodes)
                {
                    var nodeStructureType = CursorActionTarget.Map.Rules.BuildingTypes.Find(bt => bt.ININame == baseNode.StructureTypeName);

                    if (nodeStructureType == null)
                        continue;

                    if (baseNode.Position == cellCoords)
                    {
                        overlappingNodes = true;
                        break;
                    }

                    bool baseNodeExistsOnFoundation = false;
                    nodeStructureType.ArtConfig.DoForFoundationCoords(foundationOffset =>
                    {
                        Point2D foundationCellCoords = baseNode.Position + foundationOffset;
                        if (foundationCellCoords == structureFoundationPoint)
                            baseNodeExistsOnFoundation = true;
                    });

                    if (baseNodeExistsOnFoundation)
                    {
                        overlappingNodes = true;
                        break;
                    }
                }

                if (overlappingNodes)
                    break;
            }

            if (!overlappingNodes)
            {
                // All OK, create the base node
                var baseNode = new BaseNode(structureType.ININame, mapCell.Structures[0].Position);
                owner.BaseNodes.Add(baseNode);
                CursorActionTarget.Map.RegisterBaseNode(owner, baseNode);
            }

            // If the user is holding Shift, then also delete the building
            if (CursorActionTarget.WindowManager.Keyboard.IsShiftHeldDown())
            {
                CursorActionTarget.Map.RemoveBuildingsFrom(cellCoords);
            }

            CursorActionTarget.AddRefreshPoint(cellCoords);
        }

        private int GetBaseNodeIndexForHouse(House house, Point2D cellCoords)
        {
            return house.BaseNodes.FindIndex(baseNode =>
            {
                var structureType = CursorActionTarget.Map.Rules.BuildingTypes.Find(bt => bt.ININame == baseNode.StructureTypeName);
                if (structureType == null)
                    return false;

                if (structureType.ArtConfig.Foundation.Width == 0 || structureType.ArtConfig.Foundation.Height == 0)
                    return baseNode.Position == cellCoords;

                bool clickedOnFoundation = false;
                structureType.ArtConfig.DoForFoundationCoords(foundationOffset =>
                {
                    if (foundationOffset + baseNode.Position == cellCoords)
                        clickedOnFoundation = true;
                });

                return clickedOnFoundation;
            });
        }

        private void DeleteBaseNode(Point2D cellCoords)
        {
            foreach (House house in Map.GetHouses())
            {
                int index = GetBaseNodeIndexForHouse(house, cellCoords);

                if (index > -1)
                {
                    var baseNode = house.BaseNodes[index];
                    house.BaseNodes.RemoveAt(index);
                    CursorActionTarget.Map.UnregisterBaseNode(baseNode);
                    CursorActionTarget.AddRefreshPoint(cellCoords);
                    return;
                }
            }
        }

        private void ShiftBaseNodeEarlier(Point2D cellCoords)
        {
            foreach (House house in Map.GetHouses())
            {
                int index = GetBaseNodeIndexForHouse(house, cellCoords);

                if (index > -1)
                {
                    if (index == 0)
                    {
                        house.BaseNodes.Swap(0, house.BaseNodes.Count - 1);
                    }
                    else
                    {
                        house.BaseNodes.Swap(index, index - 1);
                    }

                    CursorActionTarget.InvalidateMap();
                    return;
                }
            }
        }

        private void ShiftBaseNodeLater(Point2D cellCoords)
        {
            foreach (House house in Map.GetHouses())
            {
                int index = GetBaseNodeIndexForHouse(house, cellCoords);

                if (index > -1)
                {
                    if (index == house.BaseNodes.Count - 1)
                    {
                        house.BaseNodes.Swap(0, house.BaseNodes.Count - 1);
                    }
                    else
                    {
                        house.BaseNodes.Swap(index, index + 1);
                    }

                    CursorActionTarget.InvalidateMap();
                    return;
                }
            }
        }
    }
}
