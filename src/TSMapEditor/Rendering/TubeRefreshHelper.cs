using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Rendering
{
    public static class TubeRefreshHelper
    {
        public static void MapViewRefreshTube(Tube tube, IMutationTarget mutationTarget)
        {
            Point2D point = tube.EntryPoint;
            mutationTarget.AddRefreshPoint(point, 1);
            tube.Directions.ForEach(td =>
            {
                point = point.NextPointFromTubeDirection(td);

                int refreshSize = 0;

                switch (td)
                {
                    case TubeDirection.NorthEast:
                    case TubeDirection.NorthWest:
                    case TubeDirection.SouthEast:
                    case TubeDirection.SouthWest:
                        refreshSize = 0;
                        break;
                    case TubeDirection.North:
                    case TubeDirection.East:
                    case TubeDirection.South:
                    case TubeDirection.West:
                        refreshSize = 1;
                        break;
                }

                mutationTarget.AddRefreshPoint(point, refreshSize);
            });
            mutationTarget.AddRefreshPoint(tube.ExitPoint, 1);
        }
    }
}
