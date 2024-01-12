using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;
using TSMapEditor.Rendering;

namespace TSMapEditor.UI
{
    class OverlayFrameSelectorFrame
    {
        public OverlayFrameSelectorFrame(Point location, Point offset, Point size, Texture2D texture)
        {
            Location = location;
            Offset = offset;
            Size = size;
            Texture = texture;
        }

        public Point Location { get; set; }
        public Point Offset { get; set; }
        public Point Size { get; set; }
        public Texture2D Texture { get; set; }
    }

    public class OverlayFrameSelector : XNAPanel
    {
        private const int OVERLAY_FRAME_PADDING = 10;
        private const int SCROLL_RATE = 10;

        public OverlayFrameSelector(WindowManager windowManager, TheaterGraphics theaterGraphics, EditorState editorState) : base(windowManager)
        {
            this.theaterGraphics = theaterGraphics;
            this.editorState = editorState;
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
        }

        private readonly TheaterGraphics theaterGraphics;

        private readonly EditorState editorState;

        public event EventHandler SelectedFrameChanged;

        private int _selectedFrameIndex;
        public int SelectedFrameIndex
        {
            get => _selectedFrameIndex;
            set
            {
                if (_selectedFrameIndex != value)
                {
                    _selectedFrameIndex = value;
                    SelectedFrameChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private OverlayType overlayType;

        private List<OverlayFrameSelectorFrame> framesInView = new List<OverlayFrameSelectorFrame>();

        private int viewY = 0;

        public override void Initialize()
        {
            base.Initialize();

            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 196), 2, 2);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            KeyboardCommands.Instance.NextTile.Triggered += NextTile_Triggered;
            KeyboardCommands.Instance.PreviousTile.Triggered += PreviousTile_Triggered;
            WindowManager.RenderResolutionChanged += WindowManager_RenderResolutionChanged;
        }

        public override void Kill()
        {
            KeyboardCommands.Instance.NextTile.Triggered -= NextTile_Triggered;
            KeyboardCommands.Instance.PreviousTile.Triggered -= PreviousTile_Triggered;
            WindowManager.RenderResolutionChanged -= WindowManager_RenderResolutionChanged;
            base.Kill();
        }

        private void NextTile_Triggered(object sender, EventArgs e)
        {
            NextOverlayFrame();
        }

        private void PreviousTile_Triggered(object sender, EventArgs e)
        {
            PreviousOverlayFrame();
        }

        private void WindowManager_RenderResolutionChanged(object sender, EventArgs e)
        {
            Width = WindowManager.RenderResolutionX - X;
            Y = WindowManager.RenderResolutionY - Height;
        }

        /// <summary>
        /// Handles the "Next Tile" keyboard command.
        /// </summary>
        private void NextOverlayFrame()
        {
            if (!Enabled)
                return;

            int selectedFrameIndex = SelectedFrameIndex;

            if (selectedFrameIndex < 0)
            {
                // If no frame is selected, then select the first frame

                if (framesInView.Count > 0)
                    SelectedFrameIndex = 0;

                return;
            }

            if (Keyboard.IsAltHeldDown())
                selectedFrameIndex += 5;
            else
                selectedFrameIndex++;

            // Don't cross bounds
            if (selectedFrameIndex >= framesInView.Count)
                selectedFrameIndex = framesInView.Count - 1;

            SelectedFrameIndex = selectedFrameIndex;
        }

        /// <summary>
        /// Handles the "Previous Tile" keyboard command.
        /// </summary>
        private void PreviousOverlayFrame()
        {
            if (!Enabled)
                return;

            if (SelectedFrameIndex < 0)
            {
                // If no frame is selected, then select the last frame

                if (framesInView.Count > 0)
                    SelectedFrameIndex = framesInView.Count - 1;

                return;
            }

            int selectedFrameIndex = SelectedFrameIndex;

            if (Keyboard.IsAltHeldDown())
                selectedFrameIndex -= 5;
            else
                selectedFrameIndex--;

            // Don't cross bounds
            if (selectedFrameIndex < 0)
                selectedFrameIndex = 0;

            SelectedFrameIndex = selectedFrameIndex;
        }

        protected override void OnClientRectangleUpdated()
        {
            base.OnClientRectangleUpdated();

            RefreshGraphics();
        }

        public void SetOverlayType(OverlayType overlayType)
        {
            viewY = 0;
            if (this.overlayType != overlayType)
            {
                this.overlayType = overlayType;
                RefreshGraphics();
                SelectedFrameIndex = 0;
            }
        }

        private void RefreshGraphics()
        {
            viewY = 0;
            framesInView.Clear();

            if (overlayType == null)
                return;

            var textures = theaterGraphics.OverlayTextures[overlayType.Index];
            if (textures == null)
                return;

            var tilesOnCurrentLine = new List<OverlayFrameSelectorFrame>();
            int usableWidth = Width - (Constants.UIEmptySideSpace * 2);
            int y = Constants.UIEmptyTopSpace;
            int x = Constants.UIEmptySideSpace;
            int currentLineHeight = 0;

            int overlayFrameCount = theaterGraphics.GetOverlayFrameCount(overlayType);

            for (int i = 0; i < overlayFrameCount; i++)
            {
                var frame = textures.GetFrame(i);

                if (frame == null)
                    break;

                int width = frame.Texture.Width;
                int height = frame.Texture.Height;

                if (x + width > usableWidth)
                {
                    // Start a new line of tile graphics

                    x = Constants.UIEmptySideSpace;
                    y += currentLineHeight + OVERLAY_FRAME_PADDING;
                    CenterLine(tilesOnCurrentLine, currentLineHeight);
                    currentLineHeight = 0;
                    tilesOnCurrentLine.Clear();
                }

                var tileDisplayTile = new OverlayFrameSelectorFrame(new Point(x, y), new Point(0, 0), new Point(width, height), frame.Texture);
                framesInView.Add(tileDisplayTile);

                if (height > currentLineHeight)
                    currentLineHeight = height;
                x += width + OVERLAY_FRAME_PADDING;
                tilesOnCurrentLine.Add(tileDisplayTile);
            }

            CenterLine(tilesOnCurrentLine, currentLineHeight);
        }

        /// <summary>
        /// Centers all tiles vertically relative to each other.
        /// </summary>
        private void CenterLine(List<OverlayFrameSelectorFrame> line, int lineHeight)
        {
            foreach (var tile in line)
            {
                tile.Location = new Point(tile.Location.X, tile.Location.Y + (lineHeight - tile.Size.Y) / 2);
            }
        }

        public override void OnMouseScrolled()
        {
            base.OnMouseScrolled();
            viewY += Cursor.ScrollWheelValue * SCROLL_RATE;
        }

        public override void OnMouseLeftDown()
        {
            base.OnMouseLeftDown();
            SelectedFrameIndex = GetFrameIndexUnderCursor();
        }

        private int GetFrameIndexUnderCursor()
        {
            if (!IsActive)
                return -1;

            Point cursorPoint = GetCursorPoint();

            for (int i = 0; i < framesInView.Count; i++)
            {
                var frame = framesInView[i];

                var rectangle = new Rectangle(frame.Location.X, frame.Location.Y + viewY, frame.Size.X, frame.Size.Y);
                if (rectangle.Contains(cursorPoint))
                    return i;
            }

            return -1;
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            for (int i = 0; i < framesInView.Count; i++)
            {
                var frame = framesInView[i];

                var rectangle = new Rectangle(frame.Location.X, frame.Location.Y + viewY, frame.Size.X, frame.Size.Y);
                FillRectangle(rectangle, Color.Black);

                if (frame.Texture == null)
                    continue;

                DrawTexture(frame.Texture, new Rectangle(frame.Location.X + frame.Offset.X,
                    viewY + frame.Location.Y + frame.Offset.Y, frame.Texture.Width, frame.Texture.Height), Color.White);

                if (i == SelectedFrameIndex)
                    DrawRectangle(rectangle, Color.Red, 2);
            }

            DrawChildren(gameTime);
            DrawPanelBorders();
        }
    }
}
