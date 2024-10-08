using System;
using System.IO;
using System.Linq;
using Rampastring.XNAUI;
using TSMapEditor.Models;
using TSMapEditor.Mutations;
using TSMapEditor.Scripts;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;
using TSMapEditor.UI.Windows;
using TSMapEditor.Models.Enums;
using Rampastring.Tools;
using System.Diagnostics;
using System.ComponentModel;

#if WINDOWS
using System.Windows.Forms;
#endif

namespace TSMapEditor.UI.TopBar
{
    class TopBarMenu : EditorPanel
    {
        public TopBarMenu(WindowManager windowManager, MutationManager mutationManager, MapUI mapUI, Map map, WindowController windowController) : base(windowManager)
        {
            this.mutationManager = mutationManager;
            this.mapUI = mapUI;
            this.map = map;
            this.windowController = windowController;
        }

        public event EventHandler<FileSelectedEventArgs> OnFileSelected;
        public event EventHandler InputFileReloadRequested;
        public event EventHandler MapWideOverlayLoadRequested;

        private readonly MutationManager mutationManager;
        private readonly MapUI mapUI;
        private readonly Map map;
        private readonly WindowController windowController;

        private MenuButton[] menuButtons;

        private DeleteTubeCursorAction deleteTunnelCursorAction;
        private PlaceTubeCursorAction placeTubeCursorAction;
        private ToggleIceGrowthCursorAction toggleIceGrowthCursorAction;
        private CheckDistanceCursorAction checkDistanceCursorAction;
        private CheckDistancePathfindingCursorAction checkDistancePathfindingCursorAction;
        private CalculateTiberiumValueCursorAction calculateTiberiumValueCursorAction;
        private ManageBaseNodesCursorAction manageBaseNodesCursorAction;
        private PlaceVeinholeMonsterCursorAction placeVeinholeMonsterCursorAction;

        private SelectBridgeWindow selectBridgeWindow;

        public override void Initialize()
        {
            Name = nameof(TopBarMenu);

            deleteTunnelCursorAction = new DeleteTubeCursorAction(mapUI);
            placeTubeCursorAction = new PlaceTubeCursorAction(mapUI);
            toggleIceGrowthCursorAction = new ToggleIceGrowthCursorAction(mapUI);
            checkDistanceCursorAction = new CheckDistanceCursorAction(mapUI);
            checkDistancePathfindingCursorAction = new CheckDistancePathfindingCursorAction(mapUI);
            calculateTiberiumValueCursorAction = new CalculateTiberiumValueCursorAction(mapUI);
            manageBaseNodesCursorAction = new ManageBaseNodesCursorAction(mapUI);
            placeVeinholeMonsterCursorAction = new PlaceVeinholeMonsterCursorAction(mapUI);

            selectBridgeWindow = new SelectBridgeWindow(WindowManager, map);
            var selectBridgeDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectBridgeWindow);
            selectBridgeDarkeningPanel.Hidden += SelectBridgeDarkeningPanel_Hidden;

            windowController.SelectConnectedTileWindow.ObjectSelected += SelectConnectedTileWindow_ObjectSelected;

            var fileContextMenu = new EditorContextMenu(WindowManager);
            fileContextMenu.Name = nameof(fileContextMenu);
            fileContextMenu.AddItem("New", () => windowController.CreateNewMapWindow.Open(), null, null, null);
            fileContextMenu.AddItem("Open", () => Open(), null, null, null);

