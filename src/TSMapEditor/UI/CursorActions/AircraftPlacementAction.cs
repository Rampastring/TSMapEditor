using System;
using Rampastring.XNAUI.Input;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    public class AircraftPlacementAction : CursorAction
    {
        public AircraftPlacementAction(ICursorActionTarget cursorActionTarget, RKeyboard keyboard) : base(cursorActionTarget)
        {
            this.keyboard = keyboard;
        }

        public override string GetName() => "Place Aircraft";

        private Aircraft aircraft;

        private AircraftType _aircraftType;

        private readonly RKeyboard keyboard;

        public AircraftType AircraftType
        {
            get => _aircraftType;
            set
            {
                if (_aircraftType != value)
                {
                    _aircraftType = value;

                    if (_aircraftType == null)
                    {
                        aircraft = null;
                    }
                    else
                    {
                        aircraft = new Aircraft(_aircraftType) { Owner = CursorActionTarget.MutationTarget.ObjectOwner };
                    }
                }
            }
        }

        public override void PreMapDraw(Point2D cellCoords)
        {
            // Assign preview data
            aircraft.Position = cellCoords;

            bool overlapObjects = KeyboardCommands.Instance.OverlapObjects.AreKeysOrModifiersDown(keyboard);

            bool canPlace = Map.CanPlaceObjectAt(aircraft, cellCoords, false, overlapObjects);

            if (!canPlace)
                return;

            var tile = CursorActionTarget.Map.GetTile(cellCoords);
            tile.Aircraft.Add(aircraft);
            CursorActionTarget.TechnoUnderCursor = aircraft;
            CursorActionTarget.AddRefreshPoint(cellCoords);
        }

        public override void PostMapDraw(Point2D cellCoords)
        {
            // Clear preview data
            var tile = CursorActionTarget.Map.GetTile(cellCoords);
            if (tile.Aircraft.Contains(aircraft))
            {
                tile.Aircraft.Remove(aircraft);
                CursorActionTarget.TechnoUnderCursor = null;
                CursorActionTarget.AddRefreshPoint(cellCoords);
            }
        }

        public override void LeftDown(Point2D cellCoords)
        {
            if (AircraftType == null)
                throw new InvalidOperationException(nameof(AircraftType) + " cannot be null");

            bool overlapObjects = KeyboardCommands.Instance.OverlapObjects.AreKeysOrModifiersDown(keyboard);

            bool canPlace = Map.CanPlaceObjectAt(aircraft, cellCoords, false,
                overlapObjects);

            if (!canPlace)
                return;

            var mutation = new PlaceAircraftMutation(CursorActionTarget.MutationTarget, AircraftType, cellCoords);
            CursorActionTarget.MutationManager.PerformMutation(mutation);
        }

        public override void LeftClick(Point2D cellCoords)
        {
            LeftDown(cellCoords);
        }
    }
}
