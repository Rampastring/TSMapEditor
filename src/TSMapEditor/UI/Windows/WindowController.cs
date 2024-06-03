using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Reflection;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.Notifications;
using TSMapEditor.UI.Windows.MainMenuWindows;
using TSMapEditor.UI.Windows.TerrainGenerator;

namespace TSMapEditor.UI.Windows
{
    public interface IWindowParentControl
    {
        void AddChild(XNAControl child);
        void RemoveChild(XNAControl child);

        event EventHandler RenderResolutionChanged;

        WindowManager WindowManager { get; }

        INotificationManager NotificationManager { get; }

        void SetAutoUpdateChildOrder(bool value);
    }

    public class WindowController
    {
        public const int ChildWindowOrderValue = 10000;

        private List<EditorWindow> Windows { get; } = new List<EditorWindow>();

        public event EventHandler Initialized;
        public event EventHandler RenderResolutionChanged;

        public BasicSectionConfigWindow BasicSectionConfigWindow { get; private set; }
        public TaskforcesWindow TaskForcesWindow { get; private set; }
        public ScriptsWindow ScriptsWindow { get; private set; }
        public TeamTypesWindow TeamTypesWindow { get; private set; }
        public TriggersWindow TriggersWindow { get; private set; }
        public AITriggersWindow AITriggersWindow { get; private set; }
        public PlaceWaypointWindow PlaceWaypointWindow { get; private set; }
        public LocalVariablesWindow LocalVariablesWindow { get; private set; }
        public StructureOptionsWindow StructureOptionsWindow { get; private set; }
        public VehicleOptionsWindow VehicleOptionsWindow { get; private set; }
        public InfantryOptionsWindow InfantryOptionsWindow { get; private set; }
        public AircraftOptionsWindow AircraftOptionsWindow { get; private set; }
        public HousesWindow HousesWindow { get; private set; }
        public SaveMapAsWindow SaveMapAsWindow { get; private set; }
        public CreateNewMapWindow CreateNewMapWindow { get; private set; }
        public OpenMapWindow OpenMapWindow { get; private set; }
        public AutoApplyImpassableOverlayWindow AutoApplyImpassableOverlayWindow { get; private set; }
        public TerrainGeneratorConfigWindow TerrainGeneratorConfigWindow { get; private set; }
        public MegamapWindow MinimapWindow { get; private set; }
        public CopiedEntryTypesWindow CopiedEntryTypesWindow { get; private set; }
        public LightingSettingsWindow LightingSettingsWindow { get; private set; }
        public ApplyINICodeWindow ApplyINICodeWindow { get; private set; }
        public RunScriptWindow RunScriptWindow { get; private set; }
        public HotkeyConfigurationWindow HotkeyConfigurationWindow { get; private set; }
        public MapSizeWindow MapSizeWindow { get; private set; }
        public ExpandMapWindow ExpandMapWindow { get; private set; }
        public ChangeHeightWindow ChangeHeightWindow { get; private set; }
        public FindWaypointWindow FindWaypointWindow { get; private set; }
        public DeletionModeConfigurationWindow DeletionModeConfigurationWindow { get; private set; }
        public RenderedObjectsConfigurationWindow RenderedObjectsConfigurationWindow { get; private set; }
        public ConfigureAlliesWindow ConfigureAlliesWindow { get; private set; }
        public SelectConnectedTileWindow SelectConnectedTileWindow { get; private set; }
        public AboutWindow AboutWindow { get; private set; }

        private IWindowParentControl windowParentControl;

        private EditorWindow foregroundWindow;

        /// <summary>
        /// Handles window focus switching.
        /// </summary>
        private void Window_HandleFocusSwitch(object sender, EventArgs e)
        {
            var window = (EditorWindow)sender;

            if (foregroundWindow != window)
            {
                windowParentControl.SetAutoUpdateChildOrder(false);

                Windows.Remove(window);
                Windows.Add(window);

                for (int i = 0; i < Windows.Count; i++)
                {
                    Windows[i].UpdateOrder = ChildWindowOrderValue + i;
                    Windows[i].DrawOrder = ChildWindowOrderValue + i;
                }

                windowParentControl.SetAutoUpdateChildOrder(true);

                foregroundWindow = window;

                foregroundWindow.UpdateOrder = ChildWindowOrderValue + Windows.Count;
                foregroundWindow.DrawOrder = ChildWindowOrderValue + Windows.Count;
            }
        }

