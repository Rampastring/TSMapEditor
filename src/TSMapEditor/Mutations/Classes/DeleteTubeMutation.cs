using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    public class DeleteTubeMutation : Mutation
    {
        public DeleteTubeMutation(IMutationTarget mutationTarget, Point2D cellCoords) : base(mutationTarget)
        {
            this.cellCoords = cellCoords;
        }

        private readonly Point2D cellCoords;

        private Tube deletedTube;
        private int deletedTubeIndex;

        public override void Perform()
        {
            int index = MutationTarget.Map.Tubes.FindIndex(t => t.EntryPoint == cellCoords || t.ExitPoint == cellCoords);

            if (index > -1)
            {
                deletedTubeIndex = index;
                deletedTube = MutationTarget.Map.Tubes[index];
                MutationTarget.Map.Tubes.RemoveAt(index);

                TubeRefreshHelper.MapViewRefreshTube(deletedTube, MutationTarget);
            }
            else
            {
                throw new InvalidOperationException($"{nameof(DeleteTubeMutation)}.{nameof(Perform)}: No tunnel found at tile {cellCoords}");
            }
        }

        public override void Undo()
        {
            MutationTarget.Map.Tubes.Insert(deletedTubeIndex, deletedTube);
            TubeRefreshHelper.MapViewRefreshTube(deletedTube, MutationTarget);
        }
    }
}
