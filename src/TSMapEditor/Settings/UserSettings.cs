using Rampastring.Tools;
using System;
using System.Threading.Tasks;

namespace TSMapEditor.Settings
{
    public class UserSettings
    {
        private const string General = "General";
        private const string Display = "Display";
        private const string MapView = "MapView";

        public UserSettings()
        {
            if (Instance != null)
                throw new InvalidOperationException("User settings can only be initialized once.");

            Instance = this;

            UserSettingsIni = new IniFile(Environment.CurrentDirectory + "/MapEditorSettings.ini");

            settings = new IINILoadable[]
            {
                TargetFPS,
                GraphicsLevel,
                ResolutionWidth,
                ResolutionHeight,
                RenderScale,
                Borderless,
                FullscreenWindowed,

                ScrollRate,
                MapWideOverlayOpacity,

                Theme,
                UseBoldFont,
                AutoSaveInterval,
                SidebarWidth,

                MultithreadedTextureLoading,

                GameDirectory,
                LastScenarioPath,

                TextEditorPath
            };

            foreach (var setting in settings)
                setting.LoadValue(UserSettingsIni);
        }

        public IniFile UserSettingsIni { get; }

        public void SaveSettings()
        {
            foreach (var setting in settings)
            {
                setting.WriteValue(UserSettingsIni, false);
            }

            UserSettingsIni.WriteIniFile();
        }

        public async Task SaveSettingsAsync()
        {
            await Task.Factory.StartNew(SaveSettings);
        }

        public static UserSettings Instance { get; private set; }

        private readonly IINILoadable[] settings;

        public IntSetting TargetFPS = new IntSetting(Display, "TargetFPS", 240);
        public IntSetting GraphicsLevel = new IntSetting(Display, nameof(GraphicsLevel), 1);
        public IntSetting ResolutionWidth = new IntSetting(Display, "ResolutionWidth", -1);
        public IntSetting ResolutionHeight = new IntSetting(Display, "ResolutionHeight", -1);
        public DoubleSetting RenderScale = new DoubleSetting(Display, "RenderScale", 1.0);
        public BoolSetting Borderless = new BoolSetting(Display, "Borderless", false);
        public BoolSetting FullscreenWindowed = new BoolSetting(Display, "FullscreenWindowed", false);

        public IntSetting ScrollRate = new IntSetting(MapView, nameof(ScrollRate), 15);
        public IntSetting MapWideOverlayOpacity = new IntSetting(MapView, "MapWideOverlayOpacity", 50);

        public StringSetting Theme = new StringSetting(General, "Theme", "Default");
        public BoolSetting UseBoldFont = new BoolSetting(General, "UseBoldFont", false);
        public IntSetting AutoSaveInterval = new IntSetting(General, "AutoSaveInterval", 300);
        public IntSetting SidebarWidth = new IntSetting(General, "SidebarWidth", 250);

        public BoolSetting MultithreadedTextureLoading = new BoolSetting(General, "MultithreadedTextureLoading", true);

        public StringSetting GameDirectory = new StringSetting(General, "GameDirectory", string.Empty);
        public StringSetting LastScenarioPath = new StringSetting(General, nameof(LastScenarioPath), "Maps/Custom/");

        public StringSetting TextEditorPath = new StringSetting(General, "TextEditorPath", string.Empty);
    }
}
