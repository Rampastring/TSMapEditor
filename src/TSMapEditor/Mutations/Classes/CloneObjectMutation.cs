using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that clones a game object on the map.
    /// </summary>
    public class CloneObjectMutation : Mutation
    {
        public CloneObjectMutation(IMutationTarget mutationTarget, IMovable movable, Point2D clonePosition) : base(mutationTarget)
        {
            this.objectToClone = (AbstractObject)movable;
            if (!objectToClone.IsTechno() && objectToClone.WhatAmI() != RTTIType.Terrain)
                throw new NotSupportedException(nameof(CloneObjectMutation) + " only supports cloning Technos and TerrainObjects!");

            this.clonePosition = clonePosition;
        }

        private AbstractObject objectToClone;
        private Point2D clonePosition;
        private AbstractObject placedClone;

        private void CloneObject()
        {
            var clone = objectToClone.Clone();

            switch (clone.WhatAmI())
            {
                case RTTIType.Aircraft:
                    var aircraft = (Aircraft)clone;
                    aircraft.Position = clonePosition;
                    MutationTarget.Map.PlaceAircraft(aircraft);
                    break;
                case RTTIType.Building:
                    var building = (Structure)clone;
                    building.Position = clonePosition;
                    MutationTarget.Map.PlaceBuilding(building);
                    break;
                case RTTIType.Unit:
                    var unit = (Unit)clone;
                    unit.Position = clonePosition;
                    MutationTarget.Map.PlaceUnit(unit);
                    break;
                case RTTIType.Infantry:
                    var infantry = (Infantry)clone;
                    infantry.Position = clonePosition;
                    infantry.SubCell = Map.GetTile(clonePosition).GetFreeSubCellSpot();
                    MutationTarget.Map.PlaceInfantry(infantry);
                    break;
                case RTTIType.Terrain:
                    var terrainObject = (TerrainObject)clone;
                    terrainObject.Position = clonePosition;
                    MutationTarget.Map.AddTerrainObject(terrainObject);
                    break;
            }

            placedClone = clone;

            MutationTarget.AddRefreshPoint(clonePosition);
        }

        public override void Perform()
        {
            CloneObject();
        }

        public override void Undo()
        {
            switch (objectToClone.WhatAmI())
            {
                case RTTIType.Aircraft:
                    Map.RemoveAircraft((Aircraft)placedClone);
                    break;
                case RTTIType.Building:
                    Map.RemoveBuilding((Structure)placedClone);
                    break;
                case RTTIType.Unit:
                    Map.RemoveUnit((Unit)placedClone);
                    break;
                case RTTIType.Infantry:
                    Map.RemoveInfantry((Infantry)placedClone);
                    break;
                case RTTIType.Terrain:
                    Map.RemoveTerrainObject((TerrainObject)placedClone);
                    break;
            }
        }
    }
}
