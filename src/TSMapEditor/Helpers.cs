using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;

namespace TSMapEditor
{
    public static class Helpers
    {
        public static bool IsStringNoneValue(string str)
        {
            return str.Equals(Constants.NoneValue1, StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals(Constants.NoneValue2, StringComparison.InvariantCultureIgnoreCase);
        }

        public static string BoolToIntString(bool value)
        {
            return value ? "1" : "0";
        }

        public static Point2D? CoordStringToPoint(string coordsString)
        {
            int coords = Conversions.IntFromString(coordsString, -1);
            if (coords < 0 || coordsString.Length < 4)
            {
                Logger.Log("CoordStringToPoint: invalid coord string " + coordsString);
                return null;
            }

            string xCoordPart = coordsString.Substring(coordsString.Length - 3);
            int x = Conversions.IntFromString(xCoordPart, -1);
            if (x < 0)
            {
                Logger.Log("CoordStringToPoint: invalid X coord " + x);
                return null;
            }

            string yCoordPart = coordsString.Substring(0, coordsString.Length - 3);
            int y = Conversions.IntFromString(yCoordPart, -1);
            if (y < 0)
            {
                Logger.Log("CoordStringToPoint: invalid Y coord " + y);
                return null;
            }

            return new Point2D(x, y);
        }

        public static int ReverseEndianness(int value)
        {
            return (int)((value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24);
        }

        public static string LandTypeToString(int landType)
        {
            return GetLandTypeName(landType) + " (0x" + landType.ToString("X") + ")";
        }

        private static string GetLandTypeName(int landType)
        {
            switch (landType)
            {
                case 0x0:
                case 0xD:
                    return "Clear";
                case 0x1:
                case 0x2:
                case 0x3:
                case 0x4:
                    return "Ice";
                case 0x5:
                    return "Tunnel";
                case 0x6:
                    return "Railroad";
                case 0x7:
                case 0x8:
                    return "Rock";
                case 0x9:
                    return "Water";
                case 0xA:
                    return "Beach";
                case 0xB:
                case 0xC:
                    return "Road";
                case 0xE:
                    return "Rough";
                case 0xF:
                    return "Rock";
                default:
                    return "Unknown";
            }
        }

        public static int LandTypeToInt(LandType landType)
        {
            return landType switch 
            { 
                LandType.Clear => 0x0,
                LandType.Ice => 0x1,
                LandType.Tunnel => 0x5,
                LandType.Railroad => 0x6,
                LandType.Rock => 0x7,
                LandType.Water => 0x9,
                LandType.Beach => 0xA,
                LandType.Road => 0xB,
                LandType.Rough => 0xE,
                LandType.Tiberium => 0x0, // ?? can't find any sources on this
                LandType.Weeds => 0x0, // ?? can't find any sources on this
                _ => 0x0
            };
        }

        public static bool IsLandTypeImpassable(int landType, bool considerLandUnitsOnly = false)
        {
            // TODO make this dependent on SpeedType and Rules.ini values

            switch (landType)
            {
                case 0x1:
                case 0x2:
                case 0x3:
                case 0x4:
                case 0x9:
                case 0xA:
                    return considerLandUnitsOnly;
                case 0x7:
                case 0x8:
                case 0xF:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsLandTypeImpassableForNavalUnits(int landType)
        {
            // TODO make this dependent on SpeedType and Rules.ini values

            switch (landType)
            {
                case 0x1:
                case 0x2:
                case 0x3:
                case 0x4:
                case 0x9:
                case 0xA:
                    return false;
                default:
                    return true;
            }
        }

        public static bool IsLandTypeImpassable(LandType landType, bool considerLandUnitsOnly)
        {
            return landType == LandType.Rock || (considerLandUnitsOnly && landType == LandType.Water);
        }

        public static bool IsLandTypeWater(int landType)
        {
            return landType == 0x9;
        }

        public static int GetWaypointNumberFromAlphabeticalString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return -1;

            if (str.Length < 1 || str.Length > 2 ||
                str[0] < 'A' || str[0] > 'Z' || (str.Length == 2 && (str[1] < 'A' || str[1] > 'Z')))
                throw new InvalidOperationException("Waypoint values are only valid between A and ZZ. Invalid value: " + str);

            if (str.Length == 1)
                return str[0] - 'A';

            const int CharCount = 26;

            int multiplier = (str[0] - 'A' + 1);
            return (multiplier * CharCount) + (str[1] - 'A');
        }

        public static string WaypointNumberToAlphabeticalString(int waypointNumber)
        {
            if (waypointNumber < 0)
                return string.Empty;

            const int WAYPOINT_MAX = 701;

            if (waypointNumber > WAYPOINT_MAX)
                return "A"; // matches 0

            const int CharCount = 26;

            int firstLetterValue = (waypointNumber / CharCount);
            int secondLetterValue = waypointNumber % CharCount;

            if (firstLetterValue == 0)
                return ((char)('A' + secondLetterValue)).ToString();

            return ((char)('A' + (firstLetterValue - 1))).ToString() + ((char)('A' + secondLetterValue)).ToString();
        }

        private static Point2D[] visualDirectionToPointTable = new Point2D[]
        {
            new Point2D(0, -1), new Point2D(1, -1), new Point2D(1, 0),
            new Point2D(1, 1), new Point2D(0, 1), new Point2D(-1, 1),
            new Point2D(-1, 0), new Point2D(-1, -1)
        };

        public static Point2D VisualDirectionToPoint(Direction direction) => visualDirectionToPointTable[(int)direction];

        public static List<Direction> GetDirectionsInMask(byte mask)
        {
            List<Direction> directions = new List<Direction>();

            for (int direction = 0; direction < (int)Direction.Count; direction++)
            {
                if ((mask & (byte)(0b10000000 >> direction)) > 0)
                    directions.Add((Direction)direction);
            }

            return directions;
        }

        /// <summary>
        /// Creates and returns a new UI texture.
        /// </summary>
        /// <param name="gd">A GraphicsDevice instance.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="mainColor">The background color of the texture.</param>
        /// <param name="secondaryColor">The first border color.</param>
        /// <param name="tertiaryColor">The second border color.</param>
        /// <returns></returns>
        public static Texture2D CreateUITexture(GraphicsDevice gd, int width, int height, Color mainColor, Color secondaryColor, Color tertiaryColor)
        {
            Texture2D Texture = new Texture2D(gd, width, height, false, SurfaceFormat.Color);

            Color[] color = new Color[width * height];

            // background color
            // ***

            for (int i = 0; i < color.Length; i++)
                color[i] = mainColor;

            // main border
            // ***

            // top
            for (int i = width; i < (width * 3); i++)
                color[i] = secondaryColor;

            // bottom
            for (int i = color.Length - (width * 3); i < color.Length - width; i++)
                color[i] = secondaryColor;

            // right
            for (int i = 1; i < color.Length - width - 2; i = i + width)
                color[i] = secondaryColor;

            for (int i = 2; i < color.Length - width - 2; i = i + width)
                color[i] = secondaryColor;

            // left
            for (int i = width - 3; i < color.Length; i = i + width)
                color[i] = secondaryColor;

            for (int i = width - 2; i < color.Length; i = i + width)
                color[i] = secondaryColor;

            // outer border
            // ***

            // top
            for (int i = 0; i < width; i++)
                color[i] = tertiaryColor;

            // bottom
            for (int i = color.Length - width; i < color.Length; i++)
                color[i] = tertiaryColor;

            // right
            for (int i = 0; i < color.Length - width; i = i + width)
                color[i] = tertiaryColor;

            // left
            for (int i = width - 1; i < color.Length; i = i + width)
                color[i] = tertiaryColor;

            Texture.SetData(color);

            return Texture;
        }

        public static float AngleFromVector(Vector2 vector)
        {
            return (float)Math.Atan2(vector.Y, vector.X);
        }

        public static Vector2 VectorFromLengthAndAngle(float length, float angle)
        {
            return new Vector2(length * (float)Math.Cos(angle), length * (float)Math.Sin(angle));
        }

        public static Point2D ScreenCoordsFromWorldLeptons(Vector2 coords)
        {
            coords /= Constants.CellSizeInLeptons;
            int screenX = Convert.ToInt32((coords.X - coords.Y) * Constants.CellSizeX / 2);
            int screenY = Convert.ToInt32((coords.X + coords.Y) * Constants.CellSizeY / 2);
            return new Point2D(screenX, screenY);
        }

        public static Color ColorFromString(string str)
        {
            string[] parts = str.Split(',');

            if (parts.Length < 3 || parts.Length > 4)
                throw new ArgumentException("ColorFromString: parameter was not in a valid format: " + str);

            int r = Conversions.IntFromString(parts[0], 0);
            int g = Conversions.IntFromString(parts[1], 0);
            int b = Conversions.IntFromString(parts[2], 0);
            int a = 255;

            if (parts.Length == 4)
                a = Conversions.IntFromString(parts[3], a);

            return new Color((byte)r, (byte)g, (byte)b, (byte)a);
        }

        public static string ColorToString(Color color, bool includeAlpha)
        {
            string returnValue = color.R.ToString(CultureInfo.InvariantCulture) + "," + 
                color.G.ToString(CultureInfo.InvariantCulture) + "," + 
                color.B.ToString(CultureInfo.InvariantCulture);

            if (includeAlpha)
                returnValue += "," + color.A.ToString(CultureInfo.InvariantCulture);

            return returnValue;
        }

        public static Color GetHouseUITextColor(House house)
        {
            if (house == null || IsColorDark(house.XNAColor))
                return UISettings.ActiveSettings.AltColor;

            return house.XNAColor;
        }

        public static Color GetHouseTypeUITextColor(HouseType houseType)
        {
            if (houseType == null || IsColorDark(houseType.XNAColor))
                return UISettings.ActiveSettings.AltColor;

            return houseType.XNAColor;
        }

        public static bool IsColorDark(Color color) => color.R < 32 && color.G < 32 && color.B < 64;

        public static void FindDefaultSideForNewHouseType(HouseType houseType, Rules rules)
        {
            for (int sideIndex = 0; sideIndex < rules.Sides.Count; sideIndex++)
            {
                string side = rules.Sides[sideIndex];

                if (houseType.ININame.StartsWith(side))
                {
                    houseType.Side = side;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(houseType.Side))
            {
                houseType.Side = rules.Sides[0];
            }
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToUpperInvariant();
        }

        public static Texture2D RenderTextureAsSmaller(Texture2D existingTexture, RenderTarget2D renderTarget, GraphicsDevice graphicsDevice)
        {
            Renderer.BeginDraw();
            Renderer.PushRenderTarget(renderTarget);
            graphicsDevice.Clear(Color.Transparent);

            Point maxNewTextureSize = new Point(Math.Min(renderTarget.Width, existingTexture.Width), Math.Min(renderTarget.Height, existingTexture.Height));

            double ratioX = (double)existingTexture.Width / maxNewTextureSize.X;
            double ratioY = (double)existingTexture.Height / maxNewTextureSize.Y;
            double ratio = Math.Max(ratioX, ratioY);
            Point newSize = new Point((int)(existingTexture.Width / ratio), (int)(existingTexture.Height / ratio));

            // Workaround to avoid crashing for too small textures, hopefully it's also better for visibility
            if (newSize.X < 1 && existingTexture.Width > 0)
                newSize.X = 1;

            if (newSize.Y < 1 && existingTexture.Height > 0)
                newSize.Y = 1;

            Rectangle destinationRectangle = new Rectangle(0, 0, newSize.X, newSize.Y);

            Renderer.DrawTexture(existingTexture, new Rectangle(0, 0, existingTexture.Width, existingTexture.Height),
                destinationRectangle, Color.White);
            Renderer.PopRenderTarget();
            Renderer.EndDraw();

            var texture = new Texture2D(graphicsDevice, destinationRectangle.Width, destinationRectangle.Height, false, SurfaceFormat.Color);
            var colorData = new Color[destinationRectangle.Width * destinationRectangle.Height];
            renderTarget.GetData(0, destinationRectangle, colorData, 0, destinationRectangle.Width * destinationRectangle.Height);
            texture.SetData(colorData);
            return texture;
        }

        public static (Texture2D texture, Point2D positionOffset) CropTextureToVisiblePortion(Texture2D existingTexture, GraphicsDevice graphicsDevice)
        {
            var textureData = new Color[existingTexture.Width * existingTexture.Height];
            existingTexture.GetData(textureData);

            int firstNonTransparentX = int.MaxValue;
            int firstNonTransparentY = int.MaxValue;
            int lastNonTransparentX = -1;
            int lastNonTransparentY = -1;

            // Scan through the whole image.
            // For every visible pixel, we check whether we should "expand" the bounds.
            for (int y = 0; y < existingTexture.Height; y++)
            {
                for (int x = 0; x < existingTexture.Width; x++)
                {
                    int index = y * existingTexture.Width + x;

                    Color color = textureData[index];
                    if (color.A > 0)
                    {
                        if (x < firstNonTransparentX)
                            firstNonTransparentX = x;

                        if (y < firstNonTransparentY)
                            firstNonTransparentY = y;

                        if (x > lastNonTransparentX)
                            lastNonTransparentX = x;

                        if (y > lastNonTransparentY)
                            lastNonTransparentY = y;
                    }
                }
            }

            int width = lastNonTransparentX - firstNonTransparentX;
            int height = lastNonTransparentY - firstNonTransparentY;

            // If there are no visible pixels then set the texture size to 1 to avoid crashes.
            if (width <= 0)
                width = 1;

            if (height <= 0)
                height = 1;

            // Now we know the exact rectangle of the texture that is visible.
            // Create a new texture and render only the visible portion into it.
            var renderTarget = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);

            Renderer.PushRenderTarget(renderTarget);
            graphicsDevice.Clear(Color.Transparent);
            Renderer.DrawTexture(existingTexture, new Rectangle(firstNonTransparentX, firstNonTransparentY, width, height),
                new Rectangle(0, 0, width, height), Color.White);
            Renderer.PopRenderTarget();

            var texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);
            var colorData = new Color[width * height];
            renderTarget.GetData(colorData);
            texture.SetData(colorData);
            renderTarget.Dispose();

            // Calculate offset
            Point2D oldcenter = new Point2D(existingTexture.Width / 2, existingTexture.Height / 2);
            Point2D newcenter = new Point2D(firstNonTransparentX + width / 2, firstNonTransparentY + height / 2);

            return (texture, new Point2D(newcenter.X - oldcenter.X, newcenter.Y - oldcenter.Y));
        }
    }
}
