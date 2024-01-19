using CNCMaps.FileFormats.Encodings;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.Models.MapFormat;

namespace TSMapEditor.Initialization
{
    /// <summary>
    /// Contains static methods for writing a map to an INI file.
    /// </summary>
    public static class MapWriter
    {
        private static IniSection FindOrMakeSection(string sectionName, IniFile mapIni)
        {
            var section = mapIni.GetSection(sectionName);
            if (section == null)
            {
                section = new IniSection(sectionName);
                mapIni.AddSection(section);
            }

            return section;
        }

        public static void WriteMapSection(IMap map, IniFile mapIni)
        {
            const string sectionName = "Map";

            var section = FindOrMakeSection(sectionName, mapIni);
            section.SetStringValue("Size", $"0,0,{map.Size.X},{map.Size.Y}");
            section.SetStringValue("Theater", map.TheaterName);
            section.SetStringValue("LocalSize", $"{map.LocalSize.X},{map.LocalSize.Y},{map.LocalSize.Width},{map.LocalSize.Height}");
        }

        public static void WriteBasicSection(IMap map, IniFile mapIni)
        {
            const string sectionName = "Basic";

            var section = FindOrMakeSection(sectionName, mapIni);

            // Work-around to a bug we caused earlier
            if (map.Basic.Player == "none")
                map.Basic.Player = null;

            if (string.IsNullOrWhiteSpace(map.Basic.Player))
            {
                map.Basic.MaxPlayer = map.Waypoints.Count(wp => wp.Identifier < 8);
            }

            map.Basic.WritePropertiesToIniSection(section);
        }

        public static void WriteIsoMapPack5(IMap map, IniFile mapIni)
        {
            const string sectionName = "IsoMapPack5";
            mapIni.RemoveSection(sectionName);

            var tilesToSave = new List<IsoMapPack5Tile>();

            for (int y = 0; y < map.Tiles.Length; y++)
            {
                for (int x = 0; x < map.Tiles[y].Length; x++)
                {
                    var tile = map.Tiles[y][x];
                    if (tile == null)
                        continue;

                    if (tile.Level == 0 && tile.TileIndex == 0)
                        continue;

                    tilesToSave.Add(tile);
                }
            }

            // Typically, removing the height level 0 clear tiles and then sorting 
            // the tiles first by X then by Level and then by TileIndex gives good compression. 
            // https://modenc.renegadeprojects.com/IsoMapPack5

            tilesToSave = tilesToSave.OrderBy(t => t.X).ThenBy(t => t.Level).ThenBy(t => t.TileIndex).ToList();

            // Now we pretty much have to reverse the process done in MapLoader.ReadIsoMapPack

            var buffer = new List<byte>();
            foreach (IsoMapPack5Tile tile in tilesToSave)
            {
                buffer.AddRange(BitConverter.GetBytes(tile.X));
                buffer.AddRange(BitConverter.GetBytes(tile.Y));
                buffer.AddRange(BitConverter.GetBytes(tile.TileIndex));
                buffer.Add(tile.SubTileIndex);
                buffer.Add(tile.Level);
                buffer.Add(tile.IceGrowth);
            }

            // Add 4 padding bytes
            buffer.AddRange(new byte[4]);

            const int maxOutputSize = 8192;
            // generate IsoMapPack5 blocks
            int processedBytes = 0;
            List<byte> finalData = new List<byte>();
            List<byte> block = new List<byte>(maxOutputSize);
            while (buffer.Count > processedBytes)
            {
                ushort blockOutputSize = (ushort)Math.Min(buffer.Count - processedBytes, maxOutputSize);
                for (int i = processedBytes; i < processedBytes + blockOutputSize; i++)
                {
                    block.Add(buffer[i]);
                }

                byte[] compressedBlock = MiniLZO.MiniLZO.Compress(block.ToArray());
                // InputSize
                finalData.AddRange(BitConverter.GetBytes((ushort)compressedBlock.Length));
                // OutputSize
                finalData.AddRange(BitConverter.GetBytes(blockOutputSize));
                // actual data
                finalData.AddRange(compressedBlock);

                processedBytes += blockOutputSize;
                block.Clear();
            }

            // Base64 encode
            var section = new IniSection(sectionName);
            mapIni.AddSection(section);
            WriteBase64ToSection(finalData.ToArray(), section);
        }

