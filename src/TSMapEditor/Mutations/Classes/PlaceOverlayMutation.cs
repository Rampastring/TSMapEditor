using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that allows placing regular, individual overlay.
    /// </summary>
    class PlaceOverlayMutation : Mutation
    {
        public PlaceOverlayMutation(IMutationTarget mutationTarget, OverlayType overlayType, int? forcedFrameIndex, Point2D cellCoords) : base(mutationTarget)
        {
            this.overlayType = overlayType;
            this.forcedFrameIndex = forcedFrameIndex;
            this.cellCoords = cellCoords;
            brush = mutationTarget.BrushSize;
        }

        private readonly OverlayType overlayType;
        private readonly int? forcedFrameIndex;
        private readonly BrushSize brush;
        private readonly Point2D cellCoords;

        private OriginalOverlayInfo[] undoData;

        public override void Perform()
        {
            var originalOverlayInfos = new List<OriginalOverlayInfo>();

            brush.DoForBrushSize(offset =>
            {
                var tile = MutationTarget.Map.GetTile(cellCoords + offset);
                if (tile == null)
                    return;

                originalOverlayInfos.Add(new OriginalOverlayInfo()
                {
                    CellCoords = tile.CoordsToPoint(),
                    OverlayTypeIndex = tile.Overlay == null ? -1 : tile.Overlay.OverlayType.Index,
                    FrameIndex = tile.Overlay == null ? -1 : tile.Overlay.FrameIndex,
                });

                if (overlayType != null)
                {
                    tile.Overlay = new Overlay()
                    {
                        Position = tile.CoordsToPoint(),
                        OverlayType = overlayType,
                        FrameIndex = forcedFrameIndex == null ? 0 : forcedFrameIndex.Value
                    };
                }
                else
                {
                    tile.Overlay = null;
                }
            });

            if (!forcedFrameIndex.HasValue)
            {
                for (int y = -brush.Height - 1; y <= brush.Height + 1; y++)
                {
                    for (int x = -brush.Width - 1; x <= brush.Width + 1; x++)
                    {
                        SetOverlayFrameIndexForTile(cellCoords + new Point2D(x, y));
                    }
                }
            }

            undoData = originalOverlayInfos.ToArray();
            MutationTarget.AddRefreshPoint(cellCoords, Math.Max(brush.Width, brush.Height) + 1);
        }

        private void SetOverlayFrameIndexForTile(Point2D cellCoords)
        {
            var tile = MutationTarget.Map.GetTile(cellCoords);
            if (tile == null)
                return;

            if (tile.Overlay == null)
                return;

            tile.Overlay.FrameIndex = MutationTarget.Map.GetOverlayFrameIndex(cellCoords);
        }

        public override void Undo()
        {
            foreach (OriginalOverlayInfo info in undoData)
            {
                var tile = MutationTarget.Map.GetTile(info.CellCoords);
                if (info.OverlayTypeIndex == -1)
                {
                    tile.Overlay = null;
                    continue;
                }

                tile.Overlay = new Overlay()
                {
                    OverlayType = MutationTarget.Map.Rules.OverlayTypes[info.OverlayTypeIndex],
                    Position = info.CellCoords,
                    FrameIndex = info.FrameIndex
                };
            }

            if (!forcedFrameIndex.HasValue)
            {
                for (int y = -brush.Height - 1; y <= brush.Height + 1; y++)
                {
                    for (int x = -brush.Width - 1; x <= brush.Width + 1; x++)
                    {
                        SetOverlayFrameIndexForTile(cellCoords + new Point2D(x, y));
                    }
                }
            }

            MutationTarget.AddRefreshPoint(cellCoords, Math.Max(brush.Width, brush.Height) + 1);
        }
    }
}
