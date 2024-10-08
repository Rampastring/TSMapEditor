using TSMapEditor.GameMath;
using TSMapEditor.Mutations.Classes.HeightMutations;

namespace TSMapEditor.UI.CursorActions.HeightActions
{
    /// <summary>
    /// A cursor action that allows the user to "flatten" ground by holding
    /// the left mouse button and moving it over the map.
    /// 
    /// The cell that the cursor first was on when the user started holding
    /// the mouse button tells which height level which gets assigned to
    /// all other cells that the cursor passes through.
    /// </summary>
    public class FlattenGroundCursorAction : CursorAction
    {
        public FlattenGroundCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        private int desiredHeightLevel = -1;

        public override string GetName() => "Flatten Ground";

        public override void OnActionEnter()
        {
            CursorActionTarget.BrushSize = Map.EditorConfig.BrushSizes.Find(bs => bs.Width == 2 && bs.Height == 2) ?? Map.EditorConfig.BrushSizes[0];
        }

        public override bool DrawCellCursor => true;

        public override bool SeeThrough => false;

        public override void LeftDown(Point2D cellCoords)
        {
            var cell = Map.GetTile(cellCoords);
            if (cell == null)
                return;

            if (desiredHeightLevel == -1)
            {
                // This is the first cell that the user is holding the cursor on,
                // meaning it's the origin cell that determines the height level
                // that the user wants to assign to other cells
                desiredHeightLevel = cell.Level;
                return;
            }

            // Check the area of the brush on whether it has any cells that do not match
            // the height of the origin cell.

            int xSize = CursorActionTarget.BrushSize.Width;
            int ySize = CursorActionTarget.BrushSize.Height;

            int beginY = cellCoords.Y - (ySize - 1) / 2;
            int endY = cellCoords.Y + ySize / 2;
            int beginX = cellCoords.X - (xSize - 1) / 2;
            int endX = cellCoords.X + xSize / 2;

            bool perform = false;

            for (int y = beginY; y <= endY; y++)
            {
                for (int x = beginX; x <= endX; x++)
                {
                    var targetCellCoords = new Point2D(x, y);
                    var targetCell = Map.GetTile(targetCellCoords);

                    if (targetCell != null && targetCell.Level != desiredHeightLevel)
                    {
                        perform = true;
                        break;
                    }
                }
            }

            if (!perform)
                return;

            // Don't act on non-morphable terrain
            if (!Map.TheaterInstance.Theater.TileSets[Map.TheaterInstance.GetTileSetId(cell.TileIndex)].Morphable)
            {
                return;
            }

            CursorActionTarget.MutationManager.PerformMutation(new FlattenGroundMutation(MutationTarget, cellCoords, CursorActionTarget.BrushSize, desiredHeightLevel));
        }

        public override void LeftUpOnMouseMove(Point2D cellCoords)
        {
            desiredHeightLevel = -1;
        }
    }
}