        /// <summary>
        /// Generic method for writing a byte array as a 
        /// base64-encoded line-length-limited block of data to a INI section.
        /// Used for writing IsoMapPack5, OverlayPack and OverlayDataPack.
        /// </summary>
        private static void WriteBase64ToSection(byte[] data, IniSection section)
        {
            string base64String = Convert.ToBase64String(data.ToArray());
            const int maxIsoMapPackEntryLineLength = 70;
            int lineIndex = 1; // TS/RA2 IsoMapPack5, OverlayPack and OverlayDataPack is indexed starting from 1
            int processedChars = 0;

            while (processedChars < base64String.Length)
            {
                int length = Math.Min(base64String.Length - processedChars, maxIsoMapPackEntryLineLength);

                string substring = base64String.Substring(processedChars, length);
                section.SetStringValue(lineIndex.ToString(), substring);
                lineIndex++;
                processedChars += length;
            }
        }

        public static void WriteOverlays(IMap map, IniFile mapIni)
        {
            const string overlayPackSectionName = "OverlayPack";
            const string overlayDataPackSectionName = "OverlayDataPack";

            mapIni.RemoveSection(overlayPackSectionName);
            mapIni.RemoveSection(overlayDataPackSectionName);

            var overlayArray = new byte[Constants.MAX_MAP_LENGTH_IN_DIMENSION * Constants.MAX_MAP_LENGTH_IN_DIMENSION];
            for (int i = 0; i < overlayArray.Length; i++)
                overlayArray[i] = Constants.NO_OVERLAY;

            var overlayDataArray = new byte[Constants.MAX_MAP_LENGTH_IN_DIMENSION * Constants.MAX_MAP_LENGTH_IN_DIMENSION];

            map.DoForAllValidTiles(tile =>
            {
                if (tile.Overlay == null || tile.Overlay.OverlayType == null)
                    return;

                int dataIndex = (tile.Y * Constants.MAX_MAP_LENGTH_IN_DIMENSION) + tile.X;

                overlayArray[dataIndex] = (byte)tile.Overlay.OverlayType.Index;
                overlayDataArray[dataIndex] = (byte)tile.Overlay.FrameIndex;
            });

            // Format80 compression
            byte[] compressedOverlayArray = Format5.Encode(overlayArray, Constants.OverlayPackFormat);
            byte[] compressedOverlayDataArray = Format5.Encode(overlayDataArray, Constants.OverlayPackFormat);

            // Base64 encode
            var overlayPackSection = new IniSection(overlayPackSectionName);
            mapIni.AddSection(overlayPackSection);
            WriteBase64ToSection(compressedOverlayArray, overlayPackSection);

            var overlayDataPackSection = new IniSection(overlayDataPackSectionName);
            mapIni.AddSection(overlayDataPackSection);
            WriteBase64ToSection(compressedOverlayDataArray, overlayDataPackSection);
        }

        public static void WriteSmudges(IMap map, IniFile mapIni)
        {
            const string sectionName = "Smudge";
            mapIni.RemoveSection(sectionName);

            var smudges = new List<Smudge>();

            map.DoForAllValidTiles(cell =>
            {
                if (cell.Smudge != null)
                    smudges.Add(cell.Smudge);
            });

            if (smudges.Count > 0)
            {
                var section = new IniSection(sectionName);
                
                for (int i = 0; i < smudges.Count; i++)
                {
                    var smudge = smudges[i];
                    section.SetStringValue(i.ToString(CultureInfo.InvariantCulture), $"{smudge.SmudgeType.ININame},{smudge.Position.X},{smudge.Position.Y},0");
                }

                mapIni.AddSection(section);
            }
        }

