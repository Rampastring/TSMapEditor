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
        public OverlayFrameSelectorFrame(Point location, Point offset, Point size, int overlayFrameIndex, Texture2D texture)
        {
            Location = location;
            Offset = offset;
            Size = size;
            OverlayFrameIndex = overlayFrameIndex;
            Texture = texture;
        }

        public Point Location { get; set; }
        public Point Offset { get; set; }
        public Point Size { get; set; }
        public int OverlayFrameIndex { get; set; }
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

        private int _selectedArrayIndex;
        private int SelectedArrayIndex
        {
            get => _selectedArrayIndex;
            set
            {
                if (_selectedArrayIndex != value)
                {
                    _selectedArrayIndex = value;
                }

                SelectedFrameIndex = _selectedArrayIndex < 0 || _selectedArrayIndex >= framesInView.Count ?
                    -1 : framesInView[_selectedArrayIndex].OverlayFrameIndex;
            }
        }

        private int _selectedFrameIndex = -1;

        public int SelectedFrameIndex
        {
            get
            {
                if (SelectedArrayIndex < 0 || SelectedArrayIndex >= framesInView.Count)
                    return -1;

                return framesInView[SelectedArrayIndex].OverlayFrameIndex;
            }
            set
            {
                if (value != _selectedFrameIndex)
                {
                    _selectedFrameIndex = value;
                    SelectedFrameChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private OverlayType overlayType;

        private List<OverlayFrameSelectorFrame> framesInView = new List<OverlayFrameSelectorFrame>();

        private int viewY = 0;

        private Effect palettedDrawEffect;

        private ShapeImage currentOverlayShape;

        public override void Initialize()
        {
            base.Initialize();

            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 196), 2, 2);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            palettedDrawEffect = AssetLoader.LoadEffect("Shaders/PalettedDrawNoDepth");

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

            int selectedArrayIndex = SelectedArrayIndex;

            if (selectedArrayIndex < 0)
            {
                // If no frame is selected, then select the first frame

                if (framesInView.Count > 0)
                    SelectedArrayIndex = 0;

                return;
            }

            if (Keyboard.IsAltHeldDown())
                selectedArrayIndex += 5;
            else
                selectedArrayIndex++;

            // Don't cross bounds
            if (selectedArrayIndex >= framesInView.Count)
                selectedArrayIndex = framesInView.Count - 1;

            SelectedArrayIndex = selectedArrayIndex;
        }

        /// <summary>
        /// Handles the "Previous Tile" keyboard command.
        /// </summary>
        private void PreviousOverlayFrame()
        {
            if (!Enabled)
                return;

            if (SelectedArrayIndex < 0)
            {
                // If no frame is selected, then select the last frame

                if (framesInView.Count > 0)
                    SelectedArrayIndex = framesInView.Count - 1;

                return;
            }

            int selectedArrayIndex = SelectedArrayIndex;

            if (Keyboard.IsAltHeldDown())
                selectedArrayIndex -= 5;
            else
                selectedArrayIndex--;

            // Don't cross bounds
            if (selectedArrayIndex < 0)
                selectedArrayIndex = 0;

            SelectedArrayIndex = selectedArrayIndex;
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
                SelectedArrayIndex = 0;
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

            currentOverlayShape = textures;

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
                    continue;

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

                var tileDisplayTile = new OverlayFrameSelectorFrame(new Point(x, y), new Point(0, 0), new Point(width, height), i, frame.Texture);
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
            SelectedArrayIndex = GetFrameIndexUnderCursor();
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

        private void SetOverlayRenderSettings()
        {
            Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, palettedDrawEffect));
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            for (int i = 0; i < framesInView.Count; i++)
            {
                var frame = framesInView[i];

                var rectangle = new Rectangle(frame.Location.X, frame.Location.Y + viewY, frame.Size.X, frame.Size.Y);
                FillRectangle(rectangle, Color.Black);
            }

            SetOverlayRenderSettings();

            for (int i = 0; i < framesInView.Count; i++)
            {
                var frame = framesInView[i];

                if (frame.Texture == null)
                    continue;

                palettedDrawEffect.Parameters["PaletteTexture"].SetValue(currentOverlayShape.GetPaletteTexture());
                palettedDrawEffect.Parameters["Lighting"].SetValue(Vector4.One);

                DrawTexture(frame.Texture, new Rectangle(frame.Location.X + frame.Offset.X,
                    viewY + frame.Location.Y + frame.Offset.Y, frame.Texture.Width, frame.Texture.Height), Color.White);
            }

            Renderer.PopSettings();

            // Draw frame selection rectangle
            if (SelectedArrayIndex > -1 && SelectedArrayIndex < framesInView.Count)
            {
                var frame = framesInView[SelectedArrayIndex];
                var rectangle = new Rectangle(frame.Location.X, frame.Location.Y + viewY, frame.Size.X, frame.Size.Y);
                DrawRectangle(rectangle, Color.Red, 2);
            }

            DrawChildren(gameTime);
            DrawPanelBorders();
        }
    }
}
