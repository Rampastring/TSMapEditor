using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    /// <summary>
    /// Base class for all object renderers.
    /// </summary>
    /// <typeparam name="T">The type of game object to render.</typeparam>
    public abstract class ObjectRenderer<T> where T : GameObject
    {
        protected ObjectRenderer(RenderDependencies renderDependencies)
        {
            RenderDependencies = renderDependencies;
        }

        protected RenderDependencies RenderDependencies;

        protected Map Map => RenderDependencies.Map;
        protected TheaterGraphics TheaterGraphics => RenderDependencies.TheaterGraphics;

        protected abstract Color ReplacementColor { get; }

        public void UpdateDepthRenderTarget(RenderTarget2D depthRenderTarget)
        {
            RenderDependencies.DepthRenderTarget = depthRenderTarget;
        }

        /// <summary>
        /// The entry point for rendering an object.
        ///
        /// Checks whether the object is within the visible screen space. If yes,
        /// draws the graphics of the object, or the object's replacement text in
        /// case it has no loaded graphics.
        /// </summary>
        /// <param name="gameObject">The game object to render.</param>
        public void Draw(T gameObject)
        {
            Point2D drawPointWithoutCellHeight = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position, RenderDependencies.Map);

            var mapCell = RenderDependencies.Map.GetTile(gameObject.Position);
            int heightOffset = RenderDependencies.EditorState.Is2DMode ? 0 : mapCell.Level * Constants.CellHeight;
            Point2D drawPoint = new Point2D(drawPointWithoutCellHeight.X, drawPointWithoutCellHeight.Y - heightOffset);

            CommonDrawParams drawParams = GetDrawParams(gameObject);

            PositionedTexture frame = GetFrameTexture(gameObject, drawParams);

            GetTextureDrawCoords(gameObject, frame, drawPoint,
                drawPointWithoutCellHeight.Y,
                out int finalDrawPointX, out int finalDrawPointRight,
                out int finalDrawPointY, out int finalDrawPointBottom,
                out int objectYDrawPointWithoutCellHeight);

            // If the object is not in view, skip
            if (!IsObjectInCamera(finalDrawPointX, finalDrawPointRight, finalDrawPointY, finalDrawPointBottom))
                return;

            if (frame == null && ShouldRenderReplacementText(gameObject))
            {
                DrawObjectReplacementText(gameObject, drawParams, drawPoint);
            }
            else
            {
                Render(gameObject, drawPointWithoutCellHeight.Y, drawPoint, drawParams);
            }
        }

        /// <summary>
        /// Returns a bool that determines whether a game object
        /// should be rendered as text in case it does not have
        /// regular graphics loaded.
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        protected virtual bool ShouldRenderReplacementText(T gameObject)
        {
            return true;
        }

        /// <summary>
        /// Fetches parameters for drawing the object.
        /// Override in derived classes to get the necessary parameters
        /// for drawing the type of object.
        /// </summary>
        /// <param name="gameObject">The game object to get the drawing parameters for.</param>
        protected abstract CommonDrawParams GetDrawParams(T gameObject);

        /// <summary>
        /// Renders an object. Override in derived classes to implement and customize the rendering process.
        /// </summary>
        /// <param name="gameObject">The game object to draw.</param>
        /// <param name="yDrawPointWithoutCellHeight">The Y-axis draw coordinate of the object, prior to taking cell height into account.</param>
        /// <param name="drawPoint">The draw point of the object, with cell height taken into account.</param>
        /// <param name="commonDrawParams">Draw parameters.</param>
        protected abstract void Render(T gameObject, int yDrawPointWithoutCellHeight, Point2D drawPoint, CommonDrawParams commonDrawParams);

        /// <summary>
        /// Renders the replacement text of an object, displayed when no graphics for an object have been loaded.
        /// Override in derived classes to implement and customize the rendering process.
        /// </summary>
        /// <param name="gameObject">The game object for which to draw a replacement text.</param>
        /// <param name="drawParams">Draw parameters.</param>
        /// <param name="drawPoint">The draw point of the object, with cell height taken into account.</param>
        protected virtual void DrawObjectReplacementText(T gameObject, CommonDrawParams drawParams, Point2D drawPoint)
        {
            SetEffectParams(0.0f, 0.0f, Vector2.Zero, Vector2.Zero);

            // If the object is a techno, draw an arrow that displays its facing
            if (gameObject.IsTechno())
            {
                var techno = gameObject as TechnoBase;
                var cellCenterPoint = (drawPoint + new Point2D(Constants.CellSizeX / 2, Constants.CellSizeY / 2)).ToXNAVector();

                float rad = (techno.Facing / 255.0f) * (float)Math.PI * 2.0f;

                // The in-game compass is slightly rotated compared to the usual math compass
                // and the compass used by MonoGame.
                // In the usual compass, 0 rad points directly towards the right / east, in the in-game
                // compass it points to top-right / northeast
                rad -= (float)Math.PI / 4.0f;

                var arrowEndPoint = Helpers.VectorFromLengthAndAngle(Constants.CellSizeX / 4, rad);
                arrowEndPoint += new Vector2(arrowEndPoint.X, 0f); // Isometric perspective
                RendererExtensions.DrawArrow(cellCenterPoint, cellCenterPoint + arrowEndPoint, Color.Yellow, 1f, 10f, 1);
            }

            Renderer.DrawString(drawParams.IniName, 1, drawPoint.ToXNAVector(), ReplacementColor, 1.0f);
        }

        private bool IsObjectInCamera(int drawPointX, int drawPointRight, int drawPointY, int drawPointBottom)
        {
            if (drawPointRight < RenderDependencies.Camera.TopLeftPoint.X || drawPointX > RenderDependencies.GetCameraRightXCoord())
                return false;

            if (drawPointBottom < RenderDependencies.Camera.TopLeftPoint.Y || drawPointY > RenderDependencies.GetCameraBottomYCoord())
                return false;

            return true;
        }

        private PositionedTexture GetFrameTexture(T gameObject, CommonDrawParams drawParams)
        {
            if (drawParams.Graphics != null && drawParams.Graphics.Frames != null && drawParams.Graphics.Frames.Length > 0)
            {
                int frameIndex = gameObject.GetFrameIndex(drawParams.Graphics.Frames.Length);

                if (frameIndex > -1 && frameIndex < drawParams.Graphics.Frames.Length)
                    return drawParams.Graphics.Frames[frameIndex];
            }

            return null;
        }

        private void GetTextureDrawCoords(T gameObject,
            PositionedTexture frame,
            Point2D initialDrawPoint,
            int initialYDrawPointWithoutCellHeight,
            out int finalDrawPointX, 
            out int finalDrawPointRight, 
            out int finalDrawPointY, 
            out int finalDrawPointBottom,
            out int objectYDrawPointWithoutCellHeight)
        {
            if (frame != null)
            {
                int yDrawOffset = gameObject.GetYDrawOffset();
                int xDrawOffset = gameObject.GetXDrawOffset();

                finalDrawPointX = initialDrawPoint.X - frame.ShapeWidth / 2 + frame.OffsetX + Constants.CellSizeX / 2 + xDrawOffset;
                finalDrawPointY = initialDrawPoint.Y - frame.ShapeHeight / 2 + frame.OffsetY + Constants.CellSizeY / 2 + yDrawOffset;
                objectYDrawPointWithoutCellHeight = initialYDrawPointWithoutCellHeight - frame.ShapeHeight / 2 + frame.OffsetY + Constants.CellSizeY / 2 + yDrawOffset;

                finalDrawPointRight = finalDrawPointX + frame.Texture.Width;
                finalDrawPointBottom = finalDrawPointY + frame.Texture.Height;
            }
            else
            {
                finalDrawPointX = initialDrawPoint.X;
                finalDrawPointRight = initialDrawPoint.X;
                finalDrawPointY = initialDrawPoint.Y;
                finalDrawPointBottom = initialDrawPoint.Y;
                objectYDrawPointWithoutCellHeight = initialYDrawPointWithoutCellHeight;
            }
        }

        protected void SetEffectParams(float bottomDepth, float topDepth,
            Vector2 worldTextureCoordinates, Vector2 spriteSizeToWorldSizeRatio)
            => SetEffectParams(RenderDependencies.ColorDrawEffect, bottomDepth, topDepth,
                worldTextureCoordinates, spriteSizeToWorldSizeRatio, RenderDependencies.DepthRenderTarget);

        protected void SetEffectParams(Effect effect, float bottomDepth, float topDepth,
            Vector2 worldTextureCoordinates, Vector2 spriteSizeToWorldSizeRatio, Texture2D depthTexture)
        {
            effect.Parameters["SpriteDepthBottom"].SetValue(bottomDepth);
            effect.Parameters["SpriteDepthTop"].SetValue(topDepth);
            effect.Parameters["WorldTextureCoordinates"].SetValue(worldTextureCoordinates);
            effect.Parameters["SpriteSizeToWorldSizeRatio"].SetValue(spriteSizeToWorldSizeRatio);
            RenderDependencies.GraphicsDevice.SamplerStates[1] = SamplerState.LinearClamp;

            if (depthTexture != null)
            {
                effect.Parameters["DepthTexture"].SetValue(depthTexture);
                RenderDependencies.GraphicsDevice.Textures[1] = depthTexture;
            }
        }

        protected virtual void DrawShadow(T gameObject, CommonDrawParams drawParams, Point2D drawPoint, int initialYDrawPointWithoutCellHeight)
        {
            int shadowFrameIndex = gameObject.GetShadowFrameIndex(drawParams.Graphics.Frames.Length);
            if (shadowFrameIndex > 0 && shadowFrameIndex < drawParams.Graphics.Frames.Length)
            {
                DrawObjectImage(gameObject, drawParams, drawParams.Graphics, shadowFrameIndex,
                    new Color(0, 0, 0, 128), false, Color.White, drawPoint, initialYDrawPointWithoutCellHeight);
            }
        }

        protected void DrawObjectImage(T gameObject, CommonDrawParams commonDrawParams, ObjectImage image,
            int frameIndex, Color color, bool drawRemap, Color remapColor, Point2D drawPoint, int initialYDrawPointWithoutCellHeight)
        {
            PositionedTexture frame = image.Frames[frameIndex];
            if (frame == null || frame.Texture == null)
                return;

            PositionedTexture remapFrame = null;
            if (drawRemap && Constants.HQRemap && image.RemapFrames != null)
                remapFrame = image.RemapFrames[frameIndex];

            GetTextureDrawCoords(gameObject, frame, drawPoint, initialYDrawPointWithoutCellHeight, 
                out int finalDrawPointX, out _, out int finalDrawPointY, out _, out int finalYDrawPointWithoutCellHeight);

            RenderFrame(frame, remapFrame, color, drawRemap, remapColor,
                finalDrawPointX, finalDrawPointY, finalYDrawPointWithoutCellHeight);
        }

        private void RenderFrame(PositionedTexture frame, PositionedTexture remapFrame, Color color, bool drawRemap, Color remapColor,
            int finalDrawPointX, int finalDrawPointY, int finalYDrawPointWithoutCellHeight)
        {
            Texture2D texture = frame.Texture;

            ApplyDepthEffectValues(finalYDrawPointWithoutCellHeight, texture, new Point2D(finalDrawPointX, finalDrawPointY));

            Renderer.DrawTexture(texture, 
                new Rectangle(finalDrawPointX, finalDrawPointY, texture.Width, texture.Height),
                null, color,
                0f, Vector2.Zero, SpriteEffects.None, 0f);

            if (drawRemap && Constants.HQRemap && remapFrame != null)
            {
                Renderer.DrawTexture(remapFrame.Texture,
                    new Rectangle(finalDrawPointX, finalDrawPointY, texture.Width, texture.Height),
                    null,
                    remapColor,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0f);
            }
        }

        private void ApplyDepthEffectValues(int yDrawPointWithoutCellHeight, Texture2D texture, Point2D finalDrawPoint)
        {
            int depthOffset = Constants.CellSizeY;

            float depthTop = (yDrawPointWithoutCellHeight + depthOffset) / (float)Map.HeightInPixels;
            float depthBottom = (yDrawPointWithoutCellHeight + texture.Height + depthOffset) / (float)Map.HeightInPixels;
            depthTop = 1.0f - depthTop;
            depthBottom = 1.0f - depthBottom;

            Vector2 worldTextureCoordinates = new Vector2(finalDrawPoint.X / (float)Map.WidthInPixels, finalDrawPoint.Y / (float)Map.HeightInPixels);
            Vector2 spriteSizeToWorldSizeRatio = new Vector2(texture.Width / (float)Map.WidthInPixels, texture.Height / (float)Map.HeightInPixels);

            SetEffectParams(depthBottom, depthTop, worldTextureCoordinates, spriteSizeToWorldSizeRatio);
        }

        protected void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1, float depth = 0f)
            => Renderer.DrawLine(start, end, color, thickness, depth);
    }
}
