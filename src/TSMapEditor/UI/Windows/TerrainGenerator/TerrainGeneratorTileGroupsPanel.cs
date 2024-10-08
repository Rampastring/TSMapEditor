using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.TerrainGenerator
{
    /// <summary>
    /// A panel that allows the user to customize how the terrain 
    /// generator places terrain tiles on the map.
    /// </summary>
    public class TerrainGeneratorTileGroupsPanel : EditorPanel
    {
        private const int MaxTileGroupCount = 8;

        public TerrainGeneratorTileGroupsPanel(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorPopUpSelector[] tileSetSelectors;
        private EditorTextBox[] tileIndices;
        private EditorNumberTextBox[] tileGroupOpenChances;
        private EditorNumberTextBox[] tileGroupOccupiedChances;

        private SelectTileSetWindow selectTileSetWindow;

        private EditorPopUpSelector openedTileSetSelector;

        public override void Initialize()
        {
            tileSetSelectors = new EditorPopUpSelector[MaxTileGroupCount];
            tileIndices = new EditorTextBox[MaxTileGroupCount];
            tileGroupOpenChances = new EditorNumberTextBox[MaxTileGroupCount];
            tileGroupOccupiedChances = new EditorNumberTextBox[MaxTileGroupCount];

            selectTileSetWindow = new SelectTileSetWindow(WindowManager, map);
            var tileSetDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent.Parent, selectTileSetWindow);
            tileSetDarkeningPanel.Hidden += TileSetDarkeningPanel_Hidden;

            int y = Constants.UIEmptyTopSpace;

            for (int i = 0; i < MaxTileGroupCount; i++)
            {
                var lblTileSet = new XNALabel(WindowManager);
                lblTileSet.Name = nameof(lblTileSet) + i;
                lblTileSet.X = Constants.UIEmptySideSpace;
                lblTileSet.Y = y;
                lblTileSet.FontIndex = Constants.UIBoldFont;
                lblTileSet.Text = $"Tile Set (Group #{i + 1})";
                AddChild(lblTileSet);

                var selTileSet = new EditorPopUpSelector(WindowManager);
                selTileSet.Name = nameof(selTileSet) + i;
                selTileSet.X = lblTileSet.X;
                selTileSet.Y = lblTileSet.Bottom + Constants.UIVerticalSpacing;
                selTileSet.Width = 200;
                AddChild(selTileSet);
                tileSetSelectors[i] = selTileSet;
                selTileSet.LeftClick += SelTileSet_LeftClick;

                var lblTileIndices = new XNALabel(WindowManager);
                lblTileIndices.Name = nameof(lblTileIndices) + i;
                lblTileIndices.X = selTileSet.Right + Constants.UIHorizontalSpacing;
                lblTileIndices.Y = lblTileSet.Y;
                lblTileIndices.Text = $"Indexes of tiles to place (leave blank for all)";
                AddChild(lblTileIndices);

                var tbTileIndices = new EditorTextBox(WindowManager);
                tbTileIndices.Name = nameof(selTileSet) + i;
                tbTileIndices.X = lblTileIndices.X;
                tbTileIndices.Y = lblTileIndices.Bottom + Constants.UIVerticalSpacing;
                tbTileIndices.Width = 280;
                AddChild(tbTileIndices);
                tileIndices[i] = tbTileIndices;

                var lblOpenChance = new XNALabel(WindowManager);
                lblOpenChance.Name = nameof(lblOpenChance) + i;
                lblOpenChance.X = tbTileIndices.Right + Constants.UIHorizontalSpacing;
                lblOpenChance.Y = lblTileSet.Y;
                lblOpenChance.Text = "Open cell chance:";
                AddChild(lblOpenChance);

                var tbOpenChance = new EditorNumberTextBox(WindowManager);
                tbOpenChance.Name = nameof(tbOpenChance) + i;
                tbOpenChance.X = lblOpenChance.X;
                tbOpenChance.Y = selTileSet.Y;
                tbOpenChance.AllowDecimals = true;
                tbOpenChance.Width = 120;
                AddChild(tbOpenChance);
                tileGroupOpenChances[i] = tbOpenChance;

                var lblOccupiedChance = new XNALabel(WindowManager);
                lblOccupiedChance.Name = nameof(lblOccupiedChance) + i;
                lblOccupiedChance.X = tbOpenChance.Right + Constants.UIHorizontalSpacing;
                lblOccupiedChance.Y = lblOpenChance.Y;
                lblOccupiedChance.Text = "Occupied cell chance:";
                AddChild(lblOccupiedChance);

                var tbOccupiedChance = new EditorNumberTextBox(WindowManager);
                tbOccupiedChance.Name = nameof(tbOpenChance) + i;
                tbOccupiedChance.X = lblOccupiedChance.X;
                tbOccupiedChance.Y = selTileSet.Y;
                tbOccupiedChance.AllowDecimals = true;
                tbOccupiedChance.Width = Width - tbOccupiedChance.X - Constants.UIEmptySideSpace;
                AddChild(tbOccupiedChance);
                tileGroupOccupiedChances[i] = tbOccupiedChance;

                y = tbOccupiedChance.Bottom + Constants.UIEmptyTopSpace;
            }

            base.Initialize();
        }

        private void TileSetDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            openedTileSetSelector.Tag = selectTileSetWindow.SelectedObject;

            if (selectTileSetWindow.SelectedObject == null)
                openedTileSetSelector.Text = string.Empty;
            else
                openedTileSetSelector.Text = selectTileSetWindow.SelectedObject.SetName;
        }

        private void SelTileSet_LeftClick(object sender, System.EventArgs e)
        {
            openedTileSetSelector = (EditorPopUpSelector)sender;
            selectTileSetWindow.Open((TileSet)openedTileSetSelector.Tag);
        }

        public List<TerrainGeneratorTileGroup> GetTileGroups()
        {
            var tileGroups = new List<TerrainGeneratorTileGroup>();

            for (int i = 0; i < tileSetSelectors.Length; i++)
            {
                var tileSet = (TileSet)tileSetSelectors[i].Tag;
                if (tileSet == null)
                    continue;

                List<int> tileIndexesInSet = null;
                string tileIndicesText = tileIndices[i].Text.Trim();
                if (!string.IsNullOrEmpty(tileIndicesText))
                {
                    string[] parts = tileIndicesText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    tileIndexesInSet = parts.Select(str => Conversions.IntFromString(str, -1)).ToList();
                    int invalidElement = tileIndexesInSet.Find(index => index <= -1 || index >= tileSet.LoadedTileCount);

                    if (invalidElement != 0) // this can never be 0 if an invalid element exists, because each valid tileset has at least 1 tile
                    {
                        EditorMessageBox.Show(WindowManager, "Generator Config Error",
                            $"Tile with index '{ invalidElement }' does not exist in tile set '{ tileSet.SetName }'!", MessageBoxButtons.OK);
                        return null;
                    }
                }

                var tileGroup = new TerrainGeneratorTileGroup(tileSet, tileIndexesInSet,
                    tileGroupOpenChances[i].DoubleValue,
                    tileGroupOccupiedChances[i].DoubleValue);

                tileGroups.Add(tileGroup);
            }

            return tileGroups;
        }

        public void LoadConfig(TerrainGeneratorConfiguration configuration)
        {
            for (int i = 0; i < configuration.TileGroups.Count && i < MaxTileGroupCount; i++)
            {
                var tileGroup = configuration.TileGroups[i];

                tileSetSelectors[i].Text = tileGroup.TileSet.SetName;
                tileSetSelectors[i].Tag = tileGroup.TileSet;

                if (tileGroup.TileIndicesInSet == null)
                    tileIndices[i].Text = string.Empty;
                else
                    tileIndices[i].Text = string.Join(",", tileGroup.TileIndicesInSet.Select(index => index.ToString(CultureInfo.InvariantCulture)));

                tileGroupOpenChances[i].DoubleValue = tileGroup.OpenChance;
                tileGroupOccupiedChances[i].DoubleValue = tileGroup.OverlapChance;
            }

            for (int i = configuration.TileGroups.Count; i < tileSetSelectors.Length; i++)
            {
                tileSetSelectors[i].Text = string.Empty;
                tileSetSelectors[i].Tag = null;
                tileIndices[i].Text = string.Empty;
                tileGroupOpenChances[i].DoubleValue = 0.0;
                tileGroupOccupiedChances[i].DoubleValue = 0.0;
            }
        }
    }
}
