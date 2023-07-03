using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;

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

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            string text = "Click on building to place a base node." + Environment.NewLine + Environment.NewLine +
                "Hold SHIFT while clicking to also delete the building." + Environment.NewLine +
                "Hold CTRL while clicking to erase a base node.";

            DrawText(cellCoords, cameraTopLeftPoint, text, UISettings.ActiveSettings.AltColor);
        }

        private void DrawText(Point2D cellCoords, Point2D cameraTopLeftPoint, string text, Color textColor)
        {
            Point2D cellTopLeftPoint = CellMath.CellTopLeftPointFromCellCoords(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint;
            cellTopLeftPoint = cellTopLeftPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            var textDimensions = Renderer.GetTextDimensions(text, Constants.UIBoldFont);
            int x = cellTopLeftPoint.X - (int)(textDimensions.X - Constants.CellSizeX) / 2;

            Vector2 textPosition = new Vector2(x + 60, cellTopLeftPoint.Y - 150);
            Rectangle textBackgroundRectangle = new Rectangle((int)textPosition.X - Constants.UIEmptySideSpace,
                (int)textPosition.Y - Constants.UIEmptyTopSpace,
                (int)textDimensions.X + Constants.UIEmptySideSpace * 2,
                (int)textDimensions.Y + Constants.UIEmptyBottomSpace + Constants.UIEmptyTopSpace);

            Renderer.FillRectangle(textBackgroundRectangle, UISettings.ActiveSettings.PanelBackgroundColor);
            Renderer.DrawRectangle(textBackgroundRectangle, UISettings.ActiveSettings.PanelBorderColor);

            Renderer.DrawStringWithShadow(text, Constants.UIBoldFont, textPosition, textColor);
        }

        public override void LeftClick(Point2D cellCoords)
        {
            if (!CursorActionTarget.WindowManager.Keyboard.IsCtrlHeldDown())
            {
                CreateBaseNode(cellCoords);
            }
            else
            {
                DeleteBaseNode(cellCoords);
            }

            base.LeftClick(cellCoords);
        }

        private void CreateBaseNode(Point2D cellCoords)
        {
            var mapCell = CursorActionTarget.Map.GetTile(cellCoords);

            if (mapCell.Structure == null)
                return;

            var structureType = mapCell.Structure.ObjectType;
            var cellCoordsToCheck = new List<Point2D>();
            if (structureType.ArtConfig.Foundation.Width == 0 || structureType.ArtConfig.Foundation.Height == 0)
                cellCoordsToCheck.Add(cellCoords);

            for (int y = 0; y < structureType.ArtConfig.Foundation.Height; y++)
            {
                for (int x = 0; x < structureType.ArtConfig.Foundation.Width; x++)
                {
                    cellCoordsToCheck.Add(mapCell.Structure.Position + new Point2D(x, y));
                }
            }

            House owner = mapCell.Structure.Owner;

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
                var baseNode = new BaseNode(structureType.ININame, mapCell.Structure.Position);
                owner.BaseNodes.Add(baseNode);
                CursorActionTarget.Map.RegisterBaseNode(owner, baseNode);
            }

            // If the user is holding Shift, then also delete the building
            if (CursorActionTarget.WindowManager.Keyboard.IsShiftHeldDown())
            {
                CursorActionTarget.Map.RemoveBuilding(cellCoords);
            }

            CursorActionTarget.AddRefreshPoint(cellCoords);
        }

        private void DeleteBaseNode(Point2D cellCoords)
        {
            foreach (House house in CursorActionTarget.Map.GetHouses())
            {
                int index = house.BaseNodes.FindIndex(baseNode =>
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

                if (index > -1)
                {
                    var baseNode = house.BaseNodes[index];
                    house.BaseNodes.RemoveAt(index);
                    CursorActionTarget.Map.UnregisterBaseNode(baseNode);
                    CursorActionTarget.AddRefreshPoint(cellCoords);
                }
            }
        }
    }
}
