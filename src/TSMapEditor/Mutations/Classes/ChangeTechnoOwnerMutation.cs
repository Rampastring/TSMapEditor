using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that changes the owner of an object.
    /// </summary>
    public class ChangeTechnoOwnerMutation : Mutation
    {
        public ChangeTechnoOwnerMutation(TechnoBase techno, House newOwner, IMutationTarget mutationTarget) : base(mutationTarget)
        {
            this.techno = techno;
            this.oldOwner = techno.Owner;
            this.newOwner = newOwner;
        }

        private readonly TechnoBase techno;
        private readonly House oldOwner;
        private readonly House newOwner;

        public override void Perform()
        {
            techno.Owner = newOwner;
            MutationTarget.AddRefreshPoint(techno.Position);
        }

        public override void Undo()
        {
            techno.Owner = oldOwner;
            MutationTarget.AddRefreshPoint(techno.Position);
        }
    }
}
