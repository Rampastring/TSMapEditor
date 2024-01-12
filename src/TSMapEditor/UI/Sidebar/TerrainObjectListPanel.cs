using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.UI.Sidebar
{
    public class TerrainObjectListPanel : XNAPanel, ISearchBoxContainer
    {
        public TerrainObjectListPanel(WindowManager windowManager, EditorState editorState,
            Map map, TheaterGraphics theaterGraphics, ICursorActionTarget cursorActionTarget) : base(windowManager)
        {
            EditorState = editorState;
            Map = map;
            TheaterGraphics = theaterGraphics;
            this.cursorActionTarget = cursorActionTarget;
        }

        protected EditorState EditorState { get; }
        protected Map Map { get; }
        protected TheaterGraphics TheaterGraphics { get; }

        public XNASuggestionTextBox SearchBox { get; private set; }
        public TreeView ObjectTreeView { get; private set; }

        private readonly ICursorActionTarget cursorActionTarget;

        private TerrainObjectPlacementAction terrainObjectPlacementAction;

        private TerrainObjectCollectionPlacementAction terrainObjectCollectionPlacementAction;

        public override void Initialize()
        {
            SearchBox = new XNASuggestionTextBox(WindowManager);
            SearchBox.Name = nameof(SearchBox);
            SearchBox.X = Constants.UIEmptySideSpace;
            SearchBox.Y = Constants.UIEmptyTopSpace;
            SearchBox.Width = Width - Constants.UIEmptySideSpace * 2;
            SearchBox.Height = Constants.UITextBoxHeight;
            SearchBox.Suggestion = "Search object... (CTRL + F)";
            AddChild(SearchBox);
            SearchBox.TextChanged += SearchBox_TextChanged;
            SearchBox.EnterPressed += SearchBox_EnterPressed;
            UIHelpers.AddSearchTipsBoxToControl(SearchBox);

            ObjectTreeView = new TreeView(WindowManager);
            ObjectTreeView.Name = nameof(ObjectTreeView);
            ObjectTreeView.Y = SearchBox.Bottom + Constants.UIVerticalSpacing;
            ObjectTreeView.Height = Height - ObjectTreeView.Y;
            ObjectTreeView.Width = Width;
            AddChild(ObjectTreeView);
            ObjectTreeView.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 222), 2, 2);

            base.Initialize();

            terrainObjectPlacementAction = new TerrainObjectPlacementAction(cursorActionTarget);
            terrainObjectCollectionPlacementAction = new TerrainObjectCollectionPlacementAction(cursorActionTarget);
            ObjectTreeView.SelectedItemChanged += ObjectTreeView_SelectedItemChanged;
            terrainObjectCollectionPlacementAction.ActionExited += (s, e) => ObjectTreeView.SelectedNode = null;
            terrainObjectPlacementAction.ActionExited += (s, e) => ObjectTreeView.SelectedNode = null;

            InitTerrainObjects();

            KeyboardCommands.Instance.NextSidebarNode.Triggered += NextSidebarNode_Triggered;
            KeyboardCommands.Instance.PreviousSidebarNode.Triggered += PreviousSidebarNode_Triggered;
        }

        public override void RefreshSize()
        {
            Width = Parent.Width;
            SearchBox.Width = Width - Constants.UIEmptySideSpace * 2;
            ObjectTreeView.Width = Width;
            Height = Parent.Height - Y;
            ObjectTreeView.Height = Height - ObjectTreeView.Y;
        }

        private void NextSidebarNode_Triggered(object sender, EventArgs e)
        {
            if (Enabled)
                ObjectTreeView.SelectNextNode();
        }

        private void PreviousSidebarNode_Triggered(object sender, EventArgs e)
        {
            if (Enabled)
                ObjectTreeView.SelectPreviousNode();
        }

        private void ObjectTreeView_SelectedItemChanged(object sender, EventArgs e)
        {
            if (ObjectTreeView.SelectedNode == null)
                return;

            var tag = ObjectTreeView.SelectedNode.Tag;
            if (tag == null)
                return;

            if (tag is TerrainObjectCollection collection)
            {
                terrainObjectCollectionPlacementAction.TerrainObjectCollection = collection;
                EditorState.CursorAction = terrainObjectCollectionPlacementAction;
            }
            else if (tag is TerrainType terrainType)
            {
                terrainObjectPlacementAction.TerrainType = terrainType;
                EditorState.CursorAction = terrainObjectPlacementAction;
            }
        }

        private void SearchBox_EnterPressed(object sender, EventArgs e)
        {
            ObjectTreeView.FindNode(SearchBox.Text, true);
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text) || SearchBox.Text == SearchBox.Suggestion)
                return;

            ObjectTreeView.FindNode(SearchBox.Text, false);
        }

        private void InitTerrainObjects()
        {
            var categories = new List<TreeViewCategory>();

            if (Map.EditorConfig.TerrainObjectCollections.Count > 0)
            {
                var collectionsCategory = new TreeViewCategory() { Text = "Collections" };
                categories.Add(collectionsCategory);

                foreach (var collection in Map.EditorConfig.TerrainObjectCollections)
                {
                    if (collection.Entries.Length == 0)
                        continue;

                    Texture2D texture = null;
                    var firstEntry = collection.Entries[0];
                    var textures = TheaterGraphics.TerrainObjectTextures[firstEntry.TerrainType.Index];
                    if (textures != null)
                    {
                        int frameCount = textures.GetFrameCount();
                        const int frameNumber = 0;

                        if (frameCount > frameNumber)
                        {
                            var frame = textures.GetFrame(frameNumber);
                            if (frame != null)
                                texture = frame.Texture;
                        }
                    }

                    collectionsCategory.Nodes.Add(new TreeViewNode()
                    {
                        Text = collection.Name,
                        Tag = collection,
                        Texture = texture
                    });
                }
            }

            for (int i = 0; i < Map.Rules.TerrainTypes.Count; i++)
            {
                TreeViewCategory category = null;
                TerrainType terrainType = Map.Rules.TerrainTypes[i];

                if (!terrainType.EditorVisible)
                    continue;

                if (string.IsNullOrEmpty(terrainType.EditorCategory))
                {
                    category = FindOrMakeCategory("Uncategorized", categories);
                }
                else
                {
                    category = FindOrMakeCategory(terrainType.EditorCategory, categories);
                }

                Texture2D texture = null;
                var terrainObjectGraphics = TheaterGraphics.TerrainObjectTextures[i];
                if (terrainObjectGraphics != null)
                {
                    int frameCount = terrainObjectGraphics.GetFrameCount();

                    // Find the first valid frame and use that as our texture
                    for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                    {
                        var frame = terrainObjectGraphics.GetFrame(frameIndex);
                        if (frame != null)
                        {
                            texture = frame.Texture;
                            break;
                        }
                    }
                }

                category.Nodes.Add(new TreeViewNode()
                {
                    Text = terrainType.GetEditorDisplayName() + " (" + terrainType.ININame + ")",
                    Texture = texture,
                    Tag = terrainType
                });

                category.Nodes = category.Nodes.OrderBy(n => n.Text).ToList();
            }

            categories.ForEach(c => ObjectTreeView.AddCategory(c));
        }

        private TreeViewCategory FindOrMakeCategory(string categoryName, List<TreeViewCategory> categoryList)
        {
            var category = categoryList.Find(c => c.Text == categoryName);
            if (category != null)
                return category;

            category = new TreeViewCategory() { Text = categoryName };
            categoryList.Add(category);
            return category;
        }
    }
}
