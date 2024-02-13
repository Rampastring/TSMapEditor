using TSMapEditor.GameMath;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace TSMapEditor.Models
{
    public class Structure : Techno<BuildingType>
    {
        // [Structures]
        // INDEX=OWNER,ID,HEALTH,X,Y,FACING,TAG,AI_SELLABLE,AI_REBUILDABLE,POWERED_ON,UPGRADES,SPOTLIGHT,UPGRADE_1,UPGRADE_2,UPGRADE_3,AI_REPAIRABLE,NOMINAL

        public const int MaxUpgradeCount = 3;

        public Structure(BuildingType objectType) : base(objectType)
        {
            var anims = new List<Animation>();
            foreach (var animType in objectType.ArtConfig.Anims)
            {
                if (!animType.RenderInEditor)
                    continue;

                var anim = new Animation(animType)
                {
                    IsBuildingAnim = true,
                    ParentBuilding = this
                };

                anims.Add(anim);
            }
            Anims = anims.ToArray();

            if (objectType.Turret && !objectType.TurretAnimIsVoxel && objectType.ArtConfig.TurretAnim != null)
            {
                TurretAnim = new Animation(objectType.ArtConfig.TurretAnim)
                {
                    IsTurretAnim = true,
                    IsBuildingAnim = true,
                    ParentBuilding = this,
                    ExtraDrawOffset = new Point2D(objectType.TurretAnimX, objectType.TurretAnimY)
                };
            }
        }

        public override RTTIType WhatAmI() => RTTIType.Building;

        private Point2D _position;
        public override Point2D Position
        {
            get => _position;
            set
            {
                _position = value;

                foreach (var anim in Anims)
                    anim.Position = value;

                if (TurretAnim != null)
                    TurretAnim.Position = value;
            }
        }

        private House _owner;
        public override House Owner
        {
            get => _owner;
            set
            {
                _owner = value;

                foreach (var anim in Anims)
                    anim.Owner = value;

                if (TurretAnim != null)
                    TurretAnim.Owner = value;
            }
        }

        private byte _facing;
        public override byte Facing
        {
            get => _facing;
            set
            {
                _facing = value;

                if (TurretAnim != null)
                    TurretAnim.Facing = value;
            }
        }

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
        public Animation[] Anims { get; set; }
        public Animation TurretAnim { get; set; }

        /// <summary>
        /// If set, this object instance only exists as a dummy for rendering base nodes.
        /// It is not actually present on the map.
        /// </summary>
        public bool IsBaseNodeDummy { get; set; }

        public override double GetCloakGeneratorRange()
        {
            return ObjectType.CloakGenerator ? ObjectType.CloakRadiusInCells : 0.0;
        }

        public override double GetSensorArrayRange()
        {
            return ObjectType.SensorArray ? ObjectType.CloakRadiusInCells : 0.0;
        }

        public override Color GetRadialColor()
        {
            return ObjectType.RadialColor.GetValueOrDefault(base.GetRadialColor());
        }

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

        public override int GetShadowFrameIndex(int frameCount)
        {
            if (HP < Constants.ConditionYellowHP)
                return frameCount / 2 + 1;

            return frameCount / 2;
        }

        public override bool Remapable() => ObjectType.ArtConfig.Remapable;

        public override Color GetRemapColor() => IsBaseNodeDummy ? base.GetRemapColor() * 0.25f : base.GetRemapColor();

        public override Structure Clone()
        {
            var clone = MemberwiseClone() as Structure;

            clone.Anims = Anims.Select(anim => anim.Clone() as Animation).ToArray();

            if (TurretAnim != null)
                clone.TurretAnim = TurretAnim.Clone() as Animation;

            return clone;
        }
    }
}
