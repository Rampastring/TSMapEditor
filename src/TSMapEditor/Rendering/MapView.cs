using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TSMapEditor.GameMath;
using TSMapEditor.Misc;
using TSMapEditor.Models;
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
            Camera.CameraUpdated += (s, e) => cameraMoved = true;
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
        private RenderTarget2D depthRenderTarget;                    // Depth buffer
        private RenderTarget2D depthRenderTargetCopy;                // Copy of depth buffer so it can be sampled while rendering depth
        private RenderTarget2D objectRenderTarget;                   // Object render target
        private RenderTarget2D shadowRenderTarget;                   // Currently unused
        private RenderTarget2D transparencyRenderTarget;             // Render target for map UI elements (celltags etc.) that are only refreshed if something in the map changes (due to performance reasons)
        private RenderTarget2D transparencyPerFrameRenderTarget;     // Render target for map UI elements that are redrawn each frame
        private RenderTarget2D compositeRenderTarget;                // Render target where all the above is combined
        private RenderTarget2D minimapRenderTarget;                  // For minimap and megamap rendering

        private Effect colorDrawEffect;                              // Effect for rendering RGBA textures with depth testing
        private Effect palettedColorDrawEffect;                      // Effect for rendering paletted textures with depth testing
        private Effect depthApplyEffect;                             // Effect for rendering to depth buffer

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

        private Stopwatch refreshStopwatch;

        private ulong refreshIndex;

        private bool isRenderingDepth;

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

            Map.HouseColorChanged += (s, e) => InvalidateMap();
            EditorState.HighlightImpassableCellsChanged += (s, e) => InvalidateMap();
            EditorState.HighlightIceGrowthChanged += (s, e) => InvalidateMap();
            EditorState.DrawMapWideOverlayChanged += (s, e) => MapWideOverlay.Enabled = EditorState.DrawMapWideOverlay;
            EditorState.MarbleMadnessChanged += (s, e) => InvalidateMap();
            EditorState.Is2DModeChanged += (s, e) => InvalidateMap();
            EditorState.RenderedObjectsChanged += (s, e) => InvalidateMap();

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
            WindowManager.AddAndInitializeControl(mmw);
            InvalidateMapForMinimap();
        }

        private void LoadShaders()
        {
            colorDrawEffect = AssetLoader.LoadEffect("Shaders/ColorDraw");
            palettedColorDrawEffect = AssetLoader.LoadEffect("Shaders/PalettedColorDraw");
            depthApplyEffect = AssetLoader.LoadEffect("Shaders/DepthApply");
        }

        private void RotateUnitOneStep_Triggered(object sender, EventArgs e)
        {
            if (tileUnderCursor == null)
                return;

            var firstObject = tileUnderCursor.GetObject() as TechnoBase;
            if (firstObject == null)
                return;

            const int step = 32;

            if (firstObject.Facing + step > byte.MaxValue)
                firstObject.Facing = (byte)(firstObject.Facing + step - byte.MaxValue);
            else
                firstObject.Facing += step;

            AddRefreshPoint(tileUnderCursor.CoordsToPoint());
        }

        private void Map_MapResized(object sender, EventArgs e)
        {
            // Resizing the map makes previous undo/redo entries invalid
            MutationManager.ClearUndoAndRedoLists();

            // We need to re-create our map textures
            RefreshRenderTargets();

            windowController.MinimapWindow.MegamapTexture = mapRenderTarget;

            // And then re-draw the whole map
            InvalidateMap();
        }

        private void ClearRenderTargets()
        {
            mapRenderTarget?.Dispose();
            depthRenderTarget?.Dispose();
            depthRenderTargetCopy?.Dispose();
            objectRenderTarget?.Dispose();
            // shadowRenderTarget?.Dispose();
            transparencyRenderTarget?.Dispose();
            transparencyPerFrameRenderTarget?.Dispose();
            compositeRenderTarget?.Dispose();
            minimapRenderTarget?.Dispose();
        }

        private void RefreshRenderTargets()
        {
            ClearRenderTargets();

            mapRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color, DepthFormat.Depth16);
            depthRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Single);
            depthRenderTargetCopy = CreateFullMapRenderTarget(SurfaceFormat.Single);
            objectRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);
            // shadowRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Alpha8);
            transparencyRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);
            transparencyPerFrameRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);
            compositeRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);
            minimapRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);

            aircraftRenderer?.UpdateDepthRenderTarget(depthRenderTarget);
            animRenderer?.UpdateDepthRenderTarget(depthRenderTarget);
            buildingRenderer?.UpdateDepthRenderTarget(depthRenderTarget);
            infantryRenderer?.UpdateDepthRenderTarget(depthRenderTarget);
            overlayRenderer?.UpdateDepthRenderTarget(depthRenderTarget);
            smudgeRenderer?.UpdateDepthRenderTarget(depthRenderTarget);
            terrainRenderer?.UpdateDepthRenderTarget(depthRenderTarget);
            unitRenderer?.UpdateDepthRenderTarget(depthRenderTarget);
        }

        private RenderDependencies CreateRenderDependencies()
        {
            return new RenderDependencies(Map, TheaterGraphics, EditorState, GraphicsDevice, colorDrawEffect, palettedColorDrawEffect, Camera, GetCameraRightXCoord, GetCameraBottomYCoord, depthRenderTarget);
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
               Map.HeightInPixels, false, surfaceFormat,
               depthFormat, 0, RenderTargetUsage.PreserveContents);
        }

        public void DrawVisibleMapPortion()
        {
            refreshStopwatch.Restart();

            smudgesToRender.Clear();
            gameObjectsToRender.Clear();

            if (mapInvalidated)
            {
                GraphicsDevice.SetRenderTarget(depthRenderTarget);
                GraphicsDevice.Clear(Color.White);
                GraphicsDevice.SetRenderTarget(depthRenderTargetCopy);
                GraphicsDevice.Clear(Color.White);
                GraphicsDevice.SetRenderTarget(mapRenderTarget);
                GraphicsDevice.Clear(Color.Black);
            }

            // First render depth
            Renderer.PushRenderTarget(depthRenderTarget);
            isRenderingDepth = true;
            Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, depthApplyEffect));
            DoForVisibleCells(DrawTerrainTile);
            Renderer.PopSettings();
            isRenderingDepth = false;

            Renderer.PopRenderTarget();

            // Copy rendered depth so we are able to sample the depth buffer when rendering depth on later frames
            Renderer.PushRenderTarget(depthRenderTargetCopy, new SpriteBatchSettings(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, null));
            Renderer.DrawTexture(depthRenderTarget, new Rectangle(0, 0, depthRenderTargetCopy.Width, depthRenderTargetCopy.Height), Color.White);
            Renderer.PopRenderTarget();

            // Then render color
            Renderer.PushRenderTarget(mapRenderTarget);
            // GraphicsDevice.Clear(Color.Black);

            var palettedColorDrawSettings = new SpriteBatchSettings(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, palettedColorDrawEffect);
            Renderer.PushSettings(palettedColorDrawSettings);
            DoForVisibleCells(DrawTerrainTileAndRegisterObjects);
            Renderer.PopSettings();

            var colorDrawSettings = new SpriteBatchSettings(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, palettedColorDrawEffect);
            Renderer.PushSettings(colorDrawSettings);
            Renderer.PushRenderTarget(objectRenderTarget, colorDrawSettings);
            GraphicsDevice.Clear(Color.Transparent);
            DrawSmudges();
            DrawObjects();
            Renderer.PopRenderTarget();

            Renderer.PopSettings();

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

        private void SetCommonEffectParams(Effect effect, float bottomDepth, float topDepth,
            Vector2 worldTextureCoordinates, Vector2 spriteSizeToWorldSizeRatio)
        {
            effect.Parameters["SpriteDepthBottom"].SetValue(bottomDepth);
            effect.Parameters["SpriteDepthTop"].SetValue(topDepth);
            effect.Parameters["WorldTextureCoordinates"].SetValue(worldTextureCoordinates);
            effect.Parameters["SpriteSizeToWorldSizeRatio"].SetValue(spriteSizeToWorldSizeRatio);
            GraphicsDevice.SamplerStates[1] = SamplerState.LinearClamp;
        }

        private void SetDepthEffectParams(Effect effect, float bottomDepth, float topDepth,
            Vector2 worldTextureCoordinates, Vector2 spriteSizeToWorldSizeRatio, Texture2D depthTexture)
        {
            SetCommonEffectParams(effect, bottomDepth, topDepth, worldTextureCoordinates, spriteSizeToWorldSizeRatio);

            if (depthTexture != null)
            {
                effect.Parameters["DepthTexture"].SetValue(depthTexture);
                GraphicsDevice.Textures[1] = depthTexture;
            }
        }

        private void SetPaletteEffectParams(Effect effect, float bottomDepth, float topDepth,
            Vector2 worldTextureCoordinates, Vector2 spriteSizeToWorldSizeRatio, Texture2D depthTexture, Texture2D paletteTexture, bool usePalette)
        {
            SetCommonEffectParams(effect, bottomDepth, topDepth, worldTextureCoordinates, spriteSizeToWorldSizeRatio);

            if (depthTexture != null)
            {
                effect.Parameters["DepthTexture"].SetValue(depthTexture);
                GraphicsDevice.Textures[1] = depthTexture;
            }

            if (paletteTexture != null)
            {
                effect.Parameters["PaletteTexture"].SetValue(paletteTexture);
                GraphicsDevice.Textures[2] = paletteTexture;
            }

            effect.Parameters["UsePalette"].SetValue(usePalette);
            effect.Parameters["UseRemap"].SetValue(false);
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
                tlY = 0;
                camRight = mapRenderTarget.Width;
                camBottom = mapRenderTarget.Height;
            }
            else
            {
                // Otherwise, screen contents will do.
                // Add some padding to take objects just outside of the visible screen to account
                tlX = Camera.TopLeftPoint.X - Constants.RenderPixelPadding;
                tlY = Camera.TopLeftPoint.Y - Constants.RenderPixelPadding;

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
        public int GetCameraBottomYCoord() => Math.Min(Camera.TopLeftPoint.Y + GetCameraHeight(), Map.Size.Y * Constants.CellSizeY);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Rectangle GetCameraRectangle() => new Rectangle(Camera.TopLeftPoint.X, Camera.TopLeftPoint.Y, GetCameraWidth(), GetCameraHeight());

        public void DrawTerrainTileAndRegisterObjects(MapTile tile)
        {
            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Terrain) == RenderObjectFlags.Terrain)
                DrawTerrainTile(tile);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Smudges) == RenderObjectFlags.Smudges && tile.Smudge != null)
                smudgesToRender.Add(tile.Smudge);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Overlay) == RenderObjectFlags.Overlay && tile.Overlay != null && tile.Overlay.OverlayType != null)
                gameObjectsToRender.Add(tile.Overlay);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Structures) == RenderObjectFlags.Structures)
            {
                tile.DoForAllBuildings(structure =>
                {
                    if (structure.Position == tile.CoordsToPoint())
                        gameObjectsToRender.Add(structure);

                    foreach (var anim in structure.Anims)
                        gameObjectsToRender.Add(anim);

                    if (structure.TurretAnim != null)
                        gameObjectsToRender.Add(structure.TurretAnim);
                });
            }

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Infantry) == RenderObjectFlags.Infantry)
                tile.DoForAllInfantry(gameObjectsToRender.Add);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Aircraft) == RenderObjectFlags.Aircraft)
                tile.DoForAllAircraft(gameObjectsToRender.Add);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Vehicles) == RenderObjectFlags.Vehicles)
                tile.DoForAllVehicles(gameObjectsToRender.Add);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.TerrainObjects) == RenderObjectFlags.TerrainObjects && tile.TerrainObject != null)
                gameObjectsToRender.Add(tile.TerrainObject);
        }

        public void DrawTerrainTile(MapTile tile)
        {
            if (tile.LastRefreshIndex == refreshIndex)
                return;

            if (tile.TileIndex >= TheaterGraphics.TileCount)
                return;

            Point2D drawPoint = CellMath.CellTopLeftPointFromCellCoords(new Point2D(tile.X, tile.Y), Map);

            if (!minimapNeedsRefresh)
            {
                if (drawPoint.X + Constants.CellSizeX < Camera.TopLeftPoint.X || drawPoint.X > GetCameraRightXCoord())
                    return;

                if (drawPoint.Y + Constants.CellSizeY < Camera.TopLeftPoint.Y)
                    return;
            }

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
                subTileIndex = 0;

            if (tileImage.TMPImages.Length == 0)
                return;

            MGTMPImage tmpImage = tileImage.TMPImages[subTileIndex];

            if (tmpImage.TmpImage == null)
                return;

            int extraDrawY = drawPoint.Y + tmpImage.TmpImage.YExtra - tmpImage.TmpImage.Y;

            if (!minimapNeedsRefresh && drawPoint.Y - (level * Constants.CellHeight) > GetCameraBottomYCoord())
            {
                if (tmpImage.ExtraTexture == null)
                    return;

                // If we have extra graphics, need to check whether they are on screen
                if (extraDrawY - (level * Constants.CellHeight) > GetCameraBottomYCoord())
                    return;
            }

            // We know we're going to render this tile - update refresh index
            if (!isRenderingDepth)
                tile.LastRefreshIndex = refreshIndex;

            int originalDrawPointY = drawPoint.Y;

            if (!EditorState.Is2DMode)
                drawPoint -= new Point2D(0, (Constants.CellSizeY / 2) * level);

            if (subTileIndex >= tileImage.TMPImages.Length)
            {
                DrawString(subTileIndex.ToString(), 0, new Vector2(drawPoint.X, drawPoint.Y), Color.Red);
                return;
            }

            float depthTop = 0f;
            float depthBottom = 0f;

            if (tmpImage.Texture != null)
            {
                depthTop = originalDrawPointY / (float)Map.HeightInPixels;
                depthBottom = (originalDrawPointY + tmpImage.Texture.Height) / (float)Map.HeightInPixels;
                depthTop = 1.0f - depthTop;
                depthBottom = 1.0f - depthBottom;

                Vector2 worldTextureCoordinates = new Vector2(drawPoint.X / (float)Map.WidthInPixels, drawPoint.Y / (float)Map.HeightInPixels);
                Vector2 spriteSizeToWorldSizeRatio = new Vector2(Constants.CellSizeX / (float)Map.WidthInPixels, Constants.CellSizeY / (float)Map.HeightInPixels);

                var textureToDraw = tmpImage.Texture;
                Color color = Color.White;
                bool usePalette = true;

                // Replace terrain lacking MM graphics with colored cells to denote height if we are in marble madness mode
                if (!Constants.IsFlatWorld &&
                    EditorState.IsMarbleMadness &&
                    !TheaterGraphics.HasSeparateMarbleMadnessTileGraphics(tileImage.TileID))
                {
                    textureToDraw = EditorGraphics.GenericTileWithBorderTexture;
                    color = MarbleMadnessTileHeightLevelColors[level];
                    palettedColorDrawEffect.Parameters["UsePalette"].SetValue(false);
                    usePalette = false;
                }

                if (isRenderingDepth)
                    SetDepthEffectParams(depthApplyEffect, depthBottom, depthTop, worldTextureCoordinates, spriteSizeToWorldSizeRatio, depthRenderTargetCopy);
                else
                    SetPaletteEffectParams(palettedColorDrawEffect, depthBottom, depthTop, worldTextureCoordinates, spriteSizeToWorldSizeRatio, depthRenderTarget, tmpImage.Palette.Texture, usePalette);

                DrawTexture(textureToDraw, new Rectangle(drawPoint.X, drawPoint.Y,
                    Constants.CellSizeX, Constants.CellSizeY), null, color, 0f, Vector2.Zero, SpriteEffects.None, 0f);
            }

            if (tmpImage.ExtraTexture != null && !EditorState.Is2DMode)
            {
                // Most extra graphics are drawn above the regular terrain, this gives
                // us a kind-of smooth depth transition for them
                depthBottom = depthTop;

                int exDrawPointX = drawPoint.X + tmpImage.TmpImage.XExtra - tmpImage.TmpImage.X;
                int exDrawPointY = drawPoint.Y + tmpImage.TmpImage.YExtra - tmpImage.TmpImage.Y;

                Vector2 worldTextureCoordinates = new Vector2(exDrawPointX / (float)Map.WidthInPixels, exDrawPointY / (float)Map.HeightInPixels);
                Vector2 spriteSizeToWorldSizeRatio = new Vector2(tmpImage.ExtraTexture.Width / (float)Map.WidthInPixels, tmpImage.ExtraTexture.Height / (float)Map.HeightInPixels);

                if (isRenderingDepth)
                    SetDepthEffectParams(depthApplyEffect, depthBottom, depthTop, worldTextureCoordinates, spriteSizeToWorldSizeRatio, depthRenderTargetCopy);
                else
                    SetPaletteEffectParams(palettedColorDrawEffect, depthBottom, depthTop, worldTextureCoordinates, spriteSizeToWorldSizeRatio, depthRenderTarget, tmpImage.Palette.Texture, true);

                var exDrawRectangle = new Rectangle(exDrawPointX,
                    exDrawPointY,
                    tmpImage.ExtraTexture.Width,
                    tmpImage.ExtraTexture.Height);

                DrawTexture(tmpImage.ExtraTexture,
                    exDrawRectangle,
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero, SpriteEffects.None, 0f);

                var cameraRectangle = GetCameraRectangle();

                // If this tile was only rendered partially, then we need to redraw it properly later
                if (!cameraRectangle.Contains(exDrawRectangle))
                    tile.LastRefreshIndex = 0;
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

        private Point2D GetBuildingCenterPoint(Structure structure)
        {
            Point2D topPoint = CellMath.CellCenterPointFromCellCoords(structure.Position, Map);
            var foundation = structure.ObjectType.ArtConfig.Foundation;
            Point2D bottomPoint = CellMath.CellCenterPointFromCellCoords(structure.Position + new Point2D(foundation.Width - 1, foundation.Height - 1), Map);
            return topPoint + new Point2D((bottomPoint.X - topPoint.X) / 2, (bottomPoint.Y - topPoint.Y) / 2);
        }

        private int CompareGameObjectsForRendering(GameObject obj1, GameObject obj2)
        {
            // Use pixel coords for sorting. Objects closer to the top are rendered first.
            // In case of identical Y coordinates, objects closer to the left take priority.
            // For buildings, we take their foundation into account when calculating their center pixel coords.

            // In case the pixels coords are identical, sort by RTTI type.
            Point2D obj1Point = GetObjectCoordsForComparison(obj1);
            Point2D obj2Point = GetObjectCoordsForComparison(obj2);

            // Special case: building animation compared to its own building
            if (obj1.WhatAmI() == RTTIType.Anim &&
                obj2.WhatAmI() == RTTIType.Building &&
                ((Animation)obj1).IsBuildingAnim &&
                ((Animation)obj1).ParentBuilding == obj2)
            {
                const float leptonsDiagonal = 362.0f;

                var anim = (Animation)obj1;
                int animScore = obj1Point.X + obj1Point.Y +
                                anim.AnimType.ArtConfig.BuildingAnimYSort -
                                Convert.ToInt32(anim.AnimType.ArtConfig.BuildingAnimZAdjust / leptonsDiagonal * Constants.CellSizeY);

                int buildingScore = obj2Point.X + obj2Point.Y;

                return animScore - buildingScore;
            }
            else if (obj2.WhatAmI() == RTTIType.Anim &&
                     obj1.WhatAmI() == RTTIType.Building &&
                     ((Animation)obj2).IsBuildingAnim &&
                     ((Animation)obj2).ParentBuilding == obj1)
            {
                return -CompareGameObjectsForRendering(obj2, obj1);
            }

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
                RTTIType.Building => GetBuildingCenterPoint((Structure)obj),
                RTTIType.Anim => ((Animation)obj).IsBuildingAnim ?
                    GetBuildingCenterPoint(((Animation)obj).ParentBuilding) :
                    CellMath.CellCenterPointFromCellCoords(obj.Position, Map),
                _ => CellMath.CellCenterPointFromCellCoords(obj.Position, Map)
            };
        }

        private void DrawSmudges()
        {
            smudgesToRender.Sort(CompareGameObjectsForRendering);
            smudgesToRender.ForEach(DrawObject);
        }

        private void DrawObjects()
        {
            gameObjectsToRender.Sort(CompareGameObjectsForRendering);
            gameObjectsToRender.ForEach(DrawObject);
        }

        private void DrawObject(GameObject gameObject)
        {
            switch (gameObject.WhatAmI())
            {
                case RTTIType.Aircraft:
                    aircraftRenderer.Draw(gameObject as Aircraft, !minimapNeedsRefresh);
                    return;
                case RTTIType.Anim:
                    animRenderer.Draw(gameObject as Animation, !minimapNeedsRefresh);
                    return;
                case RTTIType.Building:
                    buildingRenderer.Draw(gameObject as Structure, !minimapNeedsRefresh);
                    return;
                case RTTIType.Infantry:
                    infantryRenderer.Draw(gameObject as Infantry, !minimapNeedsRefresh);
                    return;
                case RTTIType.Overlay:
                    overlayRenderer.Draw(gameObject as Overlay, !minimapNeedsRefresh);
                    return;
                case RTTIType.Smudge:
                    smudgeRenderer.Draw(gameObject as Smudge, !minimapNeedsRefresh);
                    return;
                case RTTIType.Terrain:
                    terrainRenderer.Draw(gameObject as TerrainObject, !minimapNeedsRefresh);
                    return;
                case RTTIType.Unit:
                    unitRenderer.Draw(gameObject as Unit, !minimapNeedsRefresh);
                    return;
                default:
                    throw new NotImplementedException("No renderer implemented for type " + gameObject.WhatAmI());
            }
        }

        private void DrawBaseNodes()
        {
            Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, null, null, palettedColorDrawEffect));
            SetPaletteEffectParams(palettedColorDrawEffect, 1.0f, 1.0f, Vector2.Zero, Vector2.Zero, null, null, true);
            Map.GraphicalBaseNodes.ForEach(DrawBaseNode);
            Renderer.PopSettings();
        }

        private void DrawBaseNode(GraphicalBaseNode graphicalBaseNode)
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


            Point2D drawPoint = CellMath.CellTopLeftPointFromCellCoords_3D(graphicalBaseNode.BaseNode.Position, Map);

            ShapeImage bibGraphics = TheaterGraphics.BuildingBibTextures[graphicalBaseNode.BuildingType.Index];
            ShapeImage graphics = TheaterGraphics.BuildingTextures[graphicalBaseNode.BuildingType.Index];
            Color replacementColor = Color.DarkBlue;
            string iniName = graphicalBaseNode.BuildingType.ININame;
            Color remapColor = graphicalBaseNode.BuildingType.ArtConfig.Remapable ? graphicalBaseNode.Owner.XNAColor : Color.White;

            const float opacity = 0.25f;

            remapColor *= opacity;

            Color nonRemapBaseNodeShade = new Color(opacity, opacity, opacity * 2.0f, opacity * 2.0f);

            int yDrawOffset = Constants.CellSizeY / -2;
            int frameIndex = 0;

            if ((graphics == null || graphics.GetFrame(frameIndex) == null) && bibGraphics == null)
            {
                DrawStringWithShadow(iniName, 1, drawPoint.ToXNAVector(), replacementColor, 1.0f);
                return;
            }

            Texture2D texture;

            if (bibGraphics != null)
            {
                PositionedTexture bibFrame = bibGraphics.GetFrame(0);
                texture = bibFrame.Texture;

                int bibFinalDrawPointX = drawPoint.X - bibFrame.ShapeWidth / 2 + bibFrame.OffsetX + Constants.CellSizeX / 2;
                int bibFinalDrawPointY = drawPoint.Y - bibFrame.ShapeHeight / 2 + bibFrame.OffsetY + Constants.CellSizeY / 2 + yDrawOffset;

                palettedColorDrawEffect.Parameters["UseRemap"].SetValue(false);
                palettedColorDrawEffect.Parameters["PaletteTexture"].SetValue(bibGraphics.Palette.Texture);

                DrawTexture(texture, new Rectangle(
                    bibFinalDrawPointX, bibFinalDrawPointY,
                    texture.Width, texture.Height),
                    null, Constants.HQRemap ? nonRemapBaseNodeShade : remapColor,
                    0f, Vector2.Zero, SpriteEffects.None, 0f);

                if (Constants.HQRemap && bibGraphics.HasRemapFrames())
                {
                    palettedColorDrawEffect.Parameters["UseRemap"].SetValue(true);
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

            var frame = graphics.GetFrame(frameIndex);
            if (frame == null)
                return;

            texture = frame.Texture;

            int x = drawPoint.X - frame.ShapeWidth / 2 + frame.OffsetX + Constants.CellSizeX / 2;
            int y = drawPoint.Y - frame.ShapeHeight / 2 + frame.OffsetY + Constants.CellSizeY / 2 + yDrawOffset;
            int width = texture.Width;
            int height = texture.Height;
            Rectangle drawRectangle = new Rectangle(x, y, width, height);

            palettedColorDrawEffect.Parameters["UseRemap"].SetValue(false);
            palettedColorDrawEffect.Parameters["PaletteTexture"].SetValue(graphics.Palette.Texture);

            DrawTexture(texture, drawRectangle, Constants.HQRemap ? nonRemapBaseNodeShade : remapColor);

            if (Constants.HQRemap && graphics.HasRemapFrames())
            {
                palettedColorDrawEffect.Parameters["UseRemap"].SetValue(true);
                DrawTexture(graphics.GetRemapFrame(frameIndex).Texture, drawRectangle, remapColor);
            }
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

        private void DrawMapBorder()
        {
            const int BorderThickness = 4;
            const int InitialHeight = 3; // TS engine assumes that the first cell is at a height of 2
            const double HeightAddition = 4.5; // TS engine adds 4.5 to specified map height <3
            const int TopImpassableCellCount = 3; // The northernmost 3 cells are impassable in the TS engine, we'll also display this border

            int x = (int)(Map.LocalSize.X * Constants.CellSizeX);
            int y = (int)(Map.LocalSize.Y - InitialHeight) * Constants.CellSizeY;
            int width = (int)(Map.LocalSize.Width * Constants.CellSizeX);
            int height = (int)(Map.LocalSize.Height + HeightAddition) * Constants.CellSizeY;

            DrawRectangle(new Rectangle(x, y, width, height), Color.Blue, BorderThickness);

            int impassableY = (int)(y + (Constants.CellSizeY * TopImpassableCellCount));
            FillRectangle(new Rectangle(x, impassableY - (BorderThickness / 2), width, BorderThickness), Color.Teal * 0.25f);

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
                    CursorAction.LeftUpOnMouseMove(tileUnderCursor == null ? new Point2D(-1, -1) : tileUnderCursor.CoordsToPoint());
                }
            }

            if (CursorAction == null && tileUnderCursor != null && Cursor.LeftDown && !isDraggingObject && !isRotatingObject)
            {
                var cellObject = tileUnderCursor.GetObject();

                if (cellObject != null)
                {
                    draggedOrRotatedObject = tileUnderCursor.GetObject();

                    if (draggedOrRotatedObject != null)
                    {
                        if (KeyboardCommands.Instance.RotateUnit.AreKeysDown(Keyboard))
                            isRotatingObject = true;
                        else
                            isDraggingObject = true;
                    }
                }
                else if (tileUnderCursor.Waypoints.Count > 0)
                {
                    draggedOrRotatedObject = tileUnderCursor.Waypoints[0];
                    isDraggingObject = true;
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

                Infantry infantry = tileUnderCursor.GetFirstInfantry();
                if (infantry != null)
                    windowController.InfantryOptionsWindow.Open(infantry);
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
                TechnoUnderCursor = tileUnderCursor.GetTechno();

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
                    Camera.TopLeftPoint.Y + (int)(cursorPoint.Y / Camera.ZoomLevel));

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
                CursorAction.OnKeyPressed(e);
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
                bool isCloning = KeyboardCommands.Instance.CloneObject.AreKeysOrModifiersDown(Keyboard);
                bool overlapObjects = KeyboardCommands.Instance.OverlapObjects.AreKeysOrModifiersDown(Keyboard);

                Color lineColor = isCloning ? new Color(0, 255, 255) : Color.White;
                if (!Map.CanPlaceObjectAt(draggedOrRotatedObject, tileUnderCursor.CoordsToPoint(), isCloning, overlapObjects) ||
                    (isCloning && !draggedOrRotatedObject.IsTechno() && draggedOrRotatedObject.WhatAmI() != RTTIType.Terrain))
                    lineColor = Color.Red;

                Point2D cameraAndCellCenterOffset = new Point2D(-Camera.TopLeftPoint.X + Constants.CellSizeX / 2,
                                                 -Camera.TopLeftPoint.Y + Constants.CellSizeY / 2);

                Point2D startDrawPoint = CellMath.CellTopLeftPointFromCellCoords(draggedOrRotatedObject.Position, Map) + cameraAndCellCenterOffset;

                var startCell = Map.GetTile(draggedOrRotatedObject.Position);
                if (startCell != null)
                    startDrawPoint -= new Point2D(0, Map.GetTile(draggedOrRotatedObject.Position).Level * Constants.CellHeight);

                Point2D endDrawPoint = CellMath.CellTopLeftPointFromCellCoords(tileUnderCursor.CoordsToPoint(), Map) + cameraAndCellCenterOffset;

                endDrawPoint -= new Point2D(0, tileUnderCursor.Level * Constants.CellHeight);

                startDrawPoint = startDrawPoint.ScaleBy(Camera.ZoomLevel);
                endDrawPoint = endDrawPoint.ScaleBy(Camera.ZoomLevel);

                DrawLine(startDrawPoint.ToXNAVector(), endDrawPoint.ToXNAVector(), lineColor, 1);
            }
            else if (isRotatingObject)
            {
                Color lineColor = Color.Yellow;

                Point2D cameraAndCellCenterOffset = new Point2D(-Camera.TopLeftPoint.X + Constants.CellSizeX / 2,
                                                 -Camera.TopLeftPoint.Y + Constants.CellSizeY / 2);

                Point2D startDrawPoint = CellMath.CellTopLeftPointFromCellCoords(draggedOrRotatedObject.Position, Map) + cameraAndCellCenterOffset;

                var startCell = Map.GetTile(draggedOrRotatedObject.Position);
                if (startCell != null)
                    startDrawPoint -= new Point2D(0, Map.GetTile(draggedOrRotatedObject.Position).Level * Constants.CellHeight);

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

            Point2D cellTopLeftPoint = CellMath.CellTopLeftPointFromCellCoords_3D(cell.CoordsToPoint(), Map);

            DrawTexture(EditorGraphics.ImpassableCellHighlightTexture, cellTopLeftPoint.ToXNAPoint(), Color.White);
        }

        private void DrawIceGrowthHighlight(MapTile cell)
        {
            if (cell.IceGrowth <= 0)
                return;

            Point2D cellTopLeftPoint = CellMath.CellTopLeftPointFromCellCoords(cell.CoordsToPoint(), Map);

            DrawTexture(EditorGraphics.IceGrowthHighlightTexture, cellTopLeftPoint.ToXNAPoint(), Color.White);
        }

        public void DeleteObjectFromCell(Point2D cellCoords)
        {
            var tile = Map.GetTile(cellCoords.X, cellCoords.Y);
            if (tile == null)
                return;

            AddRefreshPoint(cellCoords, 2);
            Map.DeleteObjectFromCell(cellCoords, EditorState.DeletionMode);
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
                        (int)(-Camera.TopLeftPoint.Y * Camera.ZoomLevel),
                        (int)(mapRenderTarget.Width * Camera.ZoomLevel),
                        (int)(mapRenderTarget.Height * Camera.ZoomLevel)));
            }

            if (IsActive && tileUnderCursor != null && CursorAction != null)
            {
                CursorAction.PostMapDraw(tileUnderCursor.CoordsToPoint());
                CursorAction.DrawPreview(tileUnderCursor.CoordsToPoint(), Camera.TopLeftPoint);
            }

            DrawOnTileUnderCursor();

            DrawOnMinimap();

            base.Draw(gameTime);
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
            Renderer.PushRenderTarget(compositeRenderTarget, new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null));

            GraphicsDevice.Clear(Color.Black);

            Rectangle sourceRectangle = new Rectangle(0, 0, mapRenderTarget.Width, mapRenderTarget.Height);
            Rectangle destinationRectangle = sourceRectangle;

            DrawTexture(mapRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            DrawTexture(objectRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            if (debugRenderDepthBuffer)
            {
                DrawTexture(depthRenderTarget,
                    sourceRectangle, destinationRectangle,
                    Color.White);
            }

            DrawTexture(transparencyRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            DrawTexture(transparencyPerFrameRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            Renderer.PopRenderTarget();

            // Draw the composite render target directly to the screen

            Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null));

            DrawTexture(compositeRenderTarget,
                mapRenderSourceRectangle,
                mapRenderDestinationRectangle,
                Color.White);

            Renderer.PopSettings();
        }
    }
}
