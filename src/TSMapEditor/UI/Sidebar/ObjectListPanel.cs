using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.Models.ArtConfig;
using TSMapEditor.Rendering;

namespace TSMapEditor.UI.Sidebar
{
    /// <summary>
    /// A base class for all object type list panels.
    /// </summary>
    public abstract class ObjectListPanel : XNAPanel, ISearchBoxContainer
    {
        /// <summary>
        /// Helper structure used for building sidebar object categories.
        /// Not used after the sidebar has been initialized.
        /// </summary>
        struct ObjectCategory
        {
            public string Name;
            public Color RemapColor;

            public ObjectCategory(string name, Color remapColor)
            {
                Name = name;
                RemapColor = remapColor;
            }
        }

        public ObjectListPanel(WindowManager windowManager, EditorState editorState, Map map, TheaterGraphics theaterGraphics) : base(windowManager)
        {
            EditorState = editorState;
            Map = map;
            TheaterGraphics = theaterGraphics;
        }

        protected EditorState EditorState { get; }
        protected Map Map { get; }
        protected TheaterGraphics TheaterGraphics { get; }

        public XNASuggestionTextBox SearchBox { get; private set; }
        public TreeView ObjectTreeView { get; private set; }

        private XNADropDown ddOwner;

        public override void Initialize()
        {
            var lblOwner = new XNALabel(WindowManager);
            lblOwner.Name = nameof(lblOwner);
            lblOwner.X = Constants.UIEmptySideSpace;
            lblOwner.Y = Constants.UIEmptyTopSpace;
            lblOwner.Text = "Owner:";
            AddChild(lblOwner);

            ddOwner = new XNADropDown(WindowManager);
            ddOwner.Name = nameof(ddOwner);
            ddOwner.X = lblOwner.Right + Constants.UIHorizontalSpacing;
            ddOwner.Y = lblOwner.Y - 1;
            ddOwner.Width = Width - Constants.UIEmptySideSpace - ddOwner.X;
            AddChild(ddOwner);
            ddOwner.SelectedIndexChanged += DdOwner_SelectedIndexChanged;

            SearchBox = new XNASuggestionTextBox(WindowManager);
            SearchBox.Name = nameof(SearchBox);
            SearchBox.X = Constants.UIEmptySideSpace;
            SearchBox.Y = ddOwner.Bottom + Constants.UIEmptyTopSpace;
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

            ObjectTreeView.SelectedItemChanged += ObjectTreeView_SelectedItemChanged;
            EditorState.ObjectOwnerChanged += EditorState_ObjectOwnerChanged;

            base.Initialize();

            RefreshHouseList();

            Parent.ClientRectangleUpdated += Parent_ClientRectangleUpdated;
            RefreshPanelSize();

            InitObjects();

            KeyboardCommands.Instance.NextSidebarNode.Triggered += NextSidebarNode_Triggered;
            KeyboardCommands.Instance.PreviousSidebarNode.Triggered += PreviousSidebarNode_Triggered;

            Map.HousesChanged += (s, e) => RefreshHouseList();
            Map.HouseColorChanged += (s, e) => RefreshHouseList();
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
            if (EditorState.ObjectOwner == null)
                return;

            if (ObjectTreeView.SelectedNode != null)
                ObjectSelected();
        }

        protected abstract void ObjectSelected();

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

        protected abstract void InitObjects();

