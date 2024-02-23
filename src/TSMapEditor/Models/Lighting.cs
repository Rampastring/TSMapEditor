using Rampastring.Tools;
using System;
using Microsoft.Xna.Framework;
using TSMapEditor.CCEngine;

namespace TSMapEditor.Models
{
    public struct MapColor
    {
        public MapColor(double red, double green, double blue)
        {
            R = red;
            G = green;
            B = blue;
        }

        public double R;
        public double G;
        public double B;

        public static MapColor operator *(MapColor value, double multiplier)
        {
            return new(value.R * multiplier, value.G * multiplier, value.B * multiplier);
        }

        public static Color operator *(Color left, MapColor right)
        {
            return new
            (
                (byte)Math.Clamp(left.R * right.R, 0, 255.0),
                (byte)Math.Clamp(left.G * right.G, 0, 255.0),
                (byte)Math.Clamp(left.B * right.B, 0, 255.0),
                (byte)255
            );
        }

        public static RGBColor operator *(RGBColor left, MapColor right)
        {
            return new
            (
                (byte)Math.Clamp(left.R * right.R, 0, 255.0),
                (byte)Math.Clamp(left.G * right.G, 0, 255.0),
                (byte)Math.Clamp(left.B * right.B, 0, 255.0)
            );
        }

        public static readonly MapColor White = new(1.0, 1.0, 1.0);
    }

    public class Lighting : INIDefineable
    {
        private const string LightingIniSectionName = "Lighting";

        public event EventHandler ColorsRefreshed;

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
        public MapColor NormalColor { get; private set; }
        [INI(false)]
        public MapColor IonColor { get; private set; }
        [INI(false)]
        public MapColor? DominatorColor { get; private set; }

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

            ColorsRefreshed?.Invoke(this, EventArgs.Empty);
        }

        private static MapColor RefreshLightingColor(double? red, double? green, double? blue, double? ambient)
        {
            if (red == null || green == null || blue == null || ambient == null)
                return MapColor.White;

            return new MapColor((double)red, (double)green, (double)blue) * (double)ambient;
        }
    }
}
