using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Text;
using TSMapEditor.GameMath;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Mutations;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Rendering;
using TSMapEditor.Settings;
using TSMapEditor.UI.Windows;

namespace TSMapEditor.UI
{
    /// <summary>
    /// An interface for an object that mutations use to interact with the map.
    /// </summary>
    public interface IMutationTarget
    {
        Map Map { get; }
        TheaterGraphics TheaterGraphics { get; }
        void AddRefreshPoint(Point2D point, int size = 10);
        void InvalidateMap();
        House ObjectOwner { get; }
        BrushSize BrushSize { get; }
        Randomizer Randomizer { get; }
        bool AutoLATEnabled { get; }
        LightingPreviewMode LightingPreviewState { get; }
        bool OnlyPaintOnClearGround { get; }
    }

    /// <summary>
    /// An interface for an object that cursor actions use to interact with the map.
    /// </summary>
    public interface ICursorActionTarget : IMapView
    {
        WindowManager WindowManager { get; }
        EditorGraphics EditorGraphics { get; }
        MutationManager MutationManager { get; }
        IMutationTarget MutationTarget { get; }
        BrushSize BrushSize { get; set; }
        bool Is2DMode { get; }
        DeletionMode DeletionMode { get; }
        Randomizer Randomizer { get; }
        bool AutoLATEnabled { get; }
        bool OnlyPaintOnClearGround { get; }
        CopiedMapData CopiedMapData { get; set; }
        TechnoBase TechnoUnderCursor { get; set; }
    }

    /// <summary>
    /// Handles user input on the map and utilizes <see cref="MapView"/> to draw the map.
    /// </summary>
    public class MapUI : XNAControl, ICursorActionTarget, IMutationTarget
    {
        private const float RightClickScrollRateDivisor = 64f;
        private const double ZoomStep = 0.1;

        private static Color[] MarbleMadnessTileHeightLevelColors = new Color[]
        {
            new Color(165, 28, 68),
            new Color(202, 149, 101),
            new Color(170, 125, 76),
            new Color(149, 109, 64),
            new Color(133, 97, 56),
            new Color(226, 101, 182),
            new Color(194, 198, 255),
            new Color(20, 153, 20),
            new Color(4, 129, 16),
            new Color(40, 165, 28),
            new Color(230, 198, 109),
            new Color(153, 20, 48),
            new Color(80, 190, 56),
            new Color(56, 89, 133),
            new Color(194, 198, 255)
        };

        public MapUI(WindowManager windowManager, Map map, TheaterGraphics theaterGraphics, EditorGraphics editorGraphics,
            EditorState editorState, MutationManager mutationManager, WindowController windowController) : base(windowManager)
        {
            mapView = new MapView(windowManager, map, theaterGraphics, editorGraphics, editorState);
            EditorState = editorState;
            Map = map;
            MutationManager = mutationManager;
            this.windowController = windowController;
            SetControlSize();
        }

        public EditorState EditorState { get; private set; }
        public Map Map { get; private set; }
        public TheaterGraphics TheaterGraphics => mapView.TheaterGraphics;
        public EditorGraphics EditorGraphics => mapView.EditorGraphics;
        public MutationManager MutationManager { get; private set; }
        private WindowController windowController;

        public IMutationTarget MutationTarget => this;
        public House ObjectOwner => EditorState.ObjectOwner;
        public BrushSize BrushSize { get => EditorState.BrushSize; set => EditorState.BrushSize = value; }
        public bool Is2DMode => EditorState.Is2DMode;
        public DeletionMode DeletionMode => EditorState.DeletionMode;
        public LightingPreviewMode LightingPreviewState => EditorState.IsLighting ? EditorState.LightingPreviewState : LightingPreviewMode.NoLighting;
        public Randomizer Randomizer => EditorState.Randomizer;
        public bool AutoLATEnabled => EditorState.AutoLATEnabled;
        public bool OnlyPaintOnClearGround => EditorState.OnlyPaintOnClearGround;
        public CopiedMapData CopiedMapData
        {
            get => EditorState.CopiedMapData;
            set => EditorState.CopiedMapData = value;
        }
        public Texture2D MinimapTexture => mapView.MinimapTexture;
        public HashSet<object> MinimapUsers => mapView.MinimapUsers;

        public Camera Camera => mapView.Camera;
        public TechnoBase TechnoUnderCursor { get; set; }

