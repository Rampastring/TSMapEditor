using System;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.UI.Sidebar
{
    /// <summary>
    /// A sidebar panel for listing aircraft.
    /// </summary>
    public class AircraftListPanel : ObjectListPanel
    {
        public AircraftListPanel(WindowManager windowManager, EditorState editorState,
            Map map, TheaterGraphics theaterGraphics, ICursorActionTarget cursorActionTarget)
            : base(windowManager, editorState, map, theaterGraphics)
        {
            aircraftPlacementAction = new AircraftPlacementAction(cursorActionTarget, Keyboard);
            aircraftPlacementAction.ActionExited += AircraftPlacementAction_ActionExited;
        }

        private void AircraftPlacementAction_ActionExited(object sender, EventArgs e)
        {
            ObjectTreeView.SelectedNode = null;
        }

        private readonly AircraftPlacementAction aircraftPlacementAction;

        private RenderTarget2D renderTarget;

        protected override (Texture2D regular, Texture2D remap) GetObjectTextures<T>(T objectType, ShapeImage[] textures)
        {
            const byte facingSouthEast = 64;
            return GetTextureForVoxel(objectType, TheaterGraphics.AircraftModels, renderTarget, facingSouthEast);
        }

        protected override void InitObjects()
        {
            renderTarget = new RenderTarget2D(GraphicsDevice, ObjectTreeView.Width, ObjectTreeView.LineHeight, false, SurfaceFormat.Color, DepthFormat.None);
            InitObjectsBase(Map.Rules.AircraftTypes, null);
            renderTarget.Dispose();
        }

        protected override void ObjectSelected()
        {
            if (ObjectTreeView.SelectedNode == null)
                return;

            aircraftPlacementAction.AircraftType = (AircraftType)ObjectTreeView.SelectedNode.Tag;
            EditorState.CursorAction = aircraftPlacementAction;
        }
    }
}