        protected void InitObjectsBase<T>(List<T> objectTypeList, ShapeImage[] textures, Func<T, bool> filter = null) where T : TechnoType, IArtConfigContainer
        {
            var sideCategories = new List<TreeViewCategory>();
            for (int i = 0; i < objectTypeList.Count; i++)
            {
                var objectType = objectTypeList[i];

                if (!objectType.EditorVisible)
                    continue;

                if (objectType.WhatAmI() == RTTIType.BuildingType)
                {
                    var buildingType = (BuildingType)(TechnoType)objectType;
                    if (buildingType.PowersUpBuilding != null)
                    {
                        // Don't list upgrades
                        continue;
                    }
                }

                if (filter != null && !filter(objectType))
                    continue;

                List<ObjectCategory> categories = new List<ObjectCategory>(1);

                string categoriesString = objectType.EditorCategory;

                if (categoriesString == null)
                    categoriesString = objectType.Owner;

                if (string.IsNullOrWhiteSpace(categoriesString))
                {
                    categories.Add(new ObjectCategory("Uncategorized", Color.White));
                }
                else
                {
                    string[] owners = categoriesString.Split(',');

                    for (int ownerIndex = 0; ownerIndex < owners.Length; ownerIndex++)
                    {
                        Color remapColor = Color.White;

                        string ownerName = owners[ownerIndex];
                        ownerName = Map.EditorConfig.EditorRulesIni.GetStringValue("ObjectOwnerOverrides", ownerName, ownerName);

                        House house = Map.StandardHouses.Find(h => h.ININame == ownerName);
                        if (house != null)
                        {
                            int actsLike = house.ActsLike.GetValueOrDefault(-1);
                            if (actsLike > -1)
                                ownerName = Map.StandardHouses[actsLike].ININame;
                        }

                        House ownerHouse = Map.StandardHouses.Find(h => h.ININame == ownerName);
                        if (ownerHouse != null)
                            remapColor = ownerHouse.XNAColor;

                        // Prevent duplicates that can occur due to category overrides
                        // (For example, if objects owned by "Soviet1" are overridden to be listed under
                        // "Soviet", and a structure has both "Soviet" and "Soviet1" listed as its owner)
                        if (!categories.Exists(c => c.Name == ownerName))
                        {
                            categories.Add(new ObjectCategory(ownerName, remapColor));
                        }
                    }
                }

                Texture2D texture = null;
                Texture2D remapTexture = null;
                if (textures != null)
                {
                    if (textures[i] != null)
                    {
                        int frameCount = textures[i].GetFrameCount();

                        // Find the first valid frame and use its RGBA variant as our texture
                        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                        {
                            var frame = textures[i].GetFrame(frameIndex);
                            if (frame != null)
                            {
                                texture = textures[i].GetTextureForFrame_RGBA(frameIndex);
                                if (Constants.HQRemap && objectType.GetArtConfig().Remapable && textures[i].HasRemapFrames())
                                    remapTexture = textures[i].GetRemapTextureForFrame_RGBA(frameIndex);
                                break;
                            }
                        }
                    }
                }

                categories = categories.OrderBy(c => Map.EditorConfig.EditorRulesIni.GetIntValue("ObjectCategoryPriorities", c.Name, 0)).ToList();

                for (int categoryIndex = 0; categoryIndex < categories.Count; categoryIndex++)
                {
                    var category = FindOrMakeCategory(categories[categoryIndex].Name, sideCategories);

                    category.Nodes.Add(new TreeViewNode()
                    {
                        Text = objectType.GetEditorDisplayName() + " (" + objectType.ININame + ")",
                        Texture = texture,
                        RemapTexture = remapTexture,
                        RemapColor = categories[categoryIndex].RemapColor,
                        Tag = objectType
                    });
                }
            }

            for (int i = 0; i < sideCategories.Count; i++)
                sideCategories[i].Nodes = sideCategories[i].Nodes.OrderBy(n => n.Text).ToList();

            sideCategories = sideCategories.OrderBy(c => Map.EditorConfig.EditorRulesIni.GetIntValue("ObjectCategoryPriorities", c.Text, int.MaxValue)).ToList();
            sideCategories.ForEach(c => ObjectTreeView.AddCategory(c));
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

        private void Parent_ClientRectangleUpdated(object sender, EventArgs e)
        {
            Height = Parent.Height - Y;
            ObjectTreeView.Height = Height - ObjectTreeView.Y;
            RefreshSize();
        }

        private void RefreshPanelSize()
        {
            Width = Parent.Width;
            ddOwner.Width = Width - Constants.UIEmptySideSpace - ddOwner.X;
            SearchBox.Width = Width - Constants.UIEmptySideSpace * 2;
            ObjectTreeView.Width = Width;
        }

        private void DdOwner_SelectedIndexChanged(object sender, EventArgs e)
        {
            EditorState.ObjectOwner = Map.GetHouses()[ddOwner.SelectedIndex];
        }

        private void RefreshHouseList()
        {
            ddOwner.SelectedIndexChanged -= DdOwner_SelectedIndexChanged;

            ddOwner.Items.Clear();
            Map.GetHouses().ForEach(h => ddOwner.AddItem(h.ININame, h.XNAColor));
            ddOwner.SelectedIndex = Map.GetHouses().FindIndex(h => h == EditorState.ObjectOwner);

            ddOwner.SelectedIndexChanged += DdOwner_SelectedIndexChanged;
        }

        private void EditorState_ObjectOwnerChanged(object sender, EventArgs e)
        {
            ddOwner.SelectedIndexChanged -= DdOwner_SelectedIndexChanged;

            ddOwner.SelectedIndex = Map.GetHouses().FindIndex(h => h == EditorState.ObjectOwner);

            ddOwner.SelectedIndexChanged += DdOwner_SelectedIndexChanged;
        }
    }
}