        public TileInfoDisplay TileInfoDisplay { get; set; }

        public MapWideOverlay MapWideOverlay => mapView.MapWideOverlay;

        public CursorAction CursorAction
        {
            get => EditorState.CursorAction;
            set => EditorState.CursorAction = value;
        }

        private MapTile tileUnderCursor;
        private MapTile lastTileUnderCursor;

        private int scrollRate;

        private bool isDraggingObject = false;
        private bool isRotatingObject = false;
        private IMovable draggedOrRotatedObject = null;

        private bool isRightClickScrolling = false;
        private Point rightClickScrollInitPos = new Point(-1, -1);

        private Point lastClickedPoint;
        private Point pressedDownPoint;

        private MapView mapView;


        public void AddRefreshPoint(Point2D point, int size = 1) => mapView.InvalidateMap();

        /// <summary>
        /// Schedules the visible portion of the map to be re-rendered
        /// on the next frame.
        /// </summary>
        public void InvalidateMap() => mapView.InvalidateMap();

        /// <summary>
        /// Schedules the entire map to be re-rendered on the next frame, regardless
        /// of what is visible on the screen.
        /// </summary>
        public void InvalidateMapForMinimap() => mapView.InvalidateMapForMinimap();

        public void AddPreviewToMap() => mapView.AddPreviewToMap();

        public void ExtractMegamapTo(string path) => mapView.ExtractMegamapTo(path);

        public override void Initialize()
        {
            base.Initialize();

            scrollRate = UserSettings.Instance.ScrollRate;

            EditorState.CursorActionChanged += EditorState_CursorActionChanged;

            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;
            KeyboardCommands.Instance.FrameworkMode.Triggered += FrameworkMode_Triggered;
            KeyboardCommands.Instance.ViewMegamap.Triggered += ViewMegamap_Triggered;
            KeyboardCommands.Instance.Toggle2DMode.Triggered += Toggle2DMode_Triggered;
            KeyboardCommands.Instance.ZoomIn.Triggered += ZoomIn_Triggered;
            KeyboardCommands.Instance.ZoomOut.Triggered += ZoomOut_Triggered;
            KeyboardCommands.Instance.ResetZoomLevel.Triggered += ResetZoomLevel_Triggered;
            KeyboardCommands.Instance.RotateUnitOneStep.Triggered += RotateUnitOneStep_Triggered;

            windowController.Initialized += PostWindowControllerInit;
            Map.LocalSizeChanged += (s, e) => InvalidateMap();
            Map.MapResized += Map_MapResized;
            Map.MapHeightChanged += (s, e) => InvalidateMap();
            Map.Lighting.ColorsRefreshed += (s, e) => Map_LightingColorsRefreshed();
            Map.CellLightingModified += Map_CellLightingModified;

            Map.HouseColorChanged += (s, e) => InvalidateMap();
            EditorState.HighlightImpassableCellsChanged += (s, e) => InvalidateMap();
            EditorState.HighlightIceGrowthChanged += (s, e) => InvalidateMap();
            EditorState.DrawMapWideOverlayChanged += (s, e) => MapWideOverlay.Enabled = EditorState.DrawMapWideOverlay;
            EditorState.MarbleMadnessChanged += (s, e) => InvalidateMapForMinimap();
            EditorState.Is2DModeChanged += (s, e) => InvalidateMapForMinimap();
            EditorState.IsLightingChanged += (s, e) => LightingChanged();
            EditorState.LightingPreviewStateChanged += (s, e) => LightingChanged();
            EditorState.RenderedObjectsChanged += (s, e) => InvalidateMapForMinimap();

            windowController.RenderResolutionChanged += WindowController_RenderResolutionChanged;

            mapView.Initialize();
        }

        private void WindowController_RenderResolutionChanged(object sender, EventArgs e) => SetControlSize();

        private void SetControlSize()
        {
            Width = WindowManager.RenderResolutionX;
            Height = WindowManager.RenderResolutionY;
            mapView.InvalidateMapForMinimap();
        }

