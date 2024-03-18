using System;
using Rampastring.XNAUI.Input;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Rendering;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allows placing buildings on the map.
    /// </summary>
    public class BuildingPlacementAction : CursorAction
    {
        public BuildingPlacementAction(ICursorActionTarget cursorActionTarget, RKeyboard keyboard) : base(cursorActionTarget)
        {
            this.keyboard = keyboard;
        }

        public override string GetName() => "Place Building";

        private Structure structure;

        private BuildingType _buildingType;

        private readonly RKeyboard keyboard;

        public BuildingType BuildingType
        {
            get => _buildingType;
            set
            {
                if (_buildingType != value)
                {
                    _buildingType = value;

                    if (_buildingType == null)
                    {
                        structure = null;
                    }
                    else
                    {
                        structure = new Structure(_buildingType) { Owner = CursorActionTarget.MutationTarget.ObjectOwner };
                    }
                }
            }
        }

        public override void PreMapDraw(Point2D cellCoords)
        {
            // Assign preview data
            structure.Position = cellCoords;

            bool overlapObjects = KeyboardCommands.Instance.OverlapObjects.AreKeysOrModifiersDown(keyboard);

            bool canPlace = Map.CanPlaceObjectAt(structure, cellCoords, false,
                overlapObjects);

            if (!canPlace)
                return;

            var tile = CursorActionTarget.Map.GetTile(cellCoords);
            tile.Structures.Add(structure);
            CursorActionTarget.TechnoUnderCursor = structure;
            CursorActionTarget.AddRefreshPoint(cellCoords, 10);
        }

        public override void PostMapDraw(Point2D cellCoords)
        {
            // Clear preview data
            var tile = CursorActionTarget.Map.GetTile(cellCoords);
            if (tile.Structures.Contains(structure))
            {
                tile.Structures.Remove(structure);
                CursorActionTarget.TechnoUnderCursor = null;
                CursorActionTarget.AddRefreshPoint(cellCoords, 10);
            }
        }

        public override void LeftDown(Point2D cellCoords)
        {
            if (BuildingType == null)
                throw new InvalidOperationException(nameof(BuildingType) + " cannot be null");

            bool overlapObjects = KeyboardCommands.Instance.OverlapObjects.AreKeysOrModifiersDown(keyboard);

            bool canPlace = Map.CanPlaceObjectAt(structure, cellCoords, false,
                overlapObjects);

            if (!canPlace)
                return;

            var mutation = new PlaceBuildingMutation(CursorActionTarget.MutationTarget, BuildingType, cellCoords);
            CursorActionTarget.MutationManager.PerformMutation(mutation);
        }

        public override void LeftClick(Point2D cellCoords)
        {
            LeftDown(cellCoords);
        }
    }
}
