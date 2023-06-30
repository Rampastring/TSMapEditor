using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using TSMapEditor.Models;
using TSMapEditor.UI;
using TSMapEditor.Extensions;

namespace TSMapEditor.CCEngine
{
    public class LATGround
    {
        public LATGround(string displayName, TileSet groundTileSet, TileSet transitionTileSet, TileSet baseTileSet)
        {
            DisplayName = displayName;
            GroundTileSet = groundTileSet;
            TransitionTileSet = transitionTileSet;
            BaseTileSet = baseTileSet;
        }

        public string DisplayName { get; }
        public TileSet GroundTileSet { get; }
        public TileSet TransitionTileSet { get; }
        public TileSet BaseTileSet { get; }
    }

    public class TheaterIceTileSets
    {
        public TheaterIceTileSets(Theater theater, IniFile theaterIni)
        {
            int ice1SetId = theaterIni.GetIntValue("General", "Ice1Set", -1);
            int ice2SetId = theaterIni.GetIntValue("General", "Ice2Set", -1);
            int ice3SetId = theaterIni.GetIntValue("General", "Ice3Set", -1);
            int iceShoreSetId = theaterIni.GetIntValue("General", "IceShoreSet", -1);

            Ice1Set = theater.TryGetTileSetById(ice1SetId);
            Ice2Set = theater.TryGetTileSetById(ice2SetId);
            Ice3Set = theater.TryGetTileSetById(ice3SetId);
            IceShoreSet = theater.TryGetTileSetById(iceShoreSetId);
        }

        public TileSet Ice1Set { get; private set; }
        public TileSet Ice2Set { get; private set; }
        public TileSet Ice3Set { get; private set; }
        public TileSet IceShoreSet { get; private set; }
    }

    public class Theater : INIDefineable
    {
        public Theater(string name)
        {
            UIName = name;
        }

        public Theater(string uiName, string configIniName, List<string> contentMixName,
            string paletteName, string unitPaletteName, string fileExtension,
            char newTheaterBuildingLetter)
        {
            UIName = uiName;
            ConfigINIPath = configIniName;
            ContentMIXName = contentMixName;
            TerrainPaletteName = paletteName;
            UnitPaletteName = unitPaletteName;
            FileExtension = fileExtension;
            NewTheaterBuildingLetter = newTheaterBuildingLetter;
        }

        public string UIName { get; }
        public string ConfigINIPath { get; set; }
        public List<string> ContentMIXName { get; set; }
        public string TerrainPaletteName { get; set; }
        public string UnitPaletteName { get; set; }
        public string FileExtension { get; set; }
        public char NewTheaterBuildingLetter { get; set; }

        public TheaterIceTileSets IceTileSetInfo { get; private set; }

        public List<TileSet> TileSets = new List<TileSet>();
        public List<LATGround> LATGrounds = new List<LATGround>();
        public TileSet RampTileSet { get; set; }

        public void ReadConfigINI(string baseDirectoryPath)
        {
            TileSets.Clear();

            string iniPath = Path.Combine(baseDirectoryPath, ConfigINIPath);

            if (!File.Exists(iniPath))
            {
                throw new FileNotFoundException("Theater config INI not found: " + ConfigINIPath);
            }

            var theaterIni = new IniFileEx(iniPath);
            int i;

            for (i = 0; i < 10000; i++)
            {
                IniSection tileSetSection = theaterIni.GetSection(string.Format("TileSet{0:D4}", i));

                if (tileSetSection == null)
                    break;

                TileSet tileSet = new TileSet(i);
                tileSet.Read(tileSetSection);
                TileSets.Add(tileSet);
            }

            IceTileSetInfo = new TheaterIceTileSets(this, theaterIni);

            i = 1;
            while (true)
            {
                if (!InitLATGround(theaterIni, $"Ground{i}Tile", $"Ground{i}Lat", $"Ground{i}Base", $"Ground{i}Name", null))
                    break;

                i++;
            }

            // DTA
            InitLATGround(theaterIni, "PvmntTile", "ClearToPvmntLat", null, null, "Pavement");

            // TS terrain
            InitLATGround(theaterIni, "RoughTile", "ClearToRoughLat", null, null, "Rough");
            InitLATGround(theaterIni, "SandTile", "ClearToSandLat", null, null, "Sand");
            InitLATGround(theaterIni, "PaveTile", "ClearToPaveLat", null, null, "Pavement");
            InitLATGround(theaterIni, "GreenTile", "ClearToGreenLat", null, null, "Green");

            int rampTileSetIndex = theaterIni.GetIntValue("General", "RampBase", -1);
            if (rampTileSetIndex < 0 || rampTileSetIndex >= TileSets.Count)
            {
                throw new INIConfigException("Invalid value specified for RampBase= in the theater configuration file!");
            }

            RampTileSet = TileSets[rampTileSetIndex];
        }

        public TileSet TryGetTileSetById(int id)
        {
            if (id < 0 || id >= TileSets.Count)
                return null;

            return TileSets[id];
        }

        private bool InitLATGround(IniFile theaterIni, string tileSetKey, string transitionTileSetKey, string baseTileSetKey, string nameKey, string defaultName)
        {
            int groundTileSetIndex = theaterIni.GetIntValue("General", tileSetKey, -1);
            int transitionTileSetIndex = theaterIni.GetIntValue("General", transitionTileSetKey, -1);

            int baseTileSetIndex = -1;
            if (!string.IsNullOrEmpty(baseTileSetKey))
                baseTileSetIndex = theaterIni.GetIntValue("General", baseTileSetKey, -1);

            if (groundTileSetIndex < 0 || transitionTileSetIndex < 0)
                return false;

            if (groundTileSetIndex >= TileSets.Count || transitionTileSetIndex >= TileSets.Count)
                return false;

            string displayName = defaultName;
            if (!string.IsNullOrEmpty(nameKey))
                displayName = theaterIni.GetStringValue("General", nameKey, displayName);

            if (displayName == null)
            {
                string groundTileSetName = TileSets[groundTileSetIndex].SetName;
                displayName = groundTileSetName.Substring(0, Math.Min(groundTileSetName.Length, 4));
            }

            LATGrounds.Add(new LATGround(
                displayName,
                TileSets[groundTileSetIndex],
                TileSets[transitionTileSetIndex],
                baseTileSetIndex > -1 ? TileSets[baseTileSetIndex] : TileSets[0]));

            return true;
        }
    }
}
