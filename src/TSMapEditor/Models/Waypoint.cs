using Rampastring.Tools;
using TSMapEditor.GameMath;
using Microsoft.Xna.Framework;
using System;
using System.Globalization;
using TSMapEditor.Misc;

namespace TSMapEditor.Models
{
    public class Waypoint : AbstractObject, IMovable
    {
        public static NamedColor[] SupportedColors = new NamedColor[]
        {
            new NamedColor("Teal", new Color(0, 196, 196)),
            new NamedColor("Green", new Color(0, 255, 0)),
            new NamedColor("Dark Green", Color.Green),
            new NamedColor("Lime Green", Color.LimeGreen),
            new NamedColor("Yellow", Color.Yellow),
            new NamedColor("Orange", Color.Orange),
            new NamedColor("Red", Color.Red),
            new NamedColor("Blood Red", Color.DarkRed),
            new NamedColor("Pink", Color.HotPink),
            new NamedColor("Cherry", Color.Pink),
            new NamedColor("Purple", Color.MediumPurple),
            new NamedColor("Sky Blue", Color.SkyBlue),
            new NamedColor("Blue", new Color(40, 40, 255)),
            new NamedColor("Brown", Color.Brown),
            new NamedColor("Metalic", new Color(160, 160, 200)),
        };

        private const int Coefficient = 1000;

        public int Identifier { get; set; }
        public Point2D Position { get; set; }

        private string _editorColor;
        /// <summary>
        /// Editor-only. The color of the waypoint in the UI.
        /// If null, the waypoint should be displayed with the default waypoint color.
        /// </summary>
        public string EditorColor
        {
            get => _editorColor;
            set
            {
                _editorColor = value;

                if (_editorColor != null)
                {
                    int index = Array.FindIndex(SupportedColors, c => c.Name == value);
                    if (index > -1)
                        XNAColor = SupportedColors[index].Value;
                    else
                        // Only allow assigning colors that actually exist in the color table
                        _editorColor = null;
                }
            }
        }

        public Color XNAColor;

        public static Waypoint ParseWaypoint(string id, string coordsString)
        {
            int waypointIndex = Conversions.IntFromString(id, -1);

            if (waypointIndex < 0 || waypointIndex > Constants.MaxWaypoint)
            {
                Logger.Log("Waypoint.Read: invalid waypoint index " + waypointIndex);
                return null;
            }

            Point2D? coords = Helpers.CoordStringToPoint(coordsString);
            if (coords == null)
                return null;

            return new Waypoint() { Identifier = waypointIndex, Position = coords.Value };
        }

        public void ParseEditorInfo(IniFile iniFile)
        {
            EditorColor = iniFile.GetStringValue("EditorWaypointInfo", Identifier.ToString(CultureInfo.InvariantCulture), null);
        }

        public void WriteToIniFile(IniFile iniFile)
        {
            int tileIndex = Position.Y * 1000 + Position.X;
            iniFile.SetIntValue("Waypoints", Identifier.ToString(), tileIndex);

            // Write entry to [EditorWaypointInfo]
            if (EditorColor != null)
                iniFile.SetStringValue("EditorWaypointInfo", Identifier.ToString(CultureInfo.InvariantCulture), EditorColor);
        }

        public override RTTIType WhatAmI() => RTTIType.Waypoint;
    }
}
