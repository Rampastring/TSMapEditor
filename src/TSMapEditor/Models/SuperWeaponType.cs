using System.Globalization;

namespace TSMapEditor.Models
{
    public class SuperWeaponType : AbstractObject, INIDefined
    {
        public SuperWeaponType(string iniName)
        {
            ININame = iniName;
        }

        [INI(false)]
        public string ININame { get; }

        [INI(false)]
        public int Index { get; set; }

        public string Name { get; set; }

        public string GetDisplayString() => $"{Index.ToString(CultureInfo.InvariantCulture)} {GetDisplayStringWithoutIndex()}";

        public string GetDisplayStringWithoutIndex() => $"{Name} ({ININame})";

        public override RTTIType WhatAmI() => RTTIType.SuperWeaponType;
    }
}