        public static void WriteTerrainObjects(IMap map, IniFile mapIni)
        {
            const string sectionName = "Terrain";
            mapIni.RemoveSection(sectionName);

            var section = new IniSection(sectionName);
            mapIni.AddSection(section);

            map.DoForAllValidTiles(tile =>
            {
                if (tile.TerrainObject == null)
                    return;

                int tileIndex = tile.Y * 1000 + tile.X;
                section.SetStringValue(tileIndex.ToString(), tile.TerrainObject.TerrainType.ININame);
            });
        }

        public static void WriteWaypoints(IMap map, IniFile mapIni)
        {
            const string sectionName = "Waypoints";
            mapIni.RemoveSection(sectionName);

            if (map.Waypoints.Count == 0)
                return;

            map.SortWaypoints();

            var section = new IniSection(sectionName);
            mapIni.AddSection(section);

            map.Waypoints.ForEach(w => w.WriteToIniFile(mapIni));
        }

        public static void WriteTaskForces(IMap map, IniFile mapIni)
        {
            const string sectionName = "TaskForces";
            mapIni.RemoveSection(sectionName);

            if (map.TaskForces.Count == 0)
                return;

            var taskForcesSection = new IniSection(sectionName);
            mapIni.AddSection(taskForcesSection);

            for (int i = 0; i < map.TaskForces.Count; i++)
            {
                TaskForce taskForce = map.TaskForces[i];

                taskForcesSection.SetStringValue(i.ToString(), taskForce.ININame);

                mapIni.RemoveSection(taskForce.ININame);

                var taskForceSection = new IniSection(taskForce.ININame);
                mapIni.AddSection(taskForceSection);
                taskForce.Write(taskForceSection);
            }
        }

        public static void WriteTriggers(IMap map, IniFile mapIni)
        {
            mapIni.RemoveSection("Triggers");
            mapIni.RemoveSection("Events");
            mapIni.RemoveSection("Actions");
            mapIni.RemoveSection("EditorTriggerInfo");

            if (map.Triggers.Count == 0)
                return;

            map.Triggers.ForEach(t => t.WriteToIniFile(mapIni, map.EditorConfig));
        }

        public static void WriteTags(IMap map, IniFile mapIni)
        {
            const string sectionName = "Tags";
            mapIni.RemoveSection(sectionName);

            if (map.Tags.Count == 0)
                return;

            var tagsSection = new IniSection(sectionName);
            mapIni.AddSection(tagsSection);
            map.Tags.ForEach(t => t.WriteToIniSection(tagsSection));
        }

        public static void WriteCellTags(IMap map, IniFile mapIni)
        {
            const string sectionName = "CellTags";
            mapIni.RemoveSection(sectionName);

            if (map.CellTags.Count == 0)
                return;

            var section = new IniSection(sectionName);
            mapIni.AddSection(section);

            foreach (var cellTag in map.CellTags)
            {
                int tileIndex = cellTag.Position.Y * 1000 + cellTag.Position.X;
                section.SetStringValue(tileIndex.ToString(), cellTag.Tag.ID);
            }
        }

        public static void WriteScripts(IMap map, IniFile mapIni)
        {
            const string sectionName = "ScriptTypes";
            mapIni.RemoveSection(sectionName);

            if (map.Scripts.Count == 0)
                return;

            var scriptTypesSection = new IniSection(sectionName);
            mapIni.AddSection(scriptTypesSection);
            for (int i = 0; i < map.Scripts.Count; i++)
            {
                Script script = map.Scripts[i];
                scriptTypesSection.SetStringValue(i.ToString(), script.ININame);

                mapIni.RemoveSection(script.ININame);
                var scriptSection = new IniSection(script.ININame);
                mapIni.AddSection(scriptSection);
                script.WriteToIniSection(scriptSection);
            }
        }

        public static void WriteTeamTypes(IMap map, IniFile mapIni, List<TeamTypeFlag> teamTypeFlags)
        {
            const string sectionName = "TeamTypes";
            mapIni.RemoveSection(sectionName);

            if (map.TeamTypes.Count == 0)
                return;

            var teamTypesSection = new IniSection(sectionName);
            mapIni.AddSection(teamTypesSection);
            for (int i = 0; i < map.TeamTypes.Count; i++)
            {
                TeamType teamType = map.TeamTypes[i];
                teamTypesSection.SetStringValue(i.ToString(), teamType.ININame);

                mapIni.RemoveSection(teamType.ININame);
                var teamTypeSection = new IniSection(teamType.ININame);
                mapIni.AddSection(teamTypeSection);
                teamType.WriteToIniSection(teamTypeSection, teamTypeFlags);
            }
        }

