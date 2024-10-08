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
    /// generator places smudges on the map.
    /// </summary>
    public class TerrainGeneratorSmudgeGroupsPanel : EditorPanel
    {
        private const int MaxSmudgeTypeGroupCount = 8;

        public TerrainGeneratorSmudgeGroupsPanel(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorTextBox[] smudgeTypeTextBoxes;
        private EditorNumberTextBox[] smudgeTypeOpenChances;
        private EditorNumberTextBox[] smudgeTypeOccupiedChances;

        public override void Initialize()
        {
            smudgeTypeTextBoxes = new EditorTextBox[MaxSmudgeTypeGroupCount];
            smudgeTypeOpenChances = new EditorNumberTextBox[MaxSmudgeTypeGroupCount];
            smudgeTypeOccupiedChances = new EditorNumberTextBox[MaxSmudgeTypeGroupCount];

            int y = Constants.UIEmptyTopSpace;

            for (int i = 0; i < MaxSmudgeTypeGroupCount; i++)
            {
                var lblSmudgeTypes = new XNALabel(WindowManager);
                lblSmudgeTypes.Name = nameof(lblSmudgeTypes) + i;
                lblSmudgeTypes.X = Constants.UIEmptySideSpace;
                lblSmudgeTypes.Y = y;
                lblSmudgeTypes.FontIndex = Constants.UIBoldFont;
                lblSmudgeTypes.Text = $"Smudge Types (Group #{i + 1})";
                AddChild(lblSmudgeTypes);

                var tbSmudgeTypes = new EditorTextBox(WindowManager);
                tbSmudgeTypes.Name = nameof(tbSmudgeTypes) + i;
                tbSmudgeTypes.X = lblSmudgeTypes.X;
                tbSmudgeTypes.Y = lblSmudgeTypes.Bottom + Constants.UIVerticalSpacing;
                tbSmudgeTypes.Width = (Width - 252) - tbSmudgeTypes.X - Constants.UIEmptySideSpace;
                AddChild(tbSmudgeTypes);
                smudgeTypeTextBoxes[i] = tbSmudgeTypes;

                var lblOpenChance = new XNALabel(WindowManager);
                lblOpenChance.Name = nameof(lblOpenChance) + i;
                lblOpenChance.X = tbSmudgeTypes.Right + Constants.UIHorizontalSpacing;
                lblOpenChance.Y = lblSmudgeTypes.Y;
                lblOpenChance.Text = "Open cell chance:";
                AddChild(lblOpenChance);

                var tbOpenChance = new EditorNumberTextBox(WindowManager);
                tbOpenChance.Name = nameof(tbOpenChance) + i;
                tbOpenChance.X = lblOpenChance.X;
                tbOpenChance.Y = tbSmudgeTypes.Y;
                tbOpenChance.AllowDecimals = true;
                tbOpenChance.Width = 120;
                AddChild(tbOpenChance);
                smudgeTypeOpenChances[i] = tbOpenChance;

                var lblOccupiedChance = new XNALabel(WindowManager);
                lblOccupiedChance.Name = nameof(lblOccupiedChance) + i;
                lblOccupiedChance.X = tbOpenChance.Right + Constants.UIHorizontalSpacing;
                lblOccupiedChance.Y = lblOpenChance.Y;
                lblOccupiedChance.Text = "Occupied cell chance:";
                AddChild(lblOccupiedChance);

                var tbOccupiedChance = new EditorNumberTextBox(WindowManager);
                tbOccupiedChance.Name = nameof(tbOpenChance) + i;
                tbOccupiedChance.X = lblOccupiedChance.X;
                tbOccupiedChance.Y = tbSmudgeTypes.Y;
                tbOccupiedChance.AllowDecimals = true;
                tbOccupiedChance.Width = 120;
                AddChild(tbOccupiedChance);
                smudgeTypeOccupiedChances[i] = tbOccupiedChance;

                y = tbOccupiedChance.Bottom + Constants.UIEmptyTopSpace;
            }

            base.Initialize();
        }

        public List<TerrainGeneratorSmudgeGroup> GetSmudgeGroups()
        {
            var smudgeGroups = new List<TerrainGeneratorSmudgeGroup>();

            for (int i = 0; i < smudgeTypeTextBoxes.Length; i++)
            {
                string text = smudgeTypeTextBoxes[i].Text.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                string[] parts = text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var smudgeTypes = new List<SmudgeType>();
                for (int a = 0; a < parts.Length; a++)
                {
                    var smudgeType = map.Rules.SmudgeTypes.Find(tt => tt.ININame == parts[a]);
                    if (smudgeType == null)
                    {
                        EditorMessageBox.Show(WindowManager, "Generator Config Error",
                            $"Specified smudge type '{ parts[a] }' does not exist!", MessageBoxButtons.OK);
                        return null;
                    }
                    smudgeTypes.Add(smudgeType);
                }

                var smudgeGroup = new TerrainGeneratorSmudgeGroup(smudgeTypes,
                    smudgeTypeOpenChances[i].DoubleValue,
                    smudgeTypeOccupiedChances[i].DoubleValue);

                smudgeGroups.Add(smudgeGroup);
            }

            return smudgeGroups;
        }

        public void LoadConfig(TerrainGeneratorConfiguration configuration)
        {
            for (int i = 0; i < configuration.SmudgeGroups.Count && i < MaxSmudgeTypeGroupCount; i++)
            {
                var smudgeGroup = configuration.SmudgeGroups[i];

                smudgeTypeTextBoxes[i].Text = string.Join(",", smudgeGroup.SmudgeTypes.Select(st => st.ININame));
                smudgeTypeOpenChances[i].DoubleValue = smudgeGroup.OpenChance;
                smudgeTypeOccupiedChances[i].DoubleValue = smudgeGroup.OverlapChance;
            }

            for (int i = configuration.SmudgeGroups.Count; i < smudgeTypeTextBoxes.Length; i++)
            {
                smudgeTypeTextBoxes[i].Text = string.Empty;
                smudgeTypeOpenChances[i].DoubleValue = 0.0;
                smudgeTypeOccupiedChances[i].DoubleValue = 0.0;
            }
        }
    }
}
