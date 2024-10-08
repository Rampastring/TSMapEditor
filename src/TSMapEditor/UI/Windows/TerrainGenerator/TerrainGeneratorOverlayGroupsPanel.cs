using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.TerrainGenerator
{
    /// <summary>
    /// A panel that allows the user to customize how the terrain 
    /// generator places overlay on the map.
    /// </summary>
    public class TerrainGeneratorOverlayGroupsPanel : EditorPanel
    {
        private const int MaxOverlayGroupCount = 8;

        public TerrainGeneratorOverlayGroupsPanel(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorTextBox[] overlayNames;
        private EditorTextBox[] frameIndices;
        private EditorNumberTextBox[] overlayGroupOpenChances;
        private EditorNumberTextBox[] overlayGroupOccupiedChances;

        public override void Initialize()
        {
            overlayNames = new EditorTextBox[MaxOverlayGroupCount];
            frameIndices = new EditorTextBox[MaxOverlayGroupCount];
            overlayGroupOpenChances = new EditorNumberTextBox[MaxOverlayGroupCount];
            overlayGroupOccupiedChances = new EditorNumberTextBox[MaxOverlayGroupCount];

            int y = Constants.UIEmptyTopSpace;

            for (int i = 0; i < MaxOverlayGroupCount; i++)
            {
                var lblTileSet = new XNALabel(WindowManager);
                lblTileSet.Name = nameof(lblTileSet) + i;
                lblTileSet.X = Constants.UIEmptySideSpace;
                lblTileSet.Y = y;
                lblTileSet.FontIndex = Constants.UIBoldFont;
                lblTileSet.Text = $"Overlay Type Name (Group #{i + 1})";
                AddChild(lblTileSet);

                var selTileSet = new EditorTextBox(WindowManager);
                selTileSet.Name = nameof(selTileSet) + i;
                selTileSet.X = lblTileSet.X;
                selTileSet.Y = lblTileSet.Bottom + Constants.UIVerticalSpacing;
                selTileSet.Width = 200;
                AddChild(selTileSet);
                overlayNames[i] = selTileSet;

                var lblTileIndices = new XNALabel(WindowManager);
                lblTileIndices.Name = nameof(lblTileIndices) + i;
                lblTileIndices.X = selTileSet.Right + Constants.UIHorizontalSpacing;
                lblTileIndices.Y = lblTileSet.Y;
                lblTileIndices.Text = $"Indexes of frames to place (leave blank for all)";
                AddChild(lblTileIndices);

                var tbTileIndices = new EditorTextBox(WindowManager);
                tbTileIndices.Name = nameof(selTileSet) + i;
                tbTileIndices.X = lblTileIndices.X;
                tbTileIndices.Y = lblTileIndices.Bottom + Constants.UIVerticalSpacing;
                tbTileIndices.Width = 280;
                AddChild(tbTileIndices);
                frameIndices[i] = tbTileIndices;

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
                overlayGroupOpenChances[i] = tbOpenChance;

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
                overlayGroupOccupiedChances[i] = tbOccupiedChance;

                y = tbOccupiedChance.Bottom + Constants.UIEmptyTopSpace;
            }

            base.Initialize();
        }

        public List<TerrainGeneratorOverlayGroup> GetOverlayGroups()
        {
            var overlayGroups = new List<TerrainGeneratorOverlayGroup>();

            for (int i = 0; i < overlayNames.Length; i++)
            {
                string overlayTypeName = overlayNames[i].Text;
                if (string.IsNullOrWhiteSpace(overlayTypeName))
                    continue;

                var overlayType = map.Rules.OverlayTypes.Find(ot => ot.ININame == overlayTypeName);
                if (overlayType == null)
                {
                    EditorMessageBox.Show(WindowManager, "Generator Config Error",
                        $"An overlay type named '{ overlayTypeName }' does not exist! Make sure you typed the overlay type's INI name and spelled it correctly.", MessageBoxButtons.OK);
                    return null;
                }

                List<int> frameIndexes = null;
                string frameIndexesText = frameIndices[i].Text.Trim();
                if (!string.IsNullOrEmpty(frameIndexesText))
                {
                    string[] parts = frameIndexesText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    frameIndexes = parts.Select(str => Conversions.IntFromString(str, -1)).ToList();
                    int invalidElement = frameIndexes.Find(index => index <= -1 || index >= map.TheaterInstance.GetOverlayFrameCount(overlayType));

                    if (invalidElement != 0) // this can never be 0 if an invalid element exists, because each valid overlay has at least 1 frame
                    {
                        EditorMessageBox.Show(WindowManager, "Generator Config Error",
                            $"Frame '{ invalidElement }' does not exist in overlay type '{ overlayType.ININame }'!", MessageBoxButtons.OK);
                        return null;
                    }
                }

                var overlayGroup = new TerrainGeneratorOverlayGroup(overlayType, frameIndexes,
                    overlayGroupOpenChances[i].DoubleValue,
                    overlayGroupOccupiedChances[i].DoubleValue);

                overlayGroups.Add(overlayGroup);
            }

            return overlayGroups;
        }

        public void LoadConfig(TerrainGeneratorConfiguration configuration)
        {
            for (int i = 0; i < configuration.OverlayGroups.Count && i < MaxOverlayGroupCount; i++)
            {
                var overlayGroup = configuration.OverlayGroups[i];

                overlayNames[i].Text = overlayGroup.OverlayType.ININame;

                if (overlayGroup.FrameIndices == null)
                    frameIndices[i].Text = string.Empty;
                else
                    frameIndices[i].Text = string.Join(",", overlayGroup.FrameIndices.Select(index => index.ToString(CultureInfo.InvariantCulture)));

                overlayGroupOpenChances[i].DoubleValue = overlayGroup.OpenChance;
                overlayGroupOccupiedChances[i].DoubleValue = overlayGroup.OverlapChance;
            }

            for (int i = configuration.OverlayGroups.Count; i < overlayNames.Length; i++)
            {
                overlayNames[i].Text = string.Empty;
                frameIndices[i].Text = string.Empty;
                overlayGroupOpenChances[i].DoubleValue = 0.0;
                overlayGroupOccupiedChances[i].DoubleValue = 0.0;
            }
        }
    }
}
