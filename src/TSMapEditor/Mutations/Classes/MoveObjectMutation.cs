using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that moves a game object on the map.
    /// </summary>
    public class MoveObjectMutation : Mutation
    {
        public MoveObjectMutation(IMutationTarget mutationTarget, IMovable movable, Point2D newPosition) : base(mutationTarget)
        {
            this.movable = movable;
            oldPosition = movable.Position;
            this.newPosition = newPosition;
        }

        private IMovable movable;
        private Point2D oldPosition;
        private Point2D newPosition;

        private void MoveObject(Point2D position)
        {
            switch (movable.WhatAmI())
            {
                case RTTIType.Aircraft:
                    MutationTarget.Map.MoveAircraft((Aircraft)movable, position);
                    break;
                case RTTIType.Building:
                    MutationTarget.Map.MoveBuilding((Structure)movable, position);
                    break;
                case RTTIType.Unit:
                    MutationTarget.Map.MoveUnit((Unit)movable, position);
                    break;
                case RTTIType.Infantry:
                    MutationTarget.Map.MoveInfantry((Infantry)movable, position);
                    break;
                case RTTIType.Terrain:
                    MutationTarget.Map.MoveTerrainObject((TerrainObject)movable, position);
                    break;
                case RTTIType.Waypoint:
                    MutationTarget.Map.MoveWaypoint((Waypoint)movable, position);
                    break;
            }

            MutationTarget.AddRefreshPoint(newPosition);
            MutationTarget.AddRefreshPoint(oldPosition);
        }

        public override void Perform()
        {
            // TODO handle sub-cell for infantry
            MoveObject(newPosition);
        }

        public override void Undo()
        {
            // TODO handle sub-cell for infantry
            MoveObject(oldPosition);
        }
    }
}
