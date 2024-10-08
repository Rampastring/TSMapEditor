namespace TSMapEditor.Models
{
    public class Infantry : Foot<InfantryType>
    {
        public Infantry(InfantryType objectType) : base(objectType)
        {
        }

        // [Infantry]
        // INDEX=OWNER,ID,HEALTH,X,Y,SUB_CELL,MISSION,FACING,TAG,VETERANCY,GROUP,HIGH,AUTOCREATE_NO_RECRUITABLE,AUTOCREATE_YES_RECRUITABLE

        public override RTTIType WhatAmI() => RTTIType.Infantry;

        public SubCell SubCell { get; set; }

        public override int GetFrameIndex(int frameCount)
        {
            if (ObjectType.ArtConfig.Sequence == null)
                return 0;

            var readySequence = ObjectType.ArtConfig.Sequence.Ready;

            // Infantry have their facing frames reversed
            return readySequence.StartFrame + (((255 - Facing) / 32) * readySequence.FacingMultiplier * readySequence.FrameCount);
        }

        public override int GetShadowFrameIndex(int frameCount)
        {
            return GetFrameIndex(frameCount) + (frameCount / 2);
        }

        public override bool HasShadow() => !ObjectType.NoShadow;

        public override bool Remapable() => ObjectType.ArtConfig.Remapable;

        public override int GetHashCode()
        {
            return ((int)WhatAmI() * 10000000) + (Position.Y * 512) + (Position.X * 10) + (int)SubCell;
        }
    }
}
