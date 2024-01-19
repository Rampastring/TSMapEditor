using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that allows placing a waypoint on the map.
    /// </summary>
    public class PlaceWaypointMutation : Mutation
    {
        public PlaceWaypointMutation(IMutationTarget mutationTarget, Point2D cellCoords, int waypointNumber, string waypointColor = null) : base(mutationTarget)
        {
            this.cellCoords = cellCoords;
            this.waypointNumber = waypointNumber;
            this.waypointColor = waypointColor;
        }

        private readonly Point2D cellCoords;
        private readonly int waypointNumber;
        private readonly string waypointColor;
        private Waypoint waypoint;

        public override void Perform()
        {
            waypoint = new Waypoint() { Identifier = waypointNumber, Position = cellCoords };
            waypoint.EditorColor = waypointColor;
            MutationTarget.Map.AddWaypoint(waypoint);
            MutationTarget.AddRefreshPoint(cellCoords, 1);
        }

        public override void Undo()
        {
            MutationTarget.Map.RemoveWaypoint(waypoint);
            MutationTarget.AddRefreshPoint(cellCoords, 1);
        }
    }
}
