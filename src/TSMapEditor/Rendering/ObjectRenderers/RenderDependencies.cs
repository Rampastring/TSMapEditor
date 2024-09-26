using Microsoft.Xna.Framework.Graphics;
using System;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public struct RenderDependencies
    {
        public readonly Map Map;
        public readonly TheaterGraphics TheaterGraphics;
        public readonly EditorState EditorState;
        public readonly GraphicsDevice GraphicsDevice;
        public readonly ObjectSpriteRecord ObjectSpriteRecord;
        public readonly Effect PalettedColorDrawEffect;
        public readonly Camera Camera;
        public readonly Func<int> GetCameraRightXCoord;
        public readonly Func<int> GetCameraBottomYCoord;


        public RenderDependencies(Map map, 
            TheaterGraphics theaterGraphics,
            EditorState editorState,
            GraphicsDevice graphicsDevice,
            ObjectSpriteRecord objectSpriteRecord,
            Effect palettedColorDrawEffect,
            Camera camera,
            Func<int> getCameraRightXCoord,
            Func<int> getCameraBottomYCoord)
        {
            Map = map;
            TheaterGraphics = theaterGraphics;
            EditorState = editorState;
            GraphicsDevice = graphicsDevice;
            ObjectSpriteRecord = objectSpriteRecord;
            PalettedColorDrawEffect = palettedColorDrawEffect;
            Camera = camera;
            GetCameraRightXCoord = getCameraRightXCoord;
            GetCameraBottomYCoord = getCameraBottomYCoord;
        }
    }
}
