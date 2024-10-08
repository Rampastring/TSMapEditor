using System;
using Rampastring.Tools;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TSMapEditor.GameMath;
using TSMapEditor.UI;

namespace TSMapEditor.Models
{
    public class ConnectedOverlayFrame
    {
        public OverlayType OverlayType { get; set; }
        public int FrameIndex { get; set; }
        public byte ConnectsTo { get; set; }
    }

    public class ConnectedOverlayType
    {
        public ConnectedOverlayType(IniSection iniSection, Rules rules)
        {
            Name = iniSection.SectionName;
            UIName = iniSection.GetStringValue("UIName", Name);
            FrameCount = iniSection.GetIntValue("Frames", 0);

            if (FrameCount < 1)
                throw new INIConfigException ($"Connected overlay type {Name} has an invalid frame count {FrameCount}!");

            string connectionMaskString = iniSection.GetStringValue("ConnectionMask", null);
            if (connectionMaskString == null || connectionMaskString.Length != (int)Direction.Count || Regex.IsMatch(connectionMaskString, "[^01]"))
                throw new INIConfigException ($"Connected overlay type {Name} has an invalid connection mask {connectionMaskString}!");

            ConnectionMask = Convert.ToByte(connectionMaskString, 2);

            for (int i = 0; i < FrameCount; i++)
            {
                string overlayName = iniSection.GetStringValue($"Frame{i}.Overlay", null);
                OverlayType overlayType = rules.FindOverlayType(overlayName) ??
                              throw new INIConfigException ($"Connected overlay type {Name}, frame {i} has an invalid overlay name {overlayName}!");

                int frameIndex = iniSection.GetIntValue($"Frame{i}.FrameIndex", -1);
                if (frameIndex < 0)
                    throw new INIConfigException ($"Connected overlay type {Name}, frame {i} has an invalid frame index {frameIndex}!");

                string connectsToString = iniSection.GetStringValue($"Frame{i}.ConnectsTo", null);
                if (connectsToString == null || connectsToString.Length != (int)Direction.Count || Regex.IsMatch(connectsToString, "[^01]"))
                    throw new INIConfigException ($"Connected overlay type {Name}, frame {i} has an invalid ConnectsTo mask {connectsToString}!");

                byte connectsTo = Convert.ToByte(connectsToString, 2);

                Frames.Add(new ConnectedOverlayFrame()
                {
                    OverlayType = overlayType,
                    FrameIndex = frameIndex,
                    ConnectsTo = connectsTo
                });
            }
        }

        public string Name { get; protected set; }
        public string UIName { get; protected set; }
        public int FrameCount { get; protected set; }
        public byte ConnectionMask { get; protected set; }
        public List<ConnectedOverlayFrame> Frames { get; protected set; } = new();
        public List<ConnectedOverlayType> RelatedOverlays { get; protected set; } = new();
        private readonly Random random = new ();

        public void InitializeRelatedOverlays(IniSection iniSection, List<ConnectedOverlayType> connectedOverlays)
        {
            var relatedOverlayNames = iniSection.GetStringValue("RelatedOverlays", string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var relatedOverlayName in relatedOverlayNames)
            {
                var relatedOverlay = connectedOverlays.Find(co => co.Name == relatedOverlayName);
                if (relatedOverlay == null)
                    throw new INIConfigException($"Connected overlay type {Name} has an invalid related overlay {relatedOverlayName}!");

                RelatedOverlays.Add(relatedOverlay);
            }
        }

        public ConnectedOverlayFrame GetOverlayForCell(IMutationTarget mutationTarget, Point2D cellCoords)
        {
            byte connectionMask = 0;

            for (int direction = 0; direction < (int)Direction.Count; direction++)
            {
                var tile = mutationTarget.Map.GetTile(cellCoords + Helpers.VisualDirectionToPoint((Direction)direction));

                if (tile?.Overlay == null)
                    continue;

                if (ContainsOverlay(tile.Overlay))
                    connectionMask |= (byte)(0b10000000 >> direction);
            }

            connectionMask &= ConnectionMask;
            var frames = Frames.FindAll(frame => frame.ConnectsTo == connectionMask);

            if (frames.Count == 0)
                return Frames[0];

            return frames.ElementAt(random.Next(frames.Count));
        }

        public bool ContainsOverlay(Overlay overlay, bool searchRelated = true)
        {
            if (overlay == null)
                return false;

            return ContainsOverlay(overlay.OverlayType, overlay.FrameIndex, searchRelated);
        }

        public bool ContainsOverlay(OverlayType overlayType, int frameIndex, bool searchRelated = true)
        {
            return GetContainedConnectedOverlayType(overlayType, frameIndex, searchRelated) != null;
        }

        public ConnectedOverlayType GetContainedConnectedOverlayType(Overlay overlay, bool searchRelated = true)
        {
            if (overlay == null)
                return null;

            return GetContainedConnectedOverlayType(overlay.OverlayType, overlay.FrameIndex, searchRelated);
        }

        public ConnectedOverlayType GetContainedConnectedOverlayType(OverlayType overlayType, int frameIndex, bool searchRelated = true)
        {
            foreach (var frame in Frames)
            {
                if (overlayType == frame.OverlayType &&
                    frameIndex == frame.FrameIndex)
                {
                    return this;
                }
            }

            if (searchRelated)
            {
                foreach (var relatedOverlay in RelatedOverlays)
                {
                    if (relatedOverlay.ContainsOverlay(overlayType, frameIndex, searchRelated = false))
                        return relatedOverlay;
                }
            }

            return null;
        }
    }

}
