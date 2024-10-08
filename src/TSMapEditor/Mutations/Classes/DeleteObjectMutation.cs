using System;
using TSMapEditor.GameMath;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    public class DeleteObjectMutation : Mutation
    {
        public DeleteObjectMutation(IMutationTarget mutationTarget, Point2D cellCoords, DeletionMode deletionMode) : base(mutationTarget)
        {
            this.cellCoords = cellCoords;
            this.deletionMode = deletionMode;
        }

        private readonly Point2D cellCoords;
        private readonly DeletionMode deletionMode;

        private AbstractObject deletedObject;

        public override void Perform()
        {
            if (!Map.HasObjectToDelete(cellCoords, deletionMode))
                throw new InvalidOperationException("Cannot find an object to delete from cell at " + cellCoords);

            deletedObject = Map.DeleteObjectFromCell(cellCoords, deletionMode);
            MutationTarget.AddRefreshPoint(cellCoords, 2);

            if (deletedObject == null)
                throw new ApplicationException("wtf");
        }

        public override void Undo()
        {
            switch (deletedObject.WhatAmI())
            {
                case RTTIType.CellTag:
                    Map.AddCellTag(deletedObject as CellTag);
                    break;
                case RTTIType.Waypoint:
                    Map.AddWaypoint(deletedObject as Waypoint);
                    break;
                case RTTIType.Infantry:
                    Map.PlaceInfantry(deletedObject as Infantry);
                    break;
                case RTTIType.Aircraft:
                    Map.PlaceAircraft(deletedObject as Aircraft);
                    break;
                case RTTIType.Unit:
                    Map.PlaceUnit(deletedObject as Unit);
                    break;
                case RTTIType.Building:
                    Map.PlaceBuilding(deletedObject as Structure);
                    break;
                case RTTIType.Terrain:
                    Map.AddTerrainObject(deletedObject as TerrainObject);
                    break;
            }

            MutationTarget.AddRefreshPoint(cellCoords, 2);
        }
    }
}
