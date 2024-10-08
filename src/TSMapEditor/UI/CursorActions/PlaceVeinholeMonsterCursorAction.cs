using TSMapEditor.GameMath;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    public class PlaceVeinholeMonsterCursorAction : CursorAction
    {
        public PlaceVeinholeMonsterCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Place Veinhole Monster";

        public override bool DrawCellCursor => true;

        public override void LeftClick(Point2D cellCoords)
        {
            var cell = Map.GetTile(cellCoords);

            if (cell == null)
                return;

            if (cell.Overlay != null && cell.Overlay.OverlayType != null && (cell.Overlay.OverlayType.IsVeinholeMonster || cell.Overlay.OverlayType.IsVeins))
                return;

            // Check for morphable terrain around the cell
            if (!Constants.IsFlatWorld)
            {
                bool isMorphable = true;

                Map.DoForRectangle(cellCoords.X - 1, cellCoords.Y - 1, cellCoords.X + 1, cellCoords.Y + 1, t =>
                {
                    isMorphable = isMorphable && Map.TheaterInstance.Theater.TileSets[Map.TheaterInstance.GetTileSetId(t.TileIndex)].Morphable;
                });

                if (!isMorphable)
                    return;
            }

            CursorActionTarget.MutationManager.PerformMutation(new PlaceVeinholeMonsterMutation(MutationTarget, cellCoords));

            ExitAction();
        }
    }
}
