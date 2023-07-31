using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using System.Collections.Generic;
using TSMapEditor.Settings;

namespace TSMapEditor.UI
{
    public class KeyboardCommands
    {
        public KeyboardCommands()
        {
            Commands = new List<KeyboardCommand>()
            {
                Undo,
                Redo,
                Save,
                ConfigureCopiedObjects,
                Copy,
                Paste,
                NextTile,
                PreviousTile,
                NextTileSet,
                PreviousTileSet,
                NextSidebarNode,
                PreviousSidebarNode,
                FrameworkMode,
                NextBrushSize,
                PreviousBrushSize,
                DeleteObject,
                ToggleAutoLAT,
                ToggleMapWideOverlay,
                RotateUnit,
                RotateUnitOneStep,
                PlaceTerrainBelow,
                FillTerrain,
                ViewMegamap,
                GenerateTerrain,
                ConfigureTerrainGenerator,
                PlaceTunnel,

                AircraftMenu,
                BuildingMenu,
                VehicleMenu,
                NavalMenu,
                InfantryMenu,
                TerrainObjectMenu,
                OverlayMenu,
                SmudgeMenu
            };
        }

        public void ReadFromSettings()
        {
            IniFile iniFile = UserSettings.Instance.UserSettingsIni;

            foreach (var command in Commands)
            {
                string dataString = iniFile.GetStringValue("Keybinds", command.ININame, null);
                if (string.IsNullOrWhiteSpace(dataString))
                    continue;

                command.Key.ApplyDataString(dataString);
            }
        }

        public void WriteToSettings()
        {
            IniFile iniFile = UserSettings.Instance.UserSettingsIni;

            foreach (var command in Commands)
            {
                iniFile.SetStringValue("Keybinds", command.ININame, command.Key.GetDataString());
            }
        }


        public static KeyboardCommands Instance { get; set; }

        public List<KeyboardCommand> Commands { get; }

