using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.UI.Sidebar
{
    public class BuildingListPanel : ObjectListPanel
    {
        public BuildingListPanel(WindowManager windowManager, EditorState editorState,
            Map map, TheaterGraphics theaterGraphics, ICursorActionTarget cursorActionTarget) : 
            base(windowManager, editorState, map, theaterGraphics)
        {
            buildingPlacementAction = new BuildingPlacementAction(cursorActionTarget, Keyboard);
            buildingPlacementAction.ActionExited += BuildingPlacementAction_ActionExited;
        }

        private void BuildingPlacementAction_ActionExited(object sender, System.EventArgs e)
        {
            ObjectTreeView.SelectedNode = null;
        }

        private readonly BuildingPlacementAction buildingPlacementAction;

        protected override (Texture2D regular, Texture2D remap) GetObjectTextures<T>(T objectType, ShapeImage[] textures)
        {
            var buildingType = objectType as BuildingType;

            Texture2D bibRegular = null;
            Texture2D bibRemap = null;
            PositionedTexture bibPosition = null;

            int x = int.MaxValue;
            int y = int.MaxValue;
            int width = 0;
            int height = 0;

            var bibGraphics = TheaterGraphics.BuildingBibTextures[buildingType.Index];
            if (bibGraphics != null)
            {
                var frame = bibGraphics.GetFrame(0);
                if (frame != null)
                {
                    bibRegular = bibGraphics.GetTextureForFrame_RGBA(0);
                    bibRemap = bibGraphics.GetRemapTextureForFrame_RGBA(0);
                    bibPosition = frame;

                    x = frame.OffsetX - frame.ShapeWidth / 2;
                    y = frame.OffsetY - frame.ShapeHeight / 2;
                    width = bibRegular.Width;
                    height = bibRegular.Height;
                }
            }

            Texture2D buildingRegular = null;
            Texture2D buildingRemap = null;
            PositionedTexture buildingPosition = null;

            if (textures != null)
            {
                if (textures[objectType.Index] != null)
                {
                    int frameCount = textures[objectType.Index].GetFrameCount();

                    // Find the first valid frame and use its RGBA variant as our texture
                    for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                    {
                        var frame = textures[objectType.Index].GetFrame(frameIndex);
                        if (frame != null)
                        {
                            buildingRegular = textures[objectType.Index].GetTextureForFrame_RGBA(frameIndex);
                            buildingPosition = frame;

                            if (objectType.GetArtConfig().Remapable && textures[objectType.Index].HasRemapFrames())
                            {
                                buildingRemap = textures[objectType.Index].GetRemapTextureForFrame_RGBA(frameIndex);
                            }

                            int buildingX = frame.OffsetX - (frame.ShapeWidth / 2);
                            int buildingY = frame.OffsetY - (frame.ShapeHeight / 2);

                            if (x > buildingX)
                                x = buildingX;

                            if (y > buildingY)
                                y = buildingY;

                            if (width < buildingRegular.Width)
                                width = buildingRegular.Width;

                            if (height < buildingRegular.Height)
                                height = buildingRegular.Height;

                            break;
                        }
                    }
                }
            }

            if (width == 0 && height == 0)
                return (null, null);

            var renderTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            var remapRenderTarget = new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            Renderer.BeginDraw();

            Renderer.PushRenderTarget(renderTarget);
            GraphicsDevice.Clear(Color.Transparent);

            if (bibRegular != null)
            {
                Renderer.DrawTexture(bibRegular,
                    new Rectangle(bibPosition.OffsetX - x - bibPosition.ShapeWidth / 2,
                    bibPosition.OffsetY - y - bibPosition.ShapeHeight / 2,
                    bibRegular.Width,
                    bibRegular.Height),
                    Color.White);
            }


            if (buildingRegular != null)
            {
                Renderer.DrawTexture(buildingRegular, 
                    new Rectangle(buildingPosition.OffsetX - x - buildingPosition.ShapeWidth / 2,
                    buildingPosition.OffsetY - y - buildingPosition.ShapeHeight / 2,
                    buildingRegular.Width,
                    buildingRegular.Height),
                    Color.White);
            }
                
            Renderer.PopRenderTarget();

            Renderer.PushRenderTarget(remapRenderTarget);
            GraphicsDevice.Clear(Color.Transparent);

            if (bibRemap != null)
            {
                Renderer.DrawTexture(bibRemap,
                    new Rectangle(bibPosition.OffsetX - x - bibPosition.ShapeWidth / 2,
                    bibPosition.OffsetY - y - bibPosition.ShapeHeight / 2,
                    bibRegular.Width,
                    bibRegular.Height),
                    Color.White);
            }
            
            if (buildingRemap != null)
            {
                Renderer.DrawTexture(buildingRemap,
                    new Rectangle(buildingPosition.OffsetX - x - buildingPosition.ShapeWidth / 2,
                    buildingPosition.OffsetY - y - buildingPosition.ShapeHeight / 2,
                    buildingRegular.Width,
                    buildingRegular.Height),
                    Color.White);
            }

            Renderer.PopRenderTarget();

            Renderer.EndDraw();

            var finalRenderTarget = new RenderTarget2D(GraphicsDevice, ObjectTreeView.Width, ObjectTreeView.LineHeight, false, SurfaceFormat.Color, DepthFormat.None);
            Texture2D regularFinal = Helpers.RenderTextureAsSmaller(renderTarget, finalRenderTarget, GraphicsDevice);
            Texture2D remapFinal = Helpers.RenderTextureAsSmaller(remapRenderTarget, finalRenderTarget, GraphicsDevice);

            bibRegular?.Dispose();
            bibRemap?.Dispose();
            buildingRegular?.Dispose();
            buildingRemap?.Dispose();

            renderTarget.Dispose();
            remapRenderTarget.Dispose();
            finalRenderTarget.Dispose();

            return (regularFinal, remapFinal);
        }

        protected override void InitObjects()
        {
            InitObjectsBase(Map.Rules.BuildingTypes, TheaterGraphics.BuildingTextures);
        }

        protected override void ObjectSelected()
        {
            if (ObjectTreeView.SelectedNode == null)
                return;

            buildingPlacementAction.BuildingType = (BuildingType)ObjectTreeView.SelectedNode.Tag;
            EditorState.CursorAction = buildingPlacementAction;
        }
    }
}
