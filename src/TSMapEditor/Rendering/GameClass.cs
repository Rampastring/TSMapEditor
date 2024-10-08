using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using TSMapEditor.CCEngine;
using TSMapEditor.Settings;
using TSMapEditor.UI;

#if !DEBUG
using System.Windows.Forms;
#endif

namespace TSMapEditor.Rendering
{
    public class GameClass : Microsoft.Xna.Framework.Game
    {
        private const double PowerSavingTime = 100.0;

        private GraphicsDeviceManager graphics;

        public GameClass()
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += (s, e) => HandleUnhandledException((Exception)e.ExceptionObject);
            Application.ThreadException += (s, e) => HandleUnhandledException(e.Exception);
#endif
            Program.DisableExceptionHandler();

            Logger.WriteToConsole = true;
            Logger.WriteLogFile = true;
            Logger.Initialize(Environment.CurrentDirectory + "/", "MapEditorLog.log");

            try
            {
                File.Delete(Environment.CurrentDirectory + "/MapEditorLog.log");
            }
            catch (IOException ex)
            {
                Logger.Log("Failed to delete log file! Returned error: " + ex.Message);
            }

            Logger.Log("C&C World-Altering Editor (WAE) build " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Logger.Log("Release version: " + Constants.ReleaseVersion);

            AutoLATType.InitArray();

            Constants.Init();
            new UserSettings();

            graphics = new GraphicsDeviceManager(this);
            graphics.HardwareModeSwitch = false;
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            Content.RootDirectory = "Content";
            graphics.SynchronizeWithVerticalRetrace = false;
            Window.Title = "C&C World-Altering Editor (WAE)";

            //IsFixedTimeStep = false;
            SetTargetFPS();
        }

        private void HandleUnhandledException(Exception ex)
        {
            string exceptLogPath = Environment.CurrentDirectory + DSC + "except.txt";
            File.Delete(exceptLogPath);

            StringBuilder sb = new StringBuilder();

            string fullName = typeof(GameClass).Assembly.FullName;

            LogLineGenerate("World-Altering Editor (" + fullName + ")", sb, exceptLogPath);
            LogLineGenerate("Release version: " + Constants.ReleaseVersion, sb, exceptLogPath);
            LogLineGenerate("Unhandled exception! @ " + DateTime.Now.ToLongTimeString(), sb, exceptLogPath);
            LogLineGenerate("Message: " + ex.Message, sb, exceptLogPath);
            LogLineGenerate("Stack trace: " + ex.StackTrace, sb, exceptLogPath);

            if (ex.InnerException != null)
            {
                LogLineGenerate("***************************", sb, exceptLogPath);
                LogLineGenerate("InnerException information:", sb, exceptLogPath);
                LogLineGenerate("Message: " + ex.InnerException.Message, sb, exceptLogPath);
                LogLineGenerate("Stack trace: " + ex.InnerException.StackTrace, sb, exceptLogPath);
            }

            Logger.Log("Exiting.");

            windowManager?.HideWindow();
            System.Windows.Forms.MessageBox.Show("The map editor has crashed." + Environment.NewLine + Environment.NewLine +
                "Exception information logged into except.txt:" + Environment.NewLine + Environment.NewLine +
                sb.ToString());

            Environment.Exit(255);
        }

        private void LogLineGenerate(string text, StringBuilder sb, string exceptLogPath)
        {
            sb.Append(text + Environment.NewLine);
            Logger.ForceLog(text, exceptLogPath);
        }

        private WindowManager windowManager;

        private bool wasActiveOnPreviousFrame;

        private readonly char DSC = Path.DirectorySeparatorChar;

        protected override void Initialize()
        {
            base.Initialize();

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            AssetLoader.Initialize(GraphicsDevice, Content);
            AssetLoader.AssetSearchPaths.Add(Environment.CurrentDirectory + DSC + "Content" + DSC);

            windowManager = new WindowManager(this, graphics);
            windowManager.Initialize(Content, Environment.CurrentDirectory + DSC + "Content" + DSC);

            new Parser(windowManager);

            const int menuRenderWidth = 800;
            const int menuRenderHeight = 600;

            int menuWidth = menuRenderWidth;
            int menuHeight = menuRenderHeight;

            int dpi = NativeMethods.GetScreenDPI();
            double dpi_ratio = dpi / 96.0;

            menuWidth = (int)(menuWidth * dpi_ratio);
            menuHeight = (int)(menuHeight * dpi_ratio);

            windowManager.InitGraphicsMode(menuWidth, menuHeight, false);

            windowManager.SetRenderResolution(menuRenderWidth, menuRenderHeight);
            windowManager.CenterOnScreen();
            windowManager.Cursor.LoadNativeCursor(Environment.CurrentDirectory + DSC + "Content" + DSC + "cursor.cur");
            windowManager.SetBorderlessMode(false);

            Components.Add(windowManager);

            EditorThemes.Initialize();
            UISettings.ActiveSettings = new CustomUISettings()
            {
                PanelBackgroundColor = new Color(32, 32, 32),
                PanelBorderColor = new Color(196, 196, 196)
            };

            InitMainMenu();
        }

        private void InitMainMenu()
        {
            var mainMenu = new MainMenu(windowManager);
            windowManager.AddAndInitializeControl(mainMenu);
            windowManager.CenterControlOnScreen(mainMenu);
        }

        private void SetTargetFPS()
        {
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / UserSettings.Instance.TargetFPS);
        }

        protected override void Update(GameTime gameTime)
        {
            if (IsActive != wasActiveOnPreviousFrame)
            {
                if (!IsActive)
                    TargetElapsedTime = TimeSpan.FromMilliseconds(PowerSavingTime); // Don't run at max FPS if we're not the active window
                else
                    SetTargetFPS();

                wasActiveOnPreviousFrame = IsActive;
            }

            base.Update(gameTime);
        }
    }
}
