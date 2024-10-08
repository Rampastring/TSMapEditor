using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.Models
{
    public class TerrainType : GameObjectType
    {
        public TerrainType(string iniName) : base(iniName)
        {
        }

        public override RTTIType WhatAmI() => RTTIType.TerrainType;

        public TerrainOccupation TemperateOccupationBits { get; set; }
        public TerrainOccupation SnowOccupationBits { get; set; }

        /// <summary>
        /// If set, this terrain type should be drawn 12 pixels above the 
        /// usual drawing point and it should use the unit palette instead
        /// of the terrain palette.
        /// </summary>
        public bool SpawnsTiberium { get; set; }

        public int YDrawFudge { get; set; }

        /// <summary>
        /// Defined in Art.ini. If set to true,
        /// the art for this terrain type is theater-specific;
        /// if false, the art is a generic .SHP used for every theater.
        /// </summary>
        public bool Theater { get; set; }

        public string Image { get; set; }

        /// <summary>
        /// Impassable cell data for automatically placing impassable overlay
        /// under terrain objects.
        /// </summary>
        public List<Point2D> ImpassableCells { get; set; }
    }
}
