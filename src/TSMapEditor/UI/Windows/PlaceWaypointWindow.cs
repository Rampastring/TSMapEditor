using System;
using System.Globalization;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class PlaceWaypointWindow : INItializableWindow
    {
        public PlaceWaypointWindow(WindowManager windowManager, Map map, MutationManager mutationManager, IMutationTarget mutationTarget) : base(windowManager)
        {
            this.map = map;
            this.mutationManager = mutationManager;
            this.mutationTarget = mutationTarget;
        }

        private readonly Map map;
        private readonly MutationManager mutationManager;
        private readonly IMutationTarget mutationTarget;

        private EditorNumberTextBox tbWaypointNumber;
        private XNADropDown ddWaypointColor;

        private Point2D cellCoords;

        public override void Initialize()
        {
            Name = nameof(PlaceWaypointWindow);
            base.Initialize();

            tbWaypointNumber = FindChild<EditorNumberTextBox>(nameof(tbWaypointNumber));
            tbWaypointNumber.MaximumTextLength = (Constants.MaxWaypoint - 1).ToString(CultureInfo.InvariantCulture).Length;

            FindChild<EditorButton>("btnPlace").LeftClick += BtnPlace_LeftClick;

            // Init color dropdown options
            ddWaypointColor = FindChild<XNADropDown>(nameof(ddWaypointColor));
            ddWaypointColor.AddItem("None");
            Array.ForEach(Waypoint.SupportedColors, sc => ddWaypointColor.AddItem(sc.Name, sc.Value));
        }

        private void BtnPlace_LeftClick(object sender, EventArgs e)
        {
            // Cancel dialog if the user leaves the text box empty
            if (tbWaypointNumber.Text == string.Empty)
            {
                Hide();
                return;
            }

            if (tbWaypointNumber.Value < 0 || tbWaypointNumber.Value >= Constants.MaxWaypoint)
                return;

            if (map.Waypoints.Exists(w => w.Identifier == tbWaypointNumber.Value))
            {
                EditorMessageBox.Show(WindowManager,
                    "Waypoint already exists",
                    $"A waypoint with the given number {tbWaypointNumber.Value} already exists on the map!",
                    MessageBoxButtons.OK);

                return;
            }

            string waypointColor = ddWaypointColor.SelectedItem != null ? ddWaypointColor.SelectedItem.Text : null;

            mutationManager.PerformMutation(new PlaceWaypointMutation(mutationTarget, cellCoords, tbWaypointNumber.Value, waypointColor));

            Hide();
        }

        public void Open(Point2D cellCoords)
        {
            this.cellCoords = cellCoords;

            if (map.Waypoints.Count == Constants.MaxWaypoint)
            {
                EditorMessageBox.Show(WindowManager,
                    "Maximum waypoints reached",
                    "All valid waypoints on the map are already in use!",
                    MessageBoxButtons.OK);

                return;
            }

            for (int i = 0; i < Constants.MaxWaypoint; i++)
            {
                if (!map.Waypoints.Exists(w => w.Identifier == i))
                {
                    tbWaypointNumber.Value = i;
                    break;
                }
            }

            Show();
        }
    }
}
