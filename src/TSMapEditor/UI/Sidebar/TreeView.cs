using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;

namespace TSMapEditor.UI.Sidebar
{
    /// <summary>
    /// A category in a tree view.
    /// </summary>
    public class TreeViewCategory : TreeViewNode
    {
        public List<TreeViewNode> Nodes { get; set; } = new List<TreeViewNode>();
        public bool IsOpened { get; set; }
    }

    /// <summary>
    /// A node in a tree view.
    /// </summary>
    public class TreeViewNode
    {
        public Texture2D Texture { get; set; }
        public Texture2D RemapTexture { get; set; }
        public Color RemapColor { get; set; }
        public string Text { get; set; }
        public object Tag { get; set; }
    }

    public class TreeView : EditorPanel
    {
        private const int MARGIN = Constants.UIEmptyTopSpace;
        private const int NODE_INDENTATION = 20;

        public TreeView(WindowManager windowManager) : base(windowManager)
        {
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
            ScrollBar = new XNAScrollBar(WindowManager);
            ScrollBar.Name = "TreeViewScrollBar";
            ScrollBar.ScrollStep = LineHeight;
        }

        public event EventHandler SelectedItemChanged;
        public event EventHandler HoveredItemChanged;

        protected XNAScrollBar ScrollBar;

        private int _lineHeight = Constants.UITreeViewLineHeight;

        /// <summary>
        /// Gets or sets the height of a single item text in the tree view.
        /// </summary>
        public int LineHeight
        {
            get => _lineHeight;
            set { _lineHeight = value; ScrollBar.ScrollStep = value; }
        }

        public int CategoryMargin { get; set; } = 5;

        private int CategoryHeight => LineHeight + CategoryMargin * 2;

        /// <summary>
        /// Gets or sets the distance between the text of a tree view item
        /// and the panel border in pixels.
        /// </summary>
        public int TextBorderDistance { get; set; } = 3;

        private int _viewTop;

        public int ViewTop
        {
            get => _viewTop;
            set
            {
                if (value != _viewTop)
                {
                    _viewTop = value;
                    //TopIndexChanged?.Invoke(this, EventArgs.Empty);
                    ScrollBar.RefreshButtonY(_viewTop);
                }
            }
        }

