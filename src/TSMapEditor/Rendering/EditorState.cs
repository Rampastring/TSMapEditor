using System;
using TSMapEditor.GameMath;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.UI;

namespace TSMapEditor.Rendering
{
    /// <summary>
    /// Contains run-time settings related to the state of the editor.
    /// </summary>
    public class EditorState
    {
        public event EventHandler CursorActionChanged;
        public event EventHandler ObjectOwnerChanged;
        public event EventHandler AutoLATEnabledChanged;
        public event EventHandler OnlyPaintOnClearGroundChanged;
        public event EventHandler MapWideOverlayExistsChanged;
        public event EventHandler DrawMapWideOverlayChanged;
        public event EventHandler HighlightImpassableCellsChanged;
        public event EventHandler HighlightIceGrowthChanged;
        public event EventHandler BrushSizeChanged;
        public event EventHandler MarbleMadnessChanged;
        public event EventHandler Is2DModeChanged;
        public event EventHandler RenderedObjectsChanged;
        public event EventHandler LightingPreviewStateChanged;
        public event EventHandler IsLightingChanged;

        private CursorAction _cursorAction;
        public CursorAction CursorAction
        {
            get => _cursorAction;
            set
            {
                if (_cursorAction != value)
                {
                    if (_cursorAction != null)
                    {
                        _cursorAction.OnActionExit();
                        _cursorAction.OnExitingAction -= CursorAction_OnExitingAction;
                    }

                    _cursorAction = value;
                    if (_cursorAction != null)
                    {
                        _cursorAction.OnExitingAction += CursorAction_OnExitingAction;
                        _cursorAction.OnActionEnter();
                    }
                        
                    CursorActionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void CursorAction_OnExitingAction(object sender, EventArgs e)
        {
            CursorAction = null;
        }

        private House _objectOwner;
        public House ObjectOwner
        {
            get => _objectOwner;
            set
            {
                if (_objectOwner != value)
                {
                    _objectOwner = value;
                    ObjectOwnerChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private BrushSize _brushSize;
        public BrushSize BrushSize 
        {
            get => _brushSize; 
            set
            {
                if (_brushSize != value)
                {
                    _brushSize = value;
                    BrushSizeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public Randomizer Randomizer { get; } = new Randomizer();

        private bool _autoLatEnabled = true;
        public bool AutoLATEnabled
        {
            get => _autoLatEnabled;
            set
            {
                if (value != _autoLatEnabled)
                {
                    _autoLatEnabled = value;
                    AutoLATEnabledChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool _onlyPaintOnClearGround = false;
        public bool OnlyPaintOnClearGround
        {
            get => _onlyPaintOnClearGround;
            set
            {
                if (value != _onlyPaintOnClearGround)
                {
                    _onlyPaintOnClearGround = value;
                    OnlyPaintOnClearGroundChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool _mapWideOverlayExists;
        public bool MapWideOverlayExists 
        {
            get => _mapWideOverlayExists;
            set
            {
                if (value != _mapWideOverlayExists)
                {
                    _mapWideOverlayExists = value;
                    MapWideOverlayExistsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool _drawMapWideOverlay;
        public bool DrawMapWideOverlay 
        {
            get => _drawMapWideOverlay; 
            set
            {
                if (value != _drawMapWideOverlay)
                {
                    _drawMapWideOverlay = value;
                    DrawMapWideOverlayChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool _highlightImpassableCells;
        public bool HighlightImpassableCells
        {
            get => _highlightImpassableCells;
            set
            {
                if (value != _highlightImpassableCells)
                {
                    _highlightImpassableCells = value;
                    HighlightImpassableCellsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool _highlightIceGrowth;
        public bool HighlightIceGrowth
        {
            get => _highlightIceGrowth;
            set
            {
                if (value != _highlightIceGrowth)
                {
                    _highlightIceGrowth = value;
                    HighlightIceGrowthChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool _isMarbleMadness = false;
        public bool IsMarbleMadness
        {
            get => _isMarbleMadness;
            set
            {
                if (value != _isMarbleMadness)
                {
                    _isMarbleMadness = value;
                    MarbleMadnessChanged?.Invoke(this, EventArgs.Empty);
                    RefreshLightingEnabledState();
                }
            }
        }

        private bool _is2DMode = false;
        public bool Is2DMode
        {
            get => _is2DMode;
            set
            {
                if (value != _is2DMode)
                {
                    _is2DMode = value;
                    Is2DModeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool _isLighting = true;
        public bool IsLighting
        {
            get => _isLighting;
        }

        private void RefreshLightingEnabledState()
        {
            bool oldIsLighting = _isLighting;
            _isLighting = (LightingPreviewState != LightingPreviewMode.NoLighting) && !IsMarbleMadness;

            if (oldIsLighting != _isLighting)
                IsLightingChanged?.Invoke(this, EventArgs.Empty);
        }

        private LightingPreviewMode _lightingPreviewState = LightingPreviewMode.Normal;
        public LightingPreviewMode LightingPreviewState
        {
            get => _lightingPreviewState;
            set
            {
                if (!Enum.IsDefined(value.GetType(), value))
                    value = LightingPreviewMode.NoLighting;

                if (value == _lightingPreviewState)
                    return;

                _lightingPreviewState = value;
                RefreshLightingEnabledState();
                LightingPreviewStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public DeletionMode DeletionMode { get; set; } = DeletionMode.All;

        private RenderObjectFlags _renderObjectFlags = RenderObjectFlags.All;
        public RenderObjectFlags RenderObjectFlags 
        {
            get => _renderObjectFlags;
            set
            {
                if (value != _renderObjectFlags)
                {
                    _renderObjectFlags = value;
                    RenderedObjectsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool RenderInvisibleInGameObjects { get; set; } = true;

        public CopiedMapData CopiedMapData { get; set; }
    }
}
