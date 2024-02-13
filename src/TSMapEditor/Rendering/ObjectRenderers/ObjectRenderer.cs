using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using System;
using TSMapEditor.CCEngine;
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
        /// <param name="checkInCamera">Whether the object's presence within the camera should be checked.</param>
        public void Draw(T gameObject, bool checkInCamera)
        {
            Point2D drawPointWithoutCellHeight = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position, RenderDependencies.Map);

            var mapCell = RenderDependencies.Map.GetTile(gameObject.Position);
            int heightOffset = RenderDependencies.EditorState.Is2DMode ? 0 : mapCell.Level * Constants.CellHeight;
            Point2D drawPoint = new Point2D(drawPointWithoutCellHeight.X, drawPointWithoutCellHeight.Y - heightOffset);

            CommonDrawParams drawParams = GetDrawParams(gameObject);

            PositionedTexture frame = GetFrameTexture(gameObject, drawParams);

            Rectangle drawingBounds = GetTextureDrawCoords(gameObject, frame, drawPoint);

            // If the object is not in view, skip
            if (checkInCamera && !IsObjectInCamera(drawingBounds))
                return;

            if (frame == null && ShouldRenderReplacementText(gameObject))
            {
                DrawObjectReplacementText(gameObject, drawParams, drawPoint);
            }
            else
            {
                Render(gameObject, heightOffset, drawPoint, drawParams);
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
        /// <param name="heightOffset">The Y-axis draw offset from cell height.</param>
        /// <param name="drawPoint">The draw point of the object, with cell height taken into account.</param>
        /// <param name="drawParams">Draw parameters.</param>
        protected abstract void Render(T gameObject, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams);

        /// <summary>
        /// Renders the replacement text of an object, displayed when no graphics for an object have been loaded.
        /// Override in derived classes to implement and customize the rendering process.
        /// </summary>
        /// <param name="gameObject">The game object for which to draw a replacement text.</param>
        /// <param name="drawParams">Draw parameters.</param>
        /// <param name="drawPoint">The draw point of the object, with cell height taken into account.</param>
        protected virtual void DrawObjectReplacementText(T gameObject, in CommonDrawParams drawParams, Point2D drawPoint)
        {
            SetEffectParams_RGBADraw(0.0f, 0.0f, Vector2.Zero, Vector2.Zero, false);

            // If the object is a techno, draw an arrow that displays its facing
            if (gameObject.IsTechno())
            {
                var techno = gameObject as TechnoBase;
                DrawObjectFacingArrow(techno.Facing, drawPoint);
            }

            Renderer.DrawString(drawParams.IniName, 1, drawPoint.ToXNAVector(), ReplacementColor, 1.0f);
        }

        protected void DrawObjectFacingArrow(byte facing, Point2D drawPoint)
        {
            var cellCenterPoint = (drawPoint + new Point2D(Constants.CellSizeX / 2, Constants.CellSizeY / 2)).ToXNAVector();

            float rad = (facing / 255.0f) * (float)Math.PI * 2.0f;

            // The in-game compass is slightly rotated compared to the usual math compass
            // and the compass used by MonoGame.
            // In the usual compass, 0 rad points directly towards the right / east, in the in-game
            // compass it points to top-right / northeast
            rad -= (float)Math.PI / 4.0f;

            var arrowEndPoint = Helpers.VectorFromLengthAndAngle(Constants.CellSizeX / 4, rad);
            arrowEndPoint += new Vector2(arrowEndPoint.X, 0f); // Isometric perspective
            RendererExtensions.DrawArrow(cellCenterPoint, cellCenterPoint + arrowEndPoint, Color.Yellow, 1f, 10f, 2);
        }

        private bool IsObjectInCamera(Rectangle drawingBounds)
        {
            if (drawingBounds.X + drawingBounds.Width < RenderDependencies.Camera.TopLeftPoint.X || drawingBounds.X > RenderDependencies.GetCameraRightXCoord())
                return false;

            if (drawingBounds.Y + drawingBounds.Height < RenderDependencies.Camera.TopLeftPoint.Y || drawingBounds.Y > RenderDependencies.GetCameraBottomYCoord())
                return false;

            return true;
        }

        private PositionedTexture GetFrameTexture(T gameObject, in CommonDrawParams drawParams)
        {
            if (drawParams.ShapeImage != null && drawParams.ShapeImage.GetFrameCount() > 0)
            {
                int frameIndex = gameObject.GetFrameIndex(drawParams.ShapeImage.GetFrameCount());

                if (frameIndex > -1 && frameIndex < drawParams.ShapeImage.GetFrameCount())
                    return drawParams.ShapeImage.GetFrame(frameIndex);
            }
            else if (drawParams.MainVoxel?.Frames != null)
            {
                return drawParams.MainVoxel.GetFrame(0, RampType.None);
            }
            else if (drawParams.TurretVoxel?.Frames != null)
            {
                return drawParams.TurretVoxel.GetFrame(0, RampType.None);
            }
            else if (drawParams.BarrelVoxel?.Frames != null)
            {
                return drawParams.BarrelVoxel.GetFrame(0, RampType.None);
            }

            return null;
        }

        private Rectangle GetTextureDrawCoords(T gameObject,
            PositionedTexture frame,
            Point2D initialDrawPoint)
        {
            int finalDrawPointX;
            int finalDrawPointY;

            if (frame != null)
            {
                int yDrawOffset = gameObject.GetYDrawOffset();
                int xDrawOffset = gameObject.GetXDrawOffset();

                finalDrawPointX = initialDrawPoint.X - frame.ShapeWidth / 2 + frame.OffsetX + Constants.CellSizeX / 2 + xDrawOffset;
                finalDrawPointY = initialDrawPoint.Y - frame.ShapeHeight / 2 + frame.OffsetY + Constants.CellSizeY / 2 + yDrawOffset;

            }
            else
            {
                finalDrawPointX = initialDrawPoint.X;
                finalDrawPointY = initialDrawPoint.Y;
            }

            return new Rectangle(finalDrawPointX, finalDrawPointY,
                finalDrawPointX + frame?.Texture.Width ?? 0, finalDrawPointY + frame?.Texture.Height ?? 0);
        }

        protected void SetEffectParams_PalettedDraw(float bottomDepth, float topDepth,
            Vector2 worldTextureCoordinates, Vector2 spriteSizeToWorldSizeRatio, bool isShadow,
            Texture2D paletteTexture)
            => SetEffectParams(RenderDependencies.PalettedColorDrawEffect, bottomDepth, topDepth,
                worldTextureCoordinates, spriteSizeToWorldSizeRatio, RenderDependencies.DepthRenderTarget, isShadow, paletteTexture, true);

        protected void SetEffectParams_RGBADraw(float bottomDepth, float topDepth,
            Vector2 worldTextureCoordinates, Vector2 spriteSizeToWorldSizeRatio, bool isShadow)
            => SetEffectParams(RenderDependencies.PalettedColorDrawEffect, bottomDepth, topDepth,
                worldTextureCoordinates, spriteSizeToWorldSizeRatio, RenderDependencies.DepthRenderTarget, isShadow, null, false);

        protected void SetEffectParams(Effect effect, float bottomDepth, float topDepth,
            Vector2 worldTextureCoordinates, Vector2 spriteSizeToWorldSizeRatio, Texture2D depthTexture, bool isShadow, Texture2D paletteTexture, bool usePalette)
        {
            effect.Parameters["SpriteDepthBottom"].SetValue(bottomDepth);
            effect.Parameters["SpriteDepthTop"].SetValue(topDepth);
            effect.Parameters["WorldTextureCoordinates"].SetValue(worldTextureCoordinates);
            effect.Parameters["SpriteSizeToWorldSizeRatio"].SetValue(spriteSizeToWorldSizeRatio);
            effect.Parameters["IsShadow"].SetValue(isShadow);
            RenderDependencies.GraphicsDevice.SamplerStates[1] = SamplerState.LinearClamp;

            if (depthTexture != null)
            {
                effect.Parameters["DepthTexture"].SetValue(depthTexture);
                RenderDependencies.GraphicsDevice.Textures[1] = depthTexture;
            }

            if (paletteTexture != null)
            {
                effect.Parameters["PaletteTexture"].SetValue(paletteTexture);
                RenderDependencies.GraphicsDevice.Textures[2] = paletteTexture;
            }

            effect.Parameters["UsePalette"].SetValue(usePalette);
            RenderDependencies.PalettedColorDrawEffect.Parameters["UseRemap"].SetValue(false); // Disable remap by default
        }

        protected virtual void DrawShadow(T gameObject, in CommonDrawParams drawParams, Point2D drawPoint, int heightOffset)
        {
            if (drawParams.ShapeImage == null)
                return;

            int shadowFrameIndex = gameObject.GetShadowFrameIndex(drawParams.ShapeImage.GetFrameCount());
            if (shadowFrameIndex > 0 && shadowFrameIndex < drawParams.ShapeImage.GetFrameCount())
            {
                DrawShapeImage(gameObject, drawParams, drawParams.ShapeImage, shadowFrameIndex,
                    new Color(0, 0, 0, 128), true, false, Color.White, drawPoint, heightOffset);
            }
        }

        protected void DrawShapeImage(T gameObject, in CommonDrawParams drawParams, ShapeImage image,
            int frameIndex, Color color, bool isShadow, bool drawRemap, Color remapColor, Point2D drawPoint, int heightOffset)
        {
            if (image == null)
                return;

            PositionedTexture frame = image.GetFrame(frameIndex);
            if (frame == null || frame.Texture == null)
                return;

            PositionedTexture remapFrame = null;
            if (drawRemap && Constants.HQRemap && image.HasRemapFrames())
                remapFrame = image.GetRemapFrame(frameIndex);

            Rectangle drawingBounds = GetTextureDrawCoords(gameObject, frame, drawPoint);

            RenderFrame(frame, remapFrame, color, drawRemap, remapColor, isShadow,
                drawingBounds.X, drawingBounds.Y, heightOffset, image.Palette?.Texture);
        }

        protected void DrawVoxelModel(T gameObject, in CommonDrawParams drawParams, VoxelModel model,
            byte facing, RampType ramp, Color color, bool drawRemap, Color remapColor, Point2D drawPoint, int heightOffset)
        {
            if (model == null)
                return;

            PositionedTexture frame = model.GetFrame(facing, ramp);
            if (frame == null || frame.Texture == null)
                return;

            PositionedTexture remapFrame = null;
            if (drawRemap && Constants.HQRemap)
                remapFrame = model.GetRemapFrame(facing, ramp);

            Rectangle drawingBounds = GetTextureDrawCoords(gameObject, frame, drawPoint);

            RenderFrame(frame, remapFrame, color, drawRemap, remapColor, false,
                drawingBounds.X, drawingBounds.Y, heightOffset, null);
        }

        private void RenderFrame(PositionedTexture frame, PositionedTexture remapFrame, Color color, bool drawRemap, Color remapColor,
            bool isShadow,
            int finalDrawPointX, int finalDrawPointY, int heightOffset, Texture2D paletteTexture)
        {
            Texture2D texture = frame.Texture;

            ApplyShaderEffectValues(texture, new Point2D(finalDrawPointX, finalDrawPointY), heightOffset, isShadow, paletteTexture);

            Renderer.DrawTexture(texture, 
                new Rectangle(finalDrawPointX, finalDrawPointY, texture.Width, texture.Height),
                null, color, 0f, Vector2.Zero, SpriteEffects.None, 0f);

            if (drawRemap && Constants.HQRemap && remapFrame != null)
            {
                RenderDependencies.PalettedColorDrawEffect.Parameters["UseRemap"].SetValue(true);

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

        private void ApplyShaderEffectValues(Texture2D texture, Point2D finalDrawPoint, int heightOffset, bool isShadow, Texture2D paletteTexture)
        {
            int depthOffset = Constants.CellSizeY;

            float depthTop = (finalDrawPoint.Y + heightOffset + depthOffset) / (float)Map.HeightInPixels;
            float depthBottom = (finalDrawPoint.Y + heightOffset + texture.Height + depthOffset) / (float)Map.HeightInPixels;
            depthTop = 1.0f - depthTop;
            depthBottom = 1.0f - depthBottom;

            Vector2 worldTextureCoordinates = new Vector2(finalDrawPoint.X / (float)Map.WidthInPixels, finalDrawPoint.Y / (float)Map.HeightInPixels);
            Vector2 spriteSizeToWorldSizeRatio = new Vector2(texture.Width / (float)Map.WidthInPixels, texture.Height / (float)Map.HeightInPixels);

            if (paletteTexture == null)
                SetEffectParams_RGBADraw(depthBottom, depthTop, worldTextureCoordinates, spriteSizeToWorldSizeRatio, isShadow);
            else
                SetEffectParams_PalettedDraw(depthBottom, depthTop, worldTextureCoordinates, spriteSizeToWorldSizeRatio, isShadow, paletteTexture);
        }

        protected void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1, float depth = 0f)
            => Renderer.DrawLine(start, end, color, thickness, depth);
    }
}
