using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TSMapEditor.GameMath;
using TSMapEditor.Models.Enums;
using TSMapEditor.Models.MapFormat;
using TSMapEditor.Rendering;

namespace TSMapEditor.Models
{
    /// <summary>
    /// A cell on the map with additional logic properties for the map editor.
    /// </summary>
    public class MapTile : IsoMapPack5Tile
    {
        private const int SubCellCount = 5;

        public MapTile() { }

        public MapTile(byte[] data) : base(data) { }

        /// <summary>
        /// The cached image for this tile.
        /// This should be cleared when the tile's terrain is changed.
        /// </summary>
        public TileImage TileImage { get; set; }
        public TerrainObject TerrainObject { get; set; }
        public List<Structure> Structures { get; set; } = new List<Structure>();
        public List<Unit> Vehicles { get; set; } = new List<Unit>();
        public List<Aircraft> Aircraft { get; set; } = new List<Aircraft>();
        public Infantry[] Infantry { get; set; } = new Infantry[SubCellCount];
        public TileImage PreviewTileImage { get; set; }
        public int PreviewSubTileIndex { get; set; }
        public int PreviewLevel { get; set; } = -1;

        public Overlay Overlay { get; set; }
        public Smudge Smudge { get; set; }
        public List<Waypoint> Waypoints { get; set; } = new List<Waypoint>();

        public CellTag CellTag { get; set; }

        /// <summary>
        /// A list of objects that graphically overlap with this tile.
        /// When this tile is re-drawn, all the objects in the list should
        /// be re-drawn as well.
        /// </summary>
        public List<AbstractObject> OverlapList { get; set; }

        /// <summary>
        /// The number of the screen refresh iteration when
        /// this map tile was last rendered. If this value matches the current
        /// rendering iteration, there is no need to draw this tile.
        /// </summary>
        public ulong LastRefreshIndex;

        public MapColor CellLighting { get; set; } = new MapColor(1.0, 1.0, 1.0);

        public List<(Structure Source, double DistanceInLeptons)> LightSources { get; set; } = new();

        public void RefreshLighting(Lighting lighting, LightingPreviewMode lightingPreviewMode)
        {
            if (lightingPreviewMode == LightingPreviewMode.NoLighting)
            {
                CellLighting = new MapColor(1.0, 1.0, 1.0);
                return;
            }

            double globalAmbient = lighting.GetAmbientComponent(lightingPreviewMode);
            double globalLevel = lighting.GetLevelComponent(lightingPreviewMode);
            double globalGround = lighting.GetGroundComponent(lightingPreviewMode);
            double globalRed = lighting.GetRedComponent(lightingPreviewMode);
            double globalGreen = lighting.GetGreenComponent(lightingPreviewMode);
            double globalBlue = lighting.GetBlueComponent(lightingPreviewMode);

            double redDivisor = globalRed >= 1.0 ? globalRed : 1.0;
            double greenDivisor = globalGreen >= 1.0 ? globalGreen : 1.0;
            double blueDivisor = globalBlue >= 1.0 ? globalBlue : 1.0;

            double cellAmbient = globalAmbient;
            double cellR = globalRed;
            double cellG = globalGreen;
            double cellB = globalBlue;

            // Apply Ground
            cellAmbient *= (1.0 - globalGround);

            // Apply Level
            cellAmbient += globalLevel * Level;

            // Check all the light sources and how they affect this light
            foreach (var source in LightSources)
            {
                double distanceRatio = 1.0 - (source.DistanceInLeptons / source.Source.ObjectType.LightVisibility);

                // Intensity modifies the cell ambient value.
                // For example, if Ambient=0.5 and LightIntensity=1.0, in a cell that is fully
                // lit by the light post, the overall ambient level becomes 0.5 + 1.0 = 1.5
                cellAmbient += source.Source.ObjectType.LightIntensity * distanceRatio;

                // Apply tint. Tint does NOT depend on LightIntensity, but is independent of it
                // (as long as LightIntensity != 0).
                // Strength of tint depends on strength of global tint. For example, adding local red of 1.0
                // to global red of 1.5 leads to a much smaller change than if the local red was added to global red of 0.5.
                double redStrength = (source.Source.ObjectType.LightRedTint / redDivisor) * distanceRatio;
                double greenStrength = (source.Source.ObjectType.LightGreenTint / greenDivisor) * distanceRatio;
                double blueStrength = (source.Source.ObjectType.LightBlueTint / blueDivisor) * distanceRatio;

                cellR += redStrength;
                cellG += greenStrength;
                cellB += blueStrength;
            }

            const double lightingComponentMax = 2.0;

            // Apply Ambient to all components
            cellR *= cellAmbient;
            cellG *= cellAmbient;
            cellB *= cellAmbient;

            // In case the components exceed 2.0, they are all scaled down to fit within 0.0 to 2.0
            double highestComponentValue = Math.Max(cellR, Math.Max(cellG, cellB));
            if (highestComponentValue > lightingComponentMax)
            {
                double scale = lightingComponentMax / highestComponentValue;
                cellR *= scale;
                cellG *= scale;
                cellB *= scale;
            }

            CellLighting = new MapColor(cellR, cellG, cellB);
        }