            fileContextMenu.AddItem("Save", () => SaveMap());
            fileContextMenu.AddItem("Save As", () => SaveAs(), null, null, null);
            fileContextMenu.AddItem(" ", null, () => false, null, null);
            fileContextMenu.AddItem("Reload Input File",
                () => InputFileReloadRequested?.Invoke(this, EventArgs.Empty),
                () => !string.IsNullOrWhiteSpace(map.LoadedINI.FileName),
                null, null);
            fileContextMenu.AddItem(" ", null, () => false, null, null);
            fileContextMenu.AddItem("Extract Megamap...", ExtractMegamap);
            fileContextMenu.AddItem("Generate Map Preview...", WriteMapPreview);
            fileContextMenu.AddItem(" ", null, () => false, null, null, null);
            fileContextMenu.AddItem("Open With Text Editor", OpenWithTextEditor, () => !string.IsNullOrWhiteSpace(map.LoadedINI.FileName));
            fileContextMenu.AddItem(" ", null, () => false, null, null);
            fileContextMenu.AddItem("Exit", WindowManager.CloseGame);

            var fileButton = new MenuButton(WindowManager, fileContextMenu);
            fileButton.Name = nameof(fileButton);
            fileButton.Text = "File";
            AddChild(fileButton);

            var editContextMenu = new EditorContextMenu(WindowManager);
            editContextMenu.Name = nameof(editContextMenu);
            editContextMenu.AddItem("Configure Copied Objects...", () => windowController.CopiedEntryTypesWindow.Open(), null, null, null, KeyboardCommands.Instance.ConfigureCopiedObjects.GetKeyDisplayString());
            editContextMenu.AddItem("Copy", () => KeyboardCommands.Instance.Copy.DoTrigger(), null, null, null, KeyboardCommands.Instance.Copy.GetKeyDisplayString());
            editContextMenu.AddItem("Copy Custom Shape", () => KeyboardCommands.Instance.CopyCustomShape.DoTrigger(), null, null, null, KeyboardCommands.Instance.CopyCustomShape.GetKeyDisplayString());
            editContextMenu.AddItem("Paste", () => KeyboardCommands.Instance.Paste.DoTrigger(), null, null, null, KeyboardCommands.Instance.Paste.GetKeyDisplayString());
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Undo", () => mutationManager.Undo(), () => mutationManager.CanUndo(), null, null, KeyboardCommands.Instance.Undo.GetKeyDisplayString());
            editContextMenu.AddItem("Redo", () => mutationManager.Redo(), () => mutationManager.CanRedo(), null, null, KeyboardCommands.Instance.Redo.GetKeyDisplayString());
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Basic", () => windowController.BasicSectionConfigWindow.Open(), null, null, null);
            editContextMenu.AddItem("Map Size", () => windowController.MapSizeWindow.Open(), null, null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Lighting", () => windowController.LightingSettingsWindow.Open(), null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Place Tunnel", () => mapUI.EditorState.CursorAction = placeTubeCursorAction, null, null, null, KeyboardCommands.Instance.PlaceTunnel.GetKeyDisplayString());
            editContextMenu.AddItem("Delete Tunnel", () => mapUI.EditorState.CursorAction = deleteTunnelCursorAction, null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);

            int bridgeCount = map.EditorConfig.Bridges.Count;
            if (bridgeCount > 0)
            {
                var bridges = map.EditorConfig.Bridges;
                if (bridgeCount == 1 && bridges[0].Kind == BridgeKind.Low)
                {
                    editContextMenu.AddItem("Draw Low Bridge", () => mapUI.EditorState.CursorAction =
                        new PlaceBridgeCursorAction(mapUI, bridges[0]), null, null, null);
                }
                else
                {
                    editContextMenu.AddItem("Draw Bridge...", SelectBridge, null, null, null);
                }
            }

            var theaterMatchingCliffs = map.EditorConfig.Cliffs.Where(cliff => cliff.AllowedTheaters.Exists(
                theaterName => theaterName.Equals(map.TheaterName, StringComparison.OrdinalIgnoreCase))).ToList();
            int cliffCount = theaterMatchingCliffs.Count;
            if (cliffCount > 0)
            {
                if (cliffCount == 1)
                {
                    editContextMenu.AddItem("Draw Connected Tiles", () => mapUI.EditorState.CursorAction =
                        new DrawCliffCursorAction(mapUI, theaterMatchingCliffs[0]), null, null, null);
                }
                else
                {
                    editContextMenu.AddItem("Draw Connected Tiles...", () => windowController.SelectConnectedTileWindow.Open(), null, null, null, KeyboardCommands.Instance.PlaceConnectedTile.GetKeyDisplayString());
                }
            }

