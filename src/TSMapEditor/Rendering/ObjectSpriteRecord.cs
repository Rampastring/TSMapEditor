using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering
{
    public struct ObjectSpriteEntry
    {
        public Texture2D PaletteTexture; // 8 bytes
        public Texture2D Texture;        // 16 bytes
        public Rectangle DrawingBounds;        // 24 bytes
        public Color Color;              // 28 bytes
        public bool UseRemap;            // 29 bytes
        public bool UseShadow;           // 30 bytes
        public float Depth;              // 34 bytes

        public ObjectSpriteEntry(Texture2D paletteTexture, Texture2D texture, Rectangle drawingBounds, Color color, bool useRemap, bool useShadow, float depth)
        {
            PaletteTexture = paletteTexture;
            Texture = texture;
            DrawingBounds = drawingBounds;
            Color = color;
            UseRemap = useRemap;
            UseShadow = useShadow;
            Depth = depth;
        }
    }

    public struct ObjectDetailEntry
    {
        public Texture2D Texture;
        public Rectangle DrawingBounds;
        public Color Color;
        public float Depth;

        public ObjectDetailEntry(Texture2D texture, Rectangle drawingBounds, Color color, float depth)
        {
            Texture = texture;
            DrawingBounds = drawingBounds;
            Color = color;
            Depth = depth;
        }
    }

    public struct ShadowEntry
    {
        public Texture2D Texture;
        public Rectangle DrawingBounds;
        public float Depth;

        public ShadowEntry(Texture2D texture, Rectangle drawingBounds, float depth)
        {
            Texture = texture;
            DrawingBounds = drawingBounds;
            Depth = depth;
        }
    }

    public struct TextEntry
    {
        public string Text;
        public Color Color;
        public Point2D DrawPoint;

        public TextEntry(string text, Color color, Point2D drawPoint)
        {
            Text = text;
            Color = color;
            DrawPoint = drawPoint;
        }
    }

    public struct LineEntry
    {
        public Vector2 Source;
        public Vector2 Destination;
        public Color Color;
        public int Thickness;
        public float Depth;

        public LineEntry(Vector2 source, Vector2 destination, Color color, int thickness, float depth)
        {
            Source = source;
            Destination = destination;
            Color = color;
            Thickness = thickness;
            Depth = depth;
        }
    }

    /// <summary>
    /// Makes it possible to batch sprites that are originally
    /// processed in any order, with any kinds of required shader settings.
    /// 
    /// Keeps track of sprites and other graphics that should be drawn as the
    /// renderer processes objects. Finally, the renderer can process the lists
    /// of this class to draw the objects.
    /// </summary>
    public class ObjectSpriteRecord
    {
        public Dictionary<(Texture2D, bool), List<ObjectDetailEntry>> SpriteEntries = new Dictionary<(Texture2D, bool), List<ObjectDetailEntry>>();
        public List<ObjectDetailEntry> NonPalettedSpriteEntries = new List<ObjectDetailEntry>();
        public List<ShadowEntry> ShadowEntries = new List<ShadowEntry>();
        public List<TextEntry> TextEntries = new List<TextEntry>();
        public List<LineEntry> LineEntries = new List<LineEntry>();
        public HashSet<GameObject> ProcessedObjects = new HashSet<GameObject>();

        public void AddGraphicsEntry(in ObjectSpriteEntry entry)
        {
            if (entry.Texture == null)
                throw new ArgumentNullException(nameof(entry));

            // Shadows are handled separately
            if (entry.UseShadow)
            {
                ShadowEntries.Add(new ShadowEntry(entry.Texture, entry.DrawingBounds, entry.Depth));
                return;
            }

            // If the entry has no palette, we can store it separately
            if (entry.PaletteTexture == null)
            {
                NonPalettedSpriteEntries.Add(new ObjectDetailEntry(entry.Texture, entry.DrawingBounds, new Color(entry.Color.R / 255.0f, entry.Color.G / 255.0f, entry.Color.B / 255.0f, entry.Depth), entry.Depth));
                return;
            }

            // Paletted entries, with or without remap
            var key = (entry.PaletteTexture, entry.UseRemap);
            bool success = SpriteEntries.TryGetValue(key, out var list);
            if (!success)
            {
                list = new List<ObjectDetailEntry>();
                SpriteEntries.Add(key, list);
            }

            list.Add(new ObjectDetailEntry(entry.Texture, entry.DrawingBounds, entry.Color, entry.Depth));
        }

        public void AddTextEntry(TextEntry textEntry) => TextEntries.Add(textEntry);

        public void AddLineEntry(LineEntry lineEntry) => LineEntries.Add(lineEntry);

        public void Clear(bool noShadow)
        {
            foreach (var list in SpriteEntries.Values)
            {
                list.Clear();
            }

            NonPalettedSpriteEntries.Clear();

            if (!noShadow)
                ShadowEntries.Clear();

            TextEntries.Clear();

            LineEntries.Clear();

            ProcessedObjects.Clear();
        }
    }
}
