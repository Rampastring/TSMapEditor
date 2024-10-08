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

        /// <summary>
        /// The entry point for rendering an object.
        ///
        /// Checks whether the object is within the visible screen space. If yes,
        /// draws the graphics of the object.
        /// </summary>
        /// <param name="gameObject">The game object to render.</param>
        /// <param name="checkInCamera">Whether the object's presence within the camera should be checked.</param>
        public void Draw(T gameObject, bool checkInCamera)
        {
            Point2D drawPoint = GetDrawPoint(gameObject);

            CommonDrawParams drawParams = GetDrawParams(gameObject);

            PositionedTexture frame = GetFrameTexture(gameObject, drawParams, RenderDependencies.EditorState.IsLighting);

            if (checkInCamera)
            {
                Rectangle drawingBounds = GetTextureDrawCoords(gameObject, frame, drawPoint);

                // If the object is not in view, skip
                if (!IsObjectInCamera(drawingBounds))
                    return;
            }

            if (frame == null && ShouldRenderReplacementText(gameObject))
            {
                DrawText(gameObject, false);
            }
            else
            {
                Render(gameObject, drawPoint, drawParams);   
            }
        }

        public bool IsWithinCamera(T gameObject)
        {
            Point2D drawPointWithoutCellHeight = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position, RenderDependencies.Map);

            var mapCell = RenderDependencies.Map.GetTile(gameObject.Position);
            int heightOffset = RenderDependencies.EditorState.Is2DMode ? 0 : mapCell.Level * Constants.CellHeight;
            Point2D drawPoint = new Point2D(drawPointWithoutCellHeight.X, drawPointWithoutCellHeight.Y - heightOffset);

            CommonDrawParams drawParams = GetDrawParams(gameObject);

            PositionedTexture frame = GetFrameTexture(gameObject, drawParams, RenderDependencies.EditorState.IsLighting);

            Rectangle drawingBounds = GetTextureDrawCoords(gameObject, frame, drawPoint);

            // If the object is not in view, skip
            if (!IsObjectInCamera(drawingBounds))
                return false;

            return true;
        }

        public virtual Point2D GetDrawPoint(T gameObject)
        {
            Point2D drawPointWithoutCellHeight = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position, RenderDependencies.Map);

            var mapCell = RenderDependencies.Map.GetTile(gameObject.Position);
            int heightOffset = RenderDependencies.EditorState.Is2DMode ? 0 : mapCell.Level * Constants.CellHeight;
            Point2D drawPoint = new Point2D(drawPointWithoutCellHeight.X, drawPointWithoutCellHeight.Y - heightOffset);

            return drawPoint;
        }

        public virtual void DrawNonRemap(T gameObject, Point2D drawPoint)
        {
            // Do nothing by default
        }

        public virtual void DrawRemap(T gameObject, Point2D drawPoint)
        {
            // Do nothing by default
        }

        /// <summary>
        /// Draws a textual representation of the object.
        /// 
        /// Usually used as a fallback rendering method for an object that has no loaded graphics.
        /// </summary>
        /// <param name="gameObject">The game object for which to render a textual representation.</param>
        /// <param name="checkInCamera">Whether the object's presence within the camera should be checked.</param>
        public void DrawText(T gameObject, bool checkInCamera)
        {
            if (ShouldRenderReplacementText(gameObject))
            {
                Point2D drawPointWithoutCellHeight = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position, RenderDependencies.Map);

                var mapCell = RenderDependencies.Map.GetTile(gameObject.Position);
                int heightOffset = RenderDependencies.EditorState.Is2DMode ? 0 : mapCell.Level * Constants.CellHeight;
                Point2D drawPoint = new Point2D(drawPointWithoutCellHeight.X, drawPointWithoutCellHeight.Y - heightOffset);

                if (checkInCamera)
                {
                    Rectangle drawingBounds = new Rectangle(drawPoint.X, drawPoint.Y, 1, 1);
                    if (!IsObjectInCamera(drawingBounds))
                        return;
                }

                // DrawObjectReplacementText(gameObject, gameObject.GetObjectType().ININame, drawPoint);
                RenderDependencies.ObjectSpriteRecord.AddTextEntry(new TextEntry(gameObject.GetObjectType().ININame, ReplacementColor, drawPoint));
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
        protected virtual void Render(T gameObject, Point2D drawPoint, in CommonDrawParams drawParams) { }

        /// <summary>
        /// Renders the replacement text of an object, displayed when no graphics for an object have been loaded.
        /// Override in derived classes to implement and customize the rendering process.
        /// </summary>
        /// <param name="gameObject">The game object for which to draw a replacement text.</param>
        /// <param name="text">The string to draw.</param>
        /// <param name="drawPoint">The draw point of the object, with cell height taken into account.</param>
        protected virtual void DrawObjectReplacementText(T gameObject, string text, Point2D drawPoint)
        {
            // If the object is a techno, draw an arrow that displays its facing
            if (gameObject.IsTechno())
            {
                var techno = gameObject as TechnoBase;
                DrawObjectFacingArrow(techno.Facing, drawPoint);
            }

            Renderer.DrawString(text, 1, drawPoint.ToXNAVector(), ReplacementColor, 1.0f);
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

        protected PositionedTexture GetFrameTexture(T gameObject, in CommonDrawParams drawParams, bool affectedByLighting)
        {
            if (drawParams.ShapeImage != null && drawParams.ShapeImage.GetFrameCount() > 0)
            {
                int frameIndex = gameObject.GetFrameIndex(drawParams.ShapeImage.GetFrameCount());

                if (frameIndex > -1 && frameIndex < drawParams.ShapeImage.GetFrameCount())
                    return drawParams.ShapeImage.GetFrame(frameIndex);
            }
            else if (drawParams.MainVoxel?.Frames != null && drawParams.MainVoxel.GetFrame(0, RampType.None, affectedByLighting) is var frameMain && frameMain != null)
            {
                return frameMain;
            }
            else if (drawParams.TurretVoxel?.Frames != null && drawParams.TurretVoxel.GetFrame(0, RampType.None, affectedByLighting) is var frameTur && frameTur != null)
            {
                return frameTur;
            }
            else if (drawParams.BarrelVoxel?.Frames != null && drawParams.BarrelVoxel.GetFrame(0, RampType.None, affectedByLighting) is var frameBarl && frameBarl != null)
            {
                return frameBarl;
            }

            return null;
        }

        protected Rectangle GetTextureDrawCoords(T gameObject,
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
                frame?.Texture.Width ?? 1, frame?.Texture.Height ?? 1);
        }

        public virtual void DrawShadow(T gameObject)
        {
            Point2D drawPoint = GetDrawPoint(gameObject);
            CommonDrawParams drawParams = GetDrawParams(gameObject);

            if (drawParams.ShapeImage == null)
                return;

            int shadowFrameIndex = gameObject.GetShadowFrameIndex(drawParams.ShapeImage.GetFrameCount());
            if (shadowFrameIndex < 0 && shadowFrameIndex >= drawParams.ShapeImage.GetFrameCount())
                return;

            var regularFrame = drawParams.ShapeImage.GetFrame(gameObject.GetFrameIndex(drawParams.ShapeImage.GetFrameCount()));

            PositionedTexture shadowFrame = drawParams.ShapeImage.GetFrame(shadowFrameIndex);

            if (shadowFrame == null)
                return;

            Texture2D texture = shadowFrame.Texture;

            Rectangle drawingBounds = GetTextureDrawCoords(gameObject, shadowFrame, drawPoint);

            float textureHeight = (regularFrame != null && regularFrame.Texture != null) ? (float)regularFrame.Texture.Height : shadowFrame.Texture.Height;

            float depth = GetDepthFromPosition(gameObject);
            // depth += GetDepthAddition(gameObject);
            depth += textureHeight / Map.HeightInPixelsWithCellHeight;

            RenderDependencies.ObjectSpriteRecord.AddGraphicsEntry(new ObjectSpriteEntry(null, texture, drawingBounds, new Color(255, 255, 255, 128), false, true, depth));
        }

        private float GetDepthFromPosition(T gameObject)
        {
            var southernmostCell = GetSouthernmostCell(gameObject);

            return CellMath.GetDepthForCell(southernmostCell.CoordsToPoint(), Map);
        }

        private MapTile GetSouthernmostCell(T gameObject)
        {
            MapTile tile;

            if (gameObject.WhatAmI() == RTTIType.Building)
            {
                var structure = gameObject as Structure;
                Point2D southernmostCellCoords = structure.GetSouthernmostFoundationCell();
                tile = Map.GetTile(southernmostCellCoords);
                if (tile == null)
                    tile = Map.GetTile(structure.Position);
            }
            else
            {
                tile = Map.GetTile(gameObject.Position);
            }

            return tile;
        }

        private int GetSouthernmostCellBottomPixelCoord(T gameObject)
        {
            var southernmostCell = GetSouthernmostCell(gameObject);
            int cellTopPoint = (Constants.IsFlatWorld || RenderDependencies.EditorState.Is2DMode) ?
                CellMath.CellTopLeftPointFromCellCoords(southernmostCell.CoordsToPoint(), Map).Y : 
                CellMath.CellTopLeftPointFromCellCoords_3D(southernmostCell.CoordsToPoint(), Map).Y;

            return cellTopPoint + Constants.CellSizeY;
        }

        protected virtual float GetDepthAddition(T gameObject) => 0.0f;

        protected virtual float GetObjectBottomCornerXPos(T gameObject, PositionedTexture texture) => 0.5f;

        protected void DrawShapeImage(T gameObject, ShapeImage image, int frameIndex, Color color,
            bool drawRemap, Color remapColor, bool affectedByLighting, bool affectedByAmbient, Point2D drawPoint,
            float depthAddition = 0f, float textureWidthCenterPoint = 0.5f)
        {
            if (image == null)
                return;

            PositionedTexture frame = image.GetFrame(frameIndex);
            if (frame == null || frame.Texture == null)
                return;

            PositionedTexture remapFrame = null;
            if (drawRemap && image.HasRemapFrames())
                remapFrame = image.GetRemapFrame(frameIndex);

            Rectangle drawingBounds = GetTextureDrawCoords(gameObject, frame, drawPoint);

            double extraLight = 0.0;
            switch (gameObject.WhatAmI())
            {
                case RTTIType.Unit:
                    extraLight = Map.Rules.ExtraUnitLight;
                    break;
                case RTTIType.Infantry:
                    extraLight = Map.Rules.ExtraInfantryLight;
                    break;
                case RTTIType.Aircraft:
                    extraLight = Map.Rules.ExtraAircraftLight;
                    break;
            }

            Vector4 lighting = Vector4.One;
            var mapCell = Map.GetTile(gameObject.Position);

            if (RenderDependencies.EditorState.IsLighting && mapCell != null)
            {
                if (affectedByLighting && image.SubjectToLighting)
                {
                    lighting = mapCell.CellLighting.ToXNAVector4(extraLight);
                    remapColor = ScaleColorToAmbient(remapColor, mapCell.CellLighting);
                }
                else if (affectedByAmbient)
                {
                    lighting = mapCell.CellLighting.ToXNAVector4Ambient(extraLight);
                    remapColor = ScaleColorToAmbient(remapColor, mapCell.CellLighting);
                }
            }

            depthAddition += GetDepthFromPosition(gameObject);
            depthAddition += GetDepthAddition(gameObject);

            RenderFrame(gameObject, frame, remapFrame, color, drawRemap, remapColor,
                drawingBounds, image.GetPaletteTexture(), lighting, depthAddition, textureWidthCenterPoint, true);
        }

        protected void DrawVoxelModel(T gameObject, VoxelModel model, byte facing, RampType ramp,
            Color color, bool drawRemap, Color remapColor, bool affectedByLighting, Point2D drawPoint, 
            float depthAddition, bool compensateForBottomGap)
        {
            if (model == null)
                return;

            PositionedTexture frame = model.GetFrame(facing, ramp, false);
            if (frame == null || frame.Texture == null)
                return;

            PositionedTexture remapFrame = null;
            if (drawRemap)
                remapFrame = model.GetRemapFrame(facing, ramp, false);

            double extraLight = 0.0;
            switch (gameObject.WhatAmI())
            {
                case RTTIType.Unit:
                    extraLight = Map.Rules.ExtraUnitLight;
                    break;
                case RTTIType.Aircraft:
                    extraLight = Map.Rules.ExtraAircraftLight;
                    break;
            }

            Vector4 lighting = Vector4.One;
            var mapCell = Map.GetTile(gameObject.Position);

            if (RenderDependencies.EditorState.IsLighting && mapCell != null)
            {
                if (affectedByLighting && Constants.VoxelsAffectedByLighting)
                {
                    lighting = mapCell.CellLighting.ToXNAVector4(extraLight);
                }
                else
                {
                    lighting = mapCell.CellLighting.ToXNAVector4Ambient(extraLight);
                }
            }

            depthAddition += GetDepthFromPosition(gameObject);
            depthAddition += GetDepthAddition(gameObject);

            remapColor = ScaleColorToAmbient(remapColor, mapCell.CellLighting);

            Rectangle drawingBounds = GetTextureDrawCoords(gameObject, frame, drawPoint);

            RenderFrame(gameObject, frame, remapFrame, color, drawRemap, remapColor,
                drawingBounds, null, lighting, depthAddition, 0.5f, compensateForBottomGap);
        }

        private void RenderFrame(T gameObject, PositionedTexture frame, PositionedTexture remapFrame, Color color, bool drawRemap, Color remapColor,
            Rectangle drawingBounds, Texture2D paletteTexture, Vector4 lightingColor, float depthAddition, float textureWidthCenterPoint,
            bool compensateForBottomGap)
        {
            Texture2D texture = frame.Texture;

            if (depthAddition > 1.0f)
                depthAddition = 1.0f;

            // Add extra depth so objects show above terrain despite float imprecision
            depthAddition += Constants.DepthEpsilon;

            // There can be a gap between the end of the texture and the bottom-most cell for objects,
            // with more "circular" graphics. This can reduce depth when it's calculated in the shader.
            // Compensate for the effect here as necessary.
            if (compensateForBottomGap)
            {
                int southermostCellBottomPixelCoord = GetSouthernmostCellBottomPixelCoord(gameObject);
                if (drawingBounds.Bottom < southermostCellBottomPixelCoord)
                {
                    depthAddition += ((southermostCellBottomPixelCoord - drawingBounds.Bottom) / (float)Map.HeightInPixelsWithCellHeight) * Constants.DownwardsDepthRenderSpace;
                }
            }

            color = new Color((color.R / 255.0f) * lightingColor.X / 2f,
                (color.B / 255.0f) * lightingColor.Y / 2f,
                (color.B / 255.0f) * lightingColor.Z / 2f, textureWidthCenterPoint);

            RenderDependencies.ObjectSpriteRecord.AddGraphicsEntry(new ObjectSpriteEntry(paletteTexture, texture, drawingBounds, color, false, false, depthAddition));

            if (drawRemap && remapFrame != null)
            {
                remapColor = new Color(
                    (remapColor.R / 255.0f),
                    (remapColor.G / 255.0f),
                    (remapColor.B / 255.0f),
                    textureWidthCenterPoint);

                // RenderDependencies.PalettedColorDrawEffect.Parameters["UseRemap"].SetValue(true);
                RenderDependencies.ObjectSpriteRecord.AddGraphicsEntry(new ObjectSpriteEntry(paletteTexture, remapFrame.Texture, drawingBounds, remapColor, true, false, depthAddition));
            }
        }

        protected void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1, float depth = 0f)
            => Renderer.DrawLine(start, end, color, thickness, depth);

        protected Color ScaleColorToAmbient(Color color, MapColor mapColor)
        {
            double highestComponent = Math.Max(mapColor.R, Math.Max(mapColor.G, mapColor.B));

            return new Color((int)(color.R * highestComponent),
                (int)(color.G * highestComponent),
                (int)(color.B * highestComponent),
                color.A);
        }
    }
}
