using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allows placing down terrain objects.
    /// </summary>
    public class TerrainObjectPlacementAction : CursorAction
    {
        public TerrainObjectPlacementAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Place Terrain Object";

        private TerrainObject terrainObject;

        private TerrainType _terrainType;

        public TerrainType TerrainType
        {
            get => _terrainType;
            set
            {
                if (_terrainType != value)
                {
                    _terrainType = value;

                    if (_terrainType == null)
                    {
                        terrainObject = null;
                    }
                    else
                    {
                        terrainObject = new TerrainObject(_terrainType);
                    }
                }
            }
        }

        public override void PreMapDraw(Point2D cellCoords)
        {
            var cell = CursorActionTarget.Map.GetTile(cellCoords);
            if (cell.TerrainObject == null)
            {
                terrainObject.Position = cell.CoordsToPoint();
                cell.TerrainObject = terrainObject;
            }

            CursorActionTarget.AddRefreshPoint(cellCoords);
        }

        public override void PostMapDraw(Point2D cellCoords)
        {
            var cell = CursorActionTarget.Map.GetTile(cellCoords);
            if (cell.TerrainObject == terrainObject)
            {
                cell.TerrainObject = null;
            }

            CursorActionTarget.AddRefreshPoint(cellCoords);
        }

        public override void LeftDown(Point2D cellCoords)
        {
            if (_terrainType == null)
                throw new InvalidOperationException(nameof(TerrainType) + " cannot be null");

            var cell = CursorActionTarget.Map.GetTile(cellCoords);
            if (cell.TerrainObject != null)
                return;

            var mutation = new PlaceTerrainObjectMutation(CursorActionTarget.MutationTarget, TerrainType, cellCoords);
            CursorActionTarget.MutationManager.PerformMutation(mutation);
        }

        public override void LeftClick(Point2D cellCoords) => LeftDown(cellCoords);
    }
}
