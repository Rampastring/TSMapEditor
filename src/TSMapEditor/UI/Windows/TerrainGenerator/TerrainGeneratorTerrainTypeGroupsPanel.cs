using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.TerrainGenerator
{
    /// <summary>
    /// A panel that allows the user to customize how the terrain 
    /// generator places terrain types on the map.
    /// </summary>
    public class TerrainGeneratorTerrainTypeGroupsPanel : EditorPanel
    {
        private const int MaxTerrainTypeGroupCount = 8;

        public TerrainGeneratorTerrainTypeGroupsPanel(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorTextBox[] terrainTypeTextBoxes;
        private EditorNumberTextBox[] terrainTypeOpenChances;
        private EditorNumberTextBox[] terrainTypeOccupiedChances;

        public override void Initialize()
        {
            terrainTypeTextBoxes = new EditorTextBox[MaxTerrainTypeGroupCount];
            terrainTypeOpenChances = new EditorNumberTextBox[MaxTerrainTypeGroupCount];
            terrainTypeOccupiedChances = new EditorNumberTextBox[MaxTerrainTypeGroupCount];

            int y = Constants.UIEmptyTopSpace;

            for (int i = 0; i < MaxTerrainTypeGroupCount; i++)
            {
                var lblTerrainTypes = new XNALabel(WindowManager);
                lblTerrainTypes.Name = nameof(lblTerrainTypes) + i;
                lblTerrainTypes.X = Constants.UIEmptySideSpace;
                lblTerrainTypes.Y = y;
                lblTerrainTypes.FontIndex = Constants.UIBoldFont;
                lblTerrainTypes.Text = $"Terrain Types (Group #{i + 1})";
                AddChild(lblTerrainTypes);

                var tbTerrainTypes = new EditorTextBox(WindowManager);
                tbTerrainTypes.Name = nameof(tbTerrainTypes) + i;
                tbTerrainTypes.X = lblTerrainTypes.X;
                tbTerrainTypes.Y = lblTerrainTypes.Bottom + Constants.UIVerticalSpacing;
                tbTerrainTypes.Width = (Width - 252) - tbTerrainTypes.X - Constants.UIEmptySideSpace;
                AddChild(tbTerrainTypes);
                terrainTypeTextBoxes[i] = tbTerrainTypes;

                var lblOpenChance = new XNALabel(WindowManager);
                lblOpenChance.Name = nameof(lblOpenChance) + i;
                lblOpenChance.X = tbTerrainTypes.Right + Constants.UIHorizontalSpacing;
                lblOpenChance.Y = lblTerrainTypes.Y;
                lblOpenChance.Text = "Open cell chance:";
                AddChild(lblOpenChance);

                var tbOpenChance = new EditorNumberTextBox(WindowManager);
                tbOpenChance.Name = nameof(tbOpenChance) + i;
                tbOpenChance.X = lblOpenChance.X;
                tbOpenChance.Y = tbTerrainTypes.Y;
                tbOpenChance.AllowDecimals = true;
                tbOpenChance.Width = 120;
                AddChild(tbOpenChance);
                terrainTypeOpenChances[i] = tbOpenChance;

                var lblOccupiedChance = new XNALabel(WindowManager);
                lblOccupiedChance.Name = nameof(lblOccupiedChance) + i;
                lblOccupiedChance.X = tbOpenChance.Right + Constants.UIHorizontalSpacing;
                lblOccupiedChance.Y = lblOpenChance.Y;
                lblOccupiedChance.Text = "Occupied cell chance:";
                AddChild(lblOccupiedChance);

                var tbOccupiedChance = new EditorNumberTextBox(WindowManager);
                tbOccupiedChance.Name = nameof(tbOpenChance) + i;
                tbOccupiedChance.X = lblOccupiedChance.X;
                tbOccupiedChance.Y = tbTerrainTypes.Y;
                tbOccupiedChance.AllowDecimals = true;
                tbOccupiedChance.Width = 120;
                AddChild(tbOccupiedChance);
                terrainTypeOccupiedChances[i] = tbOccupiedChance;

                y = tbOccupiedChance.Bottom + Constants.UIEmptyTopSpace;
            }

            Height = y + Constants.UIEmptyBottomSpace;

            base.Initialize();
        }

        public List<TerrainGeneratorTerrainTypeGroup> GetTerrainTypeGroups()
        {
            var terrainTypeGroups = new List<TerrainGeneratorTerrainTypeGroup>();

            for (int i = 0; i < terrainTypeTextBoxes.Length; i++)
            {
                string text = terrainTypeTextBoxes[i].Text.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                string[] parts = text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var terrainTypes = new List<TerrainType>();
                for (int a = 0; a < parts.Length; a++)
                {
                    var terrainType = map.Rules.TerrainTypes.Find(tt => tt.ININame == parts[a]);
                    if (terrainType == null)
                    {
                        EditorMessageBox.Show(WindowManager, "Generator Config Error",
                            $"Specified terrain type '{ parts[a] }' does not exist!", MessageBoxButtons.OK);
                        return null;
                    }
                    terrainTypes.Add(terrainType);
                }

                var terrainTypeGroup = new TerrainGeneratorTerrainTypeGroup(terrainTypes,
                    terrainTypeOpenChances[i].DoubleValue,
                    terrainTypeOccupiedChances[i].DoubleValue);

                terrainTypeGroups.Add(terrainTypeGroup);
            }

            return terrainTypeGroups;
        }

        public void LoadConfig(TerrainGeneratorConfiguration configuration)
        {
            for (int i = 0; i < configuration.TerrainTypeGroups.Count && i < MaxTerrainTypeGroupCount; i++)
            {
                var ttGroup = configuration.TerrainTypeGroups[i];

                terrainTypeTextBoxes[i].Text = string.Join(",", ttGroup.TerrainTypes.Select(tt => tt.ININame));
                terrainTypeOpenChances[i].DoubleValue = ttGroup.OpenChance;
                terrainTypeOccupiedChances[i].DoubleValue = ttGroup.OverlapChance;
            }

            for (int i = configuration.TerrainTypeGroups.Count; i < terrainTypeTextBoxes.Length; i++)
            {
                terrainTypeTextBoxes[i].Text = string.Empty;
                terrainTypeOpenChances[i].DoubleValue = 0.0;
                terrainTypeOccupiedChances[i].DoubleValue = 0.0;
            }
        }
    }
}