        private void PostWindowControllerInit(object sender, EventArgs e)
        {
            windowController.MinimapWindow.MegamapClicked += MinimapWindow_MegamapClicked;
            windowController.MinimapWindow.EnabledChanged += (s, e) => { if (((MegamapWindow)s).Enabled) InvalidateMapForMinimap(); };
            windowController.Initialized -= PostWindowControllerInit;
            windowController.RunScriptWindow.ScriptRun += (s, e) => InvalidateMap();
            windowController.StructureOptionsWindow.EnabledChanged += (s, e) => { if (!((StructureOptionsWindow)s).Enabled) InvalidateMap(); };

            Map_LightingColorsRefreshed();
        }

        public void Clear()
        {
            EditorState.CursorActionChanged -= EditorState_CursorActionChanged;
            EditorState = null;
            MutationManager = null;
            MapWideOverlay.Clear();

            windowController.RenderResolutionChanged -= WindowController_RenderResolutionChanged;
            windowController = null;

            Map.MapResized -= Map_MapResized;
            Map = null;

            Keyboard.OnKeyPressed -= Keyboard_OnKeyPressed;
            KeyboardCommands.Instance.FrameworkMode.Triggered -= FrameworkMode_Triggered;
            KeyboardCommands.Instance.ViewMegamap.Triggered -= ViewMegamap_Triggered;
            KeyboardCommands.Instance.Toggle2DMode.Triggered -= Toggle2DMode_Triggered;
            KeyboardCommands.Instance.ZoomIn.Triggered -= ZoomIn_Triggered;
            KeyboardCommands.Instance.ZoomOut.Triggered -= ZoomOut_Triggered;
            KeyboardCommands.Instance.ResetZoomLevel.Triggered -= ResetZoomLevel_Triggered;
            KeyboardCommands.Instance.RotateUnitOneStep.Triggered -= RotateUnitOneStep_Triggered;

            mapView.Clear();
        }

        private void ViewMegamap_Triggered(object sender, EventArgs e)
        {
            var mmw = new MegamapWindow(WindowManager, this, false);
            mmw.Width = WindowManager.RenderResolutionX;
            mmw.Height = WindowManager.RenderResolutionY;
            mmw.DrawOrder = int.MaxValue;
            mmw.UpdateOrder = int.MaxValue;
            WindowManager.AddAndInitializeControl(mmw);
            InvalidateMapForMinimap();
        }

        private void RotateUnitOneStep_Triggered(object sender, EventArgs e)
        {
            if (tileUnderCursor == null)
                return;

            var tilePosition = GetRelativeTilePositionFromCursorPosition(tileUnderCursor);
            var selectedObject = tileUnderCursor.GetObject(tilePosition) as TechnoBase;
            if (selectedObject == null)
                return;

            const int step = 32;

            if (selectedObject.Facing + step > byte.MaxValue)
                selectedObject.Facing = (byte)(selectedObject.Facing + step - byte.MaxValue);
            else
                selectedObject.Facing += step;

            AddRefreshPoint(tileUnderCursor.CoordsToPoint());
        }

        private void Map_MapResized(object sender, EventArgs e)
        {
            // Resizing the map makes previous undo/redo entries invalid
            MutationManager.ClearUndoAndRedoLists();

            // We need to re-create our map textures
            mapView.RefreshRenderTargets();

            windowController.MinimapWindow.MegamapTexture = mapView.MinimapTexture; // mapRenderTarget;
            Map.RefreshCellLighting(EditorState.LightingPreviewState, null);

            // And then re-draw the whole map
            InvalidateMap();
        }

        private void Map_CellLightingModified(object sender, CellLightingEventArgs e)
        {
            if (EditorState.IsLighting && EditorState.LightingPreviewState != LightingPreviewMode.NoLighting)
                Map.RefreshCellLighting(EditorState.LightingPreviewState, e.AffectedTiles);
        }

        private void LightingChanged()
        {
            Map.RefreshCellLighting(EditorState.IsLighting ? EditorState.LightingPreviewState : LightingPreviewMode.NoLighting, null);

            InvalidateMapForMinimap();
            if (Constants.VoxelsAffectedByLighting)
                TheaterGraphics.InvalidateVoxelCache();
        }

        private void Map_LightingColorsRefreshed()
        {
            MapColor? color = EditorState.LightingPreviewState switch
            {
                LightingPreviewMode.Normal => Map.Lighting.NormalColor,
                LightingPreviewMode.IonStorm => Map.Lighting.IonColor,
                LightingPreviewMode.Dominator => Map.Lighting.DominatorColor,
                _ => null,
            };

            if (color != null)
                TheaterGraphics.ApplyLightingToPalettes((MapColor)color);

            LightingChanged();
        }

