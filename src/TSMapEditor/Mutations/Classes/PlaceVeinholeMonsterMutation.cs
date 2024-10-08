using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes.HeightMutations;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    public class PlaceVeinholeMonsterMutation : FSLowerGroundMutation
    {
        public PlaceVeinholeMonsterMutation(IMutationTarget mutationTarget, Point2D cellCoords) : 
            base(mutationTarget, cellCoords, mutationTarget.Map.EditorConfig.BrushSizes.Find(bs => bs.Width == 3 && bs.Height == 3))
        {
            this.cellCoords = cellCoords;
        }

        private readonly Point2D cellCoords;

        private List<OriginalOverlayInfo> originalOverlayInfos = new();

        private void AddUndoDataFromCell(MapTile cell)
        {
            originalOverlayInfos.Add(new OriginalOverlayInfo()
            {
                CellCoords = cell.CoordsToPoint(),
                OverlayTypeIndex = cell.Overlay == null ? -1 : cell.Overlay.OverlayType.Index,
                FrameIndex = cell.Overlay == null ? -1 : cell.Overlay.FrameIndex,
            });
        }

        public override void Perform()
        {
            if (!Constants.IsFlatWorld)
                base.Perform();

            var veinholeMonsterOverlayType = Map.Rules.OverlayTypes.Find(ot => ot.ININame == Constants.VeinholeMonsterTypeName);
            var veinholeDummyOverlayType = Map.Rules.OverlayTypes.Find(ot => ot.ININame == Constants.VeinholeDummyTypeName);

            if (veinholeMonsterOverlayType == null)
                throw new InvalidOperationException($"Overlay type \"{Constants.VeinholeMonsterTypeName}\" not found!");

            if (veinholeDummyOverlayType == null)
                throw new InvalidOperationException($"Overlay type \"{Constants.VeinholeDummyTypeName}\" not found!");

            var tile = Map.GetTile(cellCoords);

            AddUndoDataFromCell(tile);

            tile.Overlay = new Overlay() 
            { 
                Position = cellCoords,
                OverlayType = veinholeMonsterOverlayType,
                FrameIndex = 0 
            };

            for (int i = 0; i < (int)Direction.Count; i++)
            {
                Point2D dummyCellCoords = cellCoords + Helpers.VisualDirectionToPoint((Direction)i);

                tile = Map.GetTile(dummyCellCoords);

                if (tile == null)
                    continue;

                AddUndoDataFromCell(tile);
                tile.Overlay = new Overlay()
                {
                    Position = dummyCellCoords,
                    OverlayType = veinholeDummyOverlayType,
                    FrameIndex = 0
                };
            }

            MutationTarget.AddRefreshPoint(cellCoords, 2);
        }

        public override void Undo()
        {
            if (!Constants.IsFlatWorld)
                base.Undo();

            foreach (OriginalOverlayInfo info in originalOverlayInfos)
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

            MutationTarget.AddRefreshPoint(cellCoords, 2);
        }
    }
}
