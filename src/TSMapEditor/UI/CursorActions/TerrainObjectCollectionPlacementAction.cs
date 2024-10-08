using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    public class TerrainObjectCollectionPlacementAction : CursorAction
    {
        public TerrainObjectCollectionPlacementAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Place TerrainObject Collection";

        private TerrainObject terrainObject;
        private TerrainObjectCollection _terrainObjectCollection;
        public TerrainObjectCollection TerrainObjectCollection
        {
            get => _terrainObjectCollection;
            set
            {
                if (value.Entries.Length == 0)
                {
                    throw new InvalidOperationException($"Terrain object collection {value.Name} has no terrain object entries!");
                }

                _terrainObjectCollection = value;
                terrainObject = new TerrainObject(_terrainObjectCollection.Entries[0].TerrainType);
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
            var cell = CursorActionTarget.Map.GetTile(cellCoords);
            if (cell.TerrainObject != null)
                return;

            var mutation = new PlaceTerrainObjectCollectionMutation(CursorActionTarget.MutationTarget, TerrainObjectCollection, cellCoords);
            CursorActionTarget.MutationManager.PerformMutation(mutation);
        }

        public override void LeftClick(Point2D cellCoords) => LeftDown(cellCoords);
    }
}