        private void MinimapWindow_MegamapClicked(object sender, MegamapClickedEventArgs e)
        {
            Camera.TopLeftPoint = e.ClickedPoint - new Point2D(Width / 2, Height / 2).ScaleBy(1.0 / Camera.ZoomLevel);
        }

        private void FrameworkMode_Triggered(object sender, EventArgs e)
        {
            EditorState.IsMarbleMadness = !EditorState.IsMarbleMadness;
        }

        private void Toggle2DMode_Triggered(object sender, EventArgs e)
        {
            if (Constants.IsFlatWorld)
                return;

            EditorState.Is2DMode = !EditorState.Is2DMode;
        }

        private void ZoomIn_Triggered(object sender, EventArgs e) => Camera.ZoomLevel += ZoomStep;

        private void ZoomOut_Triggered(object sender, EventArgs e) => Camera.ZoomLevel -= ZoomStep;

        private void ResetZoomLevel_Triggered(object sender, EventArgs e) => Camera.ZoomLevel = 1.0;

        private void EditorState_CursorActionChanged(object sender, EventArgs e)
        {
            if (lastTileUnderCursor != null)
                AddRefreshPoint(lastTileUnderCursor.CoordsToPoint(), 3);

            lastTileUnderCursor = null;
        }

        public override void OnMouseScrolled()
        {
            if (Cursor.ScrollWheelValue > 0)
                Camera.ZoomLevel += ZoomStep;
            else
                Camera.ZoomLevel -= ZoomStep;

            base.OnMouseScrolled();
        }

        public override void OnMouseOnControl()
        {
            if (CursorAction == null && (isDraggingObject || isRotatingObject))
            {
                if (!Cursor.LeftDown)
                {
                    if (isDraggingObject)
                    {
                        isDraggingObject = false;

                        if (tileUnderCursor != null && tileUnderCursor.CoordsToPoint() != draggedOrRotatedObject.Position)
                        {
                            // If the clone modifier is held down, attempt cloning the object.
                            // Otherwise, move the dragged object.
                            bool overlapObjects = KeyboardCommands.Instance.OverlapObjects.AreKeysOrModifiersDown(Keyboard);
                            if (KeyboardCommands.Instance.CloneObject.AreKeysOrModifiersDown(Keyboard))
                            {
                                if ((draggedOrRotatedObject.IsTechno() || draggedOrRotatedObject.WhatAmI() == RTTIType.Terrain) &&
                                    Map.CanPlaceObjectAt(draggedOrRotatedObject, tileUnderCursor.CoordsToPoint(), true, overlapObjects))
                                {
                                    var mutation = new CloneObjectMutation(MutationTarget, draggedOrRotatedObject, tileUnderCursor.CoordsToPoint());
                                    MutationManager.PerformMutation(mutation);
                                }
                            }
                            else if (Map.CanPlaceObjectAt(draggedOrRotatedObject, tileUnderCursor.CoordsToPoint(), false, overlapObjects))
                            {
                                var mutation = new MoveObjectMutation(MutationTarget, draggedOrRotatedObject, tileUnderCursor.CoordsToPoint());
                                MutationManager.PerformMutation(mutation);
                            }
                        }
                    }
                    else if (isRotatingObject)
                    {
                        isRotatingObject = false;
                    }
                }
            }

            var cursorPoint = GetCursorPoint();

            // Record cursor position when the cursor was pressed down on an object.
            // This makes it possible to avoid drag-moving large buildings when the user just clicks on a cell at the bottom of their foundation.
            if (Cursor.LeftPressedDown)
            {
                pressedDownPoint = cursorPoint;
            }
            else if (!Cursor.LeftDown)
            {
                pressedDownPoint = new Point(-1, -1);
            }

            // Attempt dragging or rotating an object
            if (CursorAction == null && tileUnderCursor != null && Cursor.LeftDown && !isDraggingObject && !isRotatingObject && cursorPoint != pressedDownPoint)
            {
                var tilePosition = GetRelativeTilePositionFromCursorPosition(tileUnderCursor);
                var cellObject = tileUnderCursor.GetObject(tilePosition);

                if (cellObject != null)
                {
                    draggedOrRotatedObject = cellObject;

                    if (KeyboardCommands.Instance.RotateUnit.AreKeysDown(Keyboard))
                        isRotatingObject = true;
                    else
                        isDraggingObject = true;
                }
                else if (tileUnderCursor.Waypoints.Count > 0)
                {
                    draggedOrRotatedObject = tileUnderCursor.Waypoints[0];
                    isDraggingObject = true;
                }
            }

            if (isRightClickScrolling)
            {
                if (Cursor.RightDown)
                {
                    var newCursorPosition = GetCursorPoint();
                    var result = newCursorPosition - rightClickScrollInitPos;
                    float rightClickScrollRate = (float)((scrollRate / RightClickScrollRateDivisor) / Camera.ZoomLevel);

                    Camera.FloatTopLeftPoint = new Vector2(Camera.FloatTopLeftPoint.X + result.X * rightClickScrollRate,
                        Camera.FloatTopLeftPoint.Y + result.Y * rightClickScrollRate);
                }
            }

            pressedDownPoint = GetCursorPoint();

            base.OnMouseOnControl();
        }

