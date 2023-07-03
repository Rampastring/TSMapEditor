using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.Rendering;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.UI
{
    class TileDisplayTile
    {
        public TileDisplayTile(Point location, Point offset, Point size, TileImage tileImage)
        {
            Location = location;
            Offset = offset;
            Size = size;
            TileImage = tileImage;
        }

        public Point Location { get; set; }
        public Point Offset { get; set; }
        public Point Size { get; set; }
        public TileImage TileImage { get; set; }
    }

    public class TileDisplay : XNAPanel
    {
        private const int TILE_PADDING = 3;
        private const int SCROLL_RATE = 10;

        public TileDisplay(WindowManager windowManager, TheaterGraphics theaterGraphics, PlaceTerrainCursorAction placeTerrainCursorAction) : base(windowManager)
        {
            this.theaterGraphics = theaterGraphics;
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
            placeTerrainCursorAction.ActionExited += (s, e) => _selectedTile = null;
        }

        public event EventHandler SelectedTileChanged;

        private TileImage _selectedTile;
        public TileImage SelectedTile
        {
            get => _selectedTile;
            set
            {
                if (_selectedTile != value)
                {
                    _selectedTile = value;
                    SelectedTileChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private readonly TheaterGraphics theaterGraphics;

        private TileSet tileSet;

        private List<TileDisplayTile> tilesInView = new List<TileDisplayTile>();

        private int viewY = 0;

        private bool isMarbleMadness = false;

        public override void Initialize()
        {
            base.Initialize();

            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 196), 2, 2);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            KeyboardCommands.Instance.NextTile.Action = NextTile;
            KeyboardCommands.Instance.PreviousTile.Action = PreviousTile;
            KeyboardCommands.Instance.FrameworkMode.Triggered += OnMarbleMadnessChanged;
        }

        /// <summary>
        /// Handles the "Next Tile" keyboard command.
        /// </summary>
        private void NextTile()
        {
            if (!AppliesToSelfAndAllParents(c => c.Enabled))
                return;

            int selectedTileIndex = tilesInView.FindIndex(t => t.TileImage == SelectedTile);

            if (SelectedTile == null || selectedTileIndex < 0)
            {
                // If no tile from the current tileset is selected, then select the first tile

                if (tilesInView.Count > 0)
                    SelectedTile = tilesInView[0].TileImage;

                return;
            }

            if (Keyboard.IsAltHeldDown())
                selectedTileIndex += 5;
            else
                selectedTileIndex++;

            // Don't cross bounds
            if (selectedTileIndex >= tilesInView.Count)
                selectedTileIndex = tilesInView.Count - 1;

            SelectedTile = tilesInView[selectedTileIndex].TileImage;
        }

        /// <summary>
        /// Handles the "Previous Tile" keyboard command.
        /// </summary>
        private void PreviousTile()
        {
            if (!AppliesToSelfAndAllParents(c => c.Enabled))
                return;

            int selectedTileIndex = tilesInView.FindIndex(t => t.TileImage == SelectedTile);

            if (SelectedTile == null || selectedTileIndex < 0)
            {
                // If no tile from the current tileset is selected, then select the last tile

                if (tilesInView.Count > 0)
                    SelectedTile = tilesInView[tilesInView.Count - 1].TileImage;

                return;
            }

            if (Keyboard.IsAltHeldDown())
                selectedTileIndex -= 5;
            else
                selectedTileIndex--;

            // Don't cross bounds
            if (selectedTileIndex < 0)
                selectedTileIndex = 0;

            SelectedTile = tilesInView[selectedTileIndex].TileImage;
        }

        protected override void OnClientRectangleUpdated()
        {
            base.OnClientRectangleUpdated();

            RefreshGraphics();
        }

        public void SetTileSet(TileSet tileSet)
        {
            viewY = 0;
            this.tileSet = tileSet;
            RefreshGraphics();
        }

        private void RefreshGraphics()
        {
            viewY = 0;
            tilesInView.Clear();

            if (tileSet == null)
                return;

            var tilesOnCurrentLine = new List<TileDisplayTile>();
            int usableWidth = Width - (Constants.UIEmptySideSpace * 2);
            int y = Constants.UIEmptyTopSpace;
            int x = Constants.UIEmptySideSpace;
            int currentLineHeight = 0;

            for (int i = 0; i < tileSet.TilesInSet; i++)
            {
                int tileIndex = tileSet.StartTileIndex + i;
                if (tileIndex > theaterGraphics.TileCount)
                    break;

                TileImage tileImage;
                if (isMarbleMadness)
                    tileImage = theaterGraphics.GetMarbleMadnessTileGraphics(tileIndex);
                else
                    tileImage = theaterGraphics.GetTileGraphics(tileIndex);
                if (tileImage == null)
                    break;

                int width = tileImage.GetWidth(out int minX);
                int height = tileImage.GetHeight();
                int yOffset = tileImage.GetYOffset();

                if (x + width > usableWidth)
                {
                    // Start a new line of tile graphics

                    x = Constants.UIEmptySideSpace;
                    y += currentLineHeight + TILE_PADDING;
                    CenterLine(tilesOnCurrentLine, currentLineHeight);
                    currentLineHeight = 0;
                    tilesOnCurrentLine.Clear();
                }

                if (minX > 0)
                    minX = 0;

                var tileDisplayTile = new TileDisplayTile(new Point(x, y), new Point(-minX, yOffset), new Point(width, height), tileImage);
                tilesInView.Add(tileDisplayTile);

                if (height > currentLineHeight)
                    currentLineHeight = height;
                x += width + TILE_PADDING;
                tilesOnCurrentLine.Add(tileDisplayTile);
            }

            CenterLine(tilesOnCurrentLine, currentLineHeight);
        }

        /// <summary>
        /// Centers all tiles vertically relative to each other.
        /// </summary>
        private void CenterLine(List<TileDisplayTile> line, int lineHeight)
        {
            foreach (var tile in line)
            {
                tile.Location = new Point(tile.Location.X, tile.Location.Y + (lineHeight - tile.TileImage.GetHeight()) / 2);
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
            SelectedTile = GetTileUnderCursor()?.TileImage;
        }

        private TileDisplayTile GetTileUnderCursor()
        {
            if (!IsActive)
                return null;

            Point cursorPoint = GetCursorPoint();

            foreach (var tile in tilesInView)
            {
                var rectangle = new Rectangle(tile.Location.X, tile.Location.Y + viewY, tile.Size.X, tile.Size.Y);
                if (rectangle.Contains(cursorPoint))
                    return tile;
            }

            return null;
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            foreach (var tile in tilesInView)
            {
                var rectangle = new Rectangle(tile.Location.X, tile.Location.Y + viewY, tile.Size.X, tile.Size.Y);
                FillRectangle(rectangle, Color.Black);

                if (tile.TileImage.TMPImages.Length == 0)
                    continue;

                foreach (MGTMPImage image in tile.TileImage.TMPImages)
                {
                    if (image == null || image.TmpImage == null)
                        continue;

                    int subTileHeightOffset = image.TmpImage.Height * Constants.CellHeight;

                    DrawTexture(image.Texture, new Rectangle(tile.Location.X + image.TmpImage.X + tile.Offset.X,
                        viewY + tile.Location.Y + image.TmpImage.Y + tile.Offset.Y - subTileHeightOffset,
                        Constants.CellSizeX, Constants.CellSizeY), Color.White);

                    if (image.ExtraTexture != null)
                    {
                        DrawTexture(image.ExtraTexture, new Rectangle(tile.Location.X + image.TmpImage.XExtra + tile.Offset.X,
                            viewY + tile.Location.Y + image.TmpImage.YExtra + tile.Offset.Y - subTileHeightOffset,
                            image.ExtraTexture.Width, image.ExtraTexture.Height), Color.White);
                    }
                }

                if (tile.TileImage == SelectedTile)
                    DrawRectangle(rectangle, Color.Red, 2);
            }

            DrawChildren(gameTime);
            DrawPanelBorders();
        }

        public void OnMarbleMadnessChanged(object sender, EventArgs e)
        {
            isMarbleMadness = !isMarbleMadness;
            RefreshGraphics();
        }

        public override void Kill()
        {
            KeyboardCommands.Instance.FrameworkMode.Triggered -= OnMarbleMadnessChanged;

            base.Kill();
        }
    }
}
