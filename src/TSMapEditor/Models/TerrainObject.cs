using TSMapEditor.GameMath;

namespace TSMapEditor.Models
{
    /// <summary>
    /// A terrain object. For example, a tree.
    /// </summary>
    public class TerrainObject : GameObject
    {
        public TerrainObject(TerrainType terrainType)
        {
            TerrainType = terrainType;
        }

        public TerrainObject(TerrainType terrainType, Point2D position) : this(terrainType)
        {
            Position = position;
        }

        public override GameObjectType GetObjectType() => TerrainType;

        public override RTTIType WhatAmI() => RTTIType.Terrain;

        public TerrainType TerrainType { get; private set; }

        public override bool IsInvisibleInGame() => TerrainType.InvisibleInGame;

        public override int GetYDrawOffset()
        {
            if (TerrainType.SpawnsTiberium)
                return Constants.CellSizeY / -2;

            return TerrainType.YDrawFudge;
        }

        public override bool HasShadow() => true;
    }
}