        public override void OnMouseEnter()
        {
            if (isRightClickScrolling)
                rightClickScrollInitPos = GetCursorPoint();

            base.OnMouseEnter();
        }

        public override void OnMouseMove()
        {
            base.OnMouseMove();

            if (CursorAction != null)
            {
                if (Cursor.LeftDown)
                {
                    if (tileUnderCursor != null && lastTileUnderCursor != tileUnderCursor)
                    {
                        CursorAction.LeftDown(tileUnderCursor.CoordsToPoint());
                        lastTileUnderCursor = tileUnderCursor;
                    }
                }
                else
                {
                    CursorAction.LeftUpOnMouseMove(tileUnderCursor == null ? Point2D.NegativeOne : tileUnderCursor.CoordsToPoint());
                }
            }

            // Right-click scrolling
            if (Cursor.RightDown)
            {
                if (!isRightClickScrolling)
                {
                    isRightClickScrolling = true;
                    rightClickScrollInitPos = GetCursorPoint();
                    Camera.FloatTopLeftPoint = Camera.TopLeftPoint.ToXNAVector();
                }
            }
        }

        public override void OnLeftClick()
        {
            if (tileUnderCursor != null && CursorAction != null)
            {
                CursorAction.LeftClick(tileUnderCursor.CoordsToPoint());
            }
            else
            {
                var cursorPoint = GetCursorPoint();
                if (cursorPoint == lastClickedPoint)
                {
                    HandleDoubleClick();
                }
                else
                {
                    lastClickedPoint = cursorPoint;
                }
            }

            base.OnLeftClick();
        }

        private void HandleDoubleClick()
        {
            if (tileUnderCursor != null && CursorAction == null)
            {
                if (tileUnderCursor.Structures.Count > 0)
                    windowController.StructureOptionsWindow.Open(tileUnderCursor.Structures[0]);

                if (tileUnderCursor.Vehicles.Count > 0)
                    windowController.VehicleOptionsWindow.Open(tileUnderCursor.Vehicles[0]);

                if (tileUnderCursor.Aircraft.Count > 0)
                    windowController.AircraftOptionsWindow.Open(tileUnderCursor.Aircraft[0]);

                var tilePosition = GetRelativeTilePositionFromCursorPosition(tileUnderCursor);
                var closestOccupiedSubCell = tileUnderCursor.GetSubCellClosestToPosition(tilePosition, true);
                if (closestOccupiedSubCell != SubCell.None)
                {
                    Infantry infantry = tileUnderCursor.GetInfantryFromSubCellSpot(closestOccupiedSubCell);
                    if (infantry != null)
                        windowController.InfantryOptionsWindow.Open(infantry);
                }
            }
        }

        public override void OnRightClick()
        {
            if (CursorAction != null && !isRightClickScrolling)
            {
                CursorAction = null;
            }

            isRightClickScrolling = false;

            base.OnRightClick();
        }