        public KeyboardCommand Undo { get; } = new KeyboardCommand("Undo", "Undo", new KeyboardCommandInput(Keys.Z, KeyboardModifiers.Ctrl));
        public KeyboardCommand Redo { get; } = new KeyboardCommand("Redo", "Redo", new KeyboardCommandInput(Keys.Y, KeyboardModifiers.Ctrl));
        public KeyboardCommand Save { get; } = new KeyboardCommand("Save", "Save Map", new KeyboardCommandInput(Keys.S, KeyboardModifiers.Ctrl));
        public KeyboardCommand ConfigureCopiedObjects { get; } = new KeyboardCommand("ConfigureCopiedObjects", "Configure Copied Objects", new KeyboardCommandInput(Keys.None, KeyboardModifiers.None), false);
        public KeyboardCommand Copy { get; } = new KeyboardCommand("Copy", "Copy", new KeyboardCommandInput(Keys.C, KeyboardModifiers.Ctrl));
        public KeyboardCommand Paste { get; } = new KeyboardCommand("Paste", "Paste", new KeyboardCommandInput(Keys.V, KeyboardModifiers.Ctrl));
        public KeyboardCommand NextTile { get; } = new KeyboardCommand("NextTile", "Select Next Tile", new KeyboardCommandInput(Keys.M, KeyboardModifiers.None));
        public KeyboardCommand PreviousTile { get; } = new KeyboardCommand("PreviousTile", "Select Previous Tile", new KeyboardCommandInput(Keys.N, KeyboardModifiers.None));
        public KeyboardCommand NextTileSet { get; } = new KeyboardCommand("NextTileSet", "Select Next TileSet", new KeyboardCommandInput(Keys.J, KeyboardModifiers.None));
        public KeyboardCommand PreviousTileSet { get; } = new KeyboardCommand("PreviousTileSet", "Select Previous TileSet", new KeyboardCommandInput(Keys.H, KeyboardModifiers.None));
        public KeyboardCommand NextSidebarNode { get; } = new KeyboardCommand("NextSidebarNode", "Select Next Sidebar Node", new KeyboardCommandInput(Keys.P, KeyboardModifiers.None));
        public KeyboardCommand PreviousSidebarNode { get; } = new KeyboardCommand("PreviousSidebarNode", "Select Previous Sidebar Node", new KeyboardCommandInput(Keys.O, KeyboardModifiers.None));
        public KeyboardCommand FrameworkMode { get; } = new KeyboardCommand("MarbleMadness", "Framework Mode (Marble Madness)", new KeyboardCommandInput(Keys.F, KeyboardModifiers.Shift));
        public KeyboardCommand NextBrushSize { get; } = new KeyboardCommand("NextBrushSize", "Next Brush Size", new KeyboardCommandInput(Keys.OemPlus, KeyboardModifiers.None));
        public KeyboardCommand PreviousBrushSize { get; } = new KeyboardCommand("PreviousBrushSize", "Previous Brush Size", new KeyboardCommandInput(Keys.D0, KeyboardModifiers.None));
        public KeyboardCommand DeleteObject { get; } = new KeyboardCommand("DeleteObject", "Delete Object", new KeyboardCommandInput(Keys.Delete, KeyboardModifiers.None));
        public KeyboardCommand ToggleAutoLAT { get; } = new KeyboardCommand("ToggleAutoLAT", "Toggle AutoLAT", new KeyboardCommandInput(Keys.L, KeyboardModifiers.Ctrl));
        public KeyboardCommand ToggleMapWideOverlay { get; } = new KeyboardCommand("ToggleMapWideOverlay", "Toggle Map-Wide Overlay", new KeyboardCommandInput(Keys.F2, KeyboardModifiers.None));
        public KeyboardCommand RotateUnit { get; } = new KeyboardCommand("RotateUnit", "Rotate Unit", new KeyboardCommandInput(Keys.A, KeyboardModifiers.None));
        public KeyboardCommand RotateUnitOneStep { get; } = new KeyboardCommand("RotateUnitOneStep", "Rotate Object One Step", new KeyboardCommandInput(Keys.A, KeyboardModifiers.Shift));
        public KeyboardCommand PlaceTerrainBelow { get; } = new KeyboardCommand("PlaceTerrainBelow", "Place Terrain Below Cursor", new KeyboardCommandInput(Keys.None, KeyboardModifiers.Alt), true);
        public KeyboardCommand FillTerrain { get; } = new KeyboardCommand("FillTerrain", "Fill Terrain (1x1 tiles only)", new KeyboardCommandInput(Keys.None, KeyboardModifiers.Ctrl), true);
        public KeyboardCommand ViewMegamap { get; } = new KeyboardCommand("ViewMegamap", "View Megamap", new KeyboardCommandInput(Keys.F12, KeyboardModifiers.None));
        public KeyboardCommand GenerateTerrain { get; } = new KeyboardCommand("GenerateTerrain", "Generate Terrain", new KeyboardCommandInput(Keys.G, KeyboardModifiers.Ctrl));
        public KeyboardCommand ConfigureTerrainGenerator { get; } = new KeyboardCommand("ConfigureTerrainGenerator", "Configure Terrain Generator", new KeyboardCommandInput(Keys.G, KeyboardModifiers.Alt));
        public KeyboardCommand PlaceTunnel { get; } = new KeyboardCommand("PlaceTunnel", "Place Tunnel", new KeyboardCommandInput(Keys.OemPeriod, KeyboardModifiers.None));

        public KeyboardCommand AircraftMenu { get; } = new KeyboardCommand("AircraftMenu", "Aircraft Menu", new KeyboardCommandInput(Keys.D1, KeyboardModifiers.None));
        public KeyboardCommand BuildingMenu { get; } = new KeyboardCommand("BuildingMenu", "Building Menu", new KeyboardCommandInput(Keys.D2, KeyboardModifiers.None));
        public KeyboardCommand VehicleMenu { get; } = new KeyboardCommand("VehicleMenu", "Vehicle Menu", new KeyboardCommandInput(Keys.D3, KeyboardModifiers.None));
        public KeyboardCommand NavalMenu { get; } = new KeyboardCommand("NavalMenu", "Naval Menu", new KeyboardCommandInput(Keys.D4, KeyboardModifiers.None));
        public KeyboardCommand InfantryMenu { get; } = new KeyboardCommand("InfantryMenu", "Infantry Menu", new KeyboardCommandInput(Keys.D5, KeyboardModifiers.None));
        public KeyboardCommand TerrainObjectMenu { get; } = new KeyboardCommand("TerrainObjectMenu", "Terrain Objects Menu", new KeyboardCommandInput(Keys.D6, KeyboardModifiers.None));
        public KeyboardCommand OverlayMenu { get; } = new KeyboardCommand("OverlayMenu", "Overlay Menu", new KeyboardCommandInput(Keys.D7, KeyboardModifiers.None));
        public KeyboardCommand SmudgeMenu { get; } = new KeyboardCommand("SmudgeMenu", "Smudge Menu", new KeyboardCommandInput(Keys.D8, KeyboardModifiers.None));

        public Keys SkipConfirmationKey { get; } = Keys.LeftAlt;
    }
}
