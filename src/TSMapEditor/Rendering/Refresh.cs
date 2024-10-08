using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering
{
    class Refresh
    {
        public Refresh(Map map, int refreshSizeSetting)
        {
            this.map = map;
            this.refreshSizeSetting = refreshSizeSetting;
        }

        
        private readonly Map map;
        private int refreshSizeSetting;

        public Dictionary<int, MapTile> tilesToRedraw = new Dictionary<int, MapTile>();
        public Dictionary<int, GameObject> objectsToRedraw = new Dictionary<int, GameObject>();

        public bool IsInitiated { get; private set; } = false;
        public bool IsComplete { get; private set; } = false;
        public Point2D InitPoint { get; private set; }


        private LinkedList<MapTile> tilesToProcess = new LinkedList<MapTile>();
        private HashSet<int> processedTiles = new HashSet<int>();

        public void RedrawFromObject(GameObject gameObject)
        {
            int hash = GetGameObjectHash(gameObject);
            if (objectsToRedraw.ContainsKey(hash))
                return;

            objectsToRedraw[GetGameObjectHash(gameObject)] = gameObject;

            int redrawArea = 2;
            if (gameObject.WhatAmI() == RTTIType.Infantry)
                redrawArea = 1;
            else if (gameObject.WhatAmI() == RTTIType.Unit || gameObject.WhatAmI() == RTTIType.Aircraft)
                redrawArea = 1;
            else if (gameObject.WhatAmI() == RTTIType.Terrain || gameObject.WhatAmI() == RTTIType.Smudge)
                redrawArea = 1;

            for (int y = -redrawArea; y <= redrawArea; y++)
            {
                for (int x = -redrawArea; x <= redrawArea; x++)
                {
                    AddTileToRedrawFrom(gameObject.Position + new Point2D(x, y));
                }
            }
        }

        public void Initiate(int areaSize, Point2D cellCoords)
        {
            if (IsInitiated)
                throw new InvalidOperationException("RedrawFromCell should only be called once for a Refresh instance.");

            IsInitiated = true;
            InitPoint = cellCoords;

            for (int y = -areaSize; y <= areaSize; y++)
            {
                for (int x = -areaSize; x <= areaSize; x++)
                {
                    var coords = cellCoords + new Point2D(x, y);
                    var cell = map.GetTile(coords);
                    if (cell == null)
                        continue;

                    tilesToProcess.AddLast(cell);
                    processedTiles.Add(GetMapTileHash(cell));
                }
            }
        }

        public void Process()
        {
            const int singleFrameProcessLimit = 1500;
            int i = 0;
            while (i < singleFrameProcessLimit && tilesToProcess.First != null)
            {
                var cell = tilesToProcess.First.Value;
                RedrawTile(cell);
                tilesToProcess.RemoveFirst();
                i++;
            }

            if (tilesToProcess.First == null)
            {
                IsComplete = true;
            }
        }

        private void RedrawTile(MapTile mapTile)
        {
            if (AddTileToRedraw(mapTile))
            {
                // Point2D eastCellCoords = mapTile.CoordsToPoint() + new Point2D(1, -1);
                // Point2D southCellCoords = mapTile.CoordsToPoint() + new Point2D(1, 1);
                // Point2D southWestCellCoords = mapTile.CoordsToPoint() + new Point2D(0, 1);
                // Point2D southEastCellCoords = mapTile.CoordsToPoint() + new Point2D(1, 0);
                // 
                // if (mapTile.Overlay != null)
                // {
                //     AddTileToRedrawFrom(eastCellCoords);
                //     AddTileToRedrawFrom(southCellCoords);
                //     AddTileToRedrawFrom(southWestCellCoords);
                //     AddTileToRedrawFrom(southEastCellCoords);
                // }

                mapTile.DoForAllInfantry(inf => RedrawFromObject(inf));
                mapTile.DoForAllVehicles(unit => RedrawFromObject(unit));
                mapTile.DoForAllAircraft(aircraft => RedrawFromObject(aircraft));
                if (mapTile.TerrainObject != null)
                    RedrawFromObject(mapTile.TerrainObject);

                int size = refreshSizeSetting;
                for (int y = -size; y <= size; y++)
                {
                    for (int x = -size; x <= size; x++)
                    {
                        var cell = map.GetTile(mapTile.CoordsToPoint() + new Point2D(x, y));
                        if (cell != null)
                        {
                            if (cell.GetObject() != null)
                                RedrawFromObject(cell.GetObject());

                            if (cell.Overlay != null)
                                RedrawTile(cell);

                            if (cell.Smudge != null)
                                RedrawTile(cell);
                        }
                            
                    }
                }
            }
        }

        private void AddTileToRedrawFrom(Point2D cellCoords)
        {
            var mapTile = map.GetTile(cellCoords);
            if (mapTile != null)
            {
                int hash = GetMapTileHash(mapTile);
                if (!processedTiles.Contains(hash))
                {
                    tilesToProcess.AddLast(mapTile);
                }
            }
        }

        private bool AddTileToRedraw(MapTile mapTile)
        {
            int hash = GetMapTileHash(mapTile);
            if (!tilesToRedraw.ContainsKey(hash))
            {
                tilesToRedraw.Add(hash, mapTile);
                processedTiles.Add(hash);
                return true;
            }

            return false;
        }

        private int GetMapTileHash(MapTile mapTile)
        {
            return mapTile.Y * 1000 + mapTile.X;
        }

        private int GetGameObjectHash(GameObject gameObject)
        {
            if (gameObject.WhatAmI() == RTTIType.Infantry)
            {
                return (int)gameObject.WhatAmI() * 1000000 + gameObject.Position.Y * 10000 + gameObject.Position.X + (int)((Infantry)gameObject).SubCell;
            }

            return (int)gameObject.WhatAmI() * 100000 + gameObject.Position.Y * 1000 + gameObject.Position.X;
        }
    }
}