        private TreeViewNode _selectedNode;
        public TreeViewNode SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != value)
                {
                    _selectedNode = value;
                    SelectedItemChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public TreeViewNode HoveredNode { get; set; }

        public List<TreeViewCategory> Categories { get; set; } = new List<TreeViewCategory>();

        public void AddCategory(TreeViewCategory category)
        {
            Categories.Add(category);
        }

        public void SelectNextNode()
        {
            if (Categories.Count == 0)
                return;

            if (SelectedNode == null)
                return;

            bool selectedNodeFound = false;

            foreach (var category in Categories)
            {
                for (int i = 0; i < category.Nodes.Count; i++)
                {
                    if (category.Nodes[i] == SelectedNode)
                    {
                        selectedNodeFound = true;

                        if (category.Nodes.Count > i + 1)
                        {
                            SelectedNode = category.Nodes[i + 1];
                            break;
                        }
                    }
                }

                if (selectedNodeFound)
                    break;
            }
        }

        public void SelectPreviousNode()
        {
            if (Categories.Count == 0)
                return;

            if (SelectedNode == null)
                return;

            bool selectedNodeFound = false;

            foreach (var category in Categories)
            {
                for (int i = 0; i < category.Nodes.Count; i++)
                {
                    if (category.Nodes[i] == SelectedNode)
                    {
                        selectedNodeFound = true;

                        if (i > 0)
                        {
                            SelectedNode = category.Nodes[i - 1];
                            break;
                        }
                    }
                }

                if (selectedNodeFound)
                    break;
            }
        }

        /// <summary>
        /// Refreshes the scroll bar's status. Call when list box items are modified.
        /// </summary>
        public void RefreshScrollbar()
        {
            ScrollBar.Length = GetTotalContentHeight();
            ScrollBar.DisplayedPixelCount = Height - MARGIN * 2;
            ScrollBar.Refresh();
        }

        /// <summary>
        /// Finds a node from the tree view with the specified text.
        /// </summary>
        /// <param name="text">The search term.</param>
        /// <param name="findNext">Whether the find the next matching node that follows the currently selected node.
        /// If set to false, then simply finds the first matching node.</param>
        public void FindNode(string text, bool findNext)
        {
            TreeViewNode foundNode = null;
            int accumulatedHeight = 0;
            bool selectedFound = false;

            foreach (var category in Categories)
            {
                accumulatedHeight += CategoryHeight;

                for (int i = 0; i < category.Nodes.Count; i++)
                {
                    var currentNode = category.Nodes[i];

                    if (currentNode.Text.ToUpperInvariant().Contains(text.ToUpperInvariant()))
                    {
                        if (findNext && currentNode == SelectedNode)
                        {
                            selectedFound = true;
                        }
                        else if (!findNext || (findNext && selectedFound))
                        {
                            foundNode = currentNode;
                            accumulatedHeight += i * LineHeight;
                            category.IsOpened = true;
                            break;
                        }
                    }
                }

                if (foundNode != null)
                    break;

                if (category.IsOpened)
                    accumulatedHeight += category.Nodes.Count * LineHeight;
            }

            if (foundNode != null)
            {
                SelectedNode = foundNode;

                // Scroll the view to the found item
                int totalHeight = GetTotalContentHeight();
                if (accumulatedHeight + Height > totalHeight)
                    accumulatedHeight = totalHeight - Height;

                RefreshScrollbar();
                ViewTop = accumulatedHeight;
            }
        }

        private int GetTotalContentHeight()
        {
            int height = 0;

            for (int i = 0; i < Categories.Count; i++)
            {
                height += CategoryMargin * 2 + LineHeight;

                if (Categories[i].IsOpened)
                {
                    height += Categories[i].Nodes.Count * LineHeight;
                }
            }

            return height;
        }

        /// <summary>
        /// Checks whether the tree view is scrolled so that
        /// the last item in the view is entirely visible.
        /// </summary>
        public bool IsScrolledToBottom()
        {
            return ViewTop + Height >= GetTotalContentHeight();
        }

        public void ScrollToBottom()
        {
            if (GetTotalContentHeight() < Height)
            {
                ViewTop = 0;
                return;
            }

            ViewTop = GetTotalContentHeight() - Height + MARGIN * 2;
        }

        public override void Initialize()
        {
            base.Initialize();

            ScrollBar.ClientRectangle = new Rectangle(Width - ScrollBar.ScrollWidth - 1,
                1, ScrollBar.ScrollWidth, Height - 2);
            ScrollBar.Scrolled += ScrollBar_Scrolled;
            AddChild(ScrollBar);
            ScrollBar.Refresh();

            ClientRectangleUpdated += TreeView_ClientRectangleUpdated;
        }

        private void TreeView_ClientRectangleUpdated(object sender, EventArgs e)
        {
            if (ScrollBar != null)
            {
                ScrollBar.ClientRectangle = new Rectangle(Width - ScrollBar.ScrollWidth - 1,
                    1, ScrollBar.ScrollWidth, Height - 2);
                RefreshScrollbar();
            }
        }

        private void ScrollBar_Scrolled(object sender, EventArgs e)
        {
            ViewTop = ScrollBar.ViewTop;
        }

        /// <summary>
        /// Handles input from a scroll wheel.
        /// </summary>
        public override void OnMouseScrolled()
        {
            if (GetTotalContentHeight() < Height)
            {
                ViewTop = 0;
                return;
            }

            ViewTop -= Cursor.ScrollWheelValue * ScrollBar.ScrollStep;

            if (ViewTop < 0)
            {
                ViewTop = 0;
                return;
            }

            if (IsScrolledToBottom())
            {
                // Show as many items above the last item as possible
                ScrollToBottom();
            }

            base.OnMouseScrolled();
        }

        /// <summary>
        /// Updates the hovered item index while the cursor is on this control.
        /// </summary>
        public override void OnMouseOnControl()
        {
            HoveredNode = GetItemOnCursor(GetCursorPoint());

            base.OnMouseOnControl();   
        }

        public override void OnMouseLeave()
        {
            HoveredNode = null;

            base.OnMouseLeave();
        }

        public override void OnMouseLeftDown()
        {
            var node = GetItemOnCursor(GetCursorPoint());

            if (node == null)
                return;

            var treeViewCategory = node as TreeViewCategory;
            if (treeViewCategory != null)
            {
                if (treeViewCategory.Nodes.Count == 0)
                {
                    SelectedNode = node;
                }
                else
                {
                    treeViewCategory.IsOpened = !treeViewCategory.IsOpened;
                    RefreshScrollbar();
                }
            }
            else
            {
                SelectedNode = node;
            }

            base.OnMouseLeftDown();
        }

        private TreeViewNode GetItemOnCursor(Point mouseLocation)
        {
            if (mouseLocation.X < 0)
                return null;

            if (ScrollBar.Visible)
            {
                if (mouseLocation.X > Width - ScrollBar.ScrollWidth)
                    return null;
            }
            else if (mouseLocation.X > Width)
                return null;

            if (mouseLocation.Y > Height || mouseLocation.Y < MARGIN)
                return null;

            int height = MARGIN;
            int mouseY = mouseLocation.Y + ViewTop;

            for (int i = 0; i < Categories.Count; i++)
            {
                height += CategoryHeight;
                if (height > mouseY)
                    return Categories[i];

                if (Categories[i].IsOpened)
                {
                    for (int j = 0; j < Categories[i].Nodes.Count; j++)
                    {
                        height += LineHeight;
                        if (height > mouseY)
                            return Categories[i].Nodes[j];
                    }
                }
            }

            return null;
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            int drawnWidth = Width;
            int height = MARGIN;
            for (int i = 0; i < Categories.Count; i++)
            {
                if (height > ViewTop + Height)
                    break;

                TreeViewCategory category = Categories[i];
                int x = TextBorderDistance;
                int y = height - ViewTop;

                if (ViewTop < height + CategoryHeight)
                {
                    // This category header is at least partially visible, draw it!

                    if (HoveredNode == category || SelectedNode == category)
                    {
                        FillRectangle(new Rectangle(1, y, drawnWidth, CategoryHeight),
                            UISettings.ActiveSettings.FocusColor);
                    }

                    string text = null;
                    if (category.Nodes.Count > 0)
                        text = category.IsOpened ? "- " + category.Text : "+ " + category.Text;
                    else
                        text = category.Text;

                    DrawStringWithShadow(text, Constants.UIDefaultFont, new Vector2(x, y + CategoryMargin), UISettings.ActiveSettings.AltColor);
                }

                height += CategoryHeight;
                y = height - ViewTop;

                if (Categories[i].IsOpened)
                {
                    for (int j = 0; j < category.Nodes.Count; j++)
                    {
                        if (height > ViewTop + Height)
                            break;

                        if (ViewTop < height + LineHeight)
                        {
                            // This node is at least partially visible, draw it!

                            var node = category.Nodes[j];
                            if (HoveredNode == node || SelectedNode == node)
                            {
                                FillRectangle(new Rectangle(1, y, drawnWidth, LineHeight),
                                    UISettings.ActiveSettings.FocusColor);
                            }

                            x = TextBorderDistance + NODE_INDENTATION;
                            int textHeight = (int)Renderer.GetTextDimensions(node.Text, Constants.UIDefaultFont).Y;
                            int textY = y + ((LineHeight - textHeight) / 2);
                            DrawStringWithShadow(node.Text, Constants.UIDefaultFont, new Vector2(x, textY), UISettings.ActiveSettings.AltColor);

                            if (node.Texture != null)
                            {
                                int textureHeight = node.Texture.Height;
                                int textureWidth = node.Texture.Width;
                                int textureYPosition = 0;

                                if (node.Texture.Height > LineHeight)
                                {
                                    double scaleRatio = textureHeight / (double)LineHeight;
                                    textureHeight = LineHeight;
                                    textureWidth = (int)(textureWidth / scaleRatio);
                                }
                                else
                                    textureYPosition = (LineHeight - textureHeight) / 2;

                                DrawTexture(node.Texture,
                                    new Rectangle(Width - ScrollBar.Width - textureWidth - MARGIN,
                                    y + textureYPosition,
                                    textureWidth, textureHeight), Color.White);

                                if (node.RemapTexture != null)
                                {
                                    DrawTexture(node.RemapTexture,
                                        new Rectangle(Width - ScrollBar.Width - textureWidth - MARGIN,
                                        y + textureYPosition,
                                        textureWidth, textureHeight), node.RemapColor);
                                }
                            }
                        }

                        height += LineHeight;
                        y = height - ViewTop;
                    }
                }
            }

            if (DrawBorders)
                DrawPanelBorders();

            DrawChildren(gameTime);
        }
    }
}
