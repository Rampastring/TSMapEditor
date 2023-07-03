namespace TSMapEditor.Models
{
    public class Structure : Techno<BuildingType>
    {
        // [Structures]
        // INDEX=OWNER,ID,HEALTH,X,Y,FACING,TAG,AI_SELLABLE,AI_REBUILDABLE,POWERED_ON,UPGRADES,SPOTLIGHT,UPGRADE_1,UPGRADE_2,UPGRADE_3,AI_REPAIRABLE,NOMINAL

        public const int MaxUpgradeCount = 3;

        public Structure(BuildingType objectType) : base(objectType)
        {
        }

        public override RTTIType WhatAmI() => RTTIType.Building;

        public bool AISellable { get; set; }

        /// <summary>
        /// According to ModEnc, this is an obsolete key from Red Alert.
        /// </summary>
        public bool AIRebuildable { get; set; }
        public bool Powered { get; set; } = true;
        
        public SpotlightType Spotlight { get; set; }
        public BuildingType[] Upgrades { get; private set; } = new BuildingType[MaxUpgradeCount];
        public int UpgradeCount
        {
            get
            {
                int upgradeCount = 0;

                for (int i = 0; i < MaxUpgradeCount && i < ObjectType.Upgrades; i++)
                {
                    if (Upgrades[i] != null)
                        upgradeCount++;
                }

                return upgradeCount;
            }
        }

        public bool AIRepairable { get; set; }
        public bool Nominal { get; set; }

        public override int GetYDrawOffset()
        {
            return Constants.CellSizeY / -2;
        }

        public override int GetFrameIndex(int frameCount)
        {
            if (frameCount > 1 && HP < Constants.ConditionYellowHP)
                return 1;

            return 0;
        }

        public override int GetXPositionForDrawOrder()
        {
            return Position.X + ObjectType.ArtConfig.Foundation.Width / 2;
        }

        public override int GetYPositionForDrawOrder()
        {
            return Position.Y + ObjectType.ArtConfig.Foundation.Height / 2;
        }

        public override bool Remapable() => ObjectType.ArtConfig.Remapable;
    }
}