        public static void WriteAITriggerTypes(IMap map, IniFile mapIni)
        {
            const string sectionName = "AITriggerTypes";
            mapIni.RemoveSection(sectionName);

            if (map.AITriggerTypes.Count == 0)
                return;

            var aiTriggerTypesSection = new IniSection(sectionName);
            mapIni.AddSection(aiTriggerTypesSection);
            for (int i = 0; i < map.AITriggerTypes.Count; i++)
            {
                AITriggerType aiTriggerType = map.AITriggerTypes[i];
                aiTriggerType.WriteToIniSection(aiTriggerTypesSection);
            }

            const string enablesSectionName = "AITriggerTypesEnable";
            var enablesSection = mapIni.GetSection(enablesSectionName);
            if (enablesSection == null)
            {
                enablesSection = new IniSection(enablesSectionName);
                mapIni.AddSection(enablesSection);
            }

            // Enable local AI triggers that haven't been enabled or disabled
            // by the user yet
            for (int i = 0; i < map.AITriggerTypes.Count; i++)
            {
                if (!enablesSection.KeyExists(map.AITriggerTypes[i].ININame))
                    enablesSection.SetStringValue(map.AITriggerTypes[i].ININame, "yes");
            }
        }

        public static void WriteHouseTypes(IMap map, IniFile mapIni)
        {
            var houseTypes = map.HouseTypes;

            string sectionName = Constants.UseCountries ? "Countries" : "Houses";
            mapIni.RemoveSection(sectionName);

            if (map.HouseTypes.Count == 0)
                return;

            var houseTypesSection = new IniSection(sectionName);
            mapIni.AddSection(houseTypesSection);

            for (int i = 0; i < houseTypes.Count; i++)
            {
                HouseType houseType = houseTypes[i];
                houseTypesSection.SetStringValue(
                    houseType.Index > -1 ? houseType.Index.ToString(CultureInfo.InvariantCulture) : i.ToString(CultureInfo.InvariantCulture),
                    houseType.ININame);

                mapIni.RemoveSection(houseType.ININame);
                var houseTypeSection = FindOrMakeSection(houseType.ININame, mapIni);
                houseType.WriteToIniSection(houseTypeSection);
            }

            if (Constants.UseCountries)
            {
                // Write Rules house types that have been modified in the map
                for (int i = 0; i < map.Rules.RulesHouseTypes.Count; i++)
                {
                    HouseType houseType = map.Rules.RulesHouseTypes[i];
                    if (houseType.ModifiedInMap)
                    {
                        mapIni.RemoveSection(houseType.ININame);
                        var houseTypeSection = FindOrMakeSection(houseType.ININame, mapIni);
                        houseType.WriteToIniSection(houseTypeSection);
                    }
                }
            }
        }

        public static void WriteHouses(IMap map, IniFile mapIni)
        {
            const string sectionName = "Houses";
            mapIni.RemoveSection(sectionName);

            if (map.Houses.Count == 0)
                return;

            var housesSection = new IniSection(sectionName);
            mapIni.AddSection(housesSection);

            for (int i = 0; i < map.Houses.Count; i++)
            {
                House house = map.Houses[i];
                housesSection.SetStringValue(house.ID > -1 ? house.ID.ToString() : i.ToString(), house.ININame);

                // When countries are not in use, the section is already removed by WriteHouseTypes
                if (Constants.UseCountries)
                {
                    // Only remove the section if no similarly-named modified HouseType exists - if one does,
                    // the section was possibly already removed by WriteHouseTypes
                    var houseType = map.FindHouseType(house.ININame);
                    if (houseType == null)
                        mapIni.RemoveSection(house.ININame);

                    house.Country = house.HouseType.ININame; // Make sure the country property matches our model
                }

                var houseSection = FindOrMakeSection(house.ININame, mapIni);
                house.WriteToIniSection(houseSection);
            }
        }

