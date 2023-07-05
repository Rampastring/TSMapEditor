using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;
using TSMapEditor.Models.ArtConfig;
using TSMapEditor.Models.Enums;
using TSMapEditor.UI;

namespace TSMapEditor.Initialization
{
    public class MapLoadException : Exception
    {
        public MapLoadException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Initializes all different object types.
    /// </summary>
    public class Initializer : IInitializer
    {
        public Initializer(IMap map)
        {
            this.map = map;

            objectTypeInitializers = new Dictionary<Type, Action<INIDefineable, IniFile, IniSection>>()
            {
                { typeof(BuildingType), InitBuildingType },
                { typeof(InfantryType), InitInfantryType },
                { typeof(UnitType), InitUnitType },
                { typeof(AircraftType), InitAircraftType },
                { typeof(OverlayType), InitOverlayType },
                { typeof(TerrainType), InitTerrainType },
                { typeof(SmudgeType), InitSmudgeType },
                { typeof(AnimType), InitAnimType }
            };
        }

        private readonly IMap map;

        private Dictionary<Type, Action<INIDefineable, IniFile, IniSection>> objectTypeInitializers;

        private Dictionary<Type, Action<IMap, AbstractObject, IniFile, IniSection>> objectTypeArtInitializers
            = new Dictionary<Type, Action<IMap, AbstractObject, IniFile, IniSection>>()
            {
                { typeof(TerrainType), InitTerrainTypeArt },
                { typeof(SmudgeType), InitSmudgeTypeArt },
                { typeof(BuildingType), InitArtConfigGeneric },
                { typeof(OverlayType), InitArtConfigGeneric },
                { typeof(UnitType), InitArtConfigGeneric },
                { typeof(InfantryType), InitInfantryArtConfig },
                { typeof(AnimType), InitArtConfigGeneric }
            };

        public void ReadObjectTypePropertiesFromINI<T>(T obj, IniFile iniFile) where T : INIDefineable, INIDefined
        {
            IniSection objectSection = iniFile.GetSection(obj.ININame);
            if (objectSection == null)
                return;

            obj.ReadPropertiesFromIniSection(objectSection);

            if (objectTypeInitializers.TryGetValue(typeof(T), out var action))
                action(obj, iniFile, objectSection);
        }

        public void ReadObjectTypeArtPropertiesFromINI<T>(T obj, IniFile iniFile) where T : AbstractObject, INIDefined
        {
            IniSection objectSection = iniFile.GetSection(obj.ININame);
            if (objectSection == null)
                return;

            if (objectTypeArtInitializers.TryGetValue(typeof(T), out var action))
                action(map, obj, iniFile, objectSection);
        }

        public void ReadObjectTypeArtPropertiesFromINI<T>(T obj, IniFile iniFile, string sectionName) where T : AbstractObject, INIDefined
        {
            IniSection objectSection = iniFile.GetSection(sectionName);
            if (objectSection == null)
                return;

            if (objectTypeArtInitializers.TryGetValue(typeof(T), out var action))
                action(map, obj, iniFile, objectSection);
        }

        public void InitArt(IniFile iniFile)
        {

        }

        private Weapon FindOrCreateWeapon(string weaponName, IniFile rulesIni)
        {
            Weapon weapon = map.Rules.Weapons.Find(weapon => weapon.ININame == weaponName);

            if (weapon == null)
            {
                weapon = new Weapon(weaponName);
                var section = rulesIni.GetSection(weaponName);
                if (section == null)
                    throw new INIConfigException($"No section found for weapon {weaponName} while parsing Rules!");

                weapon.ReadPropertiesFromIniSection(section);
                map.Rules.Weapons.Add(weapon);
            }

            return weapon;
        }

        private Weapon FetchWeapon(IniFile rulesIni, IniSection objectSection, string keyName)
        {
            if (objectSection.KeyExists(keyName))
            {
                string weaponName = objectSection.GetStringValue(keyName, string.Empty);

                if (!string.IsNullOrWhiteSpace(weaponName) && !Helpers.IsStringNoneValue(weaponName))
                {
                    return FindOrCreateWeapon(weaponName, rulesIni);
                }
            }

            return null;
        }

        private void CommonTechnoInit(TechnoType technoType, IniFile rulesIni, IniSection section)
        {
            if (technoType.Primary == null)
                technoType.Primary = FetchWeapon(rulesIni, section, "Primary");

            if (technoType.Secondary == null)
                technoType.Secondary = FetchWeapon(rulesIni, section, "Secondary");
        }

        private void InitBuildingType(INIDefineable obj, IniFile rulesIni, IniSection section)
        {
            var buildingType = (BuildingType)obj;
            CommonTechnoInit(buildingType, rulesIni, section);
        }

        private void InitInfantryType(INIDefineable obj, IniFile rulesIni, IniSection section)
        {
            CommonTechnoInit((TechnoType)obj, rulesIni, section);
        }

        public void InitUnitType(INIDefineable obj, IniFile rulesIni, IniSection section)
        {
            CommonTechnoInit((TechnoType)obj, rulesIni, section);
        }

        private void InitAircraftType(INIDefineable obj, IniFile rulesIni, IniSection section)
        {
            CommonTechnoInit((TechnoType)obj, rulesIni, section);
        }

        private static void InitArtConfigGeneric(IMap map, AbstractObject obj, IniFile artIni, IniSection artSection)
        {
            var artConfigContainer = (IArtConfigContainer)obj;
            artConfigContainer.GetArtConfig().ReadFromIniSection(artSection);
        }

        private static void InitInfantryArtConfig(IMap map, AbstractObject obj, IniFile artIni, IniSection artSection)
        {
            var infantryType = (InfantryType)obj;
            infantryType.ArtConfig.ReadFromIniSection(artSection);
            if (infantryType.ArtConfig.Sequence == null && !string.IsNullOrWhiteSpace(infantryType.ArtConfig.SequenceName))
            {
                infantryType.ArtConfig.Sequence = map.Rules.FindOrMakeInfantrySequence(artIni, infantryType.ArtConfig.SequenceName);
            }
        }

        private static void InitOverlayType(INIDefineable obj, IniFile rulesIni, IniSection section)
        {
            var overlayType = (OverlayType)obj;
            overlayType.Land = (LandType)Enum.Parse(typeof(LandType), section.GetStringValue("Land", LandType.Clear.ToString()));
        }

        private static void InitTerrainType(INIDefineable obj, IniFile rulesIni, IniSection section)
        {
            var terrainType = (TerrainType)obj;
            terrainType.SnowOccupationBits = (TerrainOccupation)section.GetIntValue("SnowOccupationBits", 0);
            terrainType.TemperateOccupationBits = (TerrainOccupation)section.GetIntValue("TemperateOccupationBits", 0);
        }
        
        private static void InitTerrainTypeArt(IMap map, AbstractObject obj, IniFile artIni, IniSection artSection)
        {
            var terrainType = (TerrainType)obj;
            terrainType.Theater = artSection.GetBooleanValue("Theater", terrainType.Theater);
            terrainType.Image = artSection.GetStringValue("Image", terrainType.Image);
        }

        private static void InitSmudgeType(INIDefineable obj, IniFile rulesIni, IniSection section)
        {
        }

        private static void InitSmudgeTypeArt(IMap map, AbstractObject obj, IniFile artIni, IniSection artSection)
        {
            var smudgeType = (SmudgeType)obj;
            smudgeType.Theater = artSection.GetBooleanValue("Theater", smudgeType.Theater);
        }

        private static void InitAnimType(INIDefineable obj, IniFile rulesIni, IniSection section)
        {
        }
    }
}
