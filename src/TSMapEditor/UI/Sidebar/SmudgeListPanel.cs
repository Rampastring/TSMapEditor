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
    class SmudgeListPanel : XNAPanel, ISearchBoxContainer
    {
        public SmudgeListPanel(WindowManager windowManager, EditorState editorState,
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
        private PlaceSmudgeCursorAction smudgePlacementAction;
        private PlaceSmudgeCollectionCursorAction smudgeCollectionPlacementAction;


        public override void Initialize()
        {
            SearchBox = new XNASuggestionTextBox(WindowManager);
            SearchBox.Name = nameof(SearchBox);
            SearchBox.X = Constants.UIEmptySideSpace;
            SearchBox.Y = Constants.UIEmptyTopSpace;
            SearchBox.Width = Width - Constants.UIEmptySideSpace * 2;
            SearchBox.Height = Constants.UITextBoxHeight;
            SearchBox.Suggestion = "Search smudge... (CTRL + F)";
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

            ObjectTreeView.SelectedItemChanged += ObjectTreeView_SelectedItemChanged;
            smudgePlacementAction = new PlaceSmudgeCursorAction(cursorActionTarget);
            smudgeCollectionPlacementAction = new PlaceSmudgeCollectionCursorAction(cursorActionTarget);
            smudgeCollectionPlacementAction.ActionExited += (s, e) => ObjectTreeView.SelectedNode = null;
            smudgePlacementAction.ActionExited += (s, e) => ObjectTreeView.SelectedNode = null;

            InitSmudges();

            KeyboardCommands.Instance.NextSidebarNode.Triggered += NextSidebarNode_Triggered;
            KeyboardCommands.Instance.PreviousSidebarNode.Triggered += PreviousSidebarNode_Triggered;
        }

        private void ObjectTreeView_SelectedItemChanged(object sender, EventArgs e)
        {
            if (ObjectTreeView.SelectedNode == null)
                return;

            var tag = ObjectTreeView.SelectedNode.Tag;
            if (tag == null)
                return;

            if (tag is SmudgeCollection collection)
            {
                smudgeCollectionPlacementAction.SmudgeCollection = collection;
                EditorState.CursorAction = smudgeCollectionPlacementAction;
            }
            else if (tag is SmudgeType smudgeType)
            {
                smudgePlacementAction.SmudgeType = smudgeType;
                EditorState.CursorAction = smudgePlacementAction;
            }
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

        private void InitSmudges()
        {
            var categories = new List<TreeViewCategory>();

            categories.Add(new TreeViewCategory()
            {
                Text = "Erase Smudges",
                Tag = new object()
            });

            if (Map.EditorConfig.SmudgeCollections.Count > 0)
            {
                var collectionsCategory = new TreeViewCategory() { Text = "Collections" };
                categories.Add(collectionsCategory);

                foreach (var collection in Map.EditorConfig.SmudgeCollections)
                {
                    if (collection.Entries.Length == 0)
                        continue;

                    Texture2D texture = null;
                    var firstEntry = collection.Entries[0];
                    var textures = TheaterGraphics.SmudgeTextures[firstEntry.SmudgeType.Index];
                    if (textures != null)
                    {
                        var frames = textures.Frames;
                        int frameNumber = firstEntry.Frame;

                        if (frames != null && frames.Length > frameNumber)
                        {
                            var frame = frames[frameNumber];
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

            for (int i = 0; i < Map.Rules.SmudgeTypes.Count; i++)
            {
                TreeViewCategory category = null;
                SmudgeType smudgeType = Map.Rules.SmudgeTypes[i];

                if (string.IsNullOrEmpty(smudgeType.EditorCategory))
                {
                    category = FindOrMakeCategory("Uncategorized", categories);
                }
                else
                {
                    category = FindOrMakeCategory(smudgeType.EditorCategory, categories);
                }

                Texture2D texture = null;
                if (TheaterGraphics.SmudgeTextures[i] != null)
                {
                    var frames = TheaterGraphics.SmudgeTextures[i].Frames;
                    if (frames.Length > 0)
                    {
                        // Find the first valid frame and use that as our texture
                        int firstNotNullIndex = Array.FindIndex(frames, f => f != null);
                        if (firstNotNullIndex > -1)
                        {
                            texture = frames[firstNotNullIndex].Texture;
                        }
                    }
                }

                category.Nodes.Add(new TreeViewNode()
                {
                    Text = smudgeType.Name + " (" + smudgeType.ININame + ")",
                    Texture = texture,
                    Tag = smudgeType
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