        public void Initialize(IWindowParentControl windowParentControl, Map map, EditorState editorState, ICursorActionTarget cursorActionTarget)
        {
            BasicSectionConfigWindow = new BasicSectionConfigWindow(windowParentControl.WindowManager, map);
            Windows.Add(BasicSectionConfigWindow);

            TaskForcesWindow = new TaskforcesWindow(windowParentControl.WindowManager, map);
            Windows.Add(TaskForcesWindow);

            ScriptsWindow = new ScriptsWindow(windowParentControl.WindowManager, map, editorState, windowParentControl.NotificationManager, cursorActionTarget);
            Windows.Add(ScriptsWindow);

            TeamTypesWindow = new TeamTypesWindow(windowParentControl.WindowManager, map);
            Windows.Add(TeamTypesWindow);

            TriggersWindow = new TriggersWindow(windowParentControl.WindowManager, map, editorState, cursorActionTarget);
            Windows.Add(TriggersWindow);

            AITriggersWindow = new AITriggersWindow(windowParentControl.WindowManager, map);
            Windows.Add(AITriggersWindow);

            PlaceWaypointWindow = new PlaceWaypointWindow(windowParentControl.WindowManager, map, cursorActionTarget.MutationManager, cursorActionTarget.MutationTarget);
            Windows.Add(PlaceWaypointWindow);

            LocalVariablesWindow = new LocalVariablesWindow(windowParentControl.WindowManager, map);
            Windows.Add(LocalVariablesWindow);

            StructureOptionsWindow = new StructureOptionsWindow(windowParentControl.WindowManager, map);
            Windows.Add(StructureOptionsWindow);

            VehicleOptionsWindow = new VehicleOptionsWindow(windowParentControl.WindowManager, map, editorState, cursorActionTarget);
            Windows.Add(VehicleOptionsWindow);

            InfantryOptionsWindow = new InfantryOptionsWindow(windowParentControl.WindowManager, map);
            Windows.Add(InfantryOptionsWindow);

            AircraftOptionsWindow = new AircraftOptionsWindow(windowParentControl.WindowManager, map);
            Windows.Add(AircraftOptionsWindow);

            HousesWindow = new HousesWindow(windowParentControl.WindowManager, map);
            Windows.Add(HousesWindow);

            SaveMapAsWindow = new SaveMapAsWindow(windowParentControl.WindowManager, map);
            Windows.Add(SaveMapAsWindow);

            CreateNewMapWindow = new CreateNewMapWindow(windowParentControl.WindowManager, true);
            Windows.Add(CreateNewMapWindow);

            OpenMapWindow = new OpenMapWindow(windowParentControl.WindowManager);
            Windows.Add(OpenMapWindow);

            AutoApplyImpassableOverlayWindow = new AutoApplyImpassableOverlayWindow(windowParentControl.WindowManager, map, cursorActionTarget.MutationTarget);
            Windows.Add(AutoApplyImpassableOverlayWindow);

            TerrainGeneratorConfigWindow = new TerrainGeneratorConfigWindow(windowParentControl.WindowManager, map);
            Windows.Add(TerrainGeneratorConfigWindow);

            MinimapWindow = new MegamapWindow(windowParentControl.WindowManager, cursorActionTarget, true);
            Windows.Add(MinimapWindow);

            CopiedEntryTypesWindow = new CopiedEntryTypesWindow(windowParentControl.WindowManager);
            Windows.Add(CopiedEntryTypesWindow);

            LightingSettingsWindow = new LightingSettingsWindow(windowParentControl.WindowManager, map, editorState);
            Windows.Add(LightingSettingsWindow);

            ApplyINICodeWindow = new ApplyINICodeWindow(windowParentControl.WindowManager, map);
            Windows.Add(ApplyINICodeWindow);

            RunScriptWindow = new RunScriptWindow(windowParentControl.WindowManager, map);
            Windows.Add(RunScriptWindow);

            HotkeyConfigurationWindow = new HotkeyConfigurationWindow(windowParentControl.WindowManager);
            Windows.Add(HotkeyConfigurationWindow);

            MapSizeWindow = new MapSizeWindow(windowParentControl.WindowManager, map);
            Windows.Add(MapSizeWindow);
            MapSizeWindow.OnResizeMapButtonClicked += MapSizeWindow_OnResizeMapButtonClicked;

            ExpandMapWindow = new ExpandMapWindow(windowParentControl.WindowManager, map);
            Windows.Add(ExpandMapWindow);

            ChangeHeightWindow = new ChangeHeightWindow(windowParentControl.WindowManager, map);
            Windows.Add(ChangeHeightWindow);

            FindWaypointWindow = new FindWaypointWindow(windowParentControl.WindowManager, map, cursorActionTarget);
            Windows.Add(FindWaypointWindow);

            DeletionModeConfigurationWindow = new DeletionModeConfigurationWindow(windowParentControl.WindowManager, editorState);
            Windows.Add(DeletionModeConfigurationWindow);

            RenderedObjectsConfigurationWindow = new RenderedObjectsConfigurationWindow(windowParentControl.WindowManager, editorState);
            Windows.Add(RenderedObjectsConfigurationWindow);

            ConfigureAlliesWindow = new ConfigureAlliesWindow(windowParentControl.WindowManager, map);
            Windows.Add(ConfigureAlliesWindow);

            SelectConnectedTileWindow = new SelectConnectedTileWindow(windowParentControl.WindowManager, map);
            // TODO add a way for WindowController windows to use DarkeningPanels
            // DarkeningPanel.InitializeAndAddToParentControlWithChild(windowParentControl.WindowManager, windowParentControl, SelectConnectedTileWindow);
            Windows.Add(SelectConnectedTileWindow);

            AboutWindow = new AboutWindow(windowParentControl.WindowManager);
            Windows.Add(AboutWindow);

            TeamTypesWindow.TaskForceOpened += TeamTypesWindow_TaskForceOpened;
            TeamTypesWindow.ScriptOpened += TeamTypesWindow_ScriptOpened;
            TeamTypesWindow.TagOpened += Window_TagOpened;
            AITriggersWindow.TeamTypeOpened += AITriggersWindow_TeamTypeOpened;
            StructureOptionsWindow.TagOpened += Window_TagOpened;
            VehicleOptionsWindow.TagOpened += Window_TagOpened;
            InfantryOptionsWindow.TagOpened += Window_TagOpened;
            AircraftOptionsWindow.TagOpened += Window_TagOpened;

            foreach (var window in Windows)
            {
                window.DrawOrder = ChildWindowOrderValue;
                window.UpdateOrder = ChildWindowOrderValue;
                window.LeftClick += Window_HandleFocusSwitch;
                window.InteractedWith += Window_HandleFocusSwitch;
                windowParentControl.AddChild(window);

                AddFocusSwitchHandlerToChildrenRecursive(window, window);

                window.Disable();
                window.CenterOnParent();
            }

            this.windowParentControl = windowParentControl;

            Initialized?.Invoke(this, EventArgs.Empty);

            windowParentControl.RenderResolutionChanged += (s, e) => RenderResolutionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Window_TagOpened(object sender, TagEventArgs e)
        {
            if (e.Tag.Trigger == null)
            {
                EditorMessageBox.Show(windowParentControl.WindowManager, "No trigger attached",
                    "The specified Tag has no attached Trigger!", MessageBoxButtons.OK);

                return;
            }

            TriggersWindow.Open();
            TriggersWindow.SelectTrigger(e.Tag.Trigger);
        }

        private void MapSizeWindow_OnResizeMapButtonClicked(object sender, EventArgs e)
        {
            ExpandMapWindow.Open();
        }

        private void AddFocusSwitchHandlerToChildrenRecursive(EditorWindow window, XNAControl control)
        {
            EventHandler eventHandler = (s, e) => Window_HandleFocusSwitch(window, EventArgs.Empty);
            window.FocusSwitchEventHandler = eventHandler;

            foreach (var child in control.Children)
            {
                child.MouseLeftDown += eventHandler;
                child.LeftClick += eventHandler;
                AddFocusSwitchHandlerToChildrenRecursive(window, child);
            }
        }

        private void TeamTypesWindow_TaskForceOpened(object sender, TaskForceEventArgs e)
        {
            TaskForcesWindow.Open();
            TaskForcesWindow.SelectTaskForce(e.TaskForce);
        }

        private void TeamTypesWindow_ScriptOpened(object sender, ScriptEventArgs e)
        {
            ScriptsWindow.Open();
            ScriptsWindow.SelectScript(e.Script);
        }

        private void AITriggersWindow_TeamTypeOpened(object sender, TeamTypeEventArgs e)
        {
            TeamTypesWindow.Open();
            TeamTypesWindow.SelectTeamType(e.TeamType);
        }

        private void ClearFocusSwitchHandlerFromChildrenRecursive(EditorWindow window, XNAControl control)
        {
            foreach (var child in control.Children)
            {
                child.MouseLeftDown -= window.FocusSwitchEventHandler;
                child.LeftClick -= window.FocusSwitchEventHandler;
                ClearFocusSwitchHandlerFromChildrenRecursive(window, child);
            }

            window.FocusSwitchEventHandler = null;
        }

        public void Clear()
        {
            TeamTypesWindow.TaskForceOpened -= TeamTypesWindow_TaskForceOpened;
            TeamTypesWindow.ScriptOpened -= TeamTypesWindow_ScriptOpened;
            TeamTypesWindow.TagOpened -= Window_TagOpened;
            AITriggersWindow.TeamTypeOpened -= AITriggersWindow_TeamTypeOpened;
            StructureOptionsWindow.TagOpened -= Window_TagOpened;
            VehicleOptionsWindow.TagOpened -= Window_TagOpened;
            InfantryOptionsWindow.TagOpened -= Window_TagOpened;
            MapSizeWindow.OnResizeMapButtonClicked -= MapSizeWindow_OnResizeMapButtonClicked;

            foreach (var window in Windows)
            {
                window.LeftClick -= Window_HandleFocusSwitch;
                window.InteractedWith -= Window_HandleFocusSwitch;
                windowParentControl.RemoveChild(window);

                ClearFocusSwitchHandlerFromChildrenRecursive(window, window);

                window.Kill();
            }

            Windows.Clear();

            var properties = GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.PropertyType.IsAssignableTo(typeof(EditorWindow)))
                {
                    property.SetValue(this, null, BindingFlags.SetProperty | BindingFlags.NonPublic, null, null, null);
                }
            }

            foregroundWindow = null;

            windowParentControl = null;
        }
    }
}
