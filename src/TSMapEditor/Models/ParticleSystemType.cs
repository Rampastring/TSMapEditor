using System.Globalization;

namespace TSMapEditor.Models
{
    public class ParticleSystemType : AbstractObject, INIDefined
    {
        public ParticleSystemType(string iniName)
        {
            ININame = iniName;
        }

        [INI(false)]
        public string ININame { get; }

        [INI(false)]
        public int Index { get; set; }

        public string GetDisplayString() => $"{Index.ToString(CultureInfo.InvariantCulture)} {GetDisplayStringWithoutIndex()}";

        public string GetDisplayStringWithoutIndex() => ININame;

        public override RTTIType WhatAmI() => RTTIType.ParticleSystemType;
    }
}
