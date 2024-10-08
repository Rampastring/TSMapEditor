using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;
using TSMapEditor.UI.CursorActions.HeightActions;
using TSMapEditor.UI.Windows;

namespace TSMapEditor.UI.TopBar
{
    public class EditorControlsPanel : INItializableWindow
    {
        public EditorControlsPanel(WindowManager windowManager, Map map, TheaterGraphics theaterGraphics,
            EditorConfig editorConfig, EditorState editorState, WindowController windowController,
            PlaceTerrainCursorAction terrainPlacementAction,
            PlaceWaypointCursorAction placeWaypointCursorAction,
            ICursorActionTarget cursorActionTarget) : base(windowManager)
        {
            this.map = map;
            this.theaterGraphics = theaterGraphics;
            this.editorConfig = editorConfig;
            this.editorState = editorState;
            this.windowController = windowController;
            this.terrainPlacementAction = terrainPlacementAction;
            this.placeWaypointCursorAction = placeWaypointCursorAction;
            this.cursorActionTarget = cursorActionTarget;

            deletionModeCursorAction = new DeletionModeCursorAction(cursorActionTarget);
            fsRaiseGroundCursorAction = new FSRaiseGroundCursorAction(cursorActionTarget);
            fsLowerGroundCursorAction = new FSLowerGroundCursorAction(cursorActionTarget);
            raiseGroundCursorAction = new RaiseGroundCursorAction(cursorActionTarget);
            lowerGroundCursorAction = new LowerGroundCursorAction(cursorActionTarget);
            raiseCellsCursorAction = new RaiseCellsCursorAction(cursorActionTarget);
            lowerCellsCursorAction = new LowerCellsCursorAction(cursorActionTarget);
            flattenGroundCursorAction = new FlattenGroundCursorAction(cursorActionTarget);
        }

        private readonly Map map;
        private readonly TheaterGraphics theaterGraphics;
        private readonly EditorConfig editorConfig;
        private readonly EditorState editorState;
        private readonly WindowController windowController;
        private readonly ICursorActionTarget cursorActionTarget;
        private readonly PlaceTerrainCursorAction terrainPlacementAction;
        private readonly PlaceWaypointCursorAction placeWaypointCursorAction;
        private readonly DeletionModeCursorAction deletionModeCursorAction;
        private readonly FSRaiseGroundCursorAction fsRaiseGroundCursorAction;
        private readonly FSLowerGroundCursorAction fsLowerGroundCursorAction;
        private readonly RaiseGroundCursorAction raiseGroundCursorAction;
        private readonly LowerGroundCursorAction lowerGroundCursorAction;
        private readonly RaiseCellsCursorAction raiseCellsCursorAction;
        private readonly LowerCellsCursorAction lowerCellsCursorAction;
        private readonly FlattenGroundCursorAction flattenGroundCursorAction;

        private XNADropDown ddBrushSize;
        private XNACheckBox chkAutoLAT;
        private XNACheckBox chkOnlyPaintOnClearGround;
        private XNACheckBox chkDrawMapWideOverlay;

