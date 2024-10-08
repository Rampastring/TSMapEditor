using Microsoft.Xna.Framework;

namespace TSMapEditor.Misc
{
    public static class NamedColors
    {        
        public static NamedColor[] GenericSupportedNamedColors = new NamedColor[]
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
    }

    public struct NamedColor
    {
        public string Name;
        public Color Value;

        public NamedColor(string name, Color value)
        {
            Name = name;
            Value = value;
        }
    }
}
