namespace TSMapEditor.Models
{
    /// <summary>
    /// Could also be called 'vehicle', but let's respect the original game's naming.
    /// </summary>
    public class Unit : Foot<UnitType>
    {
        public Unit(UnitType objectType) : base(objectType)
        {
        }

        public override RTTIType WhatAmI() => RTTIType.Unit;

        public UnitType UnitType => ObjectType;

        /// <summary>
        /// The index of a unit (in the internal unit list) that is set up to
        /// follow this unit. -1 means none. Only fetched and used on map load,
        /// otherwise <see cref="FollowerUnit"/> is used.
        /// </summary>
        public int FollowerID { get; set; } = -1;

        /// <summary>
        /// The unit, if any, that is set up to follow this unit.
        /// </summary>
        public Unit FollowerUnit { get; set; }

        public override int GetFrameIndex(int frameCount)
        {
            return GetStandingFrame(frameCount);
        }

        public int GetTurretFrameIndex() => UnitType.GetTurretStartFrame() + GetTurretFacingIndex();

        private int GetTurretFacingIndex()
        {
            // Seems that there is no standard way of doing things with this engine...
            int facingIndex = GetFacingIndexWithOffset();
            double turretFramesPerFacing = Constants.TurretFrameCount / (double)UnitType.ArtConfig.Facings;
            return (int)(facingIndex * turretFramesPerFacing);
        }

        private int GetFacingIndex()
        {
            int facingIndex = 0;
            if (UnitType.ArtConfig.Facings == 8)
            {
                facingIndex = Facing / 32;
            }
            else if (UnitType.ArtConfig.Facings > 1)
            {
                facingIndex = Facing / (256 / UnitType.ArtConfig.Facings);

                if (facingIndex >= UnitType.ArtConfig.Facings)
                    facingIndex = 0;
            }

            return facingIndex;
        }

        private int GetFacingIndexWithOffset()
        {
            int facingIndex = GetFacingIndex();

            // Facings start to the north instead of north-east, we need to offset the facing index accordingly
            // by one "full facing" (one-eight of 256). How many frames this means depends on how many facings the unit has.

            facingIndex += (UnitType.ArtConfig.Facings / 8);

            // If the facing index turned out-of-bounds, spin it around
            if (facingIndex >= UnitType.ArtConfig.Facings)
                facingIndex -= UnitType.ArtConfig.Facings;

            return facingIndex;
        }

        private int GetStandingFrame(int frameCount)
        {
            int facingIndex = GetFacingIndexWithOffset();

            // Units with firing frames by default have their frames
            // in a different sequence compared to units without firing frames
            if (UnitType.ArtConfig.FiringFrames > 0)
            {
                int startStandFrame;

                if (UnitType.ArtConfig.StartStandFrame > -1)
                    startStandFrame = UnitType.ArtConfig.StartStandFrame;
                else
                    startStandFrame = UnitType.ArtConfig.WalkFrames * UnitType.ArtConfig.Facings;

                return startStandFrame + (facingIndex * UnitType.ArtConfig.StandingFrames);
            }
            else
            {
                if (UnitType.ArtConfig.StartStandFrame < 0)
                {
                    if (UnitType.ArtConfig.StartWalkFrame < 0)
                    {
                        return facingIndex * UnitType.ArtConfig.StandingFrames;
                    }

                    // If FiringFrames=0, StartStandFrame matches StartWalkFrame by default
                    return UnitType.ArtConfig.StartWalkFrame + (UnitType.ArtConfig.WalkFrames * facingIndex);
                }

                // Use stand frames
                return UnitType.ArtConfig.StartStandFrame + (facingIndex * UnitType.ArtConfig.StandingFrames);
            }
        }

        public override bool Remapable() => ObjectType.ArtConfig.Remapable;

        public override int GetShadowFrameIndex(int frameCount)
        {
            return GetFrameIndex(frameCount) + frameCount / 2;
        }

        public override bool HasShadow() => !ObjectType.NoShadow;
    }
}