        public override void Initialize()
        {
            SubDirectory = string.Empty;
            Name = nameof(EditorControlsPanel);
            base.Initialize();

            ddBrushSize = FindChild<XNADropDown>(nameof(ddBrushSize));
            foreach (var brushSize in editorConfig.BrushSizes)
            {
                ddBrushSize.AddItem(brushSize.Width + "x" + brushSize.Height);
            }
            ddBrushSize.SelectedIndexChanged += DdBrushSize_SelectedIndexChanged;
            ddBrushSize.SelectedIndex = 0;

            chkAutoLAT = FindChild<XNACheckBox>(nameof(chkAutoLAT));
            chkAutoLAT.Checked = editorState.AutoLATEnabled;
            chkAutoLAT.CheckedChanged += ChkAutoLat_CheckedChanged;

            chkOnlyPaintOnClearGround = FindChild<XNACheckBox>(nameof(chkOnlyPaintOnClearGround));
            chkOnlyPaintOnClearGround.Checked = editorState.OnlyPaintOnClearGround;
            chkOnlyPaintOnClearGround.CheckedChanged += ChkOnlyPaintOnClearGround_CheckedChanged;

            chkDrawMapWideOverlay = FindChild<XNACheckBox>(nameof(chkDrawMapWideOverlay));
            chkDrawMapWideOverlay.Checked = editorState.DrawMapWideOverlay;
            chkDrawMapWideOverlay.CheckedChanged += ChkDrawMapWideOverlay_CheckedChanged;
            CheckForMapWideOverlay();
            editorState.MapWideOverlayExistsChanged += (s, e) => CheckForMapWideOverlay();

            FindChild<EditorButton>("btnPlaceWaypoint").LeftClick += (s, e) => editorState.CursorAction = placeWaypointCursorAction;
            FindChild<EditorButton>("btnDeletionMode").LeftClick += (s, e) => editorState.CursorAction = deletionModeCursorAction;

            var btnRaiseGround = FindChild<EditorButton>("btnRaiseGround", true);
            if (btnRaiseGround != null)
                btnRaiseGround.LeftClick += (s, e) => { editorState.CursorAction = fsRaiseGroundCursorAction; editorState.BrushSize = map.EditorConfig.BrushSizes.Find(bs => bs.Width == 3 && bs.Height == 3); };

            var btnLowerGround = FindChild<EditorButton>("btnLowerGround", true);
            if (btnLowerGround != null)
                btnLowerGround.LeftClick += (s, e) => { editorState.CursorAction = fsLowerGroundCursorAction; editorState.BrushSize = map.EditorConfig.BrushSizes.Find(bs => bs.Width == 2 && bs.Height == 2); };

            var btnRaiseGroundSteep = FindChild<EditorButton>("btnRaiseGroundSteep", true);
            if (btnRaiseGroundSteep != null)
                btnRaiseGroundSteep.LeftClick += (s, e) => { editorState.CursorAction = raiseGroundCursorAction; editorState.BrushSize = map.EditorConfig.BrushSizes.Find(bs => bs.Width == 3 && bs.Height == 3); };

            var btnLowerGroundSteep = FindChild<EditorButton>("btnLowerGroundSteep", true);
            if (btnLowerGroundSteep != null)
                btnLowerGroundSteep.LeftClick += (s, e) => { editorState.CursorAction = lowerGroundCursorAction; editorState.BrushSize = map.EditorConfig.BrushSizes.Find(bs => bs.Width == 2 && bs.Height == 2); };

            var btnRaiseCells = FindChild<EditorButton>("btnRaiseCells", true);
            if (btnRaiseCells != null)
                btnRaiseCells.LeftClick += (s, e) => editorState.CursorAction = raiseCellsCursorAction;

            var btnLowerCells = FindChild<EditorButton>("btnLowerCells", true);
            if (btnLowerCells != null)
                btnLowerCells.LeftClick += (s, e) => editorState.CursorAction = lowerCellsCursorAction;

            var btnFlattenGround = FindChild<EditorButton>("btnFlattenGround", true);
            if (btnFlattenGround != null)
                btnFlattenGround.LeftClick += (s, e) => editorState.CursorAction = flattenGroundCursorAction;

            var btnFrameworkMode = FindChild<EditorButton>("btnFrameworkMode", true);
            if (btnFrameworkMode != null)
                btnFrameworkMode.LeftClick += (s, e) => editorState.IsMarbleMadness = !editorState.IsMarbleMadness;

            var btn2DMode = FindChild<EditorButton>("btn2DMode", true);
            if (btn2DMode != null)
                btn2DMode.LeftClick += (s, e) => editorState.Is2DMode = !editorState.Is2DMode;

            var btnGenerateTerrain = FindChild<EditorButton>("btnGenerateTerrain", true);
            if (btnGenerateTerrain != null)
                btnGenerateTerrain.LeftClick += (s, e) => EnterTerrainGenerator();

            var btnTerrainGeneratorOptions = FindChild<EditorButton>("btnTerrainGeneratorOptions", true);
            if (btnTerrainGeneratorOptions != null)
                btnTerrainGeneratorOptions.LeftClick += (s, e) => windowController.TerrainGeneratorConfigWindow.Open();

            var btnDrawConnectedTiles = FindChild<EditorButton>("btnDrawConnectedTiles", true);
            if (btnDrawConnectedTiles != null)
                btnDrawConnectedTiles.LeftClick += (s, e) => windowController.SelectConnectedTileWindow.Open();

            KeyboardCommands.Instance.NextBrushSize.Triggered += NextBrushSize_Triggered;
            KeyboardCommands.Instance.PreviousBrushSize.Triggered += PreviousBrushSize_Triggered;
            KeyboardCommands.Instance.ToggleAutoLAT.Triggered += ToggleAutoLAT_Triggered;
            KeyboardCommands.Instance.ToggleMapWideOverlay.Triggered += (s, e) => { if (editorState.MapWideOverlayExists) chkDrawMapWideOverlay.Checked = !chkDrawMapWideOverlay.Checked; };

            editorState.AutoLATEnabledChanged += (s, e) => chkAutoLAT.Checked = editorState.AutoLATEnabled;
            editorState.OnlyPaintOnClearGroundChanged += (s, e) => chkOnlyPaintOnClearGround.Checked = editorState.OnlyPaintOnClearGround;
            editorState.DrawMapWideOverlayChanged += (s, e) => chkDrawMapWideOverlay.Checked = editorState.DrawMapWideOverlay;
            editorState.BrushSizeChanged += (s, e) => ddBrushSize.SelectedIndex = map.EditorConfig.BrushSizes.FindIndex(bs => bs == editorState.BrushSize);

            InitLATPanel();
        }

