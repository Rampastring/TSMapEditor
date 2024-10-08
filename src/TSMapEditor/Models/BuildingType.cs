using Microsoft.Xna.Framework;
using TSMapEditor.Models.ArtConfig;

namespace TSMapEditor.Models
{
    public class BuildingType : TechnoType, IArtConfigContainer
    {
        public BuildingType(string iniName) : base(iniName)
        {
        }

        public int Power { get; set; }
        public int Upgrades { get; set; } = 1;
        public string PowersUpBuilding { get; set; }
        public bool HasSpotlight { get; set; }
        public bool FirestormWall { get; set; }
        public bool Turret { get; set; }
        public string TurretAnim { get; set; }
        public bool TurretAnimIsVoxel { get; set; }
        public int TurretAnimX { get; set; }
        public int TurretAnimY { get; set; }
        public int TurretAnimYSort { get; set; }
        public int TurretAnimZAdjust { get; set; }
        public string VoxelBarrelFile { get; set; }
        public bool BarrelAnimIsVoxel { get; set; }
        public bool CloakGenerator { get; set; }
        public bool SensorArray { get; set; }
        public Color? RadialColor { get; set; }
        public int CloakRadiusInCells { get; set; }
        public double LightIntensity { get; set; }
        public int LightVisibility { get; set; }
        public double LightRedTint { get; set; }
        public double LightGreenTint { get; set; }
        public double LightBlueTint { get; set; }

        public BuildingArtConfig ArtConfig { get; set; } = new BuildingArtConfig();
        public IArtConfig GetArtConfig() => ArtConfig;

        public override RTTIType WhatAmI() => RTTIType.BuildingType;
    }
}
