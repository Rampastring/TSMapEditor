﻿using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
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

        public bool IsMarbleMadness { get; set; } = false;

        private CursorAction _cursorAction;
        public CursorAction CursorAction
        {
            get => _cursorAction;
            set
            {
                if (_cursorAction != value)
                {
                    if (_cursorAction != null)
                        _cursorAction.ExitAction();

                    _cursorAction = value;
                    CursorActionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
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

        public BrushSize BrushSize { get; set; } = new BrushSize(1, 1);

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
    }
}
