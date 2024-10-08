using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.GameMath;
using TSMapEditor.Models.ArtConfig;

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

                var animArtConfig = objectType.ArtConfig.BuildingAnimConfigs.Find(x => x.ININame == animType.ININame)
                                    ?? new BuildingAnimArtConfig();

                anims.Add(new Animation(animType)
                {
                    IsBuildingAnim = true,
                    ParentBuilding = this,
                    BuildingAnimDrawConfig = new BuildingAnimDrawConfig
                    {
                        X = animArtConfig.X,
                        Y = animArtConfig.Y,
                        YSort = animArtConfig.YSort,
                        ZAdjust = animArtConfig.ZAdjust
                    }
                });
            }
            Anims = anims.ToArray();

            if (objectType.Turret && !objectType.TurretAnimIsVoxel && objectType.ArtConfig.TurretAnim != null)
            {
                TurretAnim = new Animation(objectType.ArtConfig.TurretAnim)
                {
                    IsBuildingAnim = true,
                    IsTurretAnim = true,
                    ParentBuilding = this,
                    BuildingAnimDrawConfig = new BuildingAnimDrawConfig
                    {
                        X = objectType.TurretAnimX,
                        Y = objectType.TurretAnimY,
                        YSort = objectType.TurretAnimYSort,
                        ZAdjust = objectType.TurretAnimZAdjust
                    }
                };
            }

            UpdatePowerUpAnims();
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
                    anim.Position = GetSouthernmostFoundationCell();

                foreach (var powerUpAnim in PowerUpAnims)
                {
                    if (powerUpAnim != null)
                    {
                        powerUpAnim.Position = GetSouthernmostFoundationCell();
                    }
                }

                if (TurretAnim != null)
                    TurretAnim.Position = GetSouthernmostFoundationCell();
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

                foreach (var powerUpAnim in PowerUpAnims)
                {
                    if (powerUpAnim != null)
                    {
                        powerUpAnim.Owner = value;
                    }
                }

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

                foreach (var powerUpAnim in PowerUpAnims)
                {
                    if (powerUpAnim != null)
                    {
                        powerUpAnim.Facing = value;
                    }
                }

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
        public int UpgradeCount => Upgrades.Count(u => u != null);

        public bool AIRepairable { get; set; }
        public bool Nominal { get; set; }
        public Animation[] Anims { get; set; }
        public Animation[] PowerUpAnims { get; set; }
        public Animation TurretAnim { get; set; }

        /// <summary>
        /// If set, this object instance only exists as a dummy for rendering base nodes.
        /// It is not actually present on the map.
        /// </summary>
        public bool IsBaseNodeDummy { get; set; }

        public List<MapTile> LitTiles { get; set; } = new();

        public void LightTiles(MapTile[][] tiles)
        {
            Dictionary<MapTile, double> litTiles = new();
            int foundationWidth = ObjectType.ArtConfig.Foundation.Width;
            int foundationHeight = ObjectType.ArtConfig.Foundation.Height;
            
            List<int> xCenter = foundationWidth % 2 == 0 ? new List<int> { foundationWidth / 2 - 1, foundationWidth / 2 } : new List<int> { foundationWidth / 2 };
            List<int> yCenter = foundationHeight % 2 == 0 ? new List<int> { foundationHeight / 2 - 1, foundationHeight / 2 } : new List<int> { foundationHeight / 2 };
            Point2D[] centers = xCenter.SelectMany(item1 => yCenter, (item1, item2) => new Point2D(item1, item2)).ToArray();

            int radius = (int)Math.Ceiling((double)ObjectType.LightVisibility / Constants.CellSizeInLeptons) - 1;

            foreach (Point2D center in centers)
            {
                Point2D centerPosition = Position + center;

                int startX = Math.Max(centerPosition.X - radius, 0);
                int endX = Math.Min(centerPosition.X + radius, tiles[0].Length - 1);
                int startY = Math.Max(centerPosition.Y - radius, 0);
                int endY = Math.Min(centerPosition.Y + radius, tiles.Length - 1);

                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        MapTile tile = tiles[y][x];

                        if (tile == null)
                            continue;

                        int xDifference = centerPosition.X - x;
                        int yDifference = centerPosition.Y - y;

                        double distanceInCells = Math.Sqrt(xDifference * xDifference + yDifference * yDifference);
                        double distanceInLeptons = distanceInCells * Constants.CellSizeInLeptons;

                        if (distanceInLeptons > ObjectType.LightVisibility)
                            continue;

                        if (!litTiles.ContainsKey(tile) ||
                            (litTiles.ContainsKey(tile) && litTiles[tile] > distanceInLeptons))
                        {
                            litTiles[tile] = distanceInLeptons;
                        }
                    }
                }
            }

            foreach (var kvp in litTiles)
            {
                kvp.Key.LightSources.Add((this, kvp.Value));
            }

            LitTiles = litTiles.Keys.ToList();
        }

        public void ClearLitTiles()
        {
            foreach (var tile in LitTiles)
            {
                tile.LightSources.RemoveAll(source => source.Source == this);
            }

            LitTiles.Clear();
        }

        public void UpdatePowerUpAnims()
        {
            var anims = new List<Animation>();

            for (int i = 0; i < Upgrades.Length; i++)
            {
                var upgrade = Upgrades[i];
                if (upgrade == null)
                    continue;

                string upgradeImage = upgrade.ArtConfig.Image;
                if (string.IsNullOrWhiteSpace(upgradeImage))
                    upgradeImage = upgrade.Image;
                if (string.IsNullOrWhiteSpace(upgradeImage))
                    upgradeImage = upgrade.ININame;

                var config = ObjectType.ArtConfig.PowerUpAnimConfigs[i];
                var animType = Array.Find(ObjectType.ArtConfig.PowerUpAnims, at => at.ININame == upgradeImage);

                if (animType == null)
                    continue;

                anims.Add(new Animation(animType)
                {
                    Position = this.Position,
                    Owner = this.Owner,
                    Facing = this.Facing,
                    IsBuildingAnim = true,
                    IsTurretAnim = upgrade.Turret && !upgrade.TurretAnimIsVoxel,
                    ParentBuilding = this,
                    BuildingAnimDrawConfig = new BuildingAnimDrawConfig
                    {
                        X = config.LocXX,
                        Y = config.LocYY,
                        YSort = config.YSort,
                        ZAdjust = config.LocZZ
                    }
                });
            }

            PowerUpAnims = anims.ToArray();
        }

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

        public Point2D GetSouthernmostFoundationCell()
        {
            var foundation = ObjectType.ArtConfig.Foundation;
            if (foundation.Width == 0 || foundation.Height == 0)
                return Position;

            return Position + new Point2D(foundation.Width - 1, foundation.Height - 1);
        }

        public override bool HasShadow() => !ObjectType.NoShadow;

        public override bool Remapable() => ObjectType.ArtConfig.Remapable;

        public override Color GetRemapColor() => IsBaseNodeDummy ? base.GetRemapColor() * 0.25f : base.GetRemapColor();

        public override Structure Clone()
        {
            var clone = MemberwiseClone() as Structure;

            clone.Anims = Anims.Select(anim => anim.Clone() as Animation).ToArray();
            foreach (var anim in clone.Anims)
                anim.ParentBuilding = clone;

            if (TurretAnim != null)
            {
                clone.TurretAnim = TurretAnim.Clone() as Animation;
                clone.TurretAnim.ParentBuilding = clone;
            }

            clone.UpdatePowerUpAnims();

            return clone;
        }
    }
}
