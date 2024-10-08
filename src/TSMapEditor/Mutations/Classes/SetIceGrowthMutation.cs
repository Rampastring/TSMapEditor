using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation for toggling the "IceGrowth" bit of cells.
    /// </summary>
    public class SetIceGrowthMutation : Mutation
    {
        public SetIceGrowthMutation(IMutationTarget mutationTarget, Point2D cellCoords, bool enableIceGrowth) : base(mutationTarget)
        {
            this.enableIceGrowth = enableIceGrowth;
            this.cellCoords = cellCoords;
        }

        private readonly bool enableIceGrowth;
        private readonly Point2D cellCoords;
        private BrushSize brushSize;

        private List<Point2D> undoData;

        public override void Perform()
        {
            brushSize = MutationTarget.BrushSize;
            undoData = new List<Point2D>(brushSize.Width * brushSize.Height);

            brushSize.DoForBrushSize(brushOffset =>
            {
                Point2D coords = cellCoords + brushOffset;

                var cell = MutationTarget.Map.GetTile(coords);
                if (cell == null)
                    return;

                if (enableIceGrowth)
                {
                    if (cell.IceGrowth <= 0)
                    {
                        cell.IceGrowth = 1;
                        undoData.Add(coords);
                    }
                }
                else
                {
                    if (cell.IceGrowth > 0)
                    {
                        cell.IceGrowth = 0;
                        undoData.Add(coords);
                    }    
                }
            });

            MutationTarget.AddRefreshPoint(cellCoords, Math.Max(brushSize.Width, brushSize.Height));
        }

        public override void Undo()
        {
            foreach (Point2D coords in undoData)
            {
                var cell = MutationTarget.Map.GetTileOrFail(coords);

                if (enableIceGrowth)
                {
                    cell.IceGrowth = 0;
                }
                else
                {
                    cell.IceGrowth = 1;
                }
            }

            MutationTarget.AddRefreshPoint(cellCoords, Math.Max(brushSize.Width, brushSize.Height));
        }
    }
}
