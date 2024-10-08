using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering.ObjectRenderers;
using TSMapEditor.Settings;
using TSMapEditor.UI;

namespace TSMapEditor.Rendering
{
    public interface IMapView
    {
        Map Map { get; }
        TheaterGraphics TheaterGraphics { get; }
        void AddRefreshPoint(Point2D point, int size = 10);
        void InvalidateMap();
        LightingPreviewMode LightingPreviewState { get; }
        Camera Camera { get; }
        Texture2D MinimapTexture { get; }
        HashSet<object> MinimapUsers { get; }
    }

    /// <summary>
    /// The renderer. Draws the map.
    /// </summary>
    public class MapView : IMapView
    {
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

        public MapView(WindowManager windowManager, Map map, TheaterGraphics theaterGraphics, EditorGraphics editorGraphics, EditorState editorState)
        {
            this.windowManager = windowManager;
            EditorState = editorState;
            Map = map;
            TheaterGraphics = theaterGraphics;
            EditorGraphics = editorGraphics;

            Camera = new Camera(windowManager, Map);
            Camera.CameraUpdated += (s, e) => 
            { 
                cameraMoved = true; 
                if (UserSettings.Instance.GraphicsLevel > 0) InvalidateMap(); 
            };
        }

        private WindowManager windowManager;

        private GraphicsDevice GraphicsDevice => windowManager.GraphicsDevice;

        public int Width => windowManager.RenderResolutionX;
        public int Height => windowManager.RenderResolutionY;

        public EditorState EditorState { get; private set; }
        public Map Map { get; private set; }
        public TheaterGraphics TheaterGraphics { get; private set; }
        public EditorGraphics EditorGraphics { get; private set; }

        public bool Is2DMode => EditorState.Is2DMode;
        public LightingPreviewMode LightingPreviewState => EditorState.IsLighting ? EditorState.LightingPreviewState : LightingPreviewMode.NoLighting;
        public Randomizer Randomizer => EditorState.Randomizer;

        public Texture2D MinimapTexture => minimapRenderTarget;

        /// <summary>
        /// Tracks users of the minimap.
        /// If the minimap texture is not used by anyone, we can save
        /// processing power and skip certain actions that would update it.
        /// </summary>
        public HashSet<object> MinimapUsers { get; } = new HashSet<object>();
        public Camera Camera { get; private set; }

        public MapWideOverlay MapWideOverlay { get; private set; }

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

        private bool mapInvalidated;
        private bool cameraMoved;
        private bool minimapNeedsRefresh;

        private List<Structure> structuresToRender = new List<Structure>();
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
        private DepthStencilState objectRenderStencilState;
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

        public void Initialize()
        {
            LoadShaders();

            MapWideOverlay = new MapWideOverlay();
            EditorState.MapWideOverlayExists = MapWideOverlay.HasTexture;

            RefreshRenderTargets();
            CreateDepthStencilStates();

            Map.LocalSizeChanged += (s, e) => InvalidateMap();
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

            InvalidateMapForMinimap();
            Map_LightingColorsRefreshed();
        }

        public void Clear()
        {
            EditorState = null;
            TheaterGraphics = null;
            MapWideOverlay.Clear();

            depthRenderStencilState?.Dispose();
            shadowRenderStencilState?.Dispose();
            Map = null;

            ClearRenderTargets();
        }

        private void LoadShaders()
        {
            palettedColorDrawEffect = AssetLoader.LoadEffect("Shaders/PalettedColorDraw");
            combineDrawEffect = AssetLoader.LoadEffect("Shaders/CombineWithDepth");
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
        }

