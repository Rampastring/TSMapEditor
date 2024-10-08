using System;
using Rampastring.XNAUI.Input;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allows placing units on the map.
    /// </summary>
    class UnitPlacementAction : CursorAction
    {
        public UnitPlacementAction(ICursorActionTarget cursorActionTarget, RKeyboard keyboard) : base(cursorActionTarget)
        {
            this.keyboard = keyboard;
        }

        public override string GetName() => "Place Vehicle";

        private Unit unit;

        private UnitType _unitType;

        private readonly RKeyboard keyboard;

        public UnitType UnitType
        {
            get => _unitType;
            set
            {
                if (_unitType != value)
                {
                    _unitType = value;

                    if (_unitType == null)
                    {
                        unit = null;
                    }
                    else
                    {
                        unit = new Unit(_unitType) { Owner = CursorActionTarget.MutationTarget.ObjectOwner };
                    }
                }
            }
        }

        public override void PreMapDraw(Point2D cellCoords)
        {
            // Assign preview data
            unit.Position = cellCoords;

            bool overlapObjects = KeyboardCommands.Instance.OverlapObjects.AreKeysOrModifiersDown(keyboard);

            bool canPlace = Map.CanPlaceObjectAt(unit, cellCoords, false,
                overlapObjects);

            if (!canPlace)
                return;

            var tile = CursorActionTarget.Map.GetTile(cellCoords);
            tile.Vehicles.Add(unit);
            CursorActionTarget.TechnoUnderCursor = unit;
            CursorActionTarget.AddRefreshPoint(cellCoords);
        }

        public override void PostMapDraw(Point2D cellCoords)
        {
            // Clear preview data
            var tile = CursorActionTarget.Map.GetTile(cellCoords);
            if (tile.Vehicles.Contains(unit))
            {
                tile.Vehicles.Remove(unit);
                CursorActionTarget.TechnoUnderCursor = null;
                CursorActionTarget.AddRefreshPoint(cellCoords);
            }
        }

        public override void LeftDown(Point2D cellCoords)
        {
            if (UnitType == null)
                throw new InvalidOperationException(nameof(UnitType) + " cannot be null");

            bool overlapObjects = KeyboardCommands.Instance.OverlapObjects.AreKeysOrModifiersDown(keyboard);

            bool canPlace = Map.CanPlaceObjectAt(unit, cellCoords, false,
                overlapObjects);

            if (!canPlace)
                return;

            var mutation = new PlaceVehicleMutation(CursorActionTarget.MutationTarget, UnitType, cellCoords);
            CursorActionTarget.MutationManager.PerformMutation(mutation);
        }

        public override void LeftClick(Point2D cellCoords)
        {
            LeftDown(cellCoords);
        }
    }
}
