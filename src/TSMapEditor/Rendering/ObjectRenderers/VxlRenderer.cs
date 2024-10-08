using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using static TSMapEditor.CCEngine.VxlFile;
using Color = Microsoft.Xna.Framework.Color;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public class VxlRenderer
    {
        private const float ModelScale = 0.025f;
        private static readonly Matrix Scale = Matrix.CreateScale(ModelScale, ModelScale, ModelScale);
        private static readonly Vector3 CameraPosition = new(0.0f, 0.0f, 20.0f);
        private static readonly Vector3 CameraTarget = Vector3.Zero;
        private static readonly Matrix View = Matrix.CreateLookAt(CameraPosition, CameraTarget, Vector3.Up);

        private const float NearClip = 0.01f; // the near clipping plane distance
        private const float FarClip = 100f; // the far clipping plane distance

        private static readonly Vector3 TSLight = -Vector3.UnitX;
        private static readonly Vector3 YRLight = Vector3.Transform(-Vector3.UnitX,
            Matrix.CreateRotationZ(MathHelper.ToRadians(45f)));

        private static readonly float SlopeAngle = MathHelper.ToRadians(27.5f);

        // Static table for creating triangles out of vertices
        private static readonly int[][] VertexIndexTriangles =
        {
            new [] { 0, 1, 2 }, new [] { 2, 3, 0 }, // up
            new [] { 7, 6, 5 }, new [] { 5, 4, 7 }, // down
            new [] { 4, 5, 1 }, new [] { 1, 0, 4 }, // forward
            new [] { 3, 2, 6 }, new [] { 6, 7, 3 }, // backward
            new [] { 1, 5, 6 }, new [] { 6, 2, 1 }, // right
            new [] { 4, 0, 3 }, new [] { 3, 7, 4 }, // left
        };

        public static (Texture2D texture, Point2D offset) Render(GraphicsDevice graphicsDevice, byte facing, RampType ramp, VxlFile vxl, HvaFile hva, Palette palette, VplFile vpl = null, bool forRemap = false)
        {
            if (vxl.Sections.Count > hva.Sections.Count)
                return (null, Point2D.Zero);

            /*********** Voxel space setup **********/

            float rotationFromFacing = 2 * (float)Math.PI * ((float)facing / Constants.FacingMax);

            Matrix tilt = Matrix.Identity;

            // Rotate the X axis to be parallel to the tilt of the slope, then rotate around this axis
            if (ramp is > RampType.None and < RampType.DoubleUpSWNE)
            {
                Matrix rotateTiltAxis = Matrix.CreateRotationZ(-MathHelper.ToRadians(SlopeAxisZAngles[(int)ramp - 1]));
                rotateTiltAxis *= Matrix.CreateRotationX(MathHelper.ToRadians(-60f));

                Vector3 tiltAxis = Vector3.Transform(Vector3.UnitX, rotateTiltAxis);
                tilt = Matrix.CreateFromAxisAngle(tiltAxis, SlopeAngle);
            }

            // Rotates to the game's north
            Matrix rotateToWorld = Matrix.CreateRotationZ(MathHelper.ToRadians(45f) - rotationFromFacing);
            rotateToWorld *= Matrix.CreateRotationX(MathHelper.ToRadians(-60f));
            Matrix world = rotateToWorld * tilt * Scale;

            var vertexData = new List<VertexPositionColor>();
            var vertexIndices = new List<int>();

            Rectangle imageBounds = new Rectangle();

            // Allocate memory for vertices to use in loop below
            var verticesArray = new VertexPositionColor[8];

            foreach (var section in vxl.Sections)
            {
                byte[] normalIndexToVplPage =
                    PreCalculateLighting(section.GetNormals(), section.NormalsMode, rotationFromFacing);

                var sectionHvaTransform = hva.LoadMatrix(section.Index);
                sectionHvaTransform.M41 *= section.HvaMatrixScale;
                sectionHvaTransform.M42 *= section.HvaMatrixScale;
                sectionHvaTransform.M43 *= section.HvaMatrixScale;

                var sectionTranslation = Matrix.CreateTranslation(section.MinBounds);
                var sectionScale = Matrix.CreateScale(section.Scale);

                // Move to the origin, transform however the .hva tells us, then scale
                var sectionTransform = sectionScale * sectionHvaTransform * sectionTranslation;

                for (int x = 0; x < section.SizeX; x++)
                {
                    for (int y = 0; y < section.SizeY; y++)
                    {
                        foreach (Voxel voxel in section.Spans[x, y].Voxels)
                        {
                            Vector3 position = new Vector3(voxel.X, voxel.Y, voxel.Z);
                            Vector3 transformedPosition = Vector3.Transform(position, sectionTransform);

                            byte colorIndex =
                                vpl?.GetPaletteIndex(normalIndexToVplPage[voxel.NormalIndex], voxel.ColorIndex) ??
                                voxel.ColorIndex;

                            // Don't draw first color in the palette.
                            if (colorIndex == 0)
                                continue;

                            // If we are drawing remap, draw all non-remap as magenta
                            Color color = forRemap && colorIndex is < 0x10 or > 0x1F
                                ? Color.Magenta
                                : palette.Data[colorIndex].ToXnaColor();

                            RenderVoxel(transformedPosition, color, vertexData.Count, vertexIndices, verticesArray);
                            vertexData.AddRange(verticesArray);
                        }
                    }
                }

                Rectangle sectionImageBounds = GetSectionBounds(section, world, sectionTransform);
                imageBounds = Rectangle.Union(imageBounds, sectionImageBounds);
            }

            /********** Rendering *********/

            // Hack to fix bounds, scale them up by 2
            imageBounds = new Rectangle(imageBounds.X - imageBounds.Width / 2, imageBounds.Y - imageBounds.Height / 2,
                imageBounds.Width * 2, imageBounds.Height * 2);

            // The model is actually empty, return null so we can draw replacement text
            if (vertexData.Count == 0)
                return (null, Point2D.Zero);

            var renderTarget = new RenderTarget2D(graphicsDevice, Convert.ToInt32(imageBounds.Width / ModelScale), Convert.ToInt32(imageBounds.Height / ModelScale), false, SurfaceFormat.Color, DepthFormat.Depth24);
            Renderer.PushRenderTarget(renderTarget);

            graphicsDevice.Clear(Color.Transparent);
            graphicsDevice.DepthStencilState = DepthStencilState.Default;

            Matrix projection = Matrix.CreateOrthographic(imageBounds.Width, imageBounds.Height, NearClip, FarClip);

            BasicEffect basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = View;
            basicEffect.Projection = projection;
            basicEffect.World = world;

            VertexBuffer vertexBuffer = new VertexBuffer(
                graphicsDevice,
                typeof(VertexPositionColor),
                vertexData.Count, 
                BufferUsage.None);
            vertexBuffer.SetData(vertexData.ToArray());

            IndexBuffer triangleListIndexBuffer = new IndexBuffer(
                graphicsDevice,
                IndexElementSize.ThirtyTwoBits,
                vertexIndices.Count,
                BufferUsage.None);
            triangleListIndexBuffer.SetData(vertexIndices.ToArray());

            graphicsDevice.Indices = triangleListIndexBuffer;
            graphicsDevice.SetVertexBuffer(vertexBuffer);

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexIndices.Count / 3);
            }

            // Crop the rendered voxel texture to save VRAM
            (Texture2D texture, Point2D offset) = Helpers.CropTextureToVisiblePortion(renderTarget, graphicsDevice);

            Renderer.PopRenderTarget();
            renderTarget.Dispose();
            basicEffect.Dispose();
            vertexBuffer.Dispose();
            triangleListIndexBuffer.Dispose();

            return (texture, offset);
        }

        private static readonly int[] SlopeAxisZAngles =
        {
            135, -135, -45, 45,
            180, -90, 0, 90,
            180, -90, 0, 90,
            180, -90, 0, 90,
            180, -90, 0, 90
        };

        private static void RenderVoxel(Vector3 position, Color color, int vertexIndexCount, List<int> vertexIndices, VertexPositionColor[] verticesArray)
        {
            const float radius = 0.5f;

            // Set up the coordinates of the voxel's corners
            Span<Vector3> vertexCoordinates = stackalloc Vector3[]
            {
                new(-1, 1, -1), // A1 // 0
                new(1, 1, -1), // B1 // 1           
                new(1, 1, 1), // C1 // 2
                new(-1, 1, 1), // D1 // 3

                new(-1, -1, -1), // A2 // 4
                new(1, -1, -1), // B2 // 5
                new(1, -1, 1), // C2 // 6
                new(-1, -1, 1) // D2 // 7
            };

            for (int i = 0; i < vertexCoordinates.Length; i++)
            {
                vertexCoordinates[i] *= radius;
                vertexCoordinates[i] += position;
            }

            // Set up the vertices themselves
            for (int i = 0; i < verticesArray.Length; i++)
                verticesArray[i] = new VertexPositionColor(vertexCoordinates[i], color);

            for (int i = 0; i < VertexIndexTriangles.Length; i++)
            {
                var triangle = VertexIndexTriangles[i];
                vertexIndices.Add(triangle[0] + vertexIndexCount);
                vertexIndices.Add(triangle[1] + vertexIndexCount);
                vertexIndices.Add(triangle[2] + vertexIndexCount);
            }
        }

        private static byte[] PreCalculateLighting(Vector3[] normalsTable, int normalsMode, float rotation)
        {
            Vector3 light = Constants.IsRA2YR ?
                Vector3.Transform(YRLight, Matrix.CreateRotationZ(rotation - MathHelper.ToRadians(45))) : 
                Vector3.Transform(TSLight, Matrix.CreateRotationZ(rotation - MathHelper.ToRadians(45)));

            // Center the lighting around this page to make vehicles darker in RA2
            const byte centerPage = 7;
            // Assume 8 pages per normals mode
            int maxPage = normalsMode * 8 - 1;

            byte[] normalIndexToVplPage = new byte[256];

            for (int i = 0; i < normalsTable.Length; i++)
            {
                float dot = (Vector3.Dot(normalsTable[i], light) + 1) / 2;

                byte page = dot <= 0.5 ?
                    Convert.ToByte(dot * 2 * centerPage) :
                    Convert.ToByte(2 * (maxPage - centerPage) * (dot - 0.5f) + centerPage);

                normalIndexToVplPage[i] = page;
            }

            normalIndexToVplPage[253] = 16;
            normalIndexToVplPage[254] = 16;
            normalIndexToVplPage[255] = 16;

            return normalIndexToVplPage;
        }

        private static Rectangle GetSectionBounds(Section section, Matrix worldTransform, Matrix sectionTransform)
        {
            worldTransform = sectionTransform * worldTransform;

            // floor rect of the bounding box
            Vector3 floorTopLeft = new Vector3(0, 0, 0);
            Vector3 floorTopRight = new Vector3(section.SpanX, 0, 0);
            Vector3 floorBottomRight = new Vector3(section.SpanX, section.SpanY, 0);
            Vector3 floorBottomLeft = new Vector3(0, section.SpanY, 0);

            // ceil rect of the bounding box
            Vector3 ceilTopLeft = new Vector3(0, 0, section.SpanZ);
            Vector3 ceilTopRight = new Vector3(section.SpanX, 0, section.SpanZ);
            Vector3 ceilBottomRight = new Vector3(section.SpanX, section.SpanY, section.SpanZ);
            Vector3 ceilBottomLeft = new Vector3(0, section.SpanY, section.SpanZ);

            // apply transformations
            floorTopLeft = Vector3.Transform(floorTopLeft, worldTransform);
            floorTopRight = Vector3.Transform(floorTopRight, worldTransform);
            floorBottomRight = Vector3.Transform(floorBottomRight, worldTransform);
            floorBottomLeft = Vector3.Transform(floorBottomLeft, worldTransform);

            ceilTopLeft = Vector3.Transform(ceilTopLeft, worldTransform);
            ceilTopRight = Vector3.Transform(ceilTopRight, worldTransform);
            ceilBottomRight = Vector3.Transform(ceilBottomRight, worldTransform);
            ceilBottomLeft = Vector3.Transform(ceilBottomLeft, worldTransform);

            int FminX = (int)Math.Floor(Math.Min(Math.Min(Math.Min(floorTopLeft.X, floorTopRight.X), floorBottomRight.X), floorBottomLeft.X));
            int FmaxX = (int)Math.Ceiling(Math.Max(Math.Max(Math.Max(floorTopLeft.X, floorTopRight.X), floorBottomRight.X), floorBottomLeft.X));
            int FminY = (int)Math.Floor(Math.Min(Math.Min(Math.Min(floorTopLeft.Y, floorTopRight.Y), floorBottomRight.Y), floorBottomLeft.Y));
            int FmaxY = (int)Math.Ceiling(Math.Max(Math.Max(Math.Max(floorTopLeft.Y, floorTopRight.Y), floorBottomRight.Y), floorBottomLeft.Y));

            int TminX = (int)Math.Floor(Math.Min(Math.Min(Math.Min(ceilTopLeft.X, ceilTopRight.X), ceilBottomRight.X), ceilBottomLeft.X));
            int TmaxX = (int)Math.Ceiling(Math.Max(Math.Max(Math.Max(ceilTopLeft.X, ceilTopRight.X), ceilBottomRight.X), ceilBottomLeft.X));
            int TminY = (int)Math.Floor(Math.Min(Math.Min(Math.Min(ceilTopLeft.Y, ceilTopRight.Y), ceilBottomRight.Y), ceilBottomLeft.Y));
            int TmaxY = (int)Math.Ceiling(Math.Max(Math.Max(Math.Max(ceilTopLeft.Y, ceilTopRight.Y), ceilBottomRight.Y), ceilBottomLeft.Y));

            int minX = Math.Min(FminX, TminX);
            int maxX = Math.Max(FmaxX, TmaxX);
            int minY = Math.Min(FminY, TminY);
            int maxY = Math.Max(FmaxY, TmaxY);

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }
    }
}