using Microsoft.Xna.Framework;
using System;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    internal class AircraftRenderer : ObjectRenderer<Aircraft>
    {
        public AircraftRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => Color.HotPink;

        protected override CommonDrawParams GetDrawParams(Aircraft gameObject)
        {
            return new CommonDrawParams()
            {
                IniName = gameObject.ObjectType.ININame,
                MainVoxel = TheaterGraphics.AircraftModels[gameObject.ObjectType.Index]
            };
        }

        protected override void Render(Aircraft gameObject, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            DrawVoxelModel(gameObject, drawParams, drawParams.MainVoxel,
                gameObject.Facing, RampType.None, Color.White, true, gameObject.GetRemapColor(),
                RenderDependencies.EditorState.IsLighting, drawPoint, heightOffset);
        }
    }
}
