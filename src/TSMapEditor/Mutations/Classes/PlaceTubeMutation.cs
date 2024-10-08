using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    public class PlaceTubeMutation : Mutation
    {
        public PlaceTubeMutation(IMutationTarget mutationTarget, Tube tube) : base(mutationTarget)
        {
            this.tube = tube;
        }

        private readonly Tube tube;

        public override void Perform()
        {
            MutationTarget.Map.Tubes.Add(tube);
            TubeRefreshHelper.MapViewRefreshTube(tube, MutationTarget);
        }

        public override void Undo()
        {
            MutationTarget.Map.Tubes.Remove(tube);
            TubeRefreshHelper.MapViewRefreshTube(tube, MutationTarget);
        }
    }
}
