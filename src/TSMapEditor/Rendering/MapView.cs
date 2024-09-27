using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using TSMapEditor.GameMath;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Mutations;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Rendering.ObjectRenderers;
using TSMapEditor.Settings;
using TSMapEditor.UI;
using TSMapEditor.UI.Windows;

namespace TSMapEditor.Rendering
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

    public interface IMapView
    {
        Camera Camera { get; }
    }

    /// <summary>
    /// An interface for an object that cursor actions use to interact with the map.
    /// </summary>
    public interface ICursorActionTarget : IMapView
    {
        Map Map { get; }
        TheaterGraphics TheaterGraphics { get; }
        WindowManager WindowManager { get; }
        EditorGraphics EditorGraphics { get; }
        void AddRefreshPoint(Point2D point, int size = 10);
        void InvalidateMap();
        MutationManager MutationManager { get; }
        IMutationTarget MutationTarget { get; }
        BrushSize BrushSize { get; set; }
        bool Is2DMode { get; }
        DeletionMode DeletionMode { get; }
        Randomizer Randomizer { get; }
        bool AutoLATEnabled { get; }
        bool OnlyPaintOnClearGround { get; }
        CopiedMapData CopiedMapData { get; set; }
        Texture2D MinimapTexture { get; }
        HashSet<object> MinimapUsers { get; }
        TechnoBase TechnoUnderCursor { get; set; }
    }

    public class MapView : XNAControl, ICursorActionTarget, IMutationTarget, IMapView
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

        public MapView(WindowManager windowManager, Map map, TheaterGraphics theaterGraphics, EditorGraphics editorGraphics,
            EditorState editorState, MutationManager mutationManager, WindowController windowController) : base(windowManager)
        {
            EditorState = editorState;
            Map = map;
            TheaterGraphics = theaterGraphics;
            EditorGraphics = editorGraphics;
            MutationManager = mutationManager;
            this.windowController = windowController;

            Camera = new Camera(WindowManager, Map);
            Camera.CameraUpdated += (s, e) => 
            { 
                cameraMoved = true; 
                if (UserSettings.Instance.GraphicsLevel > 0) InvalidateMap(); 
            };
            SetControlSize();
        }

        public EditorState EditorState { get; private set; }
        public Map Map { get; private set; }
        public TheaterGraphics TheaterGraphics { get; private set; }
        public EditorGraphics EditorGraphics { get; private set; }
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
        public Texture2D MinimapTexture => minimapRenderTarget;

        /// <summary>
        /// Tracks users of the minimap.
        /// If the minimap texture is not used by anyone, we can save
        /// processing power and skip certain actions that would update it.
        /// </summary>
        public HashSet<object> MinimapUsers { get; } = new HashSet<object>();
        public Camera Camera { get; private set; }
        public TechnoBase TechnoUnderCursor { get; set; }

        public TileInfoDisplay TileInfoDisplay { get; set; }

        public MapWideOverlay MapWideOverlay { get; private set; }

        public CursorAction CursorAction
        {
            get => EditorState.CursorAction;
            set => EditorState.CursorAction = value;
        }

        private RenderTarget2D mapRenderTarget;                      // Render target for terrain
        private RenderTarget2D mapDepthRenderTarget;
        private RenderTarget2D objectsRenderTarget;                  // Render target for objects
        private RenderTarget2D objectsDepthRenderTarget;
        private RenderTarget2D transparencyRenderTarget;             // Render target for map UI elements (celltags etc.) that are only refreshed if something in the map changes (due to performance reasons)
        private RenderTarget2D transparencyPerFrameRenderTarget;     // Render target for map UI elements that are redrawn each frame
        private RenderTarget2D compositeRenderTarget;                // Render target where all the above is combined
        private RenderTarget2D minimapRenderTarget;                  // For minimap and megamap rendering

        private Effect palettedColorDrawEffect;                      // Effect for rendering textures, both paletted and RGBA, with or without remap, with depth assignation to a separate render target
        private Effect combineDrawEffect;                            // Effect for combining map and object render targets into one, taking both of their depth buffers into account

        private MapTile tileUnderCursor;
        private MapTile lastTileUnderCursor;

        private bool mapInvalidated;
        private bool cameraMoved;
        private bool minimapNeedsRefresh;

        private int scrollRate;

        private bool isDraggingObject = false;
        private bool isRotatingObject = false;
        private IMovable draggedOrRotatedObject = null;

        private bool isRightClickScrolling = false;
        private Point rightClickScrollInitPos = new Point(-1, -1);

        private Point lastClickedPoint;

        private List<GameObject> gameObjectsToRender = new List<GameObject>(); 
        private List<Smudge> smudgesToRender = new List<Smudge>();
        private ObjectSpriteRecord objectSpriteRecord = new ObjectSpriteRecord();

        private Stopwatch refreshStopwatch;

        private ulong refreshIndex;

        private bool debugRenderDepthBuffer = false;

        private AircraftRenderer aircraftRenderer;
        private AnimRenderer animRenderer;
        private BuildingRenderer buildingRenderer;
        private InfantryRenderer infantryRenderer;
        private OverlayRenderer overlayRenderer;
        private SmudgeRenderer smudgeRenderer;
        private TerrainRenderer terrainRenderer;
        private UnitRenderer unitRenderer;

        private Rectangle mapRenderSourceRectangle;
        private Rectangle mapRenderDestinationRectangle;

        private DepthStencilState depthRenderStencilState;
        private DepthStencilState shadowRenderStencilState;

        public void AddRefreshPoint(Point2D point, int size = 1)
        {
            InvalidateMap();
        }

        /// <summary>
        /// Schedules the visible portion of the map to be re-rendered
        /// on the next frame.
        /// </summary>
        public void InvalidateMap()
        {
            if (!mapInvalidated)
                refreshIndex++;

            mapInvalidated = true;
        }

        /// <summary>
        /// Schedules the entire map to be re-rendered on the next frame, regardless
        /// of what is visible on the screen.
        /// </summary>
        public void InvalidateMapForMinimap()
        {
            InvalidateMap();
            minimapNeedsRefresh = true;
        }

        public override void Initialize()
        {
            base.Initialize();

            LoadShaders();

            MapWideOverlay = new MapWideOverlay();
            EditorState.MapWideOverlayExists = MapWideOverlay.HasTexture;

            RefreshRenderTargets();

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

            refreshStopwatch = new Stopwatch();

            InitRenderers();

            InvalidateMap();

            windowController.RenderResolutionChanged += WindowController_RenderResolutionChanged;
        }

        private void WindowController_RenderResolutionChanged(object sender, EventArgs e) => SetControlSize();

        private void SetControlSize()
        {
            Width = WindowManager.RenderResolutionX;
            Height = WindowManager.RenderResolutionY;
            InvalidateMapForMinimap();
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
            TheaterGraphics = null;
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

            ClearRenderTargets();
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

        private void LoadShaders()
        {
            palettedColorDrawEffect = AssetLoader.LoadEffect("Shaders/PalettedColorDraw");
            combineDrawEffect = AssetLoader.LoadEffect("Shaders/CombineWithDepth");
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
            RefreshRenderTargets();

            windowController.MinimapWindow.MegamapTexture = mapRenderTarget;
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

        private void ClearRenderTargets()
        {
            mapRenderTarget?.Dispose();
            mapDepthRenderTarget?.Dispose();
            objectsRenderTarget?.Dispose();
            objectsDepthRenderTarget?.Dispose();
            transparencyRenderTarget?.Dispose();
            transparencyPerFrameRenderTarget?.Dispose();
            compositeRenderTarget?.Dispose();
            minimapRenderTarget?.Dispose();

            depthRenderStencilState?.Dispose();
            shadowRenderStencilState?.Dispose();
        }

        private void RefreshRenderTargets()
        {
            ClearRenderTargets();

            mapRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color, DepthFormat.Depth24);
            mapDepthRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Single);
            objectsRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color, DepthFormat.Depth24);
            objectsDepthRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Single);
            transparencyRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);
            transparencyPerFrameRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);
            compositeRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);
            minimapRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);
        }

        private RenderDependencies CreateRenderDependencies()
        {
            return new RenderDependencies(Map, TheaterGraphics, EditorState, GraphicsDevice, objectSpriteRecord, palettedColorDrawEffect, Camera, GetCameraRightXCoord, GetCameraBottomYCoord);
        }

        private void InitRenderers()
        {
            aircraftRenderer = new AircraftRenderer(CreateRenderDependencies());
            animRenderer = new AnimRenderer(CreateRenderDependencies());
            buildingRenderer = new BuildingRenderer(CreateRenderDependencies());
            infantryRenderer = new InfantryRenderer(CreateRenderDependencies());
            overlayRenderer = new OverlayRenderer(CreateRenderDependencies());
            smudgeRenderer = new SmudgeRenderer(CreateRenderDependencies());
            terrainRenderer = new TerrainRenderer(CreateRenderDependencies());
            unitRenderer = new UnitRenderer(CreateRenderDependencies());
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

        private RenderTarget2D CreateFullMapRenderTarget(SurfaceFormat surfaceFormat, DepthFormat depthFormat = DepthFormat.None)
        {
           return new RenderTarget2D(GraphicsDevice,
               Map.WidthInPixels,
               Map.HeightInPixels + (Constants.CellHeight * Constants.MaxMapHeightLevel), false, surfaceFormat,
               depthFormat, 0, RenderTargetUsage.PreserveContents);
        }

        public void DrawVisibleMapPortion()
        {
            refreshStopwatch.Restart();

            smudgesToRender.Clear();
            gameObjectsToRender.Clear();

            Renderer.PushRenderTarget(mapRenderTarget);

            GraphicsDevice.SetRenderTargets(mapRenderTarget, mapDepthRenderTarget);

            if (mapInvalidated)
            {
                GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 0f, 0);
                objectSpriteRecord.Clear();
            }

            if (depthRenderStencilState == null)
            {
                depthRenderStencilState = new DepthStencilState()
                {
                    DepthBufferEnable = true,
                    DepthBufferWriteEnable = true,
                    DepthBufferFunction = CompareFunction.GreaterEqual,
                };
            }

            // Draw terrain tiles in batched mode for performance if we can.
            // In Marble Madness mode we currently need to mix and match paletted and non-paletted graphics, so there's no avoiding immediate mode.
            SpriteSortMode spriteSortMode = EditorState.IsMarbleMadness ? SpriteSortMode.Immediate : SpriteSortMode.Deferred;

            SetPaletteEffectParams(palettedColorDrawEffect, TheaterGraphics.TheaterPalette.GetTexture(), true, false, 1.0f);
            var palettedColorDrawSettings = new SpriteBatchSettings(spriteSortMode, BlendState.Opaque, null, depthRenderStencilState, null, palettedColorDrawEffect);
            Renderer.PushSettings(palettedColorDrawSettings);
            DoForVisibleCells(DrawTerrainTileAndRegisterObjects);
            Renderer.PopSettings();

            // Render objects
            GraphicsDevice.SetRenderTargets(objectsRenderTarget, objectsDepthRenderTarget);

            if (mapInvalidated)
                GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Transparent, 0f, 0);

            DrawSmudges();
            DrawGameObjects();

            // Then draw on-map UI elements
            DrawMapUIElements();

            Renderer.PopRenderTarget();

            refreshStopwatch.Stop();
            Console.WriteLine("Map render time: " + refreshStopwatch.Elapsed.TotalMilliseconds);
        }

        private void DrawMapUIElements()
        {
            Renderer.PushRenderTarget(transparencyRenderTarget, new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null));
            GraphicsDevice.Clear(Color.Transparent);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.BaseNodes) == RenderObjectFlags.BaseNodes)
                DrawBaseNodes();

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.CellTags) == RenderObjectFlags.CellTags)
                DrawCellTags();

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Waypoints) == RenderObjectFlags.Waypoints)
                DrawWaypoints();

            DrawTubes();

            if (EditorState.HighlightImpassableCells)
            {
                Map.DoForAllValidTiles(DrawImpassableHighlight);
            }

            if (EditorState.HighlightIceGrowth)
            {
                Map.DoForAllValidTiles(DrawIceGrowthHighlight);
            }

            Renderer.PopRenderTarget();
        }

        private void SetPaletteEffectParams(Effect effect, Texture2D paletteTexture, bool usePalette, bool useRemap, float opacity, bool isShadow = false)
        {
            if (paletteTexture != null)
            {
                effect.Parameters["PaletteTexture"].SetValue(paletteTexture);
                GraphicsDevice.Textures[2] = paletteTexture;
            }

            effect.Parameters["IsShadow"].SetValue(isShadow);
            effect.Parameters["UsePalette"].SetValue(usePalette);
            effect.Parameters["UseRemap"].SetValue(useRemap);
            effect.Parameters["Opacity"].SetValue(opacity);
        }

        private void DoForVisibleCells(Action<MapTile> action)
        {
            int tlX;
            int tlY;
            int camRight;
            int camBottom;

            if (minimapNeedsRefresh && MinimapUsers.Count > 0)
            {
                // If the minimap needs a full refresh, then we need to re-render the whole map
                tlX = 0;
                tlY = -Constants.MapYBaseline;
                camRight = mapRenderTarget.Width;
                camBottom = mapRenderTarget.Height;
            }
            else
            {
                // Otherwise, screen contents will do.
                // Add some padding to take objects just outside of the visible screen to account
                tlX = Camera.TopLeftPoint.X - Constants.RenderPixelPadding;
                tlY = Camera.TopLeftPoint.Y - Constants.RenderPixelPadding - Constants.MapYBaseline;

                if (tlX < 0)
                    tlX = 0;

                if (tlY < 0)
                    tlY = 0;

                camRight = GetCameraRightXCoord() + Constants.RenderPixelPadding;
                camBottom = GetCameraBottomYCoord() + Constants.RenderPixelPadding;
            }

            Point2D firstVisibleCellCoords = CellMath.CellCoordsFromPixelCoords_2D(new Point2D(tlX, tlY), Map);

            int xCellCount = (camRight - tlX) / Constants.CellSizeX;
            xCellCount += 2; // Add some padding for edge cases

            int yCellCount = (camBottom - tlY) / Constants.CellSizeY;

            // Add some padding to take height levels into account
            const int yPadding = 8;
            yCellCount += yPadding;

            for (int offset = 0; offset < yCellCount; offset++)
            {
                int x = firstVisibleCellCoords.X + offset;
                int y = firstVisibleCellCoords.Y + offset;

                // Draw two horizontal rows of the map

                for (int sx = 0; sx < xCellCount; sx++)
                {
                    int coordX = x + sx;
                    int coordY = y - sx;

                    var cell = Map.GetTile(coordX, coordY);

                    if (cell != null)
                        action(cell);
                }

                for (int sx = 0; sx < xCellCount; sx++)
                {
                    int coordX = x + 1 + sx;
                    int coordY = y - sx;

                    var cell = Map.GetTile(coordX, coordY);

                    if (cell != null)
                        action(cell);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCameraWidth() => (int)(Width / (float)Camera.ZoomLevel);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCameraHeight() => (int)(Height / (float)Camera.ZoomLevel);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCameraRightXCoord() => Math.Min(Camera.TopLeftPoint.X + GetCameraWidth(), Map.Size.X * Constants.CellSizeX);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCameraBottomYCoord() => Math.Min(Camera.TopLeftPoint.Y + GetCameraHeight(), Map.Size.Y * Constants.CellSizeY + Constants.MapYBaseline);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Rectangle GetCameraRectangle() => new Rectangle(Camera.TopLeftPoint.X, Camera.TopLeftPoint.Y, GetCameraWidth(), GetCameraHeight());

        public void DrawTerrainTileAndRegisterObjects(MapTile tile)
        {
            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Terrain) == RenderObjectFlags.Terrain)
                DrawTerrainTile(tile);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Smudges) == RenderObjectFlags.Smudges && tile.Smudge != null)
                smudgesToRender.Add(tile.Smudge);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Overlay) == RenderObjectFlags.Overlay && tile.Overlay != null && tile.Overlay.OverlayType != null)
                AddGameObjectToRender(tile.Overlay);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Structures) == RenderObjectFlags.Structures)
            {
                tile.DoForAllBuildings(structure =>
                {
                    if (structure.Position == tile.CoordsToPoint())
                        AddGameObjectToRender(structure);
                });
            }

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Infantry) == RenderObjectFlags.Infantry)
                tile.DoForAllInfantry(AddGameObjectToRender);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Aircraft) == RenderObjectFlags.Aircraft)
                tile.DoForAllAircraft(AddGameObjectToRender);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Vehicles) == RenderObjectFlags.Vehicles)
                tile.DoForAllVehicles(AddGameObjectToRender);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.TerrainObjects) == RenderObjectFlags.TerrainObjects && tile.TerrainObject != null)
                AddGameObjectToRender(tile.TerrainObject);
        }

        private void AddGameObjectToRender(GameObject gameObject)
        {
            if (objectSpriteRecord.ProcessedObjects.Contains(gameObject))
                return;

            gameObjectsToRender.Add(gameObject);
        }

        public void DrawTerrainTile(MapTile tile)
        {
            if (tile.LastRefreshIndex == refreshIndex)
                return;

            tile.LastRefreshIndex = refreshIndex;

            if (tile.TileIndex >= TheaterGraphics.TileCount)
                return;

            Point2D drawPoint = CellMath.CellTopLeftPointFromCellCoords(new Point2D(tile.X, tile.Y), Map);

            if (tile.TileImage == null)
                tile.TileImage = TheaterGraphics.GetTileGraphics(tile.TileIndex);

            TileImage tileImage;
            int subTileIndex;
            int level;
            if (tile.PreviewTileImage != null)
            {
                tileImage = tile.PreviewTileImage;
                subTileIndex = tile.PreviewSubTileIndex;
                level = tile.PreviewLevel;
            }
            else
            {
                tileImage = tile.TileImage;
                subTileIndex = tile.SubTileIndex;
                level = tile.Level;
            }

            // Framework Mode / Marble Madness support
            if (EditorState.IsMarbleMadness)
                tileImage = TheaterGraphics.GetMarbleMadnessTileGraphics(tileImage.TileID);

            if (subTileIndex >= tileImage.TMPImages.Length)
                return;

            MGTMPImage tmpImage = tileImage.TMPImages[subTileIndex];

            if (tmpImage.TmpImage == null)
                return;

            int drawX = drawPoint.X;
            int drawY = drawPoint.Y;

            if (subTileIndex >= tileImage.TMPImages.Length)
            {
                DrawString(subTileIndex.ToString(), 0, new Vector2(drawPoint.X, drawPoint.Y), Color.Red);
                return;
            }

            if (!EditorState.Is2DMode)
                drawY -= (Constants.CellSizeY / 2) * level;

            float depth = ((drawPoint.Y / (float)mapRenderTarget.Height) * Constants.DownwardsDepthRenderSpace) + (level * Constants.DepthRenderStep);

            // Divide the color by 2f. This is done because unlike map lighting which can exceed 1.0 and go up to 2.0,
            // the Color instance values are capped at 1.0.
            // We lose a bit of precision from doing this, but we'll have to accept that.
            Color color = new Color((float)tile.CellLighting.R / 2f, (float)tile.CellLighting.G / 2f, (float)tile.CellLighting.B / 2f, depth);

            if (tmpImage.Texture != null)
            {
                Texture2D textureToDraw = tmpImage.Texture;

                // Replace terrain lacking MM graphics with colored cells to denote height if we are in marble madness mode
                if (EditorState.IsMarbleMadness && !Constants.IsFlatWorld)
                {
                    if (!TheaterGraphics.HasSeparateMarbleMadnessTileGraphics(tileImage.TileID))
                    {
                        textureToDraw = EditorGraphics.GenericTileWithBorderTexture;
                        color = MarbleMadnessTileHeightLevelColors[level];
                        color = new Color(color.R, color.G, color.B, depth);
                        SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, 1.0f, false);
                    }
                    else
                    {
                        SetPaletteEffectParams(palettedColorDrawEffect, tmpImage.GetPaletteTexture(), true, false, 1.0f, false);
                    }
                }

                DrawTexture(textureToDraw, new Rectangle(drawX, drawY,
                    Constants.CellSizeX, Constants.CellSizeY), null, color, 0f, Vector2.Zero, SpriteEffects.None, depth);
            }

            if (tmpImage.ExtraTexture != null && !EditorState.Is2DMode)
            {
                drawX = drawX + tmpImage.TmpImage.XExtra - tmpImage.TmpImage.X;
                drawY = drawY + tmpImage.TmpImage.YExtra - tmpImage.TmpImage.Y;

                if (EditorState.IsMarbleMadness)
                    SetPaletteEffectParams(palettedColorDrawEffect, tmpImage.GetPaletteTexture(), true, false, 1.0f);

                var exDrawRectangle = new Rectangle(drawX, drawY,
                    tmpImage.ExtraTexture.Width,
                    tmpImage.ExtraTexture.Height);

                DrawTexture(tmpImage.ExtraTexture,
                    exDrawRectangle,
                    null,
                    color,
                    0f,
                    Vector2.Zero, SpriteEffects.None, depth);
            }
        }

        private void DrawWaypoints()
        {
            Map.Waypoints.ForEach(DrawWaypoint);
        }

        private void DrawCellTags()
        {
            DoForVisibleCells(t =>
            {
                if (t.CellTag != null)
                    DrawCellTag(t.CellTag);
            });
        }

        private int CompareGameObjectsForRendering(GameObject obj1, GameObject obj2)
        {
            // Use pixel coords for sorting. Objects closer to the top are rendered first.
            // In case of identical Y coordinates, objects closer to the left take priority.
            // For buildings, we take their foundation into account when calculating their center pixel coords.

            // In case the pixels coords are identical, sort by RTTI type.
            Point2D obj1Point = GetObjectCoordsForComparison(obj1);
            Point2D obj2Point = GetObjectCoordsForComparison(obj2);

            int result = obj1Point.Y.CompareTo(obj2Point.Y);
            if (result != 0)
                return result;

            result = obj1Point.X.CompareTo(obj2Point.X);
            if (result != 0)
                return result;

            return ((int)obj1.WhatAmI()).CompareTo((int)obj2.WhatAmI());
        }

        private Point2D GetObjectCoordsForComparison(GameObject obj)
        {
            return obj.WhatAmI() switch
            {
                RTTIType.Building => buildingRenderer.GetBuildingCenterPoint((Structure)obj),
                RTTIType.Anim => ((Animation)obj).IsBuildingAnim ?
                    buildingRenderer.GetBuildingCenterPoint(((Animation)obj).ParentBuilding) :
                    CellMath.CellCenterPointFromCellCoords(obj.Position, Map),
                _ => CellMath.CellCenterPointFromCellCoords(obj.Position, Map)
            };
        }

        private void DrawSmudges()
        {
            smudgesToRender.Sort(CompareGameObjectsForRendering);

            var colorDrawSettings = new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, depthRenderStencilState, null, palettedColorDrawEffect);
            SetPaletteEffectParams(palettedColorDrawEffect, TheaterGraphics.TheaterPalette.GetTexture(), true, false, 1.0f);
            Renderer.PushSettings(colorDrawSettings);
            for (int i = 0; i < smudgesToRender.Count; i++)
            {
                smudgeRenderer.DrawNonRemap(smudgesToRender[i], smudgeRenderer.GetDrawPoint(smudgesToRender[i]));
            }
            smudgesToRender.ForEach(DrawObject);
            Renderer.PopSettings();
        }

        private void DrawGameObjects()
        {
            gameObjectsToRender.Sort(CompareGameObjectsForRendering);

            for (int i = 0; i < gameObjectsToRender.Count; i++)
            {
                DrawObject(gameObjectsToRender[i]);
                objectSpriteRecord.ProcessedObjects.Add(gameObjectsToRender[i]);
            }

            ProcessObjectSpriteRecord();
        }

        private void DrawObject(GameObject gameObject)
        {
            if (!EditorState.RenderInvisibleInGameObjects && gameObject.IsInvisibleInGame())
                return;

            switch (gameObject.WhatAmI())
            {
                case RTTIType.Aircraft:
                    aircraftRenderer.Draw(gameObject as Aircraft, false);
                    return;
                case RTTIType.Anim:
                    animRenderer.Draw(gameObject as Animation, false);
                    return;
                case RTTIType.Building:
                    buildingRenderer.Draw(gameObject as Structure, false);
                    return;
                case RTTIType.Infantry:
                    infantryRenderer.Draw(gameObject as Infantry, false);
                    return;
                case RTTIType.Overlay:
                    overlayRenderer.Draw(gameObject as Overlay, false);
                    return;
                case RTTIType.Smudge:
                    smudgeRenderer.Draw(gameObject as Smudge, false);
                    return;
                case RTTIType.Terrain:
                    terrainRenderer.Draw(gameObject as TerrainObject, false);
                    return;
                case RTTIType.Unit:
                    unitRenderer.Draw(gameObject as Unit, false);
                    return;
                default:
                    throw new NotImplementedException("No renderer implemented for type " + gameObject.WhatAmI());
            }
        }

        private void ProcessObjectSpriteRecord()
        {
            if (objectSpriteRecord.LineEntries.Count > 0)
            {
                SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, 1.0f, false);
                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.Opaque, null, depthRenderStencilState, null, palettedColorDrawEffect));

                for (int i = 0; i < objectSpriteRecord.LineEntries.Count; i++)
                {
                    var lineEntry = objectSpriteRecord.LineEntries[i];
                    Renderer.DrawLine(lineEntry.Source, lineEntry.Destination,
                        new Color(lineEntry.Color.R / 255.0f, lineEntry.Color.G / 255.0f, lineEntry.Color.B / 255.0f, lineEntry.Depth),
                        lineEntry.Thickness, lineEntry.Depth);
                }

                Renderer.PopSettings();
            }

            foreach (var kvp in objectSpriteRecord.SpriteEntries)
            {
                Texture2D paletteTexture = kvp.Key.Item1;
                bool isRemap = kvp.Key.Item2;

                SetPaletteEffectParams(palettedColorDrawEffect, paletteTexture, true, isRemap, 1.0f, false);
                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, depthRenderStencilState, null, palettedColorDrawEffect));

                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    var spriteEntry = kvp.Value[i];
                    Renderer.DrawTexture(spriteEntry.Texture, spriteEntry.DrawingBounds, null, spriteEntry.Color, 0f, Vector2.Zero, SpriteEffects.None, spriteEntry.Depth);
                }

                Renderer.PopSettings();
            }

            if (objectSpriteRecord.NonPalettedSpriteEntries.Count > 0)
            {
                SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, 1.0f, false);
                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, depthRenderStencilState, null, palettedColorDrawEffect));

                for (int i = 0; i < objectSpriteRecord.NonPalettedSpriteEntries.Count; i++)
                {
                    var spriteEntry = objectSpriteRecord.NonPalettedSpriteEntries[i];
                    Renderer.DrawTexture(spriteEntry.Texture, spriteEntry.DrawingBounds, null, spriteEntry.Color, 0f, Vector2.Zero, SpriteEffects.None, spriteEntry.Depth);
                }

                Renderer.PopSettings();
            }

            if (objectSpriteRecord.ShadowEntries.Count > 0)
            {
                SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, 1.0f, true);
                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, depthRenderStencilState, null, palettedColorDrawEffect));

                for (int i = 0; i < objectSpriteRecord.ShadowEntries.Count; i++)
                {
                    var spriteEntry = objectSpriteRecord.ShadowEntries[i];

                    // It doesn't really matter what we give as color to the shadow
                    Renderer.DrawTexture(spriteEntry.Texture, spriteEntry.DrawingBounds, null, new Color(1.0f, 1.0f, 1.0f, spriteEntry.Depth), 0f, Vector2.Zero, SpriteEffects.None, spriteEntry.Depth);
                }

                Renderer.PopSettings();
            }

            if (objectSpriteRecord.TextEntries.Count > 0)
            {
                SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, 1.0f, false);
                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.Opaque, null, depthRenderStencilState, null, palettedColorDrawEffect));

                for (int i = 0; i < objectSpriteRecord.TextEntries.Count; i++)
                {
                    var textEntry = objectSpriteRecord.TextEntries[i];
                    Renderer.DrawStringWithShadow(textEntry.Text, Constants.UIBoldFont, textEntry.DrawPoint.ToXNAVector(), textEntry.Color, 1f, 1f, 1f);
                }

                Renderer.PopSettings();
            }
        }

        private void DrawBaseNodes()
        {
            Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, palettedColorDrawEffect));
            foreach (var baseNode in Map.GraphicalBaseNodes)
            {
                DrawBaseNode(baseNode, true);
            }
            Renderer.PopSettings();
        }

        private void DrawBaseNode(GraphicalBaseNode graphicalBaseNode, bool drawGraphics)
        {
            // TODO this approach would give us much simpler code,
            // but for some reason it causes artifacts in terrain outside of the camera

            // if (graphicalBaseNode.Structure == null)
            // {
            //     graphicalBaseNode.Structure = new Structure(graphicalBaseNode.BuildingType);
            //     graphicalBaseNode.Structure.Owner = graphicalBaseNode.Owner;
            //     graphicalBaseNode.Structure.Position = graphicalBaseNode.BaseNode.Position;
            //     graphicalBaseNode.Structure.IsBaseNodeDummy = true;
            // }
            // 
            // buildingRenderer.Draw(graphicalBaseNode.Structure, true);
            // return;

            int baseNodeIndex = graphicalBaseNode.Owner.BaseNodes.FindIndex(bn => bn == graphicalBaseNode.BaseNode);
            Color baseNodeIndexColor = Color.White * 0.7f;

            Point2D drawPoint = CellMath.CellTopLeftPointFromCellCoords_3D(graphicalBaseNode.BaseNode.Position, Map);

            ShapeImage bibGraphics = TheaterGraphics.BuildingBibTextures[graphicalBaseNode.BuildingType.Index];
            ShapeImage graphics = TheaterGraphics.BuildingTextures[graphicalBaseNode.BuildingType.Index];
            Color replacementColor = Color.DarkBlue;
            string iniName = graphicalBaseNode.BuildingType.ININame;
            Color remapColor = graphicalBaseNode.BuildingType.ArtConfig.Remapable ? graphicalBaseNode.Owner.XNAColor : Color.White;

            const float opacity = 0.25f;

            int yDrawOffset = Constants.CellSizeY / -2;
            int frameIndex = 0;

            if ((graphics == null || graphics.GetFrame(frameIndex) == null) && (bibGraphics == null || bibGraphics.GetFrame(0) == null))
            {
                SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, 1.0f);
                DrawStringWithShadow(iniName, Constants.UIBoldFont, drawPoint.ToXNAVector(), replacementColor, 1.0f);
                DrawStringWithShadow("#" + baseNodeIndex, Constants.UIBoldFont, drawPoint.ToXNAVector() + new Vector2(0f, 20f), baseNodeIndexColor);
                return;
            }

            var cell = Map.GetTile(graphicalBaseNode.BaseNode.Position);
            var lighting = cell == null ? Vector4.One : cell.CellLighting.ToXNAVector4Ambient();

            Texture2D texture;

            if (bibGraphics != null)
            {
                PositionedTexture bibFrame = bibGraphics.GetFrame(0);

                if (bibFrame != null && bibFrame.Texture != null)
                {
                    texture = bibFrame.Texture;

                    int bibFinalDrawPointX = drawPoint.X - bibFrame.ShapeWidth / 2 + bibFrame.OffsetX + Constants.CellSizeX / 2;
                    int bibFinalDrawPointY = drawPoint.Y - bibFrame.ShapeHeight / 2 + bibFrame.OffsetY + Constants.CellSizeY / 2 + yDrawOffset;

                    SetPaletteEffectParams(palettedColorDrawEffect, bibGraphics.GetPaletteTexture(), true, true, opacity);

                    DrawTexture(texture, new Rectangle(
                        bibFinalDrawPointX, bibFinalDrawPointY,
                        texture.Width, texture.Height),
                        null, remapColor,
                        0f, Vector2.Zero, SpriteEffects.None, 0f);

                    if (bibGraphics.HasRemapFrames())
                    {
                        DrawTexture(bibGraphics.GetRemapFrame(0).Texture,
                            new Rectangle(bibFinalDrawPointX, bibFinalDrawPointY, texture.Width, texture.Height),
                            null,
                            remapColor,
                            0f,
                            Vector2.Zero,
                            SpriteEffects.None,
                            0f);
                    }
                }
            }

            var frame = graphics.GetFrame(frameIndex);
            if (frame == null)
            {
                SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, 1.0f);
                DrawStringWithShadow("#" + baseNodeIndex, Constants.UIBoldFont, drawPoint.ToXNAVector(), baseNodeIndexColor);
                return;
            }

            texture = frame.Texture;

            int x = drawPoint.X - frame.ShapeWidth / 2 + frame.OffsetX + Constants.CellSizeX / 2;
            int y = drawPoint.Y - frame.ShapeHeight / 2 + frame.OffsetY + Constants.CellSizeY / 2 + yDrawOffset;
            int width = texture.Width;
            int height = texture.Height;
            Rectangle drawRectangle = new Rectangle(x, y, width, height);

            SetPaletteEffectParams(palettedColorDrawEffect, graphics.GetPaletteTexture(), true, true, opacity);

            DrawTexture(texture, drawRectangle, remapColor);

            if (graphics.HasRemapFrames())
            {
                DrawTexture(graphics.GetRemapFrame(frameIndex).Texture, drawRectangle, remapColor);
            }

            SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, 1.0f);
            DrawStringWithShadow("#" + baseNodeIndex, Constants.UIBoldFont, drawPoint.ToXNAVector(), baseNodeIndexColor);
        }

        private void DrawWaypoint(Waypoint waypoint)
        {
            Point2D drawPoint = CellMath.CellTopLeftPointFromCellCoords(waypoint.Position, Map);

            var cell = Map.GetTile(waypoint.Position);
            if (cell != null && !EditorState.Is2DMode)
                drawPoint -= new Point2D(0, cell.Level * Constants.CellHeight);

            Color waypointColor = string.IsNullOrEmpty(waypoint.EditorColor) ? Color.Fuchsia : waypoint.XNAColor;

            DrawTexture(EditorGraphics.GenericTileTexture, drawPoint.ToXNAPoint(), new Color(0, 0, 0, 128));
            DrawTexture(EditorGraphics.TileBorderTexture, drawPoint.ToXNAPoint(), waypointColor);

            int fontIndex = Constants.UIBoldFont;
            string waypointIdentifier = waypoint.Identifier.ToString();
            var textDimensions = Renderer.GetTextDimensions(waypointIdentifier, fontIndex);
            DrawStringWithShadow(waypointIdentifier,
                fontIndex,
                new Vector2(drawPoint.X + ((Constants.CellSizeX - textDimensions.X) / 2), drawPoint.Y + ((Constants.CellSizeY - textDimensions.Y) / 2)),
                waypointColor);
        }

        private void DrawCellTag(CellTag cellTag)
        {
            Point2D drawPoint = EditorState.Is2DMode ? 
                CellMath.CellTopLeftPointFromCellCoords(cellTag.Position, Map) : 
                CellMath.CellTopLeftPointFromCellCoords_3D(cellTag.Position, Map);

            const float cellTagAlpha = 0.45f;

            Color color = cellTag.Tag.Trigger.EditorColor == null ? UISettings.ActiveSettings.AltColor : cellTag.Tag.Trigger.XNAColor;
            DrawTexture(EditorGraphics.CellTagTexture, drawPoint.ToXNAPoint(), color * cellTagAlpha);
        }

        public Rectangle GetMapLocalViewRectangle()
        {
            const int InitialHeight = 3; // TS engine assumes that the first cell is at a height of 2
            const double HeightAddition = 4.5; // TS engine adds 4.5 to specified map height <3

            int x = (int)(Map.LocalSize.X * Constants.CellSizeX);
            int y = (int)(Map.LocalSize.Y - InitialHeight) * Constants.CellSizeY + Constants.MapYBaseline;
            int width = (int)(Map.LocalSize.Width * Constants.CellSizeX);
            int height = (int)(Map.LocalSize.Height + HeightAddition) * Constants.CellSizeY;

            return new Rectangle(x, y, width, height);
        }

        private void DrawMapBorder()
        {
            const int BorderThickness = 4;

            const int TopImpassableCellCount = 3; // The northernmost 3 cells are impassable in the TS engine, we'll also display this border

            var rectangle = GetMapLocalViewRectangle();

            DrawRectangle(rectangle, Color.Blue, BorderThickness);

            int impassableY = (int)(rectangle.Y + (Constants.CellSizeY * TopImpassableCellCount));
            FillRectangle(new Rectangle(rectangle.X, impassableY - (BorderThickness / 2), rectangle.Width, BorderThickness), Color.Teal * 0.25f);

            // old code for rendering directly to screen
            /*
            const int BorderThickness = 4;
            const int InitialHeight = 3; // TS engine assumes that the first cell is at a height of 2
            const double HeightAddition = 4.5; // TS engine adds 4.5 to specified map height <3
            const int TopImpassableCellCount = 3; // The northernmost 3 cells are impassable in the TS engine, we'll also display this border

            int x = (int)((Map.LocalSize.X * Constants.CellSizeX - Camera.TopLeftPoint.X) * Camera.ZoomLevel);
            int y = (int)((((Map.LocalSize.Y - InitialHeight) * Constants.CellSizeY) - Camera.TopLeftPoint.Y) * Camera.ZoomLevel);
            int width = (int)((Map.LocalSize.Width * Constants.CellSizeX) * Camera.ZoomLevel);
            int height = (int)((Map.LocalSize.Height + HeightAddition) * Constants.CellSizeY * Camera.ZoomLevel);

            DrawRectangle(new Rectangle(x, y, width, height), Color.Blue, BorderThickness);

            int impassableY = (int)(y + (Constants.CellSizeY * TopImpassableCellCount * Camera.ZoomLevel));
            FillRectangle(new Rectangle(x, impassableY - (BorderThickness / 2), width, BorderThickness), Color.Teal * 0.25f);
            */
        }

        private void DrawTechnoRangeIndicators()
        {
            if (TechnoUnderCursor == null)
                return;

            double range = TechnoUnderCursor.GetWeaponRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(TechnoUnderCursor.Position, range, TechnoUnderCursor.Owner.XNAColor);
            }

            range = TechnoUnderCursor.GetGuardRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(TechnoUnderCursor.Position, range, TechnoUnderCursor.Owner.XNAColor * 0.25f);
            }

            range = TechnoUnderCursor.GetGapGeneratorRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(TechnoUnderCursor.Position, range, Color.Black * 0.75f);
            }

            range = TechnoUnderCursor.GetCloakGeneratorRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(TechnoUnderCursor.Position, range, TechnoUnderCursor.GetRadialColor());
            }

            range = TechnoUnderCursor.GetSensorArrayRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(TechnoUnderCursor.Position, range, TechnoUnderCursor.GetRadialColor());
            }
        }

        private void DrawRangeIndicator(Point2D cellCoords, double range, Color color)
        {
            Point2D center = EditorState.Is2DMode ? 
                CellMath.CellCenterPointFromCellCoords(cellCoords, Map) : 
                CellMath.CellCenterPointFromCellCoords_3D(cellCoords, Map);

            // Range is specified in "tile edge lengths",
            // so we need a bit of trigonometry
            double horizontalPixelRange = Constants.CellSizeX / Math.Sqrt(2.0);
            double verticalPixelRange = Constants.CellSizeY / Math.Sqrt(2.0);

            int startX = center.X - (int)(range * horizontalPixelRange);
            int startY = center.Y - (int)(range * verticalPixelRange);
            int endX = center.X + (int)(range * horizontalPixelRange);
            int endY = center.Y + (int)(range * verticalPixelRange);

            // startX = Camera.ScaleIntWithZoom(startX - Camera.TopLeftPoint.X);
            // startY = Camera.ScaleIntWithZoom(startY - Camera.TopLeftPoint.Y);
            // endX = Camera.ScaleIntWithZoom(endX - Camera.TopLeftPoint.X);
            // endY = Camera.ScaleIntWithZoom(endY - Camera.TopLeftPoint.Y);

            DrawTexture(EditorGraphics.RangeIndicatorTexture,
                new Rectangle(startX, startY, endX - startX, endY - startY), color);
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

            // Attempt dragging or rotating an object
            if (CursorAction == null && tileUnderCursor != null && Cursor.LeftPressedDown && !isDraggingObject && !isRotatingObject)
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
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.F10)
            {
                debugRenderDepthBuffer = !debugRenderDepthBuffer;
            }

            if (!e.Handled && CursorAction != null && CursorAction.HandlesKeyboardInput)
            {
                CursorAction.OnKeyPressed(e, tileUnderCursor == null ? Point2D.NegativeOne : tileUnderCursor.CoordsToPoint());
            }
        }

        private void DrawOnTileUnderCursor()
        {
            if (!IsActive)
                return;

            if (tileUnderCursor == null)
            {
                DrawString("Null tile", 0, new Vector2(0f, 40f), Color.White);
                return;
            }

            if (CursorAction != null)
            {
                if (CursorAction.DrawCellCursor)
                    DrawTileCursor();

                return;
            }


            if (isDraggingObject)
            {
                var startCell = Map.GetTile(draggedOrRotatedObject.Position);
                if (startCell == tileUnderCursor)
                    return;

                bool isCloning = KeyboardCommands.Instance.CloneObject.AreKeysOrModifiersDown(Keyboard);
                bool overlapObjects = KeyboardCommands.Instance.OverlapObjects.AreKeysOrModifiersDown(Keyboard);

                Color lineColor = isCloning ? new Color(0, 255, 255) : Color.White;
                if (!Map.CanPlaceObjectAt(draggedOrRotatedObject, tileUnderCursor.CoordsToPoint(), isCloning, overlapObjects) ||
                    (isCloning && !draggedOrRotatedObject.IsTechno() && draggedOrRotatedObject.WhatAmI() != RTTIType.Terrain))
                    lineColor = Color.Red;

                Point2D cameraAndCellCenterOffset = new Point2D(-Camera.TopLeftPoint.X + Constants.CellSizeX / 2,
                                                 -Camera.TopLeftPoint.Y + Constants.CellSizeY / 2);

                Point2D startDrawPoint = CellMath.CellTopLeftPointFromCellCoords(draggedOrRotatedObject.Position, Map) + cameraAndCellCenterOffset;
                
                if (startCell != null)
                {
                    startDrawPoint -= new Point2D(0, startCell.Level * Constants.CellHeight);

                    if (draggedOrRotatedObject.WhatAmI() == RTTIType.Infantry)
                        startDrawPoint += CellMath.GetSubCellOffset(((Infantry)draggedOrRotatedObject).SubCell) - new Point2D(0, Constants.CellHeight / 2);
                }

                Point2D endDrawPoint = CellMath.CellTopLeftPointFromCellCoords(tileUnderCursor.CoordsToPoint(), Map) + cameraAndCellCenterOffset;

                endDrawPoint -= new Point2D(0, tileUnderCursor.Level * Constants.CellHeight);

                startDrawPoint = startDrawPoint.ScaleBy(Camera.ZoomLevel);
                endDrawPoint = endDrawPoint.ScaleBy(Camera.ZoomLevel);

                DrawLine(startDrawPoint.ToXNAVector(), endDrawPoint.ToXNAVector(), lineColor, 1);
            }
            else if (isRotatingObject)
            {
                var startCell = Map.GetTile(draggedOrRotatedObject.Position);
                if (startCell == tileUnderCursor)
                    return;

                Color lineColor = Color.Yellow;

                Point2D cameraAndCellCenterOffset = new Point2D(-Camera.TopLeftPoint.X + Constants.CellSizeX / 2,
                                                 -Camera.TopLeftPoint.Y + Constants.CellSizeY / 2);

                Point2D startDrawPoint = CellMath.CellTopLeftPointFromCellCoords(draggedOrRotatedObject.Position, Map) + cameraAndCellCenterOffset;
                
                if (startCell != null)
                {
                    startDrawPoint -= new Point2D(0, Map.GetTile(draggedOrRotatedObject.Position).Level * Constants.CellHeight);

                    if (draggedOrRotatedObject.WhatAmI() == RTTIType.Infantry)
                        startDrawPoint += CellMath.GetSubCellOffset(((Infantry)draggedOrRotatedObject).SubCell) - new Point2D(0, Constants.CellHeight / 2);
                }

                Point2D endDrawPoint = CellMath.CellTopLeftPointFromCellCoords(tileUnderCursor.CoordsToPoint(), Map) + cameraAndCellCenterOffset;

                endDrawPoint -= new Point2D(0, tileUnderCursor.Level * Constants.CellHeight);

                startDrawPoint = startDrawPoint.ScaleBy(Camera.ZoomLevel);
                endDrawPoint = endDrawPoint.ScaleBy(Camera.ZoomLevel);

                DrawLine(startDrawPoint.ToXNAVector(), endDrawPoint.ToXNAVector(), lineColor, 1);

                if (draggedOrRotatedObject.IsTechno())
                {
                    var techno = (TechnoBase)draggedOrRotatedObject;
                    Point2D point = tileUnderCursor.CoordsToPoint() - draggedOrRotatedObject.Position;

                    float angle = point.Angle() + ((float)Math.PI / 2.0f);
                    if (angle > (float)Math.PI * 2.0f)
                    {
                        angle = angle - ((float)Math.PI * 2.0f);
                    }
                    else if (angle < 0f)
                    {
                        angle += (float)Math.PI * 2.0f;
                    }

                    float percent = angle / ((float)Math.PI * 2.0f);
                    byte facing = (byte)Math.Ceiling(percent * (float)byte.MaxValue);

                    techno.Facing = facing;
                    AddRefreshPoint(techno.Position, 2);
                }
            }
            else
            {
                DrawTileCursor();
            }
        }

        private void DrawTileCursor()
        {
            Color lineColor = new Color(96, 168, 96, 128);
            Point2D cellTopLeftPoint = CellMath.CellTopLeftPointFromCellCoords(new Point2D(tileUnderCursor.X, tileUnderCursor.Y), Map) - Camera.TopLeftPoint;

            int height = EditorState.Is2DMode ? 0 : tileUnderCursor.Level * Constants.CellHeight;

            cellTopLeftPoint = new Point2D((int)(cellTopLeftPoint.X * Camera.ZoomLevel), (int)((cellTopLeftPoint.Y - height) * Camera.ZoomLevel));

            var cellTopPoint = new Vector2(cellTopLeftPoint.X + (int)((Constants.CellSizeX / 2) * Camera.ZoomLevel), cellTopLeftPoint.Y);
            var cellLeftPoint = new Vector2(cellTopLeftPoint.X, cellTopLeftPoint.Y + (int)((Constants.CellSizeY / 2) * Camera.ZoomLevel));
            var cellRightPoint = new Vector2(cellTopLeftPoint.X + (int)(Constants.CellSizeX * Camera.ZoomLevel), cellLeftPoint.Y);
            var cellBottomPoint = new Vector2(cellTopPoint.X, cellTopLeftPoint.Y + (int)(Constants.CellSizeY * Camera.ZoomLevel));

            DrawLine(cellTopPoint, cellLeftPoint, lineColor, 1);
            DrawLine(cellRightPoint, cellTopPoint, lineColor, 1);
            DrawLine(cellBottomPoint, cellLeftPoint, lineColor, 1);
            DrawLine(cellRightPoint, cellBottomPoint, lineColor, 1);

            var shadowColor = new Color(0, 0, 0, 128);
            var down = new Vector2(0, 1f);

            DrawLine(cellTopPoint + down, cellLeftPoint + down, shadowColor, 1);
            DrawLine(cellRightPoint + down, cellTopPoint + down, shadowColor, 1);
            DrawLine(cellBottomPoint + down, cellLeftPoint + down, shadowColor, 1);
            DrawLine(cellRightPoint + down, cellBottomPoint + down, shadowColor, 1);

            int zoomedHeight = (int)(height * Camera.ZoomLevel);

            Color heightBarColor = Color.Black * 0.25f;
            FillRectangle(new Rectangle((int)cellLeftPoint.X, (int)cellLeftPoint.Y, 1, zoomedHeight), heightBarColor);
            FillRectangle(new Rectangle((int)cellBottomPoint.X, (int)cellBottomPoint.Y, 1, zoomedHeight), heightBarColor);
            FillRectangle(new Rectangle((int)cellRightPoint.X, (int)cellRightPoint.Y, 1, zoomedHeight), heightBarColor);
        }

        private void DrawImpassableHighlight(MapTile cell)
        {
            if (!Helpers.IsLandTypeImpassable(TheaterGraphics.GetTileGraphics(cell.TileIndex).GetSubTile(cell.SubTileIndex).TmpImage.TerrainType, false) && 
                (cell.Overlay == null || cell.Overlay.OverlayType == null || !Helpers.IsLandTypeImpassable(cell.Overlay.OverlayType.Land, false)))
            {
                return;
            }

            Point2D cellTopLeftPoint = EditorState.Is2DMode ?
                CellMath.CellTopLeftPointFromCellCoords(cell.CoordsToPoint(), Map) :
                CellMath.CellTopLeftPointFromCellCoords_3D(cell.CoordsToPoint(), Map);

            DrawTexture(EditorGraphics.ImpassableCellHighlightTexture, cellTopLeftPoint.ToXNAPoint(), Color.White);
        }

        private void DrawIceGrowthHighlight(MapTile cell)
        {
            if (cell.IceGrowth <= 0)
                return;

            Point2D cellTopLeftPoint = EditorState.Is2DMode ?
                CellMath.CellTopLeftPointFromCellCoords(cell.CoordsToPoint(), Map) :
                CellMath.CellTopLeftPointFromCellCoords_3D(cell.CoordsToPoint(), Map);

            DrawTexture(EditorGraphics.IceGrowthHighlightTexture, cellTopLeftPoint.ToXNAPoint(), Color.White);
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

        private void DrawTubes()
        {
            foreach (var tube in Map.Tubes)
            {
                var entryCellCenterPoint = CellMath.CellCenterPointFromCellCoords(tube.EntryPoint, Map);
                var exitCellCenterPoint = CellMath.CellCenterPointFromCellCoords(tube.ExitPoint, Map);
                var entryCell = Map.GetTile(tube.EntryPoint);
                int height = 0;
                if (entryCell != null && !EditorState.Is2DMode)
                    height = entryCell.Level * Constants.CellHeight;

                Point2D currentPoint = tube.EntryPoint;

                Color color = tube.Pending ? Color.Orange : Color.LimeGreen;

                if (tube.Directions.Count == 0)
                {
                    DrawTexture(EditorGraphics.GenericTileWithBorderTexture,
                        CellMath.CellTopLeftPointFromCellCoords_3D(tube.EntryPoint, Map).ToXNAPoint(), color);
                }

                foreach (var direction in tube.Directions)
                {
                    Point2D nextPoint = currentPoint.NextPointFromTubeDirection(direction);

                    if (nextPoint != currentPoint)
                    {
                        var currentPixelPoint = CellMath.CellCenterPointFromCellCoords(currentPoint, Map);
                        var nextPixelPoint = CellMath.CellCenterPointFromCellCoords(nextPoint, Map);

                        DrawArrow(currentPixelPoint.ToXNAVector() - new Vector2(0, height),
                            nextPixelPoint.ToXNAVector() - new Vector2(0, height),
                            color, 0.25f, 10f, 1);
                    }

                    currentPoint = nextPoint;
                }
            }
        }

        private static void DrawArrow(Vector2 start, Vector2 end,
            Color color, float angleDiff, float sideLineLength, int thickness = 1)
            => RendererExtensions.DrawArrow(start, end, color, angleDiff, sideLineLength, thickness);

        public override void Draw(GameTime gameTime)
        {
            DrawMap();

            base.Draw(gameTime);
        }

        private void DrawMap()
        {
            if (IsActive && tileUnderCursor != null && CursorAction != null)
            {
                CursorAction.PreMapDraw(tileUnderCursor.CoordsToPoint());
            }

            if (mapInvalidated || cameraMoved)
            {
                DrawVisibleMapPortion();
                mapInvalidated = false;
                cameraMoved = false;
            }

            CalculateMapRenderRectangles();

            DrawPerFrameTransparentElements();

            DrawWorld();

            if (EditorState.DrawMapWideOverlay)
            {
                MapWideOverlay.Draw(new Rectangle(
                        (int)(-Camera.TopLeftPoint.X * Camera.ZoomLevel),
                        (int)((-Camera.TopLeftPoint.Y + Constants.MapYBaseline) * Camera.ZoomLevel),
                        (int)(mapRenderTarget.Width * Camera.ZoomLevel),
                        (int)((mapRenderTarget.Height - Constants.MapYBaseline) * Camera.ZoomLevel)));
            }

            if (IsActive && tileUnderCursor != null && CursorAction != null)
            {
                CursorAction.PostMapDraw(tileUnderCursor.CoordsToPoint());
                CursorAction.DrawPreview(tileUnderCursor.CoordsToPoint(), Camera.TopLeftPoint);
            }

            DrawOnTileUnderCursor();

            DrawOnMinimap();
        }

        private void DrawPerFrameTransparentElements()
        {
            Renderer.PushRenderTarget(transparencyPerFrameRenderTarget);

            GraphicsDevice.Clear(Color.Transparent);

            DrawMapBorder();
            DrawTechnoRangeIndicators();

            Renderer.PopRenderTarget();
        }

        /// <summary>
        /// Draws the visible part of the map to the minimap.
        /// </summary>
        private void DrawOnMinimap()
        {
            if (MinimapUsers.Count > 0)
            {
                Renderer.PushRenderTarget(minimapRenderTarget);

                if (minimapNeedsRefresh)
                {
                    DrawTexture(compositeRenderTarget,
                        new Rectangle(0, 0, mapRenderTarget.Width, mapRenderTarget.Height),
                        new Rectangle(0, 0, mapRenderTarget.Width, mapRenderTarget.Height),
                        Color.White);
                }
                else
                {
                    DrawTexture(compositeRenderTarget,
                        mapRenderSourceRectangle,
                        mapRenderSourceRectangle,
                        Color.White);
                }

                Renderer.PopRenderTarget();
            }

            minimapNeedsRefresh = false;
        }

        private void CalculateMapRenderRectangles()
        {
            int zoomedWidth = (int)(Width / Camera.ZoomLevel);
            int zoomedHeight = (int)(Height / Camera.ZoomLevel);

            // Constrain draw coordinates so that we don't draw out of bounds and cause weird artifacts on map edge

            int sourceX = Camera.TopLeftPoint.X;
            int destinationX = 0;
            int destinationWidth = Width;
            if (sourceX < 0)
            {
                sourceX = 0;
                destinationX = (int)(-Camera.TopLeftPoint.X * Camera.ZoomLevel);
                destinationWidth -= destinationX;
                zoomedWidth += Camera.TopLeftPoint.X;
            }

            int sourceY = Camera.TopLeftPoint.Y;
            int destinationY = 0;
            int destinationHeight = Height;
            if (sourceY < 0)
            {
                sourceY = 0;
                destinationY = (int)(-Camera.TopLeftPoint.Y * Camera.ZoomLevel);
                destinationHeight -= destinationY;
                zoomedHeight += Camera.TopLeftPoint.Y;
            }

            if (sourceX + zoomedWidth > mapRenderTarget.Width)
            {
                zoomedWidth = mapRenderTarget.Width - sourceX;
                destinationWidth = (int)(zoomedWidth * Camera.ZoomLevel);
            }

            if (sourceY + zoomedHeight > mapRenderTarget.Height)
            {
                zoomedHeight = mapRenderTarget.Height - sourceY;
                destinationHeight = (int)(zoomedHeight * Camera.ZoomLevel);
            }

            mapRenderSourceRectangle = new Rectangle(sourceX, sourceY, zoomedWidth, zoomedHeight);
            mapRenderDestinationRectangle = new Rectangle(destinationX, destinationY, destinationWidth, destinationHeight);
        }

        private void DrawWorld()
        {
            var dss = new DepthStencilState()
            {
                DepthBufferEnable = true,
                DepthBufferWriteEnable = true,
                DepthBufferFunction = CompareFunction.GreaterEqual,
            };

            Rectangle sourceRectangle = new Rectangle(0, 0, mapRenderTarget.Width, mapRenderTarget.Height);
            Rectangle destinationRectangle = sourceRectangle;

            combineDrawEffect.Parameters["TerrainDepthTexture"].SetValue(mapDepthRenderTarget);
            combineDrawEffect.Parameters["ObjectsDepthTexture"].SetValue(objectsDepthRenderTarget);

            GraphicsDevice.SetRenderTarget(compositeRenderTarget);

            GraphicsDevice.Clear(Color.Black);

            // First, draw the map to the composite render target as a base.
            DrawTexture(mapRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            // Then draw objects to the composite render target, making use of our custom shader.
            Renderer.PushRenderTarget(compositeRenderTarget, new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, dss, null, combineDrawEffect));
            DrawTexture(objectsRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            // Then draw transparency layers, without using a custom shader.
            Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null));

            DrawTexture(transparencyRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            DrawTexture(transparencyPerFrameRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            Renderer.PopSettings();

            Renderer.PopRenderTarget();

            // Last, draw the composite render target directly to the screen.

            Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null));

            DrawTexture(compositeRenderTarget,
                mapRenderSourceRectangle,
                mapRenderDestinationRectangle,
                Color.White);

            Renderer.PopSettings();
        }

        public void AddPreviewToMap()
        {
            InstantRenderMinimap();

            // Include only the part within LocalSize
            var visibleRectangle = GetMapLocalViewRectangle();
            var localSizeRenderTarget = new RenderTarget2D(GraphicsDevice, visibleRectangle.Width, visibleRectangle.Height, false, SurfaceFormat.Color, DepthFormat.None);
            Renderer.BeginDraw();
            Renderer.PushRenderTarget(localSizeRenderTarget);
            Renderer.DrawTexture(MinimapTexture, visibleRectangle, new Rectangle(0, 0, localSizeRenderTarget.Width, localSizeRenderTarget.Height), Color.White);
            Renderer.PopRenderTarget();
            Renderer.EndDraw();

            // Scale down the minimap texture
            var finalPreviewRenderTarget = new RenderTarget2D(GraphicsDevice, Constants.MapPreviewMaxWidth, Constants.MapPreviewMaxHeight, false, SurfaceFormat.Color, DepthFormat.None);
            var minimapTexture = Helpers.RenderTextureAsSmaller(localSizeRenderTarget, finalPreviewRenderTarget, GraphicsDevice);

            // Cleanup
            localSizeRenderTarget.Dispose();
            finalPreviewRenderTarget.Dispose();
            MinimapUsers.Remove(this);

            Map.WritePreview(minimapTexture);
            InvalidateMapForMinimap();
        }

        public void ExtractMegamapTo(string path)
        {
            InstantRenderMinimap();

            using (var stream = File.OpenWrite(path))
            {
                MinimapTexture.SaveAsPng(stream, MinimapTexture.Width, MinimapTexture.Height);
            }
        }

        private void InstantRenderMinimap()
        {
            EditorState.RenderInvisibleInGameObjects = false;

            // Register ourselves as a minimap user so the minimap texture gets refreshed
            MinimapUsers.Add(this);

            Renderer.BeginDraw();

            // Clear out existing map UI
            Renderer.PushRenderTarget(transparencyPerFrameRenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            Renderer.PopRenderTarget();

            // Maybe someone could want to generate a preview with these for some reason...?
            // EditorState.Is2DMode = false;
            // EditorState.IsMarbleMadness = false;
            // EditorState.LightingPreviewState = LightingPreviewMode.Normal;
            // EditorState.DrawMapWideOverlay = false;

            InvalidateMapForMinimap();
            DrawVisibleMapPortion();
            CalculateMapRenderRectangles();
            DrawWorld();
            DrawOnMinimap();

            mapInvalidated = false;
            cameraMoved = false;

            Renderer.EndDraw();

            MinimapUsers.Remove(this);

            EditorState.RenderInvisibleInGameObjects = true;
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
