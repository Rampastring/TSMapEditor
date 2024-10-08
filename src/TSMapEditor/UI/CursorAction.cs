using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations;

namespace TSMapEditor.UI
{
    public abstract class CursorAction
    {
        public CursorAction(ICursorActionTarget cursorActionTarget)
        {
            CursorActionTarget = cursorActionTarget;
        }

        /// <summary>
        /// Raised when the cursor action is exited. 
        /// Typically this happens through the user right-clicking or 
        /// through the activation of a different cursor action.
        /// </summary>
        public event EventHandler ActionExited;

        /// <summary>
        /// Raised when the action itself wants it to be disabled.
        /// </summary>
        public event EventHandler OnExitingAction;

        public void OnExit()
        {
            OnActionExit();
            ActionExited?.Invoke(this, EventArgs.Empty);
        }

        public void ExitAction() => OnExitingAction?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Override in derived classes to enable this cursor action to receive
        /// keyboard events through <see cref="OnKeyPressed"/>.
        /// </summary>
        public virtual bool HandlesKeyboardInput => false;

        /// <summary>
        /// Override in derived classes to enable the cell cursor to be drawn
        /// while this cursor action is active.
        /// </summary>
        public virtual bool DrawCellCursor => false;

        /// <summary>
        /// Override in derived classes to disable "see-through" cell cursor behaviour.
        /// "See-through" behaviour allows the cursor action to reach cells behind "walls"
        /// in the game world, such as cliffs.
        /// </summary>
        public virtual bool SeeThrough => true;

        public abstract string GetName();

        protected Map Map => CursorActionTarget.Map;

        protected ICursorActionTarget CursorActionTarget { get; }

        protected IMutationTarget MutationTarget => CursorActionTarget.MutationTarget;

        protected bool Is2DMode => CursorActionTarget.Is2DMode;

        protected RKeyboard Keyboard => CursorActionTarget.WindowManager.Keyboard;

        protected void PerformMutation(Mutation mutation) => CursorActionTarget.MutationManager.PerformMutation(mutation);

        /// <summary>
        /// Called when the action is activated (when it becomes the cursor action that the user is using).
        /// </summary>
        public virtual void OnActionEnter() { }

        public virtual void OnActionExit() { }

        /// <summary>
        /// Called when a keyboard key is pressed while the cursor action is active.
        /// </summary>
        /// <param name="e">The key press event from the XNAUI library.</param>
        /// <param name="cellCoords">Coordinates of the cell under the cursor.</param>
        public virtual void OnKeyPressed(Rampastring.XNAUI.Input.KeyPressEventArgs e, Point2D cellCoords) { }

        /// <summary>
        /// Called prior to drawing the map.
        /// </summary>
        /// <param name="cellCoords">The coords of the cell under the cursor.</param>
        public virtual void PreMapDraw(Point2D cellCoords) { }

        /// <summary>
        /// Called after drawing the map.
        /// Override in derived classes to clear preview data related to the action.
        /// </summary>
        /// <param name="cellCoords">The coords of the cell under the cursor.</param>
        public virtual void PostMapDraw(Point2D cellCoords) { }

        /// <summary>
        /// Called when the mouse is moved on the map with the left mouse button down while this action is active.
        /// </summary>
        /// <param name="cellCoords">The coords of the cell under the cursor.</param>
        public virtual void LeftDown(Point2D cellCoords) { }

        /// <summary>
        /// Called when the mouse is moved on the map with the left mouse button up while this action is active.
        /// </summary>
        /// <param name="cellCoords">The coords of the cell under the cursor.</param>
        public virtual void LeftUpOnMouseMove(Point2D cellCoords) { }

        /// <summary>
        /// Called when the left mouse button is clicked (pressed and released) on the map with this action us active.
        /// </summary>
        /// <param name="cellCoords">The coords of the cell under the cursor.</param>
        public virtual void LeftClick(Point2D cellCoords) { }

        /// <summary>
        /// Called after drawing the map.
        /// Override in derived classes to draw on top of the map texture.
        /// </summary>
        /// <param name="cellCoords">The coords of the cell under the cursor.</param>
        /// <param name="cameraTopLeftPoint">The top-left point of the user's screen.</param>
        public virtual void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint) { }

        protected void DrawText(Point2D cellCoords, Point2D cameraTopLeftPoint, int xOffset, int yOffset, string text, Color textColor)
        {
            Point2D cellTopLeftPoint = CellMath.CellTopLeftPointFromCellCoords(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint;
            cellTopLeftPoint = cellTopLeftPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            var textDimensions = Renderer.GetTextDimensions(text, Constants.UIBoldFont);
            int x = cellTopLeftPoint.X - (int)(textDimensions.X - Constants.CellSizeX) / 2;

            Vector2 textPosition = new Vector2(x + xOffset, cellTopLeftPoint.Y + yOffset);
            Rectangle textBackgroundRectangle = new Rectangle((int)textPosition.X - Constants.UIEmptySideSpace,
                (int)textPosition.Y - Constants.UIEmptyTopSpace,
                (int)textDimensions.X + Constants.UIEmptySideSpace * 2,
                (int)textDimensions.Y + Constants.UIEmptyBottomSpace + Constants.UIEmptyTopSpace);

            Renderer.FillRectangle(textBackgroundRectangle, UISettings.ActiveSettings.PanelBackgroundColor);
            Renderer.DrawRectangle(textBackgroundRectangle, UISettings.ActiveSettings.PanelBorderColor);

            Renderer.DrawStringWithShadow(text, Constants.UIBoldFont, textPosition, textColor);
        }
    }
}
