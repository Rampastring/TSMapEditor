using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;

namespace TSMapEditor.Models
{
    public class Lighting : INIDefineable
    {
        private const string LightingIniSectionName = "Lighting";

        public event EventHandler Changed;

        public double Red { get; set; }
        public double Green { get; set; }
        public double Blue { get; set; }
        public double Ambient { get; set; }
        public double Level { get; set; }
        public double Ground { get; set; }
        
        public double IonRed { get; set; }
        public double IonGreen { get; set; }
        public double IonBlue { get; set; }
        public double IonAmbient { get; set; }
        public double IonLevel { get; set; }
        public double IonGround { get; set; }

        public double? DominatorRed { get; set; }
        public double? DominatorGreen { get; set; }
        public double? DominatorBlue { get; set; }
        public double? DominatorAmbient { get; set; }
        public double? DominatorAmbientChangeRate { get; set; }
        public double? DominatorLevel { get; set; }
        public double? DominatorGround { get; set; }

        [INI(false)]
        public Color NormalColor { get; private set; }
        [INI(false)]
        public Color IonColor { get; private set; }
        [INI(false)]
        public Color? DominatorColor { get; private set; }

        public void ReadFromIniFile(IniFile iniFile)
        {
            var lightingSection = iniFile.GetSection(LightingIniSectionName);

            if (lightingSection == null)
                return;

            ReadPropertiesFromIniSection(lightingSection);
            RefreshLightingColors();
        }

        public void WriteToIniFile(IniFile iniFile)
        {
            var lightingSection = iniFile.GetSection(LightingIniSectionName);
            if (lightingSection == null)
            {
                lightingSection = new IniSection(LightingIniSectionName);
                iniFile.AddSection(lightingSection);
            }

            WritePropertiesToIniSection(lightingSection);
        }

        public void RefreshLightingColors()
        {
            NormalColor = RefreshLightingColor(Red, Green, Blue, Ambient);
            IonColor = RefreshLightingColor(IonRed, IonGreen, IonBlue, IonAmbient);
            DominatorColor = RefreshLightingColor(DominatorRed, DominatorGreen, DominatorBlue, DominatorAmbient);

            Changed?.Invoke(this, EventArgs.Empty);
        }

        private static Color RefreshLightingColor(double? red, double? green, double? blue, double? ambient)
        {
            if (red == null || green == null || blue == null || ambient == null)
                return Color.White;

            return new Color((float)red, (float)green, (float)blue) * (float)ambient;
        }
    }
}
