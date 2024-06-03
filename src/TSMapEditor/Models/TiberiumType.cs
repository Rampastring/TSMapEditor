using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TSMapEditor.Models
{
    public class TiberiumType : INIDefineable, INIDefined
    {
        public TiberiumType(string iniName)
        {
            ININame = iniName;
        }

        public string ININame { get; }
        public int Index { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
        public int Power { get; set; }
        public int Image { get; set; }
        public int Growth { get; set; }
        public double GrowthPercentage { get; set; }
        public int Spread { get; set; }
        public int SpreadPercentage { get; set; }
        public string Color { get; set; } = string.Empty;

        [INI(false)]
        public Color XNAColor { get; set; }

        [INI(false)]
        public List<OverlayType> Overlays { get; set; } = new List<OverlayType>();
    }
}