        public void RefreshRenderTargets()
        {
            ClearRenderTargets();

            mapRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color, DepthFormat.Depth24);
            mapDepthRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Single);
            objectsRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
            objectsDepthRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Single);
            transparencyRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);
            transparencyPerFrameRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);
            compositeRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);
            minimapRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);

            palettedColorDrawEffect.Parameters["WorldTextureHeight"].SetValue((float)mapRenderTarget.Height);
        }

        private void CreateDepthStencilStates()
        {
            if (depthRenderStencilState == null)
            {
                depthRenderStencilState = new DepthStencilState()
                {
                    DepthBufferEnable = true,
                    DepthBufferWriteEnable = true,
                    DepthBufferFunction = CompareFunction.GreaterEqual,
                };
            }

            // Depth stencil state for rendering objects.
            // Sets the stencil value in the stencil buffer to prevent shadows from being drawn over objects.
            // While it'd usually look nicer, shadows cannot be cast over objects in the C&C engine.
            if (objectRenderStencilState == null)
            {
                objectRenderStencilState = new DepthStencilState()
                {
                    DepthBufferEnable = true,
                    DepthBufferWriteEnable = true,
                    DepthBufferFunction = CompareFunction.GreaterEqual,
                    StencilEnable = true,
                    StencilPass = StencilOperation.Replace,
                    StencilFunction = CompareFunction.Always,
                    ReferenceStencil = 1
                };
            }

            if (shadowRenderStencilState == null)
            {
                shadowRenderStencilState = new DepthStencilState()
                {
                    DepthBufferEnable = true,
                    DepthBufferWriteEnable = true,
                    DepthBufferFunction = CompareFunction.GreaterEqual,
                    StencilEnable = true,
                    StencilFail = StencilOperation.Keep,
                    StencilPass = StencilOperation.Replace,
                    StencilFunction = CompareFunction.Greater,
                    ReferenceStencil = 1
                };
            }
        }

        private RenderDependencies CreateRenderDependencies()
        {
            return new RenderDependencies(Map, TheaterGraphics, EditorState, windowManager.GraphicsDevice, objectSpriteRecord, palettedColorDrawEffect, Camera, GetCameraRightXCoord, GetCameraBottomYCoord);
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
            structuresToRender.Clear();
            gameObjectsToRender.Clear();

            Renderer.PushRenderTargets(mapRenderTarget, mapDepthRenderTarget);

            if (mapInvalidated)
            {
                GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 0f, 0);
                objectSpriteRecord.Clear(false);
            }

            // Draw terrain tiles in batched mode for performance if we can.
            // In Marble Madness mode we currently need to mix and match paletted and non-paletted graphics, so there's no avoiding immediate mode.
            SpriteSortMode spriteSortMode = EditorState.IsMarbleMadness ? SpriteSortMode.Immediate : SpriteSortMode.Deferred;

            SetPaletteEffectParams(palettedColorDrawEffect, TheaterGraphics.TheaterPalette.GetTexture(), true, false, 1.0f);
            palettedColorDrawEffect.Parameters["ComplexDepth"].SetValue(false);
            palettedColorDrawEffect.Parameters["IncreaseDepthUpwards"].SetValue(false);
            var palettedColorDrawSettings = new SpriteBatchSettings(spriteSortMode, BlendState.Opaque, null, depthRenderStencilState, null, palettedColorDrawEffect);
            Renderer.PushSettings(palettedColorDrawSettings);
            DoForVisibleCells(DrawTerrainTileAndRegisterObjects);
            Renderer.PopSettings();

            Renderer.PopRenderTarget();

            // Render objects
            Renderer.PushRenderTargets(objectsRenderTarget, objectsDepthRenderTarget);

            if (mapInvalidated)
                GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 0f, 0);

            DrawSmudges();

            // We need to enable this for buildings and game objects.
            palettedColorDrawEffect.Parameters["IncreaseDepthUpwards"].SetValue(true);
            DrawBuildings();
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

        private void SetPaletteEffectParams(Effect effect, Texture2D paletteTexture, bool usePalette, bool useRemap, float opacity, bool isShadow = false, bool complexDepth = false)
        {
            if (paletteTexture != null)
            {
                effect.Parameters["PaletteTexture"].SetValue(paletteTexture);
                // GraphicsDevice.Textures[2] = paletteTexture;
            }

            effect.Parameters["IsShadow"].SetValue(isShadow);
            effect.Parameters["UsePalette"].SetValue(usePalette);
            effect.Parameters["UseRemap"].SetValue(useRemap);
            effect.Parameters["Opacity"].SetValue(opacity);
            effect.Parameters["ComplexDepth"].SetValue(complexDepth);
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
                        AddStructureToRender(structure);
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

        private void AddStructureToRender(Structure structure)
        {
            if (objectSpriteRecord.ProcessedObjects.Contains(structure))
                return;

            structuresToRender.Add(structure);
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
                Renderer.DrawString(subTileIndex.ToString(), 0, new Vector2(drawPoint.X, drawPoint.Y), Color.Red);
                return;
            }

            if (!EditorState.Is2DMode)
                drawY -= (Constants.CellSizeY / 2) * level;

            float depth = CellMath.GetDepthForCell(tile.CoordsToPoint(), Map);

            // Divide the color by 2f. This is done because unlike map lighting which can exceed 1.0 and go up to 2.0,
            // the Color instance values are capped at 1.0.
            // We lose a bit of precision from doing this, but we'll have to accept that.
            Color color = new Color((float)tile.CellLighting.R / 2f, (float)tile.CellLighting.G / 2f, (float)tile.CellLighting.B / 2f, 0.5f);

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
                        color = color * 0.5f;
                        SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, 1.0f, false, false);
                    }
                    else
                    {
                        SetPaletteEffectParams(palettedColorDrawEffect, tmpImage.GetPaletteTexture(), true, false, 1.0f, false);
                    }
                }

                Renderer.DrawTexture(textureToDraw, new Rectangle(drawX, drawY,
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

                Renderer.DrawTexture(tmpImage.ExtraTexture,
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

        /// <summary>
        /// Draws smudges.
        /// Smudges are the "bottom-most" layer after terrain tiles and cannot ever overlap
        /// other objects, making them convenient to render separately from others.
        /// </summary>
        private void DrawSmudges()
        {
            smudgesToRender.Sort(CompareGameObjectsForRendering);

            var colorDrawSettings = new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.Opaque, null, depthRenderStencilState, null, palettedColorDrawEffect);
            SetPaletteEffectParams(palettedColorDrawEffect, TheaterGraphics.TheaterPalette.GetTexture(), true, false, 1.0f);
            Renderer.PushSettings(colorDrawSettings);
            for (int i = 0; i < smudgesToRender.Count; i++)
            {
                smudgeRenderer.DrawNonRemap(smudgesToRender[i], smudgeRenderer.GetDrawPoint(smudgesToRender[i]));
            }
            smudgesToRender.ForEach(DrawObject);
            Renderer.PopSettings();
        }

        /// <summary>
        /// Draws buildings. Due to their large size and non-flat shape in the game world,
        /// buildings are rendered with different shader settings from other objects and
        /// thus need to be drawn separately.
        /// </summary>
        private void DrawBuildings()
        {
            structuresToRender.Sort(CompareGameObjectsForRendering);
            for (int i = 0; i < structuresToRender.Count; i++)
            {
                DrawObject(structuresToRender[i]);
                objectSpriteRecord.ProcessedObjects.Add(structuresToRender[i]);
            }

            ProcessObjectSpriteRecord(true, false); // Do not process building shadows yet, let DrawGameObjects do it
            objectSpriteRecord.Clear(true);
        }

        /// <summary>
        /// Draws all game objects that have been queued for rendering.
        /// </summary>
        private void DrawGameObjects()
        {
            gameObjectsToRender.Sort(CompareGameObjectsForRendering);

            for (int i = 0; i < gameObjectsToRender.Count; i++)
            {
                DrawObject(gameObjectsToRender[i]);
                objectSpriteRecord.ProcessedObjects.Add(gameObjectsToRender[i]);
            }

            ProcessObjectSpriteRecord(false, true);
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

        private void ProcessObjectSpriteRecord(bool complexDepth, bool processShadows)
        {
            if (objectSpriteRecord.LineEntries.Count > 0)
            {
                SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, 1.0f, false, false);
                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.Opaque, null, objectRenderStencilState, null, palettedColorDrawEffect));

                for (int i = 0; i < objectSpriteRecord.LineEntries.Count; i++)
                {
                    var lineEntry = objectSpriteRecord.LineEntries[i];
                    Renderer.DrawLine(lineEntry.Source, lineEntry.Destination,
                        new Color(lineEntry.Color.R / 255.0f, lineEntry.Color.G / 255.0f, lineEntry.Color.B / 255.0f, 0),
                        lineEntry.Thickness, lineEntry.Depth);
                }

                Renderer.PopSettings();
            }

            foreach (var kvp in objectSpriteRecord.SpriteEntries)
            {
                Texture2D paletteTexture = kvp.Key.Item1;
                bool isRemap = kvp.Key.Item2;

                SetPaletteEffectParams(palettedColorDrawEffect, paletteTexture, true, isRemap, 1.0f, false, complexDepth);
                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.Opaque, null, objectRenderStencilState, null, palettedColorDrawEffect));

                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    var spriteEntry = kvp.Value[i];
                    Renderer.DrawTexture(spriteEntry.Texture, spriteEntry.DrawingBounds, null, spriteEntry.Color, 0f, Vector2.Zero, SpriteEffects.None, spriteEntry.Depth);
                }

                Renderer.PopSettings();
            }

            if (objectSpriteRecord.NonPalettedSpriteEntries.Count > 0)
            {
                SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, 1.0f, false, complexDepth);
                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, objectRenderStencilState, null, palettedColorDrawEffect));

                for (int i = 0; i < objectSpriteRecord.NonPalettedSpriteEntries.Count; i++)
                {
                    var spriteEntry = objectSpriteRecord.NonPalettedSpriteEntries[i];
                    Renderer.DrawTexture(spriteEntry.Texture, spriteEntry.DrawingBounds, null, spriteEntry.Color, 0f, Vector2.Zero, SpriteEffects.None, spriteEntry.Depth);
                }

                Renderer.PopSettings();
            }

            if (processShadows && objectSpriteRecord.ShadowEntries.Count > 0)
            {
                SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, 1.0f, true, complexDepth);
                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, shadowRenderStencilState, null, palettedColorDrawEffect));

                for (int i = 0; i < objectSpriteRecord.ShadowEntries.Count; i++)
                {
                    var spriteEntry = objectSpriteRecord.ShadowEntries[i];

                    // It doesn't really matter what we give as color to the shadow
                    Renderer.DrawTexture(spriteEntry.Texture, spriteEntry.DrawingBounds, null, new Color(1.0f, 1.0f, 1.0f, 0), 0f, Vector2.Zero, SpriteEffects.None, spriteEntry.Depth);
                }

                Renderer.PopSettings();
            }

            if (objectSpriteRecord.TextEntries.Count > 0)
            {
                SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, 1.0f, false, false);
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
                DrawBaseNode(baseNode);
            }
            Renderer.PopSettings();
        }

        private void DrawBaseNode(GraphicalBaseNode graphicalBaseNode)
        {
            // TODO add base nodes to the regular rendering code

            int baseNodeIndex = graphicalBaseNode.Owner.BaseNodes.FindIndex(bn => bn == graphicalBaseNode.BaseNode);
            Color baseNodeIndexColor = Color.White * 0.7f;

            Point2D drawPoint = CellMath.CellTopLeftPointFromCellCoords_3D(graphicalBaseNode.BaseNode.Position, Map);

            // Base nodes can be large, let's increase the level of padding for them.
            int padding = Constants.RenderPixelPadding * 2;
            if (Camera.TopLeftPoint.X > drawPoint.X + padding || Camera.TopLeftPoint.Y > drawPoint.Y + padding ||
                GetCameraRightXCoord() < drawPoint.X - padding || GetCameraBottomYCoord() < drawPoint.Y - padding)
            {
                return;
            }

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
                Renderer.DrawStringWithShadow(iniName, Constants.UIBoldFont, drawPoint.ToXNAVector(), replacementColor, 1.0f);
                Renderer.DrawStringWithShadow("#" + baseNodeIndex, Constants.UIBoldFont, drawPoint.ToXNAVector() + new Vector2(0f, 20f), baseNodeIndexColor);
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

                    Renderer.DrawTexture(texture, new Rectangle(
                        bibFinalDrawPointX, bibFinalDrawPointY,
                        texture.Width, texture.Height),
                        null, remapColor,
                        0f, Vector2.Zero, SpriteEffects.None, 0f);

                    if (bibGraphics.HasRemapFrames())
                    {
                        Renderer.DrawTexture(bibGraphics.GetRemapFrame(0).Texture,
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
                Renderer.DrawStringWithShadow("#" + baseNodeIndex, Constants.UIBoldFont, drawPoint.ToXNAVector(), baseNodeIndexColor);
                return;
            }

            texture = frame.Texture;

            int x = drawPoint.X - frame.ShapeWidth / 2 + frame.OffsetX + Constants.CellSizeX / 2;
            int y = drawPoint.Y - frame.ShapeHeight / 2 + frame.OffsetY + Constants.CellSizeY / 2 + yDrawOffset;
            int width = texture.Width;
            int height = texture.Height;
            Rectangle drawRectangle = new Rectangle(x, y, width, height);

            SetPaletteEffectParams(palettedColorDrawEffect, graphics.GetPaletteTexture(), true, true, opacity);

            Renderer.DrawTexture(texture, drawRectangle, remapColor);

            if (graphics.HasRemapFrames())
            {
                Renderer.DrawTexture(graphics.GetRemapFrame(frameIndex).Texture, drawRectangle, remapColor);
            }

            SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, 1.0f);
            Renderer.DrawStringWithShadow("#" + baseNodeIndex, Constants.UIBoldFont, drawPoint.ToXNAVector(), baseNodeIndexColor);
        }

        private void DrawWaypoint(Waypoint waypoint)
        {
            Point2D drawPoint = CellMath.CellTopLeftPointFromCellCoords(waypoint.Position, Map);

            var cell = Map.GetTile(waypoint.Position);
            if (cell != null && !EditorState.Is2DMode)
                drawPoint -= new Point2D(0, cell.Level * Constants.CellHeight);

            if (Camera.TopLeftPoint.X > drawPoint.X + EditorGraphics.TileBorderTexture.Width ||
                Camera.TopLeftPoint.Y > drawPoint.Y + EditorGraphics.TileBorderTexture.Height ||
                GetCameraRightXCoord() < drawPoint.X ||
                GetCameraBottomYCoord() < drawPoint.Y)
            {
                // This waypoint is outside the camera
                return;
            }

            Color waypointColor = string.IsNullOrEmpty(waypoint.EditorColor) ? Color.Fuchsia : waypoint.XNAColor;
            var drawRectangle = new Rectangle(drawPoint.X, drawPoint.Y, EditorGraphics.GenericTileTexture.Width, EditorGraphics.GenericTileTexture.Height);

            Renderer.DrawTexture(EditorGraphics.GenericTileTexture, drawRectangle, new Color(0, 0, 0, 128));
            Renderer.DrawTexture(EditorGraphics.TileBorderTexture, drawRectangle, waypointColor);

            int fontIndex = Constants.UIBoldFont;
            string waypointIdentifier = waypoint.Identifier.ToString();
            var textDimensions = Renderer.GetTextDimensions(waypointIdentifier, fontIndex);
            Renderer.DrawStringWithShadow(waypointIdentifier,
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
            Renderer.DrawTexture(EditorGraphics.CellTagTexture, 
                new Rectangle(drawPoint.X, drawPoint.Y, EditorGraphics.CellTagTexture.Width, EditorGraphics.CellTagTexture.Height), color * cellTagAlpha);
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

            Renderer.DrawRectangle(rectangle, Color.Blue, BorderThickness);

            int impassableY = (int)(rectangle.Y + (Constants.CellSizeY * TopImpassableCellCount));
            Renderer.FillRectangle(new Rectangle(rectangle.X, impassableY - (BorderThickness / 2), rectangle.Width, BorderThickness), Color.Teal * 0.25f);
        }

        public void DrawTechnoRangeIndicators(TechnoBase techno)
        {
            if (techno == null)
                return;

            double range = techno.GetWeaponRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(techno.Position, range, techno.Owner.XNAColor);
            }

            range = techno.GetGuardRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(techno.Position, range, techno.Owner.XNAColor * 0.25f);
            }

            range = techno.GetGapGeneratorRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(techno.Position, range, Color.Black * 0.75f);
            }

            range = techno.GetCloakGeneratorRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(techno.Position, range, techno.GetRadialColor());
            }

            range = techno.GetSensorArrayRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(techno.Position, range, techno.GetRadialColor());
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

            Renderer.DrawTexture(EditorGraphics.RangeIndicatorTexture,
                new Rectangle(startX, startY, endX - startX, endY - startY), color);
        }

        public void DrawOnTileUnderCursor(MapTile tileUnderCursor, CursorAction cursorAction, bool isDraggingObject, bool isRotatingObject,
            IMovable draggedOrRotatedObject, bool isCloning, bool overlapObjects)
        {
            if (tileUnderCursor == null)
            {
                Renderer.DrawString("Null tile", 0, new Vector2(0f, 40f), Color.White);
                return;
            }

            if (cursorAction != null)
            {
                if (cursorAction.DrawCellCursor)
                    DrawTileCursor(tileUnderCursor);

                return;
            }

            if (isDraggingObject)
            {
                var startCell = Map.GetTile(draggedOrRotatedObject.Position);
                if (startCell == tileUnderCursor)
                    return;

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

                Renderer.DrawLine(startDrawPoint.ToXNAVector(), endDrawPoint.ToXNAVector(), lineColor, 1);
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

                Renderer.DrawLine(startDrawPoint.ToXNAVector(), endDrawPoint.ToXNAVector(), lineColor, 1);

                if (draggedOrRotatedObject.IsTechno())
                {
                    var techno = (TechnoBase)draggedOrRotatedObject;
                    Point2D point = tileUnderCursor.CoordsToPoint() - draggedOrRotatedObject.Position;

                    float angle = point.Angle() + ((float)Math.PI / 2.0f);
                    if (angle > (float)Math.PI * 2.0f)
                    {
                        angle -= ((float)Math.PI * 2.0f);
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
                DrawTileCursor(tileUnderCursor);
            }
        }

        private void DrawTileCursor(MapTile tileUnderCursor)
        {
            Color lineColor = new Color(96, 168, 96, 128);
            Point2D cellTopLeftPoint = CellMath.CellTopLeftPointFromCellCoords(new Point2D(tileUnderCursor.X, tileUnderCursor.Y), Map) - Camera.TopLeftPoint;

            int height = EditorState.Is2DMode ? 0 : tileUnderCursor.Level * Constants.CellHeight;

            cellTopLeftPoint = new Point2D((int)(cellTopLeftPoint.X * Camera.ZoomLevel), (int)((cellTopLeftPoint.Y - height) * Camera.ZoomLevel));

            var cellTopPoint = new Vector2(cellTopLeftPoint.X + (int)((Constants.CellSizeX / 2) * Camera.ZoomLevel), cellTopLeftPoint.Y);
            var cellLeftPoint = new Vector2(cellTopLeftPoint.X, cellTopLeftPoint.Y + (int)((Constants.CellSizeY / 2) * Camera.ZoomLevel));
            var cellRightPoint = new Vector2(cellTopLeftPoint.X + (int)(Constants.CellSizeX * Camera.ZoomLevel), cellLeftPoint.Y);
            var cellBottomPoint = new Vector2(cellTopPoint.X, cellTopLeftPoint.Y + (int)(Constants.CellSizeY * Camera.ZoomLevel));

            Renderer.DrawLine(cellTopPoint, cellLeftPoint, lineColor, 1);
            Renderer.DrawLine(cellRightPoint, cellTopPoint, lineColor, 1);
            Renderer.DrawLine(cellBottomPoint, cellLeftPoint, lineColor, 1);
            Renderer.DrawLine(cellRightPoint, cellBottomPoint, lineColor, 1);

            var shadowColor = new Color(0, 0, 0, 128);
            var down = new Vector2(0, 1f);

            Renderer.DrawLine(cellTopPoint + down, cellLeftPoint + down, shadowColor, 1);
            Renderer.DrawLine(cellRightPoint + down, cellTopPoint + down, shadowColor, 1);
            Renderer.DrawLine(cellBottomPoint + down, cellLeftPoint + down, shadowColor, 1);
            Renderer.DrawLine(cellRightPoint + down, cellBottomPoint + down, shadowColor, 1);

            int zoomedHeight = (int)(height * Camera.ZoomLevel);

            Color heightBarColor = new Color(16, 16, 16, (int)byte.MaxValue) * 0.75f;
            const int baseHeightLineSpaceAtBeginningOfStep = 6;
            int heightLineSpaceAtBeginningOfStep = Camera.ScaleIntWithZoom(baseHeightLineSpaceAtBeginningOfStep);
            int heightBarStep = Camera.ScaleIntWithZoom(Constants.CellHeight - baseHeightLineSpaceAtBeginningOfStep);
            const int heightBarWidth = 2;

            int y = 0;
            while (y < zoomedHeight - heightBarStep)
            {
                y += heightLineSpaceAtBeginningOfStep;
                Renderer.FillRectangle(new Rectangle((int)cellLeftPoint.X - 1, (int)cellLeftPoint.Y + y, heightBarWidth, heightBarStep), heightBarColor);
                Renderer.FillRectangle(new Rectangle((int)cellBottomPoint.X - 1, (int)cellBottomPoint.Y + y, heightBarWidth, heightBarStep), heightBarColor);
                Renderer.FillRectangle(new Rectangle((int)cellRightPoint.X - 1, (int)cellRightPoint.Y + y, heightBarWidth, heightBarStep), heightBarColor);
                y += heightBarStep;
            }
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

            Renderer.DrawTexture(EditorGraphics.ImpassableCellHighlightTexture, 
                new Rectangle(cellTopLeftPoint.X, cellTopLeftPoint.Y, 
                EditorGraphics.ImpassableCellHighlightTexture.Width, EditorGraphics.ImpassableCellHighlightTexture.Height),
                Color.White);
        }

        private void DrawIceGrowthHighlight(MapTile cell)
        {
            if (cell.IceGrowth <= 0)
                return;

            Point2D cellTopLeftPoint = EditorState.Is2DMode ?
                CellMath.CellTopLeftPointFromCellCoords(cell.CoordsToPoint(), Map) :
                CellMath.CellTopLeftPointFromCellCoords_3D(cell.CoordsToPoint(), Map);

            Renderer.DrawTexture(EditorGraphics.IceGrowthHighlightTexture,
                new Rectangle(cellTopLeftPoint.X, cellTopLeftPoint.Y,
                EditorGraphics.IceGrowthHighlightTexture.Width, EditorGraphics.IceGrowthHighlightTexture.Height),
                Color.White);
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
                    var drawPoint = CellMath.CellTopLeftPointFromCellCoords_3D(tube.EntryPoint, Map).ToXNAPoint();
                    var drawRectangle = new Rectangle(drawPoint.X, drawPoint.Y, EditorGraphics.GenericTileWithBorderTexture.Width, EditorGraphics.GenericTileWithBorderTexture.Height);
                    Renderer.DrawTexture(EditorGraphics.GenericTileWithBorderTexture, drawRectangle, color);
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

        public void Draw(bool isActive, TechnoBase technoUnderCursor, MapTile tileUnderCursor, CursorAction cursorAction)
        {
            if (isActive && tileUnderCursor != null && cursorAction != null)
            {
                cursorAction.PreMapDraw(tileUnderCursor.CoordsToPoint());
            }

            if (mapInvalidated || cameraMoved)
            {
                DrawVisibleMapPortion();
                mapInvalidated = false;
                cameraMoved = false;
            }

            CalculateMapRenderRectangles();

            DrawPerFrameTransparentElements(technoUnderCursor);

            DrawWorld();

            if (EditorState.DrawMapWideOverlay)
            {
                MapWideOverlay.Draw(new Rectangle(
                        (int)(-Camera.TopLeftPoint.X * Camera.ZoomLevel),
                        (int)((-Camera.TopLeftPoint.Y + Constants.MapYBaseline) * Camera.ZoomLevel),
                        (int)(mapRenderTarget.Width * Camera.ZoomLevel),
                        (int)((mapRenderTarget.Height - Constants.MapYBaseline) * Camera.ZoomLevel)));
            }

            if (isActive && tileUnderCursor != null && cursorAction != null)
            {
                cursorAction.PostMapDraw(tileUnderCursor.CoordsToPoint());
                cursorAction.DrawPreview(tileUnderCursor.CoordsToPoint(), Camera.TopLeftPoint);
            }
        }

        private void DrawPerFrameTransparentElements(TechnoBase technoUnderCursor)
        {
            Renderer.PushRenderTarget(transparencyPerFrameRenderTarget);

            GraphicsDevice.Clear(Color.Transparent);

            DrawMapBorder();
            DrawTechnoRangeIndicators(technoUnderCursor);

            Renderer.PopRenderTarget();
        }

        /// <summary>
        /// Draws the visible part of the map to the minimap.
        /// </summary>
        public void DrawOnMinimap()
        {
            if (MinimapUsers.Count > 0)
            {
                Renderer.PushRenderTarget(minimapRenderTarget);

                if (minimapNeedsRefresh)
                {
                    Renderer.DrawTexture(compositeRenderTarget,
                        new Rectangle(0, 0, mapRenderTarget.Width, mapRenderTarget.Height),
                        new Rectangle(0, 0, mapRenderTarget.Width, mapRenderTarget.Height),
                        Color.White);
                }
                else
                {
                    Renderer.DrawTexture(compositeRenderTarget,
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
            Rectangle sourceRectangle = new Rectangle(0, 0, mapRenderTarget.Width, mapRenderTarget.Height);
            Rectangle destinationRectangle = sourceRectangle;

            combineDrawEffect.Parameters["TerrainDepthTexture"].SetValue(mapDepthRenderTarget);
            combineDrawEffect.Parameters["ObjectsDepthTexture"].SetValue(objectsDepthRenderTarget);

            GraphicsDevice.SetRenderTarget(compositeRenderTarget);

            GraphicsDevice.Clear(Color.Black);

            // First, draw the map to the composite render target as a base.
            Renderer.DrawTexture(mapRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            // Then draw objects to the composite render target, making use of our custom shader.
            Renderer.PushRenderTarget(compositeRenderTarget,
                new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, depthRenderStencilState, null, combineDrawEffect));

            Renderer.DrawTexture(objectsRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            // Then draw transparency layers, without using a custom shader.
            Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null));

            Renderer.DrawTexture(transparencyRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            Renderer.DrawTexture(transparencyPerFrameRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            Renderer.PopSettings();

            Renderer.PopRenderTarget();

            // Last, draw the composite render target directly to the screen.

            Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null));

            Renderer.DrawTexture(compositeRenderTarget,
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
    }
}