        private static string GetAttachedTagName(TechnoBase techno)
        {
            return techno.AttachedTag == null ? Constants.NoneValue2 : techno.AttachedTag.ID;
        }

        public static void WriteAircraft(IMap map, IniFile mapIni)
        {
            const string sectionName = "Aircraft";

            mapIni.RemoveSection(sectionName);
            if (map.Aircraft.Count == 0)
                return;

            var section = new IniSection(sectionName);
            mapIni.AddSection(section);

            for (int i = 0; i < map.Aircraft.Count; i++)
            {
                var aircraft = map.Aircraft[i];

                // INDEX = OWNER,ID,HEALTH,X,Y,FACING,MISSION,TAG,VETERANCY,GROUP,AUTOCREATE_NO_RECRUITABLE,AUTOCREATE_YES_RECRUITABLE

                string attachedTag = GetAttachedTagName(aircraft);

                string value = $"{aircraft.Owner.ININame},{aircraft.ObjectType.ININame},{aircraft.HP}," +
                               $"{aircraft.Position.X},{aircraft.Position.Y},{aircraft.Facing}," +
                               $"{aircraft.Mission},{attachedTag},{aircraft.Veterancy}," +
                               $"{aircraft.Group}," + 
                               $"{BoolToObjectStyle(aircraft.AutocreateNoRecruitable)}," +
                               $"{BoolToObjectStyle(aircraft.AutocreateYesRecruitable)}";

                section.SetStringValue(i.ToString(), value);
            }
        }

        public static void WriteUnits(IMap map, IniFile mapIni)
        {
            const string sectionName = "Units";

            mapIni.RemoveSection(sectionName);
            if (map.Units.Count == 0)
                return;

            var section = new IniSection(sectionName);
            mapIni.AddSection(section);

            for (int i = 0; i < map.Units.Count; i++)
            {
                var unit = map.Units[i];

                // INDEX=OWNER,ID,HEALTH,X,Y,FACING,MISSION,TAG,VETERANCY,GROUP,HIGH,FOLLOWS_INDEX,AUTOCREATE_NO_RECRUITABLE,AUTOCREATE_YES_RECRUITABLE

                string attachedTag = GetAttachedTagName(unit);
                string followsIndex = unit.FollowerUnit == null ? "-1" : map.Units.IndexOf(unit.FollowerUnit).ToString(CultureInfo.InvariantCulture);

                string value = $"{unit.Owner.ININame},{unit.ObjectType.ININame},{unit.HP}," +
                               $"{unit.Position.X},{unit.Position.Y},{unit.Facing}," +
                               $"{unit.Mission},{attachedTag},{unit.Veterancy}," +
                               $"{unit.Group},{BoolToObjectStyle(unit.High)}," +
                               $"{followsIndex}," +
                               $"{BoolToObjectStyle(unit.AutocreateNoRecruitable)}," +
                               $"{BoolToObjectStyle(unit.AutocreateYesRecruitable)}";

                section.SetStringValue(i.ToString(), value);
            }
        }

        public static void WriteInfantry(IMap map, IniFile mapIni)
        {
            const string sectionName = "Infantry";

            mapIni.RemoveSection(sectionName);
            if (map.Infantry.Count == 0)
                return;

            var section = new IniSection(sectionName);
            mapIni.AddSection(section);

            for (int i = 0; i < map.Infantry.Count; i++)
            {
                var infantry = map.Infantry[i];

                // INDEX=OWNER,ID,HEALTH,X,Y,SUB_CELL,MISSION,FACING,TAG,VETERANCY,GROUP,HIGH,AUTOCREATE_NO_RECRUITABLE,AUTOCREATE_YES_RECRUITABLE

                string attachedTag = GetAttachedTagName(infantry);

                string value = $"{infantry.Owner.ININame},{infantry.ObjectType.ININame},{infantry.HP}," +
                               $"{infantry.Position.X},{infantry.Position.Y},{(int)infantry.SubCell}," +
                               $"{infantry.Mission},{infantry.Facing},{attachedTag},{infantry.Veterancy}," +
                               $"{infantry.Group},{BoolToObjectStyle(infantry.High)}," +
                               $"{BoolToObjectStyle(infantry.AutocreateNoRecruitable)}," +
                               $"{BoolToObjectStyle(infantry.AutocreateYesRecruitable)}";

                section.SetStringValue(i.ToString(), value);
            }
        }

