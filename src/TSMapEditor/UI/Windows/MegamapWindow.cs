using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI;
using System;
using TSMapEditor.GameMath;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class MegamapClickedEventArgs : EventArgs
    {
        public MegamapClickedEventArgs(Point2D clickedPoint)
        {
            ClickedPoint = clickedPoint;
        }

        /// <summary>
        /// The pixel point on the entire map that was clicked on.
        /// </summary>
        public Point2D ClickedPoint { get; }
    }

    public class MegamapWindow : EditorWindow
    {
        private const int DefaultSizeChange = 20;
        private const int MinSize = 100;

        public MegamapWindow(WindowManager windowManager, ICursorActionTarget cursorActionTarget, bool enableToolbar) : base(windowManager)
        {
            this.cursorActionTarget = cursorActionTarget;
            this.enableToolbar = enableToolbar;
            MegamapTexture = cursorActionTarget.MinimapTexture;

            double ratio = MegamapTexture.Width / (double)MegamapTexture.Height;
            if (ratio > 2.0)
            {
                Width = 300;
                Height = (int)(Width / ratio);
            }
            else
            {
                Height = 150;
                Width = (int)(Height * ratio);
            }

            ClientRectangleUpdated += MegamapWindow_ClientRectangleUpdated;
            EnabledChanged += MegamapWindow_EnabledChanged;
            MegamapWindow_EnabledChanged(this, EventArgs.Empty);
        }

        private void MarkMinimapUsage()
        {
            if (Enabled)
                cursorActionTarget.MinimapUsers.Add(this);
            else
                cursorActionTarget.MinimapUsers.Remove(this);
        }

        private void MegamapWindow_EnabledChanged(object sender, EventArgs e)
        {
            MarkMinimapUsage();
        }

        public event EventHandler<MegamapClickedEventArgs> MegamapClicked;

        public Rectangle CameraRectangle { get; set; }

        private void MegamapWindow_ClientRectangleUpdated(object sender, EventArgs e)
        {
            CalculateTextureDrawRectangle();

            if (closeButton != null)
                closeButton.X = Width - closeButton.Width;
        }

        private readonly ICursorActionTarget cursorActionTarget;
        private readonly bool enableToolbar;

        private Texture2D _megamapTexture;
        public Texture2D MegamapTexture
        {
            get => _megamapTexture;
            set
            {
                if (value != _megamapTexture)
                {
                    _megamapTexture = value;
                    CalculateTextureDrawRectangle();
                }
            }
        }

        private EditorButton closeButton;

        private Rectangle textureDrawRectangle;

        private bool wasLeftDown = false;
        private Point2D oldWindowPosition;


        private void CalculateTextureDrawRectangle()
        {
            int startY;
            int previewHeight;

            if (enableToolbar)
            {
                startY = Constants.UIButtonHeight;
                previewHeight = Height - Constants.UIButtonHeight;
            }
            else
            {
                startY = 0;
                previewHeight = Height;
            }

            float xRatio = Width / (float)MegamapTexture.Width;
            float yRatio = previewHeight / (float)MegamapTexture.Height;
            float drawRatio = Math.Min(xRatio, yRatio);

            int drawWidth = (int)(MegamapTexture.Width * drawRatio);
            int drawHeight = (int)(MegamapTexture.Height * drawRatio);

            textureDrawRectangle = new Rectangle((Width - drawWidth) / 2, startY + (previewHeight - drawHeight) / 2, drawWidth, drawHeight);
        }

        public override void Initialize()
        {
            if (enableToolbar)
            {
                closeButton = new EditorButton(WindowManager);
                closeButton.Name = "btnCloseX";
                closeButton.Width = Constants.UIButtonHeight;
                closeButton.Height = Constants.UIButtonHeight;
                closeButton.Text = "X";
                closeButton.X = Width - closeButton.Width;
                closeButton.Y = 0;
                AddChild(closeButton);
                closeButton.LeftClick += (s, e) => Hide();

                var btnIncreaseSize = new EditorButton(WindowManager);
                btnIncreaseSize.Name = nameof(btnIncreaseSize);
                btnIncreaseSize.Width = Constants.UIButtonHeight;
                btnIncreaseSize.Height = Constants.UIButtonHeight;
                btnIncreaseSize.Text = "+";
                btnIncreaseSize.X = 0;
                btnIncreaseSize.Y = 0;
                AddChild(btnIncreaseSize);
                btnIncreaseSize.LeftClick += (s, e) => IncreaseSize();

                var btnDecreaseSize = new EditorButton(WindowManager);
                btnDecreaseSize.Name = nameof(btnDecreaseSize);
                btnDecreaseSize.Width = Constants.UIButtonHeight;
                btnDecreaseSize.Height = Constants.UIButtonHeight;
                btnDecreaseSize.Text = "-";
                btnDecreaseSize.X = btnIncreaseSize.Right;
                btnDecreaseSize.Y = 0;
                AddChild(btnDecreaseSize);
                btnDecreaseSize.LeftClick += (s, e) => DecreaseSize();
            }

            Name = nameof(MegamapWindow);
            base.Initialize();
        }

        private void IncreaseSize()
        {
            ChangeSize(DefaultSizeChange);
        }

        private void DecreaseSize()
        {
            ChangeSize(-DefaultSizeChange);
        }

        private void ChangeSize(int amount)
        {
            int increaseX;
            int increaseY;

            double ratio = MegamapTexture.Width / (double)MegamapTexture.Height;
            if (ratio > Constants.CellSizeX / Constants.CellSizeY)
            {
                increaseX = amount;
                increaseY = (int)(amount / ratio);
            }
            else
            {
                increaseY = (int)(amount / ratio);
                increaseX = (int)(increaseY * ratio);
            }

            if ((amount > 0 && Width < WindowManager.RenderResolutionX) ||
                (amount < 0 && Width > MinSize))
            {
                Width += increaseX;
            }

            if ((amount > 0 && Height < WindowManager.RenderResolutionY) ||
                (amount < 0 && Height > MinSize))
            {
                Height += increaseY;
            }
        }

        public void Open()
        {
            Show();
        }

        private void MoveCamera()
        {
            var cursorPoint = GetCursorPoint();

            double x = cursorPoint.X - textureDrawRectangle.X;
            x /= textureDrawRectangle.Width;
            x *= MegamapTexture.Width;

            double y = cursorPoint.Y - textureDrawRectangle.Y;
            y /= textureDrawRectangle.Height;
            y *= MegamapTexture.Height;

            MegamapClicked?.Invoke(this, new MegamapClickedEventArgs(new Point2D((int)x, (int)y)));
        }

        public override void Update(GameTime gameTime)
        {
            if (!enableToolbar && Keyboard.IsKeyHeldDown(Keys.Escape))
            {
                Enabled = false;
                WindowManager.RemoveControl(this);
            }

            if (!IsChildActive)
            {
                if (IsActive && Cursor.LeftDown)
                {
                    if (!wasLeftDown)
                        oldWindowPosition = new Point2D(X, Y);

                    CanBeMoved = !Keyboard.IsShiftHeldDown();
                    if (!CanBeMoved)
                        MoveCamera();

                    wasLeftDown = true;
                }
                else if (wasLeftDown)
                {
                    if (oldWindowPosition == new Point2D(X, Y))
                    {
                        MoveCamera();
                    }

                    wasLeftDown = false;
                }
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, null, null, null));
            DrawTexture(MegamapTexture, textureDrawRectangle, Color.White);
            Renderer.PopSettings();

            if (CameraRectangle.Width > 0 && CameraRectangle.Height > 0)
            {
                double xPos = (CameraRectangle.X / (double)MegamapTexture.Width) * textureDrawRectangle.Width;
                double yPos = (CameraRectangle.Y / (double)MegamapTexture.Height) * textureDrawRectangle.Height;
                double width = (CameraRectangle.Width / (double)MegamapTexture.Width) * textureDrawRectangle.Width;
                double height = (CameraRectangle.Height / (double)MegamapTexture.Height) * textureDrawRectangle.Height;

                DrawRectangle(new Rectangle(textureDrawRectangle.X + (int)xPos, textureDrawRectangle.Y + (int)yPos, (int)width, (int)height), Color.White, 1);
            }

            DrawChildren(gameTime);

            if (!enableToolbar)
            {
                DrawStringWithShadow("Press ESC to close", 1, new Vector2(Constants.UIEmptySideSpace, Constants.UIEmptyTopSpace), Color.Red, 1.0f);
            }

            DrawPanelBorders();
        }
    }
}