        private void EnterTerrainGenerator()
        {
            if (windowController.TerrainGeneratorConfigWindow.TerrainGeneratorConfig == null)
            {
                windowController.TerrainGeneratorConfigWindow.Open();
                return;
            }

            var generateTerrainCursorAction = new GenerateTerrainCursorAction(cursorActionTarget);
            generateTerrainCursorAction.TerrainGeneratorConfiguration = windowController.TerrainGeneratorConfigWindow.TerrainGeneratorConfig;
            editorState.CursorAction = generateTerrainCursorAction;
        }

        private void CheckForMapWideOverlay()
        {
            chkDrawMapWideOverlay.AllowChecking = editorState.MapWideOverlayExists;
        }

        private void InitLATPanel()
        {
            var latPanel = FindChild<XNAPanel>("LATPanel");

            var btnClearTerrain = new EditorButton(WindowManager);
            btnClearTerrain.Name = nameof(btnClearTerrain);
            btnClearTerrain.X = 0;
            btnClearTerrain.Y = 0;
            btnClearTerrain.Width = Constants.CellSizeX + Constants.UIHorizontalSpacing * 2;
            btnClearTerrain.Height = Constants.CellSizeY + 2;
            btnClearTerrain.ExtraTexture = theaterGraphics.GetTileGraphics(0).TMPImages[0].TextureFromTmpImage_RGBA(GraphicsDevice);
            btnClearTerrain.LeftClick += (s, e) => EnterLATPlacementMode(0);
            latPanel.AddChild(btnClearTerrain);
            var clearToolTip = new ToolTip(WindowManager, btnClearTerrain);
            clearToolTip.Text = "Clear";
            clearToolTip.ToolTipDelay = 0;

            int prevRight = btnClearTerrain.Right;
            int y = btnClearTerrain.Y;

            for (int i = 0; i < map.TheaterInstance.Theater.LATGrounds.Count; i++)
            {
                LATGround autoLATGround = map.TheaterInstance.Theater.LATGrounds[i];

                // If this LAT is for Marble Madness mode only, skip
                // (some modders might do this if they don't use all the
                // TS LAT slots)
                if (autoLATGround.GroundTileSet.NonMarbleMadness > -1)
                    continue;

                if (!autoLATGround.GroundTileSet.AllowToPlace)
                    continue;

                // If we already have a button for this ground type, then skip it
                // The editor can automatically place the correct LAT variations
                // of a tile based on its surroundings
                bool alreadyExists = false;
                for (int j = i - 1; j > -1; j--)
                {
                    if (map.TheaterInstance.Theater.LATGrounds[j].GroundTileSet == autoLATGround.GroundTileSet)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (alreadyExists)
                    continue;

                var btn = new EditorButton(WindowManager);
                btn.Name = "btn" + autoLATGround.GroundTileSet.SetName;
                btn.X = prevRight + Constants.UIHorizontalSpacing;
                btn.Y = y;
                btn.Width = btnClearTerrain.Width;
                btn.Height = btnClearTerrain.Height;
                var tileGraphics = theaterGraphics.GetTileGraphics(autoLATGround.GroundTileSet.StartTileIndex);
                btn.ExtraTexture = tileGraphics != null && tileGraphics.TMPImages.Length > 0 ? tileGraphics.TMPImages[0].TextureFromTmpImage_RGBA(GraphicsDevice) : null;
                btn.Tag = autoLATGround;
                btn.LeftClick += (s, e) => EnterLATPlacementMode(autoLATGround.GroundTileSet.StartTileIndex);
                latPanel.AddChild(btn);

                if (btn.Right > latPanel.Width)
                {
                    btn.X = btnClearTerrain.X;
                    prevRight = btn.Right;

                    y += btnClearTerrain.Height + Constants.UIVerticalSpacing;
                    btn.Y = y;
                }

                var toolTip = new ToolTip(WindowManager, btn);
                string[] allBases = map.TheaterInstance.Theater.LATGrounds.FindAll(lg => lg.GroundTileSet == autoLATGround.GroundTileSet).Select(lg =>
                {
                    if (lg.BaseTileSet == null)
                        return "Clear";

                    return lg.BaseTileSet.SetName;
                }).ToArray();

                toolTip.Text = $"{autoLATGround.GroundTileSet.SetName} (placed on top of {string.Join(" or ", allBases)})";

                toolTip.ToolTipDelay = 0;

                prevRight = btn.Right;
            }
        }

        private void EnterLATPlacementMode(int tileIndex)
        {
            terrainPlacementAction.Tile = theaterGraphics.GetTileGraphics(tileIndex);
            editorState.CursorAction = terrainPlacementAction;
        }

        private void DdBrushSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            editorState.BrushSize = editorConfig.BrushSizes[ddBrushSize.SelectedIndex];
        }

        private void ChkAutoLat_CheckedChanged(object sender, EventArgs e)
        {
            editorState.AutoLATEnabled = chkAutoLAT.Checked;
        }

        private void ChkOnlyPaintOnClearGround_CheckedChanged(object sender, EventArgs e)
        {
            editorState.OnlyPaintOnClearGround = chkOnlyPaintOnClearGround.Checked;
        }

        private void ChkDrawMapWideOverlay_CheckedChanged(object sender, EventArgs e)
        {
            editorState.DrawMapWideOverlay = chkDrawMapWideOverlay.Checked;
        }

        private void PreviousBrushSize_Triggered(object sender, EventArgs e)
        {
            if (ddBrushSize.SelectedIndex < 1)
                return;

            ddBrushSize.SelectedIndex--;
        }

        private void NextBrushSize_Triggered(object sender, EventArgs e)
        {
            if (ddBrushSize.SelectedIndex >= ddBrushSize.Items.Count - 1)
                return;

            ddBrushSize.SelectedIndex++;
        }

        private void ToggleAutoLAT_Triggered(object sender, EventArgs e)
        {
            chkAutoLAT.Checked = !chkAutoLAT.Checked;
        }
    }
}
