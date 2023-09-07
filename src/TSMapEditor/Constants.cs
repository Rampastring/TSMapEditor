using Rampastring.Tools;
using System;

namespace TSMapEditor
{
    public static class Constants
    {
        public static int CellSizeX = 48;
        public static int CellSizeY = 24;
        public static int CellHeight => CellSizeY / 2;
        public static int TileColorBufferSize = 576;

        public static int RenderPixelPadding = 50;

        public static bool IsFlatWorld = false;
        public static bool TheaterPaletteForTiberium = false;
        public static bool NewTheaterGenericBuilding = false;
        public static bool DrawBuildingAnimationShadows = false;

        public static string ExpectedClientExecutableName = "DTA.exe";
        public static string GameRegistryInstallPath = "SOFTWARE\\DawnOfTheTiberiumAge";
        public static string OpenFileDialogFilter = "TS maps|*.map|All files|*.*";

        public static bool EnableIniInclude = false;
        public static bool EnableIniInheritance = false;

        public static bool IntegerVariables = false;

        public static string RulesIniPath;
        public static string FirestormIniPath;
        public static string ArtIniPath;
        public static string FirestormArtIniPath;
        public static string TutorialIniPath;
        public static string ThemeIniPath;

        public const int TextureSizeLimit = 16384;

        public static int MaxMapWidth;
        public static int MaxMapHeight;

        public const int MaxMapHeightLevel = 14;

        public static int MaxWaypoint = 100;

        public const int ObjectHealthMax = 256;
        public const int FacingMax = 255;

        public const int TurretFrameCount = 32;

        // TODO parse from Rules.ini
        public const int ConditionYellowHP = 128;

        public const int UIEmptySideSpace = 10;
        public const int UIEmptyTopSpace = 10;
        public const int UIEmptyBottomSpace = 10;

        public const int UIHorizontalSpacing = 6;
        public const int UIVerticalSpacing = 6;

        public const int UIDefaultFont = 0;
        public const int UIBoldFont = 1;

        public const int UITextBoxHeight = 21;
        public const int UIButtonHeight = 23;

        public const int UITopBarMenuHeight = 23;

        public const int MAX_MAP_LENGTH_IN_DIMENSION = 512;
        public const int NO_OVERLAY = 255; // 0xFF
        public const int OverlayPackFormat = 80;

        public const string NoneValue1 = "<none>";
        public const string NoneValue2 = "None";

        public const bool HQRemap = true;
        public const float RemapBrightenFactor = 1.25f;

        public const bool DrawShadows = true;

        public const string ClipboardMapDataFormatValue = "ScenarioEditorCopiedMapData";
        public const string UserDataFolder = "UserData";

        public const char NewTheaterGenericLetter = 'G';

        public static void Init()
        {
            const string ConstantsSectionName = "Constants";
            const string FilePathsSectionName = "FilePaths";

            IniFile constantsIni = new IniFile(Environment.CurrentDirectory + "/Config/Constants.ini");

            CellSizeX = constantsIni.GetIntValue(ConstantsSectionName, nameof(CellSizeX), CellSizeX);
            MaxMapWidth = TextureSizeLimit / CellSizeX;
            CellSizeY = constantsIni.GetIntValue(ConstantsSectionName, nameof(CellSizeY), CellSizeY);
            MaxMapHeight = TextureSizeLimit / CellSizeY;

            TileColorBufferSize = constantsIni.GetIntValue(ConstantsSectionName, nameof(TileColorBufferSize), TileColorBufferSize);

            RenderPixelPadding = constantsIni.GetIntValue(ConstantsSectionName, nameof(RenderPixelPadding), RenderPixelPadding);

            IsFlatWorld = constantsIni.GetBooleanValue(ConstantsSectionName, nameof(IsFlatWorld), IsFlatWorld);
            TheaterPaletteForTiberium = constantsIni.GetBooleanValue(ConstantsSectionName, nameof(TheaterPaletteForTiberium), TheaterPaletteForTiberium);
            NewTheaterGenericBuilding = constantsIni.GetBooleanValue(ConstantsSectionName, nameof(NewTheaterGenericBuilding), NewTheaterGenericBuilding);
            DrawBuildingAnimationShadows = constantsIni.GetBooleanValue(ConstantsSectionName, nameof(DrawBuildingAnimationShadows), DrawBuildingAnimationShadows);

            ExpectedClientExecutableName = constantsIni.GetStringValue(ConstantsSectionName, nameof(ExpectedClientExecutableName), ExpectedClientExecutableName);
            GameRegistryInstallPath = constantsIni.GetStringValue(ConstantsSectionName, nameof(GameRegistryInstallPath), GameRegistryInstallPath);
            OpenFileDialogFilter = constantsIni.GetStringValue(ConstantsSectionName, nameof(OpenFileDialogFilter), OpenFileDialogFilter);

            EnableIniInclude = constantsIni.GetBooleanValue(ConstantsSectionName, nameof(EnableIniInclude), EnableIniInclude);
            EnableIniInheritance = constantsIni.GetBooleanValue(ConstantsSectionName, nameof(EnableIniInheritance), EnableIniInheritance);

            IntegerVariables = constantsIni.GetBooleanValue(ConstantsSectionName, nameof(IntegerVariables), IntegerVariables);

            MaxWaypoint = constantsIni.GetIntValue(ConstantsSectionName, nameof(MaxWaypoint), MaxWaypoint);

            RulesIniPath = constantsIni.GetStringValue(FilePathsSectionName, "Rules", "INI/Rules.ini");
            FirestormIniPath = constantsIni.GetStringValue(FilePathsSectionName, "Firestorm", "INI/Enhance.ini");
            ArtIniPath = constantsIni.GetStringValue(FilePathsSectionName, "Art", "INI/Art.ini");
            FirestormArtIniPath = constantsIni.GetStringValue(FilePathsSectionName, "ArtFS", "INI/ArtE.ini");
            TutorialIniPath = constantsIni.GetStringValue(FilePathsSectionName, "Tutorial", "INI/Tutorial.ini");
            ThemeIniPath = constantsIni.GetStringValue(FilePathsSectionName, "Theme", "INI/Theme.ini");
        }
    }
}
