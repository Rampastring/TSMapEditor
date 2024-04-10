using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.IO;
using TSMapEditor.CCEngine;
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
        private static CCFileManager ccFileManager;

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
        public static string InitializeMap(string gameDirectory, bool createNew, string existingMapPath, CreateNewMapEventArgs newMapParameters, WindowManager windowManager)
        {
            ccFileManager = new() { GameDirectory = gameDirectory };
            ccFileManager.ReadConfig();

            var gameConfigIniFiles = new GameConfigINIFiles(gameDirectory, ccFileManager);

            var tutorialLines = new TutorialLines(Path.Combine(gameDirectory, Constants.TutorialIniPath), a => windowManager.AddCallback(a, null));
            var themes = new Themes(IniFileEx.FromPathOrMix(Constants.ThemeIniPath, gameDirectory, ccFileManager));

            Map map = new Map(ccFileManager);

            if (createNew)
            {
                if (newMapParameters == null)
                    throw new NullReferenceException("Null new map parameters encountered when creating a new map!");

                map.InitNew(gameConfigIniFiles, newMapParameters.Theater, newMapParameters.MapSize, newMapParameters.StartingLevel);
            }
            else
            {
                try
                {
                    IniFileEx mapIni = new(Path.Combine(gameDirectory, existingMapPath), ccFileManager);

                    MapLoader.PreCheckMapIni(mapIni);

                    map.LoadExisting(gameConfigIniFiles, mapIni);
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

            foreach (string theaterMIXName in theater.OptionalContentMIXName)
                ccFileManager.LoadSecondaryMixFile(theaterMIXName);

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
                    "One or more errors were encountered while loading the map:\r\n\r\n" + errorList, MessageBoxButtons.OK);
            }
        }
    }
}
