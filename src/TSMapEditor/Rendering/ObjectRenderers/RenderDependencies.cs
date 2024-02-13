using Microsoft.Xna.Framework.Graphics;
using System;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public struct RenderDependencies
    {
        public Map Map;
        public TheaterGraphics TheaterGraphics;
        public EditorState EditorState;
        public GraphicsDevice GraphicsDevice;
        public Effect ColorDrawEffect;
        public Effect PalettedColorDrawEffect;
        public Camera Camera;
        public Func<int> GetCameraRightXCoord;
        public Func<int> GetCameraBottomYCoord;
        public RenderTarget2D DepthRenderTarget;


        public RenderDependencies(Map map, 
            TheaterGraphics theaterGraphics,
            EditorState editorState,
            GraphicsDevice graphicsDevice,
            Effect colorDrawEffect,
            Effect palettedColorDrawEffect,
            Camera camera,
            Func<int> getCameraRightXCoord,
            Func<int> getCameraBottomYCoord,
            RenderTarget2D depthRenderTarget)
        {
            Map = map;
            TheaterGraphics = theaterGraphics;
            EditorState = editorState;
            GraphicsDevice = graphicsDevice;
            ColorDrawEffect = colorDrawEffect;
            PalettedColorDrawEffect = palettedColorDrawEffect;
            Camera = camera;
            GetCameraRightXCoord = getCameraRightXCoord;
            GetCameraBottomYCoord = getCameraBottomYCoord;
            DepthRenderTarget = depthRenderTarget;
        }
    }
}