        public void ShiftPosition(int x, int y)
        {
            X += (short)x; 
            Y += (short)y;

            // If we have overlay and/or a smudge, also move their position
            if (Overlay != null)
                Overlay.Position += new Point2D(x, y);

            if (Smudge != null)
                Smudge.Position += new Point2D(x, y);
        }

        public void AddObjectsToList(List<AbstractObject> objects)
        {
            if (Structures.Count > 0)
                objects.AddRange(Structures);

            if (Vehicles.Count > 0)
                objects.AddRange(Vehicles);
        }

        public void AddInfantry(Infantry infantry)
        {
            Infantry[(int)infantry.SubCell] = infantry;
        }

        public void DoForAllInfantry(Action<Infantry> action)
        {
            for (int i = 0; i < Infantry.Length; i++)
            {
                if (Infantry[i] != null)
                    action(Infantry[i]);
            }
        }

        public void DoForAllVehicles(Action<Unit> action)
        {
            foreach (var unit in Vehicles)
            {
                action(unit);
            }
        }

        public void DoForAllAircraft(Action<Aircraft> action)
        {
            foreach (var aircraft in Aircraft)
            {
                action(aircraft);
            }
        }

        public void DoForAllBuildings(Action<Structure> action)
        {
            foreach (var structure in Structures)
            {
                action(structure);
            }
        }

        public void DoForAllWaypoints(Action<Waypoint> action)
        {
            foreach (var waypoint in Waypoints)
            {
                action(waypoint);
            }
        }       

        public SubCell GetSubCellClosestToPosition(Point2D position, bool onlyOccupiedCells)
        { 
            IEnumerable<SubCell> subCells = [SubCell.Bottom, SubCell.Left, SubCell.Right];
            if (onlyOccupiedCells)
            {
                subCells = subCells.Where(sc => GetInfantryFromSubCellSpot(sc) != null);
            }

            SubCell closestSubcell = SubCell.None;
            float shortestDistance = float.MaxValue;

            foreach (var subCell in subCells)
            {
                var subCellCoords = GetTileCenter() + CellMath.GetSubCellOffset(subCell);
                var distanceToSubCell = Vector2.Distance(position.ToXNAVector(), subCellCoords.ToXNAVector());
                if (distanceToSubCell < shortestDistance)
                {
                    closestSubcell = subCell;
                    shortestDistance = distanceToSubCell;
                }
            }

            return closestSubcell;
        }

        public SubCell GetFreeSubCellSpot()
        {
            if (GetInfantryFromSubCellSpot(SubCell.Bottom) == null)
                return SubCell.Bottom;

            if (GetInfantryFromSubCellSpot(SubCell.Left) == null)
                return SubCell.Left;

            if (GetInfantryFromSubCellSpot(SubCell.Right) == null)
                return SubCell.Right;

            return SubCell.None;
        }

        public Infantry GetInfantryFromSubCellSpot(SubCell subCell)
        {
            return Infantry[(int)subCell];
        }

        public Infantry GetFirstInfantry()
        {
            for (int i = 0; i < Infantry.Length; i++)
            {
                if (Infantry[i] != null)
                    return Infantry[i];
            }

            return null;
        }

        public bool HasInfantry() => GetFirstInfantry() != null;

