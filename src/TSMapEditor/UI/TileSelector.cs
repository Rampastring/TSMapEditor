using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.UI
{
    public class TileSelector : XNAControl
    {
        private const int TileSetListWidth = 180;
        private const int ResizeDragThreshold = 30;

        public TileSelector(WindowManager windowManager, Map map, TheaterGraphics theaterGraphics,
            PlaceTerrainCursorAction placeTerrainCursorAction, EditorState editorState) : base(windowManager)
        {
            this.map = map;
            this.theaterGraphics = theaterGraphics;
            this.placeTerrainCursorAction = placeTerrainCursorAction;
            this.editorState = editorState;
        }

        protected override void OnClientRectangleUpdated()
        {
            if (Initialized)
            {
                lbTileSetList.Height = Height - tbSearch.Bottom;
                lbTileSetList.Width = TileSetListWidth;
                TileDisplay.Height = Height;
                TileDisplay.Width = Width - TileSetListWidth;
            }

            base.OnClientRectangleUpdated();
        }

        private readonly Map map;
        private readonly TheaterGraphics theaterGraphics;
        private readonly PlaceTerrainCursorAction placeTerrainCursorAction;
        private readonly EditorState editorState;

        public TileDisplay TileDisplay { get; private set; }

        private EditorSuggestionTextBox tbSearch;
        private XNAListBox lbTileSetList;

        private bool isBeingDragged = false;
        private int previousMouseY;

        public override void Initialize()
        {
            Name = nameof(TileSelector);

            tbSearch = new EditorSuggestionTextBox(WindowManager);
            tbSearch.Name = nameof(tbSearch);
            tbSearch.Width = TileSetListWidth;
            tbSearch.Suggestion = "Search TileSet...";
            AddChild(tbSearch);
            UIHelpers.AddSearchTipsBoxToControl(tbSearch);
            tbSearch.TextChanged += TbSearch_TextChanged;

            lbTileSetList = new XNAListBox(WindowManager);
            lbTileSetList.Name = nameof(lbTileSetList);
            lbTileSetList.Y = tbSearch.Bottom;
            lbTileSetList.Height = Height - tbSearch.Bottom;
            lbTileSetList.Width = TileSetListWidth;
            lbTileSetList.SelectedIndexChanged += LbTileSetList_SelectedIndexChanged;
            AddChild(lbTileSetList);

            TileDisplay = new TileDisplay(WindowManager, map, theaterGraphics, placeTerrainCursorAction, editorState);
            TileDisplay.Name = nameof(TileDisplay);
            TileDisplay.Height = Height;
            TileDisplay.Width = Width - TileSetListWidth;
            TileDisplay.X = TileSetListWidth;
            AddChild(TileDisplay);

            lbTileSetList.BackgroundTexture = TileDisplay.BackgroundTexture;
            lbTileSetList.PanelBackgroundDrawMode = TileDisplay.PanelBackgroundDrawMode;

            base.Initialize();

            RefreshTileSets();

            KeyboardCommands.Instance.NextTileSet.Action = NextTileSet;
            KeyboardCommands.Instance.PreviousTileSet.Action = PreviousTileSet;
            WindowManager.RenderResolutionChanged += WindowManager_RenderResolutionChanged;
        }

        private void WindowManager_RenderResolutionChanged(object sender, EventArgs e)
        {
            Width = WindowManager.RenderResolutionX - X;
            Y = WindowManager.RenderResolutionY - Height;
        }

        public override void Kill()
        {
            WindowManager.RenderResolutionChanged -= WindowManager_RenderResolutionChanged;
            base.Kill();
        }

        private void TbSearch_TextChanged(object sender, EventArgs e)
        {
            lbTileSetList.ViewTop = 0;

            if (string.IsNullOrWhiteSpace(tbSearch.Text) || tbSearch.Text == tbSearch.Suggestion)
            {
                foreach (var item in lbTileSetList.Items)
                    item.Visible = true;
            }
            else
            {
                lbTileSetList.SelectedIndex = -1;

                for (int i = 0; i < lbTileSetList.Items.Count; i++)
                {
                    var item = lbTileSetList.Items[i];
                    item.Visible = item.Text.Contains(tbSearch.Text, StringComparison.OrdinalIgnoreCase);

                    if (item.Visible && lbTileSetList.SelectedIndex == -1)
                        lbTileSetList.SelectedIndex = i;
                }
            }

            lbTileSetList.RefreshScrollbar();
        }

        private void NextTileSet()
        {
            if (lbTileSetList.Items.Count == 0)
                return;

            if (lbTileSetList.SelectedItem == null)
                lbTileSetList.SelectedIndex = 0;

            if (lbTileSetList.SelectedIndex == lbTileSetList.Items.Count - 1)
                return;

            lbTileSetList.SelectedIndex++;
        }

        private void PreviousTileSet()
        {
            if (lbTileSetList.Items.Count == 0)
                return;

            if (lbTileSetList.SelectedItem == null)
                lbTileSetList.SelectedIndex = lbTileSetList.Items.Count - 1;

            if (lbTileSetList.SelectedIndex == 0)
                return;

            lbTileSetList.SelectedIndex--;
        }

        public override void OnMouseLeftDown()
        {
            if (IsActive)
            {
                var cursorPoint = GetCursorPoint();

                if (!isBeingDragged && cursorPoint.Y > 0 && cursorPoint.Y < ResizeDragThreshold && Cursor.LeftDown)
                {
                    isBeingDragged = true;
                    previousMouseY = GetCursorPoint().Y;
                }
            }

            base.OnMouseLeftDown();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (isBeingDragged)
            {
                var cursorPoint = GetCursorPoint();

                if (cursorPoint.Y < previousMouseY)
                {
                    int difference = previousMouseY - cursorPoint.Y;
                    Y -= difference;
                    Height += difference;

                    if (Height > WindowManager.RenderResolutionY)
                    {
                        Height = WindowManager.RenderResolutionY;
                        Y = 0;
                    }
                }
                else if (cursorPoint.Y > previousMouseY)
                {
                    int difference = cursorPoint.Y - previousMouseY;
                    Y += difference;
                    Height -= difference;

                    if (Height <= 10)
                    {
                        Height = 10;
                        Y = WindowManager.RenderResolutionY - ScaledHeight;
                    }
                }

                previousMouseY = GetCursorPoint().Y;

                if (!Cursor.LeftDown)
                    isBeingDragged = false;
            }
        }

        private void LbTileSetList_SelectedIndexChanged(object sender, EventArgs e)
        {
            TileSet tileSet = null;
            if (lbTileSetList.SelectedItem != null)
                tileSet = lbTileSetList.SelectedItem.Tag as TileSet;

            TileDisplay.SetTileSet(tileSet);

            // Unselect the listbox
            if (WindowManager.SelectedControl == lbTileSetList)
                WindowManager.SelectedControl = null;
        }

        private void RefreshTileSets()
        {
            lbTileSetList.Clear();
            var sortedTileSets = theaterGraphics.Theater.TileSets.ToList(); // TODO sort tilesets

            foreach (TileSet tileSet in sortedTileSets)
            {
                if (tileSet.NonMarbleMadness > -1)
                    continue;

                if (tileSet.AllowToPlace && tileSet.LoadedTileCount > 0)
                {
                    lbTileSetList.AddItem(new XNAListBoxItem()
                    {
                        Text = tileSet.SetName,
                        Tag = tileSet,
                        TextColor = tileSet.Color.HasValue ? tileSet.Color.Value : UISettings.ActiveSettings.AltColor
                    });
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            FillRectangle(new Rectangle(0, 0, Width, ResizeDragThreshold), new Color(0, 0, 0, 64));
            DrawChildren(gameTime);
        }
    }
}
