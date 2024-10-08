using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that allows placing infantry on the map.
    /// </summary>
    public class PlaceInfantryMutation : Mutation
    {
        public PlaceInfantryMutation(IMutationTarget mutationTarget, InfantryType infantryType, Point2D cellCoords, SubCell subCell) : base(mutationTarget)
        {
            this.infantryType = infantryType;
            this.cellCoords = cellCoords;
            this.subCell = subCell;
        }

        private readonly InfantryType infantryType;
        private readonly Point2D cellCoords;
        private readonly SubCell subCell;

        private Infantry placedInfantry;

        public override void Perform()
        {
            var cell = MutationTarget.Map.GetTile(cellCoords);
            if (cell == null)
                throw new InvalidOperationException("Invalid cell coords");

            if (cell.Infantry[(int)subCell] != null)
                throw new InvalidOperationException(nameof(PlaceInfantryMutation) + ": cannot place infantry on an occupied sub-cell spot!");

            var infantry = new Infantry(infantryType);
            infantry.Owner = MutationTarget.ObjectOwner;
            infantry.Position = cellCoords;
            infantry.SubCell = subCell;
            placedInfantry = infantry;

            MutationTarget.Map.PlaceInfantry(infantry);
            MutationTarget.AddRefreshPoint(cellCoords);
        }

        public override void Undo()
        {
            MutationTarget.Map.RemoveInfantry(placedInfantry);
            MutationTarget.AddRefreshPoint(placedInfantry.Position);
        }
    }
}
