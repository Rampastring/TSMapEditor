using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.IO;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Initialization;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.Extensions;

namespace TSMapEditor.UI.Windows.MainMenuWindows
{
    /// <summary>
    /// Helper class for setting up a map.
    /// </summary>
    public static class MapSetup
    {
        private static Map LoadedMap;
        private static CCFileManager ccFileManager = new();

        /// <summary>
        /// Tries to load a map. If successful, returns null. If loading the map
        /// fails, returns an error message.
        /// </summary>
        /// <param name="gameDirectory">The path to the game directory.</param>
        /// <param name="createNew">Whether a new map should be created (instead of loading an existing map).</param>
        /// <param name="existingMapPath">The path to the existing map file to load, if loading an existing map. Can be null if creating a new map.</param>
        /// <param name="newMapTheater">The theater of the map, if creating a new map.</param>
        /// <param name="newMapSize">The size of the map, if creating a new map.</param>
        /// <param name="windowManager">The XNAUI window manager.</param>
        /// <returns>Null of loading the map was successful, otherwise an error message.</returns>
        public static string InitializeMap(string gameDirectory, bool createNew, string existingMapPath, string newMapTheater, Point2D newMapSize, WindowManager windowManager)
        {
            ccFileManager.GameDirectory = gameDirectory;
            ccFileManager.ReadConfig();

            IniFileEx rulesIni = IniFileEx.FromPathOrMix(Constants.RulesIniPath, gameDirectory, ccFileManager);
            IniFileEx firestormIni = IniFileEx.FromPathOrMix(Constants.FirestormIniPath, gameDirectory, ccFileManager);
            IniFileEx artIni = IniFileEx.FromPathOrMix(Constants.ArtIniPath, gameDirectory, ccFileManager);
            IniFileEx artFSIni = IniFileEx.FromPathOrMix(Constants.FirestormArtIniPath, gameDirectory, ccFileManager);
            IniFile artOverridesIni = new(Path.Combine(Environment.CurrentDirectory, "Config/ArtOverrides.ini"));
            IniFile.ConsolidateIniFiles(artFSIni, artOverridesIni);

            var tutorialLines = new TutorialLines(Path.Combine(gameDirectory, Constants.TutorialIniPath), a => windowManager.AddCallback(a, null));
            var themes = new Themes(IniFileEx.FromPathOrMix(Constants.ThemeIniPath, gameDirectory, ccFileManager));

            Map map = new Map();

            if (createNew)
            {
                map.InitNew(rulesIni, firestormIni, artIni, artFSIni, newMapTheater, newMapSize);
            }
            else
            {
                try
                {
                    IniFile mapIni = new IniFile(Path.Combine(gameDirectory, existingMapPath));

                    MapLoader.PreCheckMapIni(mapIni);

                    map.LoadExisting(rulesIni, firestormIni, artIni, artFSIni, mapIni);
                }
                catch (IniParseException ex)
                {
                    return "The selected file does not appear to be a proper map file (INI file). Maybe it's corrupted?\r\n\r\nReturned error: " + ex.Message;
                }
                catch (MapLoadException ex)
                {
                    return "Failed to load the selected map file.\r\n\r\nReturned error: " + ex.Message;
                }
            }

            map.Rules.TutorialLines = tutorialLines;
            map.Rules.Themes = themes;

            Console.WriteLine();
            Console.WriteLine("Map created.");

            LoadedMap = map;

            return null;
        }

        /// <summary>
        /// Loads the theater graphics for the last-loaded map.
        /// </summary>
        /// <param name="windowManager">The window manager.</param>
        /// <param name="gameDirectory">The path to the game directory.</param>
        public static void LoadTheaterGraphics(WindowManager windowManager, string gameDirectory)
        {
            Theater theater = LoadedMap.EditorConfig.Theaters.Find(t => t.UIName.Equals(LoadedMap.TheaterName, StringComparison.InvariantCultureIgnoreCase));
            if (theater == null)
            {
                throw new InvalidOperationException("Theater of map not found: " + LoadedMap.TheaterName);
            }
            theater.ReadConfigINI(gameDirectory, ccFileManager);

            foreach (string theaterMIXName in theater.ContentMIXName)
                ccFileManager.LoadPrimaryMixFile(theaterMIXName);

            TheaterGraphics theaterGraphics = new TheaterGraphics(windowManager.GraphicsDevice, theater, ccFileManager, LoadedMap.Rules);
            LoadedMap.TheaterInstance = theaterGraphics;
            MapLoader.PostCheckMap(LoadedMap, theaterGraphics);

            EditorGraphics editorGraphics = new EditorGraphics();

            var uiManager = new UIManager(windowManager, LoadedMap, theaterGraphics, editorGraphics);
            windowManager.AddAndInitializeControl(uiManager);

            const int margin = 60;
            string errorList = string.Join("\r\n\r\n", MapLoader.MapLoadErrors);
            int errorListHeight = (int)Renderer.GetTextDimensions(errorList, Constants.UIDefaultFont).Y;

            if (errorListHeight > windowManager.RenderResolutionY - margin)
            {
                EditorMessageBox.Show(windowManager, "Errors while loading map",
                    "A massive number of errors was encountered while loading the map. See MapEditorLog.log for details.", MessageBoxButtons.OK);
            }
            else if (MapLoader.MapLoadErrors.Count > 0)
            {
                EditorMessageBox.Show(windowManager, "Errors while loading map",
                    "One of more errors were encountered while loading the map:\r\n\r\n" + errorList, MessageBoxButtons.OK);
            }
        }
    }
}
