using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;

namespace TSMapEditor.UI.Controls
{
    public class EditorWindow : EditorPanel
    {
        private const float AppearingRate = 0.9f;
        private const float DisappearingRate = -1.0f;

        public EditorWindow(WindowManager windowManager) : base(windowManager)
        {
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
        }

        public event EventHandler Closed;
        public event EventHandler InteractedWith;


        /// <summary>
        /// This is stored here for the purposes of being able
        /// to clean up event handlers when the window controller
        /// for a session is cleaned.
        /// </summary>
        public EventHandler FocusSwitchEventHandler { get; set; }

        protected bool CanBeMoved { get; set; } = true;

        protected bool IsDragged;
        private Point lastCursorPoint;

        public override void Initialize()
        {
            Color baseColor = UISettings.ActiveSettings.PanelBackgroundColor;
            var backgroundColor = new Color(baseColor.R / 2, baseColor.G / 2, baseColor.B / 2, 222);
            BackgroundTexture = AssetLoader.CreateTexture(backgroundColor, 2, 2);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            base.Initialize();

            WindowManager.RenderResolutionChanged += WindowManager_RenderResolutionChanged;
        }

        private void WindowManager_RenderResolutionChanged(object sender, EventArgs e)
        {
            ConstrainPosition();
        }

        public override void Kill()
        {
            WindowManager.RenderResolutionChanged -= WindowManager_RenderResolutionChanged;

            base.Kill();
        }

        private void CloseButton_LeftClick(object sender, EventArgs e)
        {
            Hide();
            Closed?.Invoke(this, EventArgs.Empty);
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == nameof(CanBeMoved))
            {
                CanBeMoved = Conversions.BooleanFromString(value, CanBeMoved);
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public void PutOnBackground()
        {
            // hack time! allow the other window to show on top of this one
            WindowManager.AddCallback(new Action(() => { DrawOrder -= 500; UpdateOrder -= 500; }));
        }

        public void Hide()
        {
            AlphaRate = DisappearingRate;
        }

        protected virtual void Show()
        {
            AlphaRate = AppearingRate;
            Alpha = 0f;
            Enable();
            IsDragged = false;

            ConstrainPosition();

            InteractedWith?.Invoke(this, EventArgs.Empty);
        }

        private void ConstrainPosition()
        {
            if (ScaledWidth > WindowManager.RenderResolutionX)
                X = (WindowManager.RenderResolutionX - ScaledWidth) / 2;
            else if (X + ScaledWidth > WindowManager.RenderResolutionX)
                X = WindowManager.RenderResolutionX - ScaledWidth;
            else if (X < 0)
                X = 0;

            if (ScaledHeight > WindowManager.RenderResolutionY)
                Y = (WindowManager.RenderResolutionY - ScaledHeight) / 2;
            else if (Y + ScaledHeight > WindowManager.RenderResolutionY)
                Y = WindowManager.RenderResolutionY - ScaledHeight;
            else if (Y < 0)
                Y = 0;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Alpha <= 0f && AlphaRate < 0.0f)
                Disable();

            if (IsDragged)
            {
                Point newCursorPoint = GetCursorPoint();
                X = X + (newCursorPoint.X - lastCursorPoint.X) * Scaling;
                Y = Y + (newCursorPoint.Y - lastCursorPoint.Y) * Scaling;

                ConstrainPosition();
                lastCursorPoint = GetCursorPoint();
                IsDragged = Cursor.LeftDown;
            }

            if (IsActive && CanBeMoved && Cursor.LeftPressedDown)
            {
                var activeChild = Children.FirstOrDefault(c => c.IsActive);

                if (activeChild != null)
                {
                    // Find the last active child from the control hierarchy
                    while (true)
                    {
                        var childOfChild = activeChild.Children.FirstOrDefault(c => c.IsActive);
                        if (childOfChild == null)
                            break;

                        activeChild = childOfChild;
                    }
                }

                // Only allow moving window if the active child is not a control that is used by dragging
                // TODO this could be made more object-oriented with a property at XNAControl level
                if (activeChild == null || !(activeChild is XNAPanel || activeChild is XNAScrollBar || activeChild is XNATrackbar))
                {
                    InteractedWith?.Invoke(this, EventArgs.Empty);
                    IsDragged = true;
                    lastCursorPoint = GetCursorPoint();
                }
            }
        }
    }
}
