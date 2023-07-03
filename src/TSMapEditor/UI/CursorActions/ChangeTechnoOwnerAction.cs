using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Rendering;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allows changing the owner of a techno object.
    /// </summary>
    public class ChangeTechnoOwnerAction : CursorAction
    {
        public ChangeTechnoOwnerAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Change Object Owner";

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            House newOwner = CursorActionTarget.MutationTarget.ObjectOwner;

            MapTile tile = CursorActionTarget.Map.GetTile(cellCoords);
            Point2D cellTopLeftPoint = CellMath.CellTopLeftPointFromCellCoords(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint;
            cellTopLeftPoint.Y -= tile.Level * Constants.CellHeight;
            cellTopLeftPoint = cellTopLeftPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            Renderer.DrawTexture(
                CursorActionTarget.EditorGraphics.GenericTileTexture,
                new Rectangle(cellTopLeftPoint.X, cellTopLeftPoint.Y, Constants.CellSizeX, Constants.CellSizeY),
                newOwner.XNAColor * 0.5f);

            if (tile.HasTechnoThatPassesCheck(techno => techno.Owner != newOwner))
            {
                const string text = "Change Owner";
                var textDimensions = Renderer.GetTextDimensions(text, Constants.UIBoldFont);
                int x = cellTopLeftPoint.X - (int)(textDimensions.X - Constants.CellSizeX) / 2;

                Renderer.DrawStringWithShadow(text,
                    Constants.UIBoldFont,
                    new Vector2(x, cellTopLeftPoint.Y),
                    newOwner.XNAColor);
            }
        }

        public override void LeftClick(Point2D cellCoords)
        {
            House newOwner = CursorActionTarget.MutationTarget.ObjectOwner;
            MapTile tile = CursorActionTarget.Map.GetTile(cellCoords);
            TechnoBase targetObject = tile.GetFirstTechnoThatPassesCheck(techno => techno.Owner != newOwner);
            if (targetObject != null)
            {
                var mutation = new ChangeTechnoOwnerMutation(targetObject, newOwner, CursorActionTarget.MutationTarget);
                CursorActionTarget.MutationManager.PerformMutation(mutation);
            }
        }
    }
}