        public override void Update(GameTime gameTime)
        {
            // Make scroll rate independent of FPS
            // Scroll rate is designed for 60 FPS
            // 1000 ms (1 second) divided by 60 frames =~ 16.667 ms / frame
            int scrollRate = (int)(this.scrollRate * (gameTime.ElapsedGameTime.TotalMilliseconds / 16.667));

            if (IsActive && !(WindowManager.SelectedControl is XNATextBox))
            {
                Camera.KeyboardUpdate(Keyboard, scrollRate);
            }

            windowController.MinimapWindow.CameraRectangle = new Rectangle(Camera.TopLeftPoint.ToXNAPoint(), new Point2D(Width, Height).ScaleBy(1.0 / Camera.ZoomLevel).ToXNAPoint());

            Point2D cursorMapPoint = GetCursorMapPoint();
            Point2D tileCoords = EditorState.Is2DMode ?
                CellMath.CellCoordsFromPixelCoords_2D(cursorMapPoint, Map) :
                CellMath.CellCoordsFromPixelCoords(cursorMapPoint, Map, CursorAction == null || CursorAction.SeeThrough);

            var tile = Map.GetTile(tileCoords.X, tileCoords.Y);

            tileUnderCursor = tile;
            TileInfoDisplay.MapTile = tile;

            if (IsActive && tileUnderCursor != null)
            {
                var tilePosition = GetRelativeTilePositionFromCursorPosition(tileUnderCursor);
                TechnoUnderCursor = tileUnderCursor.GetTechno(tilePosition);

                if (KeyboardCommands.Instance.DeleteObject.AreKeysDown(Keyboard))
                {
                    if (WindowManager.SelectedControl == null || WindowManager.SelectedControl is not XNATextBox)
                        DeleteObjectFromCell(tileUnderCursor.CoordsToPoint());
                }
            }

            base.Update(gameTime);
        }

        private Point2D GetCursorMapPoint()
        {
            Point cursorPoint = GetCursorPoint();
            Point2D cursorMapPoint = new Point2D(Camera.TopLeftPoint.X + (int)(cursorPoint.X / Camera.ZoomLevel),
                    Camera.TopLeftPoint.Y - Constants.MapYBaseline + (int)(cursorPoint.Y / Camera.ZoomLevel));

            return cursorMapPoint;
        }

        private void Keyboard_OnKeyPressed(object sender, Rampastring.XNAUI.Input.KeyPressEventArgs e)
        {
            if (!IsActive)
                return;

            if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.F1)
            {
                var text = new StringBuilder();

                foreach (KeyboardCommand command in KeyboardCommands.Instance.Commands)
                {
                    text.Append(command.UIName + ": " + command.GetKeyDisplayString());
                    text.Append(Environment.NewLine);
                }

                EditorMessageBox.Show(WindowManager, "Hotkey Help", text.ToString(), MessageBoxButtons.OK);
            }

            if (!e.Handled && CursorAction != null && CursorAction.HandlesKeyboardInput)
            {
                CursorAction.OnKeyPressed(e, tileUnderCursor == null ? Point2D.NegativeOne : tileUnderCursor.CoordsToPoint());
            }
        }

        public void DeleteObjectFromCell(Point2D cellCoords)
        {
            var tile = Map.GetTile(cellCoords.X, cellCoords.Y);
            if (tile == null)
                return;

            if (Map.HasObjectToDelete(cellCoords, EditorState.DeletionMode))
                MutationManager.PerformMutation(new DeleteObjectMutation(MutationTarget, tile.CoordsToPoint(), EditorState.DeletionMode));

            AddRefreshPoint(cellCoords, 2);
        }

        public override void Draw(GameTime gameTime)
        {
            mapView.Draw(IsActive, TechnoUnderCursor, tileUnderCursor, CursorAction);

            mapView.DrawOnTileUnderCursor(tileUnderCursor, CursorAction, isDraggingObject,
                isRotatingObject, draggedOrRotatedObject,
                KeyboardCommands.Instance.CloneObject.AreKeysOrModifiersDown(Keyboard),
                KeyboardCommands.Instance.OverlapObjects.AreKeysOrModifiersDown(Keyboard));

            mapView.DrawOnMinimap();

            base.Draw(gameTime);
        }

        private Point2D GetRelativeTilePositionFromCursorPosition(MapTile tile)
        {
            var cellTopLeft = Constants.IsFlatWorld && EditorState.Is2DMode ?
                CellMath.CellTopLeftPointFromCellCoords_NoBaseline(tile.CoordsToPoint(), Map) :
                CellMath.CellTopLeftPointFromCellCoords_3D_NoBaseline(tile.CoordsToPoint(), Map);

            return GetCursorMapPoint() - cellTopLeft;
        }
    }
}
