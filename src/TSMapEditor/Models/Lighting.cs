using Rampastring.Tools;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TSMapEditor.CCEngine;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.Models
{
    public class CellLightingEventArgs : EventArgs
    {
        public List<MapTile> AffectedTiles { get; set; }
    }

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

        public Vector4 ToXNAVector4(double extraLight) => new Vector4(
            (float)(R + extraLight),
            (float)(G + extraLight),
            (float)(B + extraLight),
            1.0f);

        public Vector4 ToXNAVector4() => new Vector4((float)R, (float)G, (float)B, 1.0f);

        public Vector4 ToXNAVector4Ambient(double extraLight)
        {
            double average = ((R + G + B) / 3.0) + extraLight;
            return new Vector4((float)average, (float)average, (float)average, 1.0f);
        }

        public Vector4 ToXNAVector4Ambient()
        {
            // double highestComponent = Math.Max(R, Math.Max(G, B));
            // return new Vector4((float)highestComponent, (float)highestComponent, (float)highestComponent, 1.0f);
            double average = (R + G + B) / 3.0;
            return new Vector4((float)average, (float)average, (float)average, 1.0f);
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
            NormalColor = GetLightingColor(Red, Green, Blue, Ambient);
            IonColor = GetLightingColor(IonRed, IonGreen, IonBlue, IonAmbient);
            DominatorColor = GetLightingColor(DominatorRed, DominatorGreen, DominatorBlue, DominatorAmbient);

            ColorsRefreshed?.Invoke(this, EventArgs.Empty);
        }

        public MapColor MapColorFromPreviewMode(LightingPreviewMode lightingPreviewMode)
        {
            return lightingPreviewMode switch
            {
                LightingPreviewMode.NoLighting => new MapColor(1.0, 1.0, 1.0),
                LightingPreviewMode.Normal => GetLightingColor(Red, Green, Blue, Ambient),
                LightingPreviewMode.IonStorm => GetLightingColor(IonRed, IonGreen, IonBlue, IonAmbient),
                LightingPreviewMode.Dominator => GetLightingColor(DominatorRed, DominatorGreen, DominatorBlue, DominatorAmbient),
                _ => new MapColor(1.0, 1.0, 1.0)
            };
        }

        public double GetAmbientComponent(LightingPreviewMode lightingPreviewMode)
        {
            return lightingPreviewMode switch
            {
                LightingPreviewMode.NoLighting => 1.0,
                LightingPreviewMode.Normal => Ambient,
                LightingPreviewMode.IonStorm => IonAmbient,
                LightingPreviewMode.Dominator => DominatorAmbient.GetValueOrDefault(1.0),
                _ => 1.0
            };
        }

        public double GetLevelComponent(LightingPreviewMode lightingPreviewMode)
        {
            return lightingPreviewMode switch
            {
                LightingPreviewMode.NoLighting => 0.0,
                LightingPreviewMode.Normal => Level,
                LightingPreviewMode.IonStorm => IonLevel,
                LightingPreviewMode.Dominator => DominatorLevel.GetValueOrDefault(0.0),
                _ => 0.0
            };
        }

        public double GetGroundComponent(LightingPreviewMode lightingPreviewMode)
        {
            return lightingPreviewMode switch
            {
                LightingPreviewMode.NoLighting => 0.0,
                LightingPreviewMode.Normal => Ground,
                LightingPreviewMode.IonStorm => IonGround,
                LightingPreviewMode.Dominator => DominatorGround.GetValueOrDefault(0.0),
                _ => 0.0
            };
        }

        public double GetRedComponent(LightingPreviewMode lightingPreviewMode)
        {
            return lightingPreviewMode switch
            {
                LightingPreviewMode.NoLighting => 1.0,
                LightingPreviewMode.Normal => Red,
                LightingPreviewMode.IonStorm => IonRed,
                LightingPreviewMode.Dominator => DominatorRed.GetValueOrDefault(1.0),
                _ => 1.0
            };
        }

        public double GetGreenComponent(LightingPreviewMode lightingPreviewMode)
        {
            return lightingPreviewMode switch
            {
                LightingPreviewMode.NoLighting => 1.0,
                LightingPreviewMode.Normal => Green,
                LightingPreviewMode.IonStorm => IonGreen,
                LightingPreviewMode.Dominator => DominatorGreen.GetValueOrDefault(1.0),
                _ => 1.0
            };
        }

        public double GetBlueComponent(LightingPreviewMode lightingPreviewMode)
        {
            return lightingPreviewMode switch
            {
                LightingPreviewMode.NoLighting => 1.0,
                LightingPreviewMode.Normal => Blue,
                LightingPreviewMode.IonStorm => IonBlue,
                LightingPreviewMode.Dominator => DominatorBlue.GetValueOrDefault(1.0),
                _ => 1.0
            };
        }

        private static MapColor GetLightingColor(double? red, double? green, double? blue, double? ambient)
        {
            if (red == null || green == null || blue == null || ambient == null)
                return MapColor.White;

            return GetLightingColor(red.Value, green.Value, blue.Value, ambient.Value);
        }

        private static MapColor GetLightingColor(double red, double green, double blue, double ambient)
        {
            const double TotalAmbientCap = 2.0;

            red *= ambient;
            blue *= ambient;
            green *= ambient;

            double highestComponent = Math.Max(red, Math.Max(green, blue));

            // Tiberian Sun and Red Alert 2 limit the total ambient level.
            // If any of the components exceed the cap, we need to scale
            // them accordingly to fit within the cap.
            // Strength of each color tint (R,G,B) is based on ratio to highest component.
            if (highestComponent > TotalAmbientCap)
            {
                double scale = TotalAmbientCap / highestComponent;
                red *= scale;
                green *= scale;
                blue *= scale;
            }

            return new MapColor(red, green, blue);
        }
    }
}