        public bool HasTechno()
        {
            return Structures.Count > 0 || Vehicles.Count > 0 || Aircraft.Count > 0 || Array.Exists(Infantry, inf => inf != null);
        }

        public bool HasTechnoThatPassesCheck(Predicate<TechnoBase> predicate)
        {
            return GetFirstTechnoThatPassesCheck(predicate) != null;
        }

        public TechnoBase GetFirstTechnoThatPassesCheck(Predicate<TechnoBase> predicate)
        {
            var structure = Structures.Find(predicate);
            if (structure != null) return structure;

            var unit = Vehicles.Find(predicate);
            if (unit != null) return unit;

            var aircraft = Aircraft.Find(predicate);
            if (aircraft != null) return aircraft;

            return Array.Find(Infantry, inf => inf != null && predicate(inf));
        }

        public TechnoBase GetTechno(Point2D? position = null)
        {
            if (Structures.Count > 0)
                return Structures[0];

            if (Vehicles.Count > 0)
                return Vehicles[0];

            if (Aircraft.Count > 0)
                return Aircraft[0];

            if (position != null)
            {
                var closestSubcell = GetSubCellClosestToPosition((Point2D)position, true);
                if (closestSubcell == SubCell.None)
                    return null;

                return GetInfantryFromSubCellSpot(closestSubcell);                
            }
            else
            {
                return GetFirstInfantry();
            }            
        }        

        public GameObject GetObject(Point2D? position = null)
        {
            GameObject obj = GetTechno(position);
            if (obj != null)
                return obj;

            if (TerrainObject != null)
                return TerrainObject;

            return null;
        }

        /// <summary>
        /// Determines whether a specific game object can be assigned to this tile.
        /// </summary>
        public bool CanAddObject(GameObject gameObject, bool blocksSelf, bool overlapObjects)
        {
            switch (gameObject.WhatAmI())
            {
                case RTTIType.Building:
                {
                    bool multipleStructuresExist = Structures.Count > 1;
                    bool anotherExists = Structures.Count == 1 && !Structures.Contains((Structure)gameObject);
                    bool clone = Structures.Count == 1 && Structures.Contains((Structure)gameObject) && blocksSelf;

                    if ((multipleStructuresExist || anotherExists || clone) && !overlapObjects)
                        return false;
                    return true;
                }
                case RTTIType.Unit:
                    return Vehicles.Count == 0 || overlapObjects;
                case RTTIType.Aircraft:
                    return Aircraft.Count == 0 || overlapObjects;
                case RTTIType.Infantry:
                    return GetFreeSubCellSpot() != SubCell.None;
                case RTTIType.Terrain:
                    return TerrainObject == null;
            }

            return false;
        }

        public bool ContainsObject(AbstractObject abstractObject)
        {
            switch (abstractObject.WhatAmI())
            {
                case RTTIType.Aircraft:
                    return Aircraft.Contains((Aircraft)abstractObject);
                case RTTIType.Terrain:
                    return TerrainObject == abstractObject;
                case RTTIType.Building:
                    return Structures.Contains((Structure)abstractObject);
                case RTTIType.Unit:
                    return Vehicles.Contains((Unit)abstractObject);
                case RTTIType.Infantry:
                    return Array.Exists(Infantry, inf => inf == abstractObject);
                case RTTIType.Overlay:
                    return Overlay == abstractObject;
                case RTTIType.Smudge:
                    return Smudge == abstractObject;
                case RTTIType.Waypoint:
                    return Waypoints.Contains((Waypoint)abstractObject);
            }

            return false;
        }

        public bool IsClearGround()
        {
            return TileIndex == 0;
        }

        /// <summary>
        /// Returns a value that tells whether the cell has a harvestable resource (tiberium or ore) on it.
        /// </summary>
        public bool HasTiberium()
        {
            return Overlay != null && Overlay.OverlayType != null && Overlay.OverlayType.Tiberium;
        }

        public void ChangeTileIndex(int newTileIndex, byte newSubTileIndex)
        {
            TileImage = null;
            TileIndex = newTileIndex;
            SubTileIndex = newSubTileIndex;
        }

        public Point2D CoordsToPoint() => new Point2D(X, Y);

        public Point2D GetTileCenter() => new Point2D(Constants.CellSizeX / 2, Constants.CellSizeY / 2);
    }
}
