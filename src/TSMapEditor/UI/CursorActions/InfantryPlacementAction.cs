using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allows placing down infantry.
    /// </summary>
    public class InfantryPlacementAction : CursorAction
    {
        public InfantryPlacementAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Place Infantry";

        private Infantry infantry;

        private InfantryType _infantryType;
        

        public InfantryType InfantryType
        {
            get => _infantryType;
            set
            {
                if (_infantryType != value)
                {
                    _infantryType = value;

                    if (_infantryType == null)
                    {
                        infantry = null;
                    }
                    else
                    {
                        infantry = new Infantry(_infantryType) { Owner = CursorActionTarget.MutationTarget.ObjectOwner };
                    }
                }
            }
        }

        public override void PreMapDraw(Point2D cellCoords)
        {
            // Assign preview data
            infantry.Position = cellCoords;

            var tile = CursorActionTarget.Map.GetTile(cellCoords);
            SubCell freeSubCell = tile.GetFreeSubCellSpot();
            infantry.SubCell = freeSubCell;
            if (freeSubCell != SubCell.None)
            {
                infantry.SubCell = freeSubCell;
                tile.AddInfantry(infantry);
                CursorActionTarget.TechnoUnderCursor = infantry;
                CursorActionTarget.AddRefreshPoint(cellCoords);
            }
        }

        public override void PostMapDraw(Point2D cellCoords)
        {
            // Clear preview data
            var tile = CursorActionTarget.Map.GetTile(cellCoords);
            if (infantry.SubCell != SubCell.None)
            {
                // If SubCell != none, then the infantry was placed on the tile
                tile.Infantry[(int)infantry.SubCell] = null;
                CursorActionTarget.TechnoUnderCursor = tile.GetTechno();
                CursorActionTarget.AddRefreshPoint(cellCoords);
            }
        }

        public override void LeftDown(Point2D cellCoords)
        {
            if (InfantryType == null)
                throw new InvalidOperationException(nameof(InfantryType) + " cannot be null");

            var tile = CursorActionTarget.Map.GetTile(cellCoords);
            SubCell freeSubCell = tile.GetFreeSubCellSpot();
            if (freeSubCell == SubCell.None)
                return;

            var mutation = new PlaceInfantryMutation(CursorActionTarget.MutationTarget, InfantryType, cellCoords, freeSubCell);
            CursorActionTarget.MutationManager.PerformMutation(mutation);
        }

        public override void LeftClick(Point2D cellCoords)
        {
            LeftDown(cellCoords);
        }
    }
}