        private static string UpgradeToString(BuildingType upgrade)
        {
            if (upgrade == null)
                return Constants.NoneValue2;

            return upgrade.ININame;
        }

        public static void WriteBuildings(IMap map, IniFile mapIni)
        {
            const string sectionName = "Structures";

            mapIni.RemoveSection(sectionName);
            if (map.Structures.Count == 0)
                return;

            var section = new IniSection(sectionName);
            mapIni.AddSection(section);

            for (int i = 0; i < map.Structures.Count; i++)
            {
                var structure = map.Structures[i];

                // INDEX=OWNER,ID,HEALTH,X,Y,FACING,TAG,AI_SELLABLE,AI_REBUILDABLE,POWERED_ON,UPGRADES,SPOTLIGHT,UPGRADE_1,UPGRADE_2,UPGRADE_3,AI_REPAIRABLE,NOMINAL

                string attachedTag = GetAttachedTagName(structure);
                string upgrade1 = UpgradeToString(structure.Upgrades[0]);
                string upgrade2 = UpgradeToString(structure.Upgrades[1]);
                string upgrade3 = UpgradeToString(structure.Upgrades[2]);

                string value = $"{structure.Owner.ININame},{structure.ObjectType.ININame},{structure.HP}," +
                               $"{structure.Position.X},{structure.Position.Y}," +
                               $"{structure.Facing},{attachedTag}," +
                               $"{BoolToObjectStyle(structure.AISellable)}," +
                               $"{BoolToObjectStyle(structure.AIRebuildable)}," +
                               $"{BoolToObjectStyle(structure.Powered)}," +
                               $"{structure.UpgradeCount}," +
                               $"{(int)structure.Spotlight}," + 
                               $"{upgrade1},{upgrade2},{upgrade3}," +
                               $"{BoolToObjectStyle(structure.AIRepairable)}," +
                               $"{BoolToObjectStyle(structure.Nominal)}";

                section.SetStringValue(i.ToString(), value);
            }
        }

        public static void WriteLocalVariables(IMap map, IniFile mapIni)
        {
            const string sectionName = "VariableNames";

            mapIni.RemoveSection(sectionName);
            if (map.LocalVariables.Count == 0)
                return;

            var section = new IniSection(sectionName);
            mapIni.AddSection(section);

            foreach (var localVariable in map.LocalVariables)
            {
                section.SetStringValue(localVariable.Index.ToString(CultureInfo.InvariantCulture), $"{localVariable.Name},{localVariable.InitialState.ToString(CultureInfo.InvariantCulture)}");
            }
        }

        public static void WriteTubes(IMap map, IniFile mapIni)
        {
            const string sectionName = "Tubes";
            mapIni.RemoveSection(sectionName);
            if (map.Tubes.Count == 0)
                return;

            var section = new IniSection(sectionName);
            mapIni.AddSection(section);

            for (int i = 0; i < map.Tubes.Count; i++)
            {
                var tube = map.Tubes[i];

                // Index=ENTER_X,ENTER_Y,FACING,EXIT_X,EXIT_Y,DIRECTIONS

                string directionsString = string.Join(",", tube.Directions.Select(dir => (int)dir));
                if (!directionsString.EndsWith("-1"))
                    directionsString += ",-1"; // Directions need to end with -1

                section.SetStringValue(i.ToString(), $"{tube.EntryPoint.X},{tube.EntryPoint.Y},{(int)tube.UnitInitialFacing},{tube.ExitPoint.X},{tube.ExitPoint.Y},{directionsString}");
            }
        }

        private static string BoolToObjectStyle(bool value)
        {
            return Conversions.BooleanToString(value, BooleanStringStyle.ONEZERO);
        }
    }
}