            editContextMenu.AddItem("Toggle IceGrowth", () => { mapUI.EditorState.CursorAction = toggleIceGrowthCursorAction; toggleIceGrowthCursorAction.ToggleIceGrowth = true; mapUI.EditorState.HighlightIceGrowth = true; }, null, null, null);
            editContextMenu.AddItem("Clear IceGrowth", () => { mapUI.EditorState.CursorAction = toggleIceGrowthCursorAction; toggleIceGrowthCursorAction.ToggleIceGrowth = false; mapUI.EditorState.HighlightIceGrowth = true; }, null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Manage Base Nodes", ManageBaseNodes_Selected, null, null, null);

            if (map.Rules.OverlayTypes.Exists(ot => ot.ININame == Constants.VeinholeMonsterTypeName) && map.Rules.OverlayTypes.Exists(ot => ot.ININame == Constants.VeinholeDummyTypeName))
            {
                editContextMenu.AddItem(" ", null, () => false, null, null);
                editContextMenu.AddItem("Place Veinhole Monster", () => mapUI.EditorState.CursorAction = placeVeinholeMonsterCursorAction, null, null, null, null);
            }

            var editButton = new MenuButton(WindowManager, editContextMenu);
            editButton.Name = nameof(editButton);
            editButton.X = fileButton.Right;
            editButton.Text = "Edit";
            AddChild(editButton);

            var viewContextMenu = new EditorContextMenu(WindowManager);
            viewContextMenu.Name = nameof(viewContextMenu);
            viewContextMenu.AddItem("Configure Rendered Objects...", () => windowController.RenderedObjectsConfigurationWindow.Open());
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("Toggle Impassable Cells", () => mapUI.EditorState.HighlightImpassableCells = !mapUI.EditorState.HighlightImpassableCells, null, null, null);
            viewContextMenu.AddItem("Toggle IceGrowth Preview", () => mapUI.EditorState.HighlightIceGrowth = !mapUI.EditorState.HighlightIceGrowth, null, null, null);
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("View Minimap", () => windowController.MinimapWindow.Open());
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("Find Waypoint...", () => windowController.FindWaypointWindow.Open());
            viewContextMenu.AddItem("Center of Map", () => mapUI.Camera.CenterOnMapCenterCell());
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("No Lighting", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.NoLighting);
            viewContextMenu.AddItem("Normal Lighting", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.Normal);
            if (Constants.IsRA2YR)
            {
                viewContextMenu.AddItem("Lightning Storm Lighting", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.IonStorm);
                viewContextMenu.AddItem("Dominator Lighting", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.Dominator);
            }
            else
            {
                viewContextMenu.AddItem("Ion Storm Lighting", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.IonStorm);
            }
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("Toggle Fullscreen Mode", () => KeyboardCommands.Instance.ToggleFullscreen.DoTrigger());

            var viewButton = new MenuButton(WindowManager, viewContextMenu);
            viewButton.Name = nameof(viewButton);
            viewButton.X = editButton.Right;
            viewButton.Text = "View";
            AddChild(viewButton);

            var toolsContextMenu = new EditorContextMenu(WindowManager);
            toolsContextMenu.Name = nameof(toolsContextMenu);
            // toolsContextMenu.AddItem("Options");
            if (windowController.AutoApplyImpassableOverlayWindow.IsAvailable)
                toolsContextMenu.AddItem("Apply Impassable Overlay...", () => windowController.AutoApplyImpassableOverlayWindow.Open(), null, null, null);

            toolsContextMenu.AddItem("Terrain Generator Options...", () => windowController.TerrainGeneratorConfigWindow.Open(), null, null, null, KeyboardCommands.Instance.ConfigureTerrainGenerator.GetKeyDisplayString());
            toolsContextMenu.AddItem("Generate Terrain", () => EnterTerrainGenerator(), null, null, null, KeyboardCommands.Instance.GenerateTerrain.GetKeyDisplayString());
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Apply INI Code...", () => windowController.ApplyINICodeWindow.Open(), null, null, null);
            toolsContextMenu.AddItem("Run Script...", () => windowController.RunScriptWindow.Open(), null, null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Deletion Options...", () => windowController.DeletionModeConfigurationWindow.Open());
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Change Map Height...", () => windowController.ChangeHeightWindow.Open(), null, () => !Constants.IsFlatWorld, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, () => !Constants.IsFlatWorld, null);
            toolsContextMenu.AddItem("Smoothen Ice", SmoothenIce, null, null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Check Distance...", () => mapUI.EditorState.CursorAction = checkDistanceCursorAction, null, null, null);
            toolsContextMenu.AddItem("Check Distance (Pathfinding)...", () => mapUI.EditorState.CursorAction = checkDistancePathfindingCursorAction);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Calculate Credits...", () => mapUI.EditorState.CursorAction = calculateTiberiumValueCursorAction, null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Load Map-Wide Overlay...", () => MapWideOverlayLoadRequested?.Invoke(this, EventArgs.Empty), null, null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Configure Hotkeys...", () => windowController.HotkeyConfigurationWindow.Open(), null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("About", () => windowController.AboutWindow.Open(), null, null, null, null);

            var toolsButton = new MenuButton(WindowManager, toolsContextMenu);
            toolsButton.Name = nameof(toolsButton);
            toolsButton.X = viewButton.Right;
            toolsButton.Text = "Tools";
            AddChild(toolsButton);

            var scriptingContextMenu = new EditorContextMenu(WindowManager);
            scriptingContextMenu.Name = nameof(scriptingContextMenu);
            scriptingContextMenu.AddItem("Houses", () => windowController.HousesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("Triggers", () => windowController.TriggersWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("TaskForces", () => windowController.TaskForcesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("Scripts", () => windowController.ScriptsWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("TeamTypes", () => windowController.TeamTypesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("Local Variables", () => windowController.LocalVariablesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("AITriggers", () => windowController.AITriggersWindow.Open(), null, null, null, null);

            var scriptingButton = new MenuButton(WindowManager, scriptingContextMenu);
            scriptingButton.Name = nameof(scriptingButton);
            scriptingButton.X = toolsButton.Right;
            scriptingButton.Text = "Scripting";
            AddChild(scriptingButton);

            base.Initialize();

            Height = fileButton.Height;

            menuButtons = new MenuButton[] { fileButton, editButton, viewButton, toolsButton, scriptingButton };
            Array.ForEach(menuButtons, b => b.MouseEnter += MenuButton_MouseEnter);

            KeyboardCommands.Instance.ConfigureCopiedObjects.Triggered += (s, e) => windowController.CopiedEntryTypesWindow.Open();
            KeyboardCommands.Instance.GenerateTerrain.Triggered += (s, e) => EnterTerrainGenerator();
            KeyboardCommands.Instance.ConfigureTerrainGenerator.Triggered += (s, e) => windowController.TerrainGeneratorConfigWindow.Open();
            KeyboardCommands.Instance.PlaceTunnel.Triggered += (s, e) => mapUI.EditorState.CursorAction = placeTubeCursorAction;
            KeyboardCommands.Instance.PlaceConnectedTile.Triggered += (s, e) => windowController.SelectConnectedTileWindow.Open();
            KeyboardCommands.Instance.Save.Triggered += (s, e) => SaveMap();

            windowController.TerrainGeneratorConfigWindow.ConfigApplied += TerrainGeneratorConfigWindow_ConfigApplied;
        }

        private void TerrainGeneratorConfigWindow_ConfigApplied(object sender, EventArgs e)
        {
            EnterTerrainGenerator();
        }

        private void SaveMap()
        {
            if (string.IsNullOrWhiteSpace(map.LoadedINI.FileName))
            {
                SaveAs();
                return;
            }

            TrySaveMap();
        }

        private void TrySaveMap()
        {
            try
            {
                map.Save();
            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedAccessException || ex is IOException)
                {
                    Logger.Log("Failed to save the map file. Returned error message: " + ex.Message);

                    EditorMessageBox.Show(WindowManager, "Failed to save map",
                        "Failed to write the map file. Please make sure that WAE has write access to the path." + Environment.NewLine + Environment.NewLine +
                        "A common source of this error is trying to save the map to Program Files or another" + Environment.NewLine +
                        "write-protected directory without running WAE with administrative rights." + Environment.NewLine + Environment.NewLine +
                        "Returned error was: " + ex.Message, Windows.MessageBoxButtons.OK);
                }
                else
                {
                    throw;
                }
            }
        }

        private void ExtractMegamap()
        {
#if WINDOWS
            string initialPath = string.IsNullOrWhiteSpace(UserSettings.Instance.LastScenarioPath.GetValue()) ? UserSettings.Instance.GameDirectory : UserSettings.Instance.LastScenarioPath.GetValue();

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(initialPath);
                saveFileDialog.FileName = Path.ChangeExtension(Path.GetFileName(initialPath), ".png");
                saveFileDialog.Filter = "PNG files|*.png|All files|*.*";
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    mapUI.ExtractMegamapTo(saveFileDialog.FileName);
                }
            }
#else
            mapUI.ExtractMegamapTo(Path.Combine(Environment.CurrentDirectory, "megamap.png"));
#endif
        }

        private void WriteMapPreview()
        {
            var messageBox = EditorMessageBox.Show(WindowManager, "Confirmation",
                "This will write the current minimap as the map preview to the map file." + Environment.NewLine + Environment.NewLine +
                "This provides the map with a preview if it is used as a custom map" + Environment.NewLine + 
                "in the CnCNet Client or in-game, but is not necessary if the map will" + Environment.NewLine +
                "have an external preview. It will also significantly increase the size" + Environment.NewLine +
                "of the map file." + Environment.NewLine + Environment.NewLine +
                "Do you want to continue?" + Environment.NewLine + Environment.NewLine +
                "Note: The preview won't be actually written to the map before" + Environment.NewLine + 
                "you save the map.", Windows.MessageBoxButtons.YesNo);

            messageBox.YesClickedAction = _ => mapUI.AddPreviewToMap();
        }

        private void OpenWithTextEditor()
        {
            string textEditorPath = UserSettings.Instance.TextEditorPath;

            if (string.IsNullOrWhiteSpace(textEditorPath) || !File.Exists(textEditorPath))
            {
                textEditorPath = GetDefaultTextEditorPath();

                if (textEditorPath == null)
                {
                    EditorMessageBox.Show(WindowManager, "No text editor found!", "No valid text editor has been configured and no default choice was found.", Windows.MessageBoxButtons.OK);
                    return;
                }
            }

            try
            {
                Process.Start(textEditorPath, "\"" + map.LoadedINI.FileName + "\"");
            }
            catch (Exception ex) when (ex is Win32Exception || ex is ObjectDisposedException)
            {
                Logger.Log("Failed to launch text editor! Message: " + ex.Message);
                EditorMessageBox.Show(WindowManager, "Failed to launch text editor",
                    "An error occurred when trying to open the map file with the text editor." + Environment.NewLine + Environment.NewLine +
                    "Received error was: " + ex.Message, Windows.MessageBoxButtons.OK);
            }
        }

        private string GetDefaultTextEditorPath()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var pathsToSearch = new[]
            {
                Path.Combine(programFiles, "Notepad++", "notepad++.exe"),
                Path.Combine(programFilesX86, "Notepad++", "notepad++.exe"),
                Path.Combine(programFiles, "Microsoft VS Code", "vscode.exe"),
                Path.Combine(Environment.SystemDirectory, "notepad.exe"),
            };

            foreach (string path in pathsToSearch)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        private void ManageBaseNodes_Selected()
        {
            if (map.Houses.Count == 0)
            {
                EditorMessageBox.Show(WindowManager, "Houses Required",
                    "The map has no houses set up. Houses need to be configured before base nodes can be added." + Environment.NewLine + Environment.NewLine +
                    "You can configure Houses from Scripting -> Houses.", TSMapEditor.UI.Windows.MessageBoxButtons.OK);

                return;
            }

            mapUI.EditorState.CursorAction = manageBaseNodesCursorAction;
        }

        private void SmoothenIce()
        {
            new SmoothenIceScript().Perform(map);
            mapUI.InvalidateMap();
        }

        private void EnterTerrainGenerator()
        {
            if (windowController.TerrainGeneratorConfigWindow.TerrainGeneratorConfig == null)
            {
                windowController.TerrainGeneratorConfigWindow.Open();
                return;
            }

            var generateTerrainCursorAction = new GenerateTerrainCursorAction(mapUI);
            generateTerrainCursorAction.TerrainGeneratorConfiguration = windowController.TerrainGeneratorConfigWindow.TerrainGeneratorConfig;
            mapUI.CursorAction = generateTerrainCursorAction;
        }

        private void SelectBridge()
        {
            selectBridgeWindow.Open();
        }

        private void SelectBridgeDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectBridgeWindow.SelectedObject != null)
                mapUI.EditorState.CursorAction = new PlaceBridgeCursorAction(mapUI, selectBridgeWindow.SelectedObject);
        }

        private void SelectConnectedTileWindow_ObjectSelected(object sender, EventArgs e)
        {
            mapUI.EditorState.CursorAction = new DrawCliffCursorAction(mapUI, windowController.SelectConnectedTileWindow.SelectedObject);
        }

        private void Open()
        {
#if WINDOWS
            string initialPath = string.IsNullOrWhiteSpace(UserSettings.Instance.LastScenarioPath.GetValue()) ? UserSettings.Instance.GameDirectory : Path.GetDirectoryName(UserSettings.Instance.LastScenarioPath.GetValue());

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = initialPath;
                openFileDialog.Filter = Constants.OpenFileDialogFilter.Replace(':', ';');
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    OnFileSelected?.Invoke(this, new FileSelectedEventArgs(openFileDialog.FileName));
                }
            }
#else
            windowController.OpenMapWindow.Open();
#endif
        }

        private void SaveAs()
        {
#if WINDOWS
            string initialPath = string.IsNullOrWhiteSpace(UserSettings.Instance.LastScenarioPath.GetValue()) ? UserSettings.Instance.GameDirectory : UserSettings.Instance.LastScenarioPath.GetValue();

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(initialPath);
                saveFileDialog.FileName = Path.GetFileName(initialPath);
                saveFileDialog.Filter = Constants.OpenFileDialogFilter.Replace(':', ';');
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    map.LoadedINI.FileName = saveFileDialog.FileName;
                    TrySaveMap();

                    if (UserSettings.Instance.LastScenarioPath.GetValue() != saveFileDialog.FileName)
                    {
                        UserSettings.Instance.LastScenarioPath.UserDefinedValue = saveFileDialog.FileName;
                        _ = UserSettings.Instance.SaveSettingsAsync();
                    }
                }
            }
#else
            windowController.SaveMapAsWindow.Open();
#endif
        }

        private void MenuButton_MouseEnter(object sender, EventArgs e)
        {
            var menuButton = (MenuButton)sender;

            // Is a menu open?
            int openIndex = Array.FindIndex(menuButtons, b => b.ContextMenu.Enabled);
            if (openIndex > -1)
            {
                // Switch to the new button's menu
                menuButtons[openIndex].ContextMenu.Disable();
                menuButton.OpenContextMenu();
            }
        }
    }
}
