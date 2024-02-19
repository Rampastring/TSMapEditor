using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.Rendering.ObjectRenderers;
using TSMapEditor.Settings;

namespace TSMapEditor.Rendering
{
    /// <summary>
    /// An interface for an object that can be used to fetch 
    /// game logic related information about a theater.
    /// </summary>
    public interface ITheater
    {
        int GetTileSetId(int uniqueTileIndex);
        int TileCount { get; }
        ITileImage GetTile(int id);
        int GetOverlayFrameCount(OverlayType overlayType);
        Theater Theater { get; }
    }

    public class VoxelModel : IDisposable
    {
        public VoxelModel(GraphicsDevice graphicsDevice, VxlFile vxl, HvaFile hva, XNAPalette palette,
            bool remapable = false, bool subjectToLighting = false, VplFile vpl = null)
        {
            this.graphicsDevice = graphicsDevice;
            this.vxl = vxl;
            this.hva = hva;
            this.vpl = vpl;
            this.palette = palette;
            this.remapable = remapable;
            this.subjectToLighting = subjectToLighting;
        }

        private readonly GraphicsDevice graphicsDevice;
        private readonly VxlFile vxl;
        private readonly HvaFile hva;
        private readonly VplFile vpl;
        private readonly XNAPalette palette;
        private readonly bool remapable;
        private readonly bool subjectToLighting;

        public void Dispose()
        {
            foreach (var frame in Frames)
                frame.Value?.Dispose();

            foreach (var frame in RemapFrames)
                frame.Value?.Dispose();
        }

        public PositionedTexture GetFrame(byte facing, RampType ramp, bool affectedByLighting)
        {
            // The game only renders 32 facings, so round it to the closest true facing
            facing = Convert.ToByte(Math.Clamp(
                Math.Round((float)facing / 8, MidpointRounding.AwayFromZero) * 8,
                byte.MinValue,
                byte.MaxValue));

            var key = (facing, ramp);
            if (Frames.TryGetValue(key, out PositionedTexture value))
                return value;

            Palette palette = this.palette.GetPalette(subjectToLighting && affectedByLighting);
            var texture = VxlRenderer.Render(graphicsDevice, facing, ramp, vxl, hva, palette, vpl, forRemap: false);
            if (texture == null)
            {
                Frames[key] = null;
                return Frames[key];
            }

            var positionedTexture = new PositionedTexture(texture.Width, texture.Height, 0, 0, texture);
            Frames[key] = positionedTexture;
            return Frames[key];
        }

        public PositionedTexture GetRemapFrame(byte facing, RampType ramp, bool affectedByLighting)
        {
            if (!(remapable && Constants.HQRemap))
                return null;

            // The game only renders 32 facings, so round it to the closest true facing
            facing = Convert.ToByte(Math.Clamp(
                Math.Round((float)facing / 8, MidpointRounding.AwayFromZero) * 8,
                byte.MinValue,
                byte.MaxValue));

            var key = (facing, ramp);
            if (RemapFrames.TryGetValue(key, out PositionedTexture value))
                return value;

            Palette palette = this.palette.GetPalette(subjectToLighting && affectedByLighting);
            var texture = VxlRenderer.Render(graphicsDevice, facing, ramp, vxl, hva, palette, vpl, forRemap: true);
            if (texture == null)
            {
                RemapFrames[key] = null;
                return RemapFrames[key];
            }

            var colorData = new Color[texture.Width * texture.Height];
            texture.GetData(colorData);

            // The renderer has rendered the rest of the unit as Magenta, now strip it out
            for (int i = 0; i < colorData.Length; i++)
            {
                if (colorData[i] == Color.Magenta)
                    colorData[i] = Color.Transparent;
            }

            if (Constants.HQRemap)
            {
                Color[] remapColorArray = colorData.Select(color =>
                {
                    // Convert the color to grayscale
                    float remapColor = Math.Max(color.R / 255.0f, Math.Max(color.G / 255.0f, color.B / 255.0f));

                    // Brighten it up a bit
                    remapColor *= Constants.RemapBrightenFactor;
                    return new Color(remapColor, remapColor, remapColor, color.A);

                }).ToArray();

                var remapTexture = new Texture2D(graphicsDevice, texture.Width, texture.Height, false, SurfaceFormat.Color);
                remapTexture.SetData(remapColorArray);
                RemapFrames[key] = new PositionedTexture(remapTexture.Width, remapTexture.Height, 0, 0, remapTexture);
                return RemapFrames[key];
            }
            else
            {
                // Convert colors to grayscale
                // Get HSV value, change S = 0, convert back to RGB and assign
                // With S = 0, the formula for converting HSV to RGB can be reduced to a quite simple form :)

                System.Drawing.Color[] sdColorArray = colorData.Select(c => System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B)).ToArray();
                for (int j = 0; j < sdColorArray.Length; j++)
                {
                    if (colorData[j] == Color.Transparent)
                        continue;

                    float remapColor = sdColorArray[j].GetBrightness() * Constants.RemapBrightenFactor;
                    if (remapColor > 1.0f)
                        remapColor = 1.0f;
                    colorData[j] = new Color(remapColor, remapColor, remapColor, colorData[j].A);
                }

                var remapTexture = new Texture2D(graphicsDevice, texture.Width, texture.Height, false, SurfaceFormat.Color);
                remapTexture.SetData(colorData);
                RemapFrames[key] = new PositionedTexture(remapTexture.Width, remapTexture.Height, 0, 0, remapTexture);
                return RemapFrames[key];
            }
        }

        public void ClearFrames()
        {
            Frames.Clear();
            RemapFrames.Clear();
        }

        public Dictionary<(byte facing, RampType ramp), PositionedTexture> Frames { get; set; } = new();
        public Dictionary<(byte facing, RampType ramp), PositionedTexture> RemapFrames { get; set; } = new();
    }

    public class ShapeImage : IDisposable
    {
        public ShapeImage(GraphicsDevice graphicsDevice, ShpFile shp, byte[] shpFileData, XNAPalette palette,
            bool subjectToLighting, bool remapable = false, PositionedTexture pngTexture = null)
        {
            shpFile = shp;
            this.shpFileData = shpFileData;
            Palette = palette;
            this.remapable = remapable;
            this.subjectToLighting = subjectToLighting;
            this.graphicsDevice = graphicsDevice;

            if (pngTexture != null && !remapable)
            {
                Frames = new PositionedTexture[] { pngTexture };
                return;
            }

            Frames = new PositionedTexture[shp.FrameCount];
            if (remapable && Constants.HQRemap)
                RemapFrames = new PositionedTexture[Frames.Length];
        }

        public void Dispose()
        {
            Array.ForEach(Frames, frame =>
            {
                if (frame != null)
                    frame.Dispose();
            });

            if (RemapFrames != null)
            {
                Array.ForEach(RemapFrames, frame =>
                {
                    if (frame != null)
                        frame.Dispose();
                });
            }
        }

        private XNAPalette Palette { get; }
        private ShpFile shpFile;
        private byte[] shpFileData;
        private bool remapable;
        private bool subjectToLighting;
        private GraphicsDevice graphicsDevice;

        public int GetFrameCount() => Frames.Length;

        /// <summary>
        /// Gets a specific frame of this object image if it exists, otherwise returns null.
        /// If a valid frame has not been yet converted into a texture, first converts the frame into a texture.
        /// </summary>
        public PositionedTexture GetFrame(int index)
        {
            if (Frames == null || index < 0 || index >= Frames.Length)
                return null;

            if (Frames[index] != null)
                return Frames[index];

            GenerateTexturesForFrame_Paletted(index);
            // GenerateTexturesForFrame_RGBA(index);
            return Frames[index];
        }

        public bool HasRemapFrames() => RemapFrames != null;

        public PositionedTexture GetRemapFrame(int index)
        {
            if (index < 0 || index >= RemapFrames.Length)
                return null;

            return RemapFrames[index];
        }

        public Texture2D GetPaletteTexture(bool useLighting)
        {
            return Palette.GetTexture(useLighting && subjectToLighting);
        }

        private void GetFrameInfoAndData(int frameIndex, out ShpFrameInfo frameInfo, out byte[] frameData)
        {
            frameInfo = shpFile.GetShpFrameInfo(frameIndex);
            frameData = shpFile.GetUncompressedFrameData(frameIndex, shpFileData);
        }

        public void GenerateTexturesForFrame_RGBA(int index)
        {
            GetFrameInfoAndData(index, out ShpFrameInfo frameInfo, out byte[] frameData);

            if (frameData == null)
                return;

            var texture = GetTextureForFrame_RGBA(index, frameInfo, frameData);
            Frames[index] = new PositionedTexture(shpFile.Width, shpFile.Height, frameInfo.XOffset, frameInfo.YOffset, texture);

            if (remapable)
            {
                var remapTexture = GetRemapTextureForFrame_RGBA(index, frameInfo, frameData);
                RemapFrames[index] = new PositionedTexture(shpFile.Width, shpFile.Height, frameInfo.XOffset, frameInfo.YOffset, remapTexture);
            }
        }

        public void GenerateTexturesForFrame_Paletted(int index)
        {
            GetFrameInfoAndData(index, out ShpFrameInfo frameInfo, out byte[] frameData);

            if (frameData == null)
                return;

            var texture = GetTextureForFrame_Paletted(index, frameInfo, frameData);
            Frames[index] = new PositionedTexture(shpFile.Width, shpFile.Height, frameInfo.XOffset, frameInfo.YOffset, texture);

            if (remapable)
            {
                var remapTexture = GetRemapTextureForFrame_Paletted(index, frameInfo, frameData);
                RemapFrames[index] = new PositionedTexture(shpFile.Width, shpFile.Height, frameInfo.XOffset, frameInfo.YOffset, remapTexture);
            }
        }

        public Texture2D GetTextureForFrame_Paletted(int index, ShpFrameInfo frameInfo, byte[] frameData)
        {
            var texture = new Texture2D(graphicsDevice, frameInfo.Width, frameInfo.Height, false, SurfaceFormat.Alpha8);
            texture.SetData(frameData);
            return texture;
        }

        public Texture2D GetTextureForFrame_RGBA(int index, ShpFrameInfo frameInfo, byte[] frameData)
        {
            var texture = new Texture2D(graphicsDevice, frameInfo.Width, frameInfo.Height, false, SurfaceFormat.Color);
            Color[] colorArray = frameData.Select(b => b == 0 ? Color.Transparent : Palette.Data[b].ToXnaColor()).ToArray();
            texture.SetData<Color>(colorArray);

            return texture;
        }

        public Texture2D GetTextureForFrame_RGBA(int index)
        {
            GetFrameInfoAndData(index, out ShpFrameInfo frameInfo, out byte[] frameData);

            if (frameData == null)
                return null;

            return GetTextureForFrame_RGBA(index, frameInfo, frameData);
        }

        public Texture2D GetRemapTextureForFrame_Paletted(int index, ShpFrameInfo frameInfo, byte[] frameData)
        {
            byte[] remapColorArray = frameData.Select(b =>
            {
                if (b >= 0x10 && b <= 0x1F)
                {
                    // This is a remap color
                    return (byte)b;
                }

                return (byte)0;
            }).ToArray();

            var remapTexture = new Texture2D(graphicsDevice, frameInfo.Width, frameInfo.Height, false, SurfaceFormat.Alpha8);
            remapTexture.SetData(remapColorArray);
            return remapTexture;
        }

        public Texture2D GetRemapTextureForFrame_RGBA(int index, ShpFrameInfo frameInfo, byte[] frameData)
        {
            Color[] remapColorArray = frameData.Select(b =>
            {
                if (b >= 0x10 && b <= 0x1F)
                {
                    // This is a remap color, convert to grayscale
                    Color xnaColor = Palette.Data[b].ToXnaColor();
                    float value = Math.Max(xnaColor.R / 255.0f, Math.Max(xnaColor.G / 255.0f, xnaColor.B / 255.0f));

                    // Brighten it up a bit
                    value *= Constants.RemapBrightenFactor;
                    return new Color(value, value, value);
                }

                return Color.Transparent;
            }).ToArray();

            var remapTexture = new Texture2D(graphicsDevice, frameInfo.Width, frameInfo.Height, false, SurfaceFormat.Color);
            remapTexture.SetData<Color>(remapColorArray);
            return remapTexture;
        }

        public Texture2D GetRemapTextureForFrame_RGBA(int index)
        {
            GetFrameInfoAndData(index, out ShpFrameInfo frameInfo, out byte[] frameData);

            if (frameData == null)
                return null;

            return GetRemapTextureForFrame_RGBA(index, frameInfo, frameData);
        }

        private PositionedTexture[] Frames { get; set; }
        private PositionedTexture[] RemapFrames { get; set; }
    }

    public class PositionedTexture
    {
        public int ShapeWidth;
        public int ShapeHeight;
        public int OffsetX;
        public int OffsetY;
        public Texture2D Texture;

        public PositionedTexture(int shapeWidth, int shapeHeight, int offsetX, int offsetY, Texture2D texture)
        {
            ShapeWidth = shapeWidth;
            ShapeHeight = shapeHeight;
            OffsetX = offsetX;
            OffsetY = offsetY;
            Texture = texture;
        }

        public void Dispose()
        {
            if (Texture != null)
                Texture.Dispose();
        }
    }

    /// <summary>
    /// Graphical layer for the theater.
    /// </summary>
    public class TheaterGraphics : ITheater
    {
        private const string SHP_FILE_EXTENSION = ".SHP";
        private const string VXL_FILE_EXTENSION = ".VXL";
        private const string HVA_FILE_EXTENSION = ".HVA";
        private const string PNG_FILE_EXTENSION = ".PNG";
        private const string TURRET_FILE_SUFFIX = "TUR";
        private const string BARREL_FILE_SUFFIX = "BARL";

        public TheaterGraphics(GraphicsDevice graphicsDevice, Theater theater, CCFileManager fileManager, Rules rules)
        {
            this.graphicsDevice = graphicsDevice;
            Theater = theater;
            this.fileManager = fileManager;

            theaterPalette = GetPaletteOrFail(theater.TerrainPaletteName);
            unitPalette = GetPaletteOrFail(Theater.UnitPaletteName);
            animPalette = GetPaletteOrFail("anim.pal");
            if (!string.IsNullOrEmpty(Theater.TiberiumPaletteName))
                tiberiumPalette = GetPaletteOrFail(Theater.TiberiumPaletteName);
            vplFile = GetVplFile();

            if (UserSettings.Instance.MultithreadedTextureLoading)
            {
                var task1 = Task.Factory.StartNew(() => ReadTileTextures());
                var task2 = Task.Factory.StartNew(() => ReadTerrainObjectTextures(rules.TerrainTypes));
                var task3 = Task.Factory.StartNew(() => ReadBuildingTextures(rules.BuildingTypes));
                var task4 = Task.Factory.StartNew(() => ReadBuildingTurretModels(rules.BuildingTypes));
                var task5 = Task.Factory.StartNew(() => ReadBuildingBarrelModels(rules.BuildingTypes));
                var task6 = Task.Factory.StartNew(() => ReadUnitTextures(rules.UnitTypes));
                var task7 = Task.Factory.StartNew(() => ReadUnitModels(rules.UnitTypes));
                var task8 = Task.Factory.StartNew(() => ReadUnitTurretModels(rules.UnitTypes));
                var task9 = Task.Factory.StartNew(() => ReadUnitBarrelModels(rules.UnitTypes));
                var task10 = Task.Factory.StartNew(() => ReadAircraftModels(rules.AircraftTypes));
                var task11 = Task.Factory.StartNew(() => ReadInfantryTextures(rules.InfantryTypes));
                var task12 = Task.Factory.StartNew(() => ReadOverlayTextures(rules.OverlayTypes));
                var task13 = Task.Factory.StartNew(() => ReadSmudgeTextures(rules.SmudgeTypes));
                var task14 = Task.Factory.StartNew(() => ReadAnimTextures(rules.AnimTypes));
                Task.WaitAll(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14);
            }
            else
            {
                ReadTileTextures();
                ReadTerrainObjectTextures(rules.TerrainTypes);
                ReadBuildingTextures(rules.BuildingTypes);
                ReadBuildingTurretModels(rules.BuildingTypes);
                ReadBuildingBarrelModels(rules.BuildingTypes);
                ReadUnitTextures(rules.UnitTypes);
                ReadUnitModels(rules.UnitTypes);
                ReadUnitTurretModels(rules.UnitTypes);
                ReadUnitBarrelModels(rules.UnitTypes);
                ReadAircraftModels(rules.AircraftTypes);
                ReadInfantryTextures(rules.InfantryTypes);
                ReadOverlayTextures(rules.OverlayTypes);
                ReadSmudgeTextures(rules.SmudgeTypes);
                ReadAnimTextures(rules.AnimTypes);
            }

            LoadBuildingZData();
        }

        private readonly GraphicsDevice graphicsDevice;


        private static string[] NewTheaterHardcodedPrefixes = new string[] { "CA", "CT", "GA", "GT", "NA", "NT" };

        private void LoadBuildingZData()
        {
            return;

            /*
            var buildingZData = fileManager.LoadFile("BUILDNGZ.SHP");

            byte[] rgbBuffer = new byte[256 * 3];
            for (int i = 0; i < 256; i++)
            {
                rgbBuffer[i * 3] = (byte)(i / 4);
                rgbBuffer[(i * 3) + 1] = (byte)(i / 4);
                rgbBuffer[(i * 3) + 2] = (byte)(i / 4);
            }

            // for (int i = 16; i < 108; i++)
            // {
            //     byte color = (byte)((i - 16) * (256 / 92.0));
            //     rgbBuffer[i * 3] = (byte)(color / 4);
            //     rgbBuffer[(i * 3) + 1] = (byte)(color / 4);
            //     rgbBuffer[(i * 3) + 2] = (byte)(color / 4);
            // }

            var palette = new Palette(rgbBuffer);

            var shpFile = new ShpFile();
            shpFile.ParseFromBuffer(buildingZData);
            BuildingZ = new ShapeImage(graphicsDevice, shpFile, buildingZData, palette);
            */
        }

        private void ReadTileTextures()
        {
            Logger.Log("Loading tile textures.");

            int currentTileIndex = 0; // Used for setting the starting tile ID of a tileset

            for (int tsId = 0; tsId < Theater.TileSets.Count; tsId++)
            {
                TileSet tileSet = Theater.TileSets[tsId];
                tileSet.StartTileIndex = currentTileIndex;
                tileSet.LoadedTileCount = 0;

                Console.WriteLine("Loading " + tileSet.SetName);

                for (int i = 0; i < tileSet.TilesInSet; i++)
                {
                    // Console.WriteLine("#" + i);

                    var tileGraphics = new List<TileImage>();

                    // Handle graphics variation (clear00.tem, clear00a.tem, clear00b.tem etc.)
                    for (int v = 0; v < 'g' - 'a'; v++)
                    {
                        string baseName = tileSet.FileName + (i + 1).ToString("D2", CultureInfo.InvariantCulture);

                        if (v > 0)
                        {
                            baseName = baseName + ((char)('a' + (v - 1)));
                        }

                        byte[] data = fileManager.LoadFile(baseName + Theater.FileExtension);

                        if (data == null)
                        {
                            if (v == 0)
                            {
                                tileGraphics.Add(new TileImage(0, 0, tsId, i, currentTileIndex, new MGTMPImage[0]));
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }

                        var tmpFile = new TmpFile();
                        tmpFile.ParseFromBuffer(data);

                        var tmpImages = new List<MGTMPImage>();
                        for (int img = 0; img < tmpFile.ImageCount; img++)
                        {
                            tmpImages.Add(new MGTMPImage(graphicsDevice, tmpFile.GetImage(img), theaterPalette, tsId));
                        }
                        tileGraphics.Add(new TileImage(tmpFile.CellsX, tmpFile.CellsY, tsId, i, currentTileIndex, tmpImages.ToArray()));
                    }

                    tileSet.LoadedTileCount++;
                    currentTileIndex++;
                    terrainGraphicsList.Add(tileGraphics.ToArray());
                }
            }

            Logger.Log("Assigning marble madness mode tile textures.");

            // Assign marble-madness (MM) mode tile graphics
            int tileIndex = 0;
            for (int tsId = 0; tsId < Theater.TileSets.Count; tsId++)
            {
                TileSet tileSet = Theater.TileSets[tsId];
                if (tileSet.NonMarbleMadness > -1 || tileSet.MarbleMadness < 0 || tileSet.MarbleMadness >= Theater.TileSets.Count)
                {
                    // This is a MM tileset or a tileset with no MM graphics
                    for (int i = 0; i < tileSet.LoadedTileCount; i++)
                    {
                        mmTerrainGraphicsList.Add(terrainGraphicsList[tileIndex + i]);
                        hasMMGraphics.Add(tileSet.NonMarbleMadness > -1);
                    }

                    tileIndex += tileSet.LoadedTileCount;
                    continue;
                }

                // For non-MM tilesets with MM graphics, fetch the MM tileset
                TileSet mmTileSet = Theater.TileSets[tileSet.MarbleMadness];
                for (int i = 0; i < tileSet.LoadedTileCount; i++)
                {
                    mmTerrainGraphicsList.Add(terrainGraphicsList[mmTileSet.StartTileIndex + i]);
                    hasMMGraphics.Add(true);
                }
                tileIndex += tileSet.LoadedTileCount;
            }

            Logger.Log("Finished loading tile textures.");
        }

        public void ReadTerrainObjectTextures(List<TerrainType> terrainTypes)
        {
            Logger.Log("Loading terrain object textures.");

            var unitPalette = GetPaletteOrFail(Theater.UnitPaletteName);

            TerrainObjectTextures = new ShapeImage[terrainTypes.Count];
            for (int i = 0; i < terrainTypes.Count; i++)
            {
                string shpFileName = terrainTypes[i].Image != null ? terrainTypes[i].Image : terrainTypes[i].ININame;
                string pngFileName = shpFileName + PNG_FILE_EXTENSION;

                if (terrainTypes[i].Theater)
                    shpFileName += Theater.FileExtension;
                else
                    shpFileName += SHP_FILE_EXTENSION;

                byte[] data = fileManager.LoadFile(pngFileName);

                bool subjectToLighting = !terrainTypes[i].SpawnsTiberium || Constants.TerrainObjectsAffectedByLighting;

                if (data != null)
                {
                    // Load graphics as PNG

                    TerrainObjectTextures[i] = new ShapeImage(graphicsDevice, null, null, null, subjectToLighting, false, PositionedTextureFromBytes(data));
                }
                else
                {
                    // Try to load graphics as SHP

                    data = fileManager.LoadFile(shpFileName);

                    if (data == null)
                        continue;

                    var shpFile = new ShpFile(shpFileName);
                    shpFile.ParseFromBuffer(data);
                    TerrainObjectTextures[i] = new ShapeImage(graphicsDevice, shpFile, data,
                        terrainTypes[i].SpawnsTiberium ? unitPalette : theaterPalette, subjectToLighting);
                }
            }

            Logger.Log("Finished loading terrain object textures.");
        }

        public void ReadBuildingTextures(List<BuildingType> buildingTypes)
        {
            Logger.Log("Loading building textures.");

            BuildingTextures = new ShapeImage[buildingTypes.Count];
            BuildingBibTextures = new ShapeImage[buildingTypes.Count];

            for (int i = 0; i < buildingTypes.Count; i++)
            {
                var buildingType = buildingTypes[i];

                string shpFileName = string.IsNullOrWhiteSpace(buildingType.Image) ? buildingType.ArtConfig.Image : buildingType.Image;

                if (string.IsNullOrEmpty(shpFileName))
                    shpFileName = buildingType.ININame;

                if (buildingType.ArtConfig.Theater)
                    shpFileName += Theater.FileExtension;
                else
                    shpFileName += SHP_FILE_EXTENSION;

                // The game has hardcoded NewTheater=yes behaviour for buildings that start with a specific prefix
                bool hardcodedNewTheater = Array.Exists(NewTheaterHardcodedPrefixes, prefix => buildingType.ININame.ToUpperInvariant().StartsWith(prefix));

                string loadedShpName = "";

                byte[] shpData = null;
                if (buildingType.ArtConfig.NewTheater || hardcodedNewTheater)
                {
                    string newTheaterShpName = shpFileName.Substring(0, 1) + Theater.NewTheaterBuildingLetter + shpFileName.Substring(2);

                    shpData = fileManager.LoadFile(newTheaterShpName);
                    loadedShpName = newTheaterShpName;
                }

                // Support generic building letter
                if (Constants.NewTheaterGenericBuilding && shpData == null)
                {
                    string newTheaterShpName = shpFileName.Substring(0, 1) + Constants.NewTheaterGenericLetter + shpFileName.Substring(2);

                    shpData = fileManager.LoadFile(newTheaterShpName);
                    loadedShpName = newTheaterShpName;
                }

                // The game can apparently fall back to the non-theater-specific SHP file name
                // if the theater-specific SHP is not found
                if (shpData == null)
                {
                    shpData = fileManager.LoadFile(shpFileName);
                    loadedShpName = shpFileName;

                    if (shpData == null)
                    {
                        continue;
                    }
                }

                // Palette override in RA2/YR
                XNAPalette palette = buildingType.ArtConfig.TerrainPalette ? theaterPalette : unitPalette;
                if (!string.IsNullOrWhiteSpace(buildingType.ArtConfig.Palette))
                    palette = GetPaletteOrDefault(buildingType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal", palette);

                var shpFile = new ShpFile(loadedShpName);
                shpFile.ParseFromBuffer(shpData);

                bool affectedByLighting = !buildingType.ArtConfig.TerrainPalette || Constants.TerrainPaletteBuildingsAffectedByLighting;

                BuildingTextures[i] = new ShapeImage(graphicsDevice, shpFile, shpData, palette,
                    affectedByLighting, buildingType.ArtConfig.Remapable);

                // If this building has a bib, attempt to load it
                if (!string.IsNullOrWhiteSpace(buildingType.ArtConfig.BibShape))
                {
                    string bibShpFileName = buildingType.ArtConfig.BibShape;

                    if (buildingType.ArtConfig.Theater)
                        bibShpFileName += Theater.FileExtension;
                    else
                        bibShpFileName += SHP_FILE_EXTENSION;

                    shpData = null;
                    if (buildingType.ArtConfig.NewTheater)
                    {
                        string newTheaterBibShpName = bibShpFileName.Substring(0, 1) + Theater.NewTheaterBuildingLetter + bibShpFileName.Substring(2);

                        shpData = fileManager.LoadFile(newTheaterBibShpName);
                        loadedShpName = newTheaterBibShpName;
                    }

                    if (Constants.NewTheaterGenericBuilding && shpData == null)
                    {
                        string newTheaterBibShpName = bibShpFileName.Substring(0, 1) + Constants.NewTheaterGenericLetter + bibShpFileName.Substring(2);

                        shpData = fileManager.LoadFile(newTheaterBibShpName);
                    }

                    if (shpData == null)
                    {
                        shpData = fileManager.LoadFile(bibShpFileName);
                        loadedShpName = bibShpFileName;
                    }
                        
                    if (shpData == null)
                    {
                        continue;
                    }

                    var bibShpFile = new ShpFile(loadedShpName);
                    bibShpFile.ParseFromBuffer(shpData);
                    BuildingBibTextures[i] = new ShapeImage(graphicsDevice, bibShpFile, shpData, palette,
                        affectedByLighting, buildingType.ArtConfig.Remapable);
                }
            }

            Logger.Log("Finished loading building textures.");
        }

        public void ReadBuildingTurretModels(List<BuildingType> buildingTypes)
        {
            Logger.Log("Loading building turrets' voxel models.");

            var loadedModels = new Dictionary<string, VoxelModel>();
            BuildingTurretModels = new VoxelModel[buildingTypes.Count];

            for (int i = 0; i < buildingTypes.Count; i++)
            {
                var buildingType = buildingTypes[i];

                if (!(buildingType.Turret && buildingType.TurretAnimIsVoxel))
                    continue;

                string artName = string.IsNullOrWhiteSpace(buildingType.Image) ? buildingType.ArtConfig.Image : buildingType.Image;
                if (string.IsNullOrEmpty(artName))
                    artName = buildingType.ININame;

                string turretModelName = string.IsNullOrEmpty(buildingType.TurretAnim) ? artName + TURRET_FILE_SUFFIX : buildingType.TurretAnim;
                if (loadedModels.TryGetValue(turretModelName, out VoxelModel loadedModel))
                {
                    BuildingTurretModels[i] = loadedModel;
                    continue;
                }

                byte[] vxlData = fileManager.LoadFile(turretModelName + VXL_FILE_EXTENSION);
                if (vxlData == null)
                    continue;

                byte[] hvaData = fileManager.LoadFile(turretModelName + HVA_FILE_EXTENSION);

                if (hvaData == null)
                {
                    Logger.Log($"WARNING: Building {buildingType.ININame} is missing .hva file for its turret {turretModelName + HVA_FILE_EXTENSION}! This will cause the game to crash!");
                    continue;
                }

                var vxlFile = new VxlFile(vxlData, turretModelName);
                var hvaFile = new HvaFile(hvaData, turretModelName);

                XNAPalette palette = buildingType.ArtConfig.TerrainPalette ? theaterPalette : unitPalette;
                if (!string.IsNullOrWhiteSpace(buildingType.ArtConfig.Palette))
                    palette = GetPaletteOrFail(buildingType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal");

                BuildingTurretModels[i] = new VoxelModel(graphicsDevice, vxlFile, hvaFile, palette,
                    buildingType.ArtConfig.Remapable, Constants.VoxelsAffectedByLighting, vplFile);
                loadedModels[turretModelName] = BuildingTurretModels[i];
            }

            Logger.Log("Finished loading building turrets' voxel models.");
        }

        public void ReadBuildingBarrelModels(List<BuildingType> buildingTypes)
        {
            Logger.Log("Loading building barrels' voxel models.");

            var loadedModels = new Dictionary<string, VoxelModel>();
            BuildingBarrelModels = new VoxelModel[buildingTypes.Count];

            for (int i = 0; i < buildingTypes.Count; i++)
            {
                var buildingType = buildingTypes[i];

                bool hasVoxelTurret = buildingType.Turret && buildingType.TurretAnimIsVoxel;
                bool hasShapeTurretAndVoxelBarrel = buildingType.Turret && !buildingType.TurretAnimIsVoxel &&
                                                    buildingType.BarrelAnimIsVoxel;

                if (!(hasVoxelTurret || hasShapeTurretAndVoxelBarrel))
                    continue;

                string artName = string.IsNullOrWhiteSpace(buildingType.Image) ? buildingType.ArtConfig.Image : buildingType.Image;
                if (string.IsNullOrEmpty(artName))
                    artName = buildingType.ININame;

                string barrelModelName = string.IsNullOrEmpty(buildingType.VoxelBarrelFile) ? artName + BARREL_FILE_SUFFIX : buildingType.VoxelBarrelFile;
                if (loadedModels.TryGetValue(barrelModelName, out VoxelModel loadedModel))
                {
                    BuildingBarrelModels[i] = loadedModel;
                    continue;
                }

                byte[] vxlData = fileManager.LoadFile(barrelModelName + VXL_FILE_EXTENSION);
                if (vxlData == null)
                    continue;

                byte[] hvaData = fileManager.LoadFile(barrelModelName + HVA_FILE_EXTENSION);

                if (hvaData == null)
                {
                    Logger.Log($"WARNING: Building {buildingType.ININame} is missing .hva file for its barrel {barrelModelName + HVA_FILE_EXTENSION}! This will cause the game to crash!");
                    continue;
                }

                var vxlFile = new VxlFile(vxlData, barrelModelName);
                var hvaFile = new HvaFile(hvaData, barrelModelName);

                XNAPalette palette = buildingType.ArtConfig.TerrainPalette ? theaterPalette : unitPalette;
                if (!string.IsNullOrWhiteSpace(buildingType.ArtConfig.Palette))
                    palette = GetPaletteOrFail(buildingType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal");

                BuildingBarrelModels[i] = new VoxelModel(graphicsDevice, vxlFile, hvaFile, palette,
                    buildingType.ArtConfig.Remapable, Constants.VoxelsAffectedByLighting, vplFile);
                loadedModels[barrelModelName] = BuildingBarrelModels[i];
            }

            Logger.Log("Finished loading building barrels' voxel models.");
        }

        public void ReadAnimTextures(List<AnimType> animTypes)
        {
            Logger.Log("Loading animation textures.");

            AnimTextures = new ShapeImage[animTypes.Count];

            for (int i = 0; i < animTypes.Count; i++)
            {
                var animType = animTypes[i];

                string shpFileName = string.IsNullOrWhiteSpace(animType.ArtConfig.Image) ? animType.ININame : animType.ArtConfig.Image;
                string loadedShpName = "";

                if (animType.ArtConfig.Theater)
                    shpFileName += Theater.FileExtension;
                else
                    shpFileName += SHP_FILE_EXTENSION;

                byte[] shpData = null;
                if (animType.ArtConfig.NewTheater)
                {
                    string newTheaterShpName = shpFileName.Substring(0, 1) + Theater.NewTheaterBuildingLetter + shpFileName.Substring(2);

                    shpData = fileManager.LoadFile(newTheaterShpName);
                    loadedShpName = newTheaterShpName;
                }

                // Support generic theater letter
                if (Constants.NewTheaterGenericBuilding && shpData == null)
                {
                    string newTheaterShpName = shpFileName.Substring(0, 1) + Constants.NewTheaterGenericLetter + shpFileName.Substring(2);

                    shpData = fileManager.LoadFile(newTheaterShpName);
                    loadedShpName = newTheaterShpName;
                }

                // The game can apparently fall back to the non-theater-specific SHP file name
                // if the theater-specific SHP is not found
                if (shpData == null)
                {
                    shpData = fileManager.LoadFile(shpFileName);
                    loadedShpName = shpFileName;

                    if (shpData == null)
                    {
                        continue;
                    }
                }

                // Palette override in RA2/YR
                // NOTE: Until we use indexed color rendering, we have to assume that a building
                // anim will only be used as a building anim (because it forces unit palette).
                XNAPalette palette = animType.ArtConfig.IsBuildingAnim || animType.ArtConfig.AltPalette ? unitPalette : animPalette;
                if (!string.IsNullOrWhiteSpace(animType.ArtConfig.CustomPalette))
                {
                    palette = GetPaletteOrDefault(
                        animType.ArtConfig.CustomPalette.Replace("~~~", Theater.FileExtension.Substring(1)),
                        palette);
                }

                var shpFile = new ShpFile(loadedShpName);
                shpFile.ParseFromBuffer(shpData);
                AnimTextures[i] = new ShapeImage(graphicsDevice, shpFile, shpData, palette,
                    animType.ArtConfig.IsBuildingAnim, animType.ArtConfig.Remapable || animType.ArtConfig.IsBuildingAnim);
            }

            Logger.Log("Finished loading animation textures.");
        }

        public void ReadUnitTextures(List<UnitType> unitTypes)
        {
            Logger.Log("Loading unit textures.");

            var loadedTextures = new Dictionary<string, ShapeImage>();
            UnitTextures = new ShapeImage[unitTypes.Count];

            for (int i = 0; i < unitTypes.Count; i++)
            {
                var unitType = unitTypes[i];

                if (unitType.ArtConfig.Voxel)
                    continue;

                string shpFileName = string.IsNullOrWhiteSpace(unitType.Image) ? unitType.ININame : unitType.Image;
                shpFileName += SHP_FILE_EXTENSION;
                if (loadedTextures.TryGetValue(shpFileName, out ShapeImage loadedImage))
                {
                    UnitTextures[i] = loadedImage;
                    continue;
                }

                byte[] shpData = fileManager.LoadFile(shpFileName);

                if (shpData == null)
                    continue;

                var shpFile = new ShpFile(shpFileName);
                shpFile.ParseFromBuffer(shpData);

                UnitTextures[i] = new ShapeImage(graphicsDevice, shpFile, shpData, unitPalette, true, unitType.ArtConfig.Remapable);
                loadedTextures[shpFileName] = UnitTextures[i];
            }

            Logger.Log("Finished loading unit textures.");
        }

        public void ReadUnitModels(List<UnitType> unitTypes)
        {
            Logger.Log("Loading unit voxel models.");

            var loadedModels = new Dictionary<string, VoxelModel>();
            UnitModels = new VoxelModel[unitTypes.Count];

            for (int i = 0; i < unitTypes.Count; i++)
            {
                var unitType = unitTypes[i];

                if (!unitType.ArtConfig.Voxel)
                    continue;

                string unitImage = string.IsNullOrWhiteSpace(unitType.Image) ? unitType.ININame : unitType.Image;
                if (loadedModels.TryGetValue(unitImage, out VoxelModel loadedModel))
                {
                    UnitModels[i] = loadedModel;
                    continue;
                }

                byte[] vxlData = fileManager.LoadFile(unitImage + VXL_FILE_EXTENSION);
                if (vxlData == null)
                    continue;

                byte[] hvaData = fileManager.LoadFile(unitImage + HVA_FILE_EXTENSION);

                if (hvaData == null)
                {
                    Logger.Log($"WARNING: Unit {unitType.ININame} is missing its .hva file {unitImage + HVA_FILE_EXTENSION}! This will cause the game to crash!");
                    continue;
                }

                var vxlFile = new VxlFile(vxlData, unitImage);
                var hvaFile = new HvaFile(hvaData, unitImage);

                UnitModels[i] = new VoxelModel(graphicsDevice, vxlFile, hvaFile, unitPalette, unitType.ArtConfig.Remapable,
                    Constants.VoxelsAffectedByLighting, vplFile);
                loadedModels[unitImage] = UnitModels[i];
            }

            Logger.Log("Finished loading unit voxel models.");
        }

        public void ReadUnitTurretModels(List<UnitType> unitTypes)
        {
            Logger.Log("Loading unit turrets' voxel models.");

            var loadedModels = new Dictionary<string, VoxelModel>();
            UnitTurretModels = new VoxelModel[unitTypes.Count];

            for (int i = 0; i < unitTypes.Count; i++)
            {
                var unitType = unitTypes[i];

                if (!unitType.Turret)
                    continue;

                string turretModelName = string.IsNullOrWhiteSpace(unitType.Image) ? unitType.ININame : unitType.Image;
                turretModelName += TURRET_FILE_SUFFIX;
                if (loadedModels.TryGetValue(turretModelName, out VoxelModel loadedModel))
                {
                    UnitTurretModels[i] = loadedModel;
                    continue;
                }

                byte[] vxlData = fileManager.LoadFile(turretModelName + VXL_FILE_EXTENSION);
                if (vxlData == null)
                    continue;

                byte[] hvaData = fileManager.LoadFile(turretModelName + HVA_FILE_EXTENSION);

                if (hvaData == null)
                {
                    Logger.Log($"WARNING: Unit {unitType.ININame} is missing .hva file for its turret {turretModelName + HVA_FILE_EXTENSION}! This will cause the game to crash!");
                    continue;
                }

                var vxlFile = new VxlFile(vxlData, turretModelName);
                var hvaFile = new HvaFile(hvaData, turretModelName);

                UnitTurretModels[i] = new VoxelModel(graphicsDevice, vxlFile, hvaFile, unitPalette, unitType.ArtConfig.Remapable,
                    Constants.VoxelsAffectedByLighting, vplFile);
                loadedModels[turretModelName] = UnitTurretModels[i];
            }

            Logger.Log("Finished loading unit turrets' voxel models.");
        }

        public void ReadUnitBarrelModels(List<UnitType> unitTypes)
        {
            Logger.Log("Loading unit barrels' voxel models.");

            var loadedModels = new Dictionary<string, VoxelModel>();
            UnitBarrelModels = new VoxelModel[unitTypes.Count];

            for (int i = 0; i < unitTypes.Count; i++)
            {
                var unitType = unitTypes[i];

                if (!unitType.Turret)
                    continue;

                string barrelModelName = string.IsNullOrWhiteSpace(unitType.Image) ? unitType.ININame : unitType.Image;
                barrelModelName += BARREL_FILE_SUFFIX;
                if (loadedModels.TryGetValue(barrelModelName, out VoxelModel loadedModel))
                {
                    UnitBarrelModels[i] = loadedModel;
                    continue;
                }

                byte[] vxlData = fileManager.LoadFile(barrelModelName + VXL_FILE_EXTENSION);
                if (vxlData == null)
                    continue;

                byte[] hvaData = fileManager.LoadFile(barrelModelName + HVA_FILE_EXTENSION);

                if (hvaData == null)
                {
                    Logger.Log($"WARNING: Unit {unitType.ININame} is missing .hva file for its barrel {barrelModelName + HVA_FILE_EXTENSION}! This will cause the game to crash!");
                    continue;
                }

                var vxlFile = new VxlFile(vxlData, barrelModelName);
                var hvaFile = new HvaFile(hvaData, barrelModelName);

                UnitBarrelModels[i] = new VoxelModel(graphicsDevice, vxlFile, hvaFile, unitPalette, unitType.ArtConfig.Remapable,
                    Constants.VoxelsAffectedByLighting, vplFile);
                loadedModels[barrelModelName] = UnitBarrelModels[i];
            }

            Logger.Log("Finished loading unit barrels' voxel models.");
        }

        public void ReadAircraftModels(List<AircraftType> aircraftTypes)
        {
            Logger.Log("Loading aircraft voxel models.");

            var loadedModels = new Dictionary<string, VoxelModel>();
            AircraftModels = new VoxelModel[aircraftTypes.Count];

            for (int i = 0; i < aircraftTypes.Count; i++)
            {
                var aircraftType = aircraftTypes[i];

                string aircraftImage = string.IsNullOrWhiteSpace(aircraftType.Image) ? aircraftType.ININame : aircraftType.Image;
                if (loadedModels.TryGetValue(aircraftImage, out VoxelModel loadedModel))
                {
                    AircraftModels[i] = loadedModel;
                    continue;
                }

                byte[] vxlData = fileManager.LoadFile(aircraftImage + VXL_FILE_EXTENSION);
                if (vxlData == null)
                    continue;

                byte[] hvaData = fileManager.LoadFile(aircraftImage + HVA_FILE_EXTENSION);

                if (hvaData == null)
                {
                    Logger.Log($"WARNING: Aircraft {aircraftType.ININame} is missing its .hva file {aircraftImage + HVA_FILE_EXTENSION}! This will cause the game to crash!");
                    continue;
                }

                var vxlFile = new VxlFile(vxlData, aircraftImage);
                var hvaFile = new HvaFile(hvaData, aircraftImage);

                AircraftModels[i] = new VoxelModel(graphicsDevice, vxlFile, hvaFile, unitPalette, aircraftType.ArtConfig.Remapable,
                    Constants.VoxelsAffectedByLighting, vplFile);
                loadedModels[aircraftImage] = AircraftModels[i];
            }

            Logger.Log("Finished loading aircraft voxel models.");
        }

        public void ReadInfantryTextures(List<InfantryType> infantryTypes)
        {
            Logger.Log("Loading infantry textures.");

            var loadedTextures = new Dictionary<string, ShapeImage>();
            InfantryTextures = new ShapeImage[infantryTypes.Count];

            for (int i = 0; i < infantryTypes.Count; i++)
            {
                var infantryType = infantryTypes[i];

                string image = string.IsNullOrWhiteSpace(infantryType.Image) ? infantryType.ININame : infantryType.Image;
                string shpFileName = string.IsNullOrWhiteSpace(infantryType.ArtConfig.Image) ? image : infantryType.ArtConfig.Image;
                shpFileName += SHP_FILE_EXTENSION;
                if (loadedTextures.TryGetValue(shpFileName, out ShapeImage loadedImage))
                {
                    InfantryTextures[i] = loadedImage;
                    continue;
                }

                if (infantryType.ArtConfig.Sequence == null)
                {
                    continue;
                }

                byte[] shpData = fileManager.LoadFile(shpFileName);

                if (shpData == null)
                    continue;

                var framesToLoad = new List<int>();
                const int FACING_COUNT = 8;
                var readySequence = infantryType.ArtConfig.Sequence.Ready;
                for (int j = 0; j < FACING_COUNT; j++)
                {
                    framesToLoad.Add(readySequence.StartFrame + (readySequence.FrameCount * readySequence.FacingMultiplier * j));
                }

                var shpFile = new ShpFile(shpFileName);
                shpFile.ParseFromBuffer(shpData);

                // Load shadow frames
                int regularFrameCount = framesToLoad.Count;
                for (int j = 0; j < regularFrameCount; j++)
                    framesToLoad.Add(framesToLoad[j] + (shpFile.FrameCount / 2));

                InfantryTextures[i] = new ShapeImage(graphicsDevice, shpFile, shpData, unitPalette, true, infantryType.ArtConfig.Remapable);
                loadedTextures[shpFileName] = InfantryTextures[i];
            }

            Logger.Log("Finished loading infantry textures.");
        }

        public void ReadOverlayTextures(List<OverlayType> overlayTypes)
        {
            Logger.Log("Loading overlay textures.");

            OverlayTextures = new ShapeImage[overlayTypes.Count];
            for (int i = 0; i < overlayTypes.Count; i++)
            {
                var overlayType = overlayTypes[i];

                string imageName = overlayType.ININame;
                if (overlayType.ArtConfig.Image != null)
                    imageName = overlayType.ArtConfig.Image;
                else if (overlayType.Image != null)
                    imageName = overlayType.Image;

                string pngFileName = imageName + PNG_FILE_EXTENSION;

                byte[] pngData = fileManager.LoadFile(pngFileName);

                if (pngData != null)
                {
                    // Load graphics as PNG

                    OverlayTextures[i] = new ShapeImage(graphicsDevice, null, null, null,
                        true, false, PositionedTextureFromBytes(pngData));
                }
                else
                {
                    // Load graphics as SHP

                    string loadedShpName = "";

                    byte[] shpData;

                    if (overlayType.ArtConfig.NewTheater)
                    {
                        string shpFileName = imageName + SHP_FILE_EXTENSION;
                        string newTheaterImageName = shpFileName.Substring(0, 1) + Theater.NewTheaterBuildingLetter + shpFileName.Substring(2);
                        
                        shpData = fileManager.LoadFile(newTheaterImageName);
                        loadedShpName = newTheaterImageName;

                        if (shpData == null)
                        {
                            newTheaterImageName = shpFileName.Substring(0, 1) + Constants.NewTheaterGenericLetter + shpFileName.Substring(2);
                            shpData = fileManager.LoadFile(newTheaterImageName);
                            loadedShpName = newTheaterImageName;
                        }
                    }
                    else
                    {
                        string fileExtension = overlayType.ArtConfig.Theater ? Theater.FileExtension : SHP_FILE_EXTENSION;
                        shpData = fileManager.LoadFile(imageName + fileExtension);
                        loadedShpName = imageName + fileExtension;
                    }

                    if (shpData == null)
                        continue;

                    var shpFile = new ShpFile(loadedShpName);
                    shpFile.ParseFromBuffer(shpData);
                    XNAPalette palette = theaterPalette;

                    if (overlayType.Tiberium)
                    {
                        palette = unitPalette;

                        if (Constants.TheaterPaletteForTiberium)
                            palette = tiberiumPalette ?? theaterPalette;
                    }

                    if (overlayType.Wall || overlayType.IsVeins)
                        palette = unitPalette;

                    bool isRemapable = overlayType.Tiberium && !Constants.TheaterPaletteForTiberium;
                    bool affectedByLighting = !overlayType.Tiberium || Constants.TiberiumAffectedByLighting;

                    OverlayTextures[i] = new ShapeImage(graphicsDevice, shpFile, shpData, palette, affectedByLighting, isRemapable, null);
                }
            }

            Logger.Log("Finished loading overlay textures.");
        }

        public void ReadSmudgeTextures(List<SmudgeType> smudgeTypes)
        {
            Logger.Log("Loading smudge textures.");

            SmudgeTextures = new ShapeImage[smudgeTypes.Count];
            for (int i = 0; i < smudgeTypes.Count; i++)
            {
                var smudgeType = smudgeTypes[i];

                string imageName = smudgeType.ININame;
                string fileExtension = smudgeType.Theater ? Theater.FileExtension : SHP_FILE_EXTENSION;
                string finalShpName = imageName + fileExtension;
                byte[] shpData = fileManager.LoadFile(finalShpName);

                if (shpData == null)
                    continue;

                var shpFile = new ShpFile(finalShpName);
                shpFile.ParseFromBuffer(shpData);
                XNAPalette palette = theaterPalette;
                SmudgeTextures[i] = new ShapeImage(graphicsDevice, shpFile, shpData, palette, true);
            }

            Logger.Log("Finished loading smudge textures.");
        }

        public void ApplyLightingToPalettes(Color lighting)
        {
            theaterPalette.ApplyLighting(lighting);
            unitPalette.ApplyLighting(lighting);
            tiberiumPalette.ApplyLighting(lighting);
            animPalette.ApplyLighting(lighting);
            palettes.ForEach(p => p.ApplyLighting(lighting));
        }

        public void InvalidateVoxelCache()
        {
            Array.ForEach(BuildingTurretModels, m => m?.ClearFrames());
            Array.ForEach(BuildingBarrelModels, m => m?.ClearFrames());
            Array.ForEach(UnitModels, m => m?.ClearFrames());
            Array.ForEach(UnitTurretModels, m => m?.ClearFrames());
            Array.ForEach(UnitBarrelModels, m => m?.ClearFrames());
            Array.ForEach(AircraftModels, m => m?.ClearFrames());
        }

        private Random random = new Random();

        public Theater Theater { get; }

        private CCFileManager fileManager;

        private readonly XNAPalette theaterPalette;
        private readonly XNAPalette unitPalette;
        private readonly XNAPalette tiberiumPalette;
        private readonly XNAPalette animPalette;
        private readonly VplFile vplFile;

        private readonly List<XNAPalette> palettes = new List<XNAPalette>();

        private List<TileImage[]> terrainGraphicsList = new List<TileImage[]>();
        private List<TileImage[]> mmTerrainGraphicsList = new List<TileImage[]>();
        private List<bool> hasMMGraphics = new List<bool>();

        public int TileCount => terrainGraphicsList.Count;

        public TileImage GetTileGraphics(int id) => terrainGraphicsList[id][random.Next(terrainGraphicsList[id].Length)];
        public TileImage GetTileGraphics(int id, int randomId) => terrainGraphicsList[id][randomId];
        public TileImage GetMarbleMadnessTileGraphics(int id) => mmTerrainGraphicsList[id][0];
        public bool HasSeparateMarbleMadnessTileGraphics(int id) => hasMMGraphics[id];

        public ITileImage GetTile(int id) => GetTileGraphics(id);

        public int GetOverlayFrameCount(OverlayType overlayType)
        {
            int frameCount = OverlayTextures[overlayType.Index].GetFrameCount();

            // We only consider non-blank frames as valid frames, so we need to look up
            // the first blank frame to get the proper frame count
            // According to Bittah, when we find an empty overlay frame,
            // we can assume the rest of the overlay frames to be empty too
            for (int i = 0; i < frameCount; i++)
            {
                var texture = OverlayTextures[overlayType.Index].GetFrame(i);
                if (texture == null || texture.Texture == null)
                    return i;
            }

            // No blank overlay frame existed - return the full frame count divided by two (the rest are used up by shadows)
            return OverlayTextures[overlayType.Index].GetFrameCount() / 2;
        }

        public ShapeImage[] TerrainObjectTextures { get; set; }
        public ShapeImage[] BuildingTextures { get; set; }
        public ShapeImage[] BuildingBibTextures { get; set; }
        public VoxelModel[] BuildingTurretModels { get; set; }
        public VoxelModel[] BuildingBarrelModels { get; set; }
        public ShapeImage[] UnitTextures { get; set; }
        public VoxelModel[] UnitModels { get; set; }
        public VoxelModel[] UnitTurretModels { get; set; }
        public VoxelModel[] UnitBarrelModels { get; set; }
        public VoxelModel[] AircraftModels { get; set; }
        public ShapeImage[] InfantryTextures { get; set; }
        public ShapeImage[] OverlayTextures { get; set; }
        public ShapeImage[] SmudgeTextures { get; set; }
        public ShapeImage[] AnimTextures { get; set; }


        public ShapeImage BuildingZ { get; set; }

        /// <summary>
        /// Frees up all memory used by the theater graphics textures
        /// (or more precisely, diposes them so the garbage collector can free them).
        /// Make sure no rendering is attempted afterwards!
        /// </summary>
        public void DisposeAll()
        {
            var task1 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(TerrainObjectTextures));
            var task2 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(BuildingTextures));
            var task3 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(BuildingTurretModels));
            var task4 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(BuildingBarrelModels));
            var task5 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(UnitTextures));
            var task6 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(UnitModels));
            var task7 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(UnitTurretModels));
            var task8 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(UnitBarrelModels));
            var task9 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(AircraftModels));
            var task10 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(InfantryTextures));
            var task11 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(OverlayTextures));
            var task12 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(SmudgeTextures));
            var task13 = Task.Factory.StartNew(() => { terrainGraphicsList.ForEach(tileImageArray => Array.ForEach(tileImageArray, tileImage => tileImage.Dispose())); terrainGraphicsList.Clear(); });
            var task14 = Task.Factory.StartNew(() => { mmTerrainGraphicsList.ForEach(tileImageArray => Array.ForEach(tileImageArray, tileImage => tileImage.Dispose())); mmTerrainGraphicsList.Clear(); });
            var task15 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(AnimTextures));
            Task.WaitAll(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14, task15);

            TerrainObjectTextures = null;
            BuildingTextures = null;
            UnitTextures = null;
            InfantryTextures = null;
            OverlayTextures = null;
            SmudgeTextures = null;
            AnimTextures = null;
        }

        private void DisposeObjectImagesFromArray(IDisposable[] objImageArray)
        {
            Array.ForEach(objImageArray, objectImage => { if (objectImage != null) objectImage.Dispose(); });
            Array.Clear(objImageArray);
        }

        private XNAPalette GetPaletteOrFail(string paletteFileName)
        {
            var existing = palettes.Find(p => p.Name == paletteFileName);
            if (existing != null)
                return existing;

            byte[] paletteData = fileManager.LoadFile(paletteFileName);
            if (paletteData == null)
                throw new KeyNotFoundException(paletteFileName + " not found from loaded MIX files!");

            var newPalette = new XNAPalette(paletteFileName, paletteData, graphicsDevice);
            palettes.Add(newPalette);
            return newPalette;
        }

        private XNAPalette GetPaletteOrDefault(string paletteFileName, XNAPalette palette)
        {
            var existing = palettes.Find(p => p.Name == paletteFileName);
            if (existing != null)
                return existing;

            byte[] paletteData = fileManager.LoadFile(paletteFileName);
            if (paletteData == null)
                return palette;

            var newPalette = new XNAPalette(paletteFileName, paletteData, graphicsDevice);
            palettes.Add(newPalette);
            return newPalette;
        }

        private VplFile GetVplFile(string filename = "voxels.vpl")
        {
            byte[] vplData = fileManager.LoadFile(filename);
            if (vplData == null)
                throw new KeyNotFoundException(filename + " not found from loaded MIX files!");

            return new VplFile(vplData);
        }

        private PositionedTexture PositionedTextureFromBytes(byte[] data)
        {
            using (var memstream = new MemoryStream(data))
            {
                var tex2d = Texture2D.FromStream(graphicsDevice, memstream);

                // premultiply alpha
                Color[] colorData = new Color[tex2d.Width * tex2d.Height];
                tex2d.GetData(colorData);
                for (int i = 0; i < colorData.Length; i++)
                {
                    var color = colorData[i];
                    color.R = (byte)((color.R * color.A) / byte.MaxValue);
                    color.G = (byte)((color.G * color.A) / byte.MaxValue);
                    color.B = (byte)((color.B * color.A) / byte.MaxValue);
                    colorData[i] = color;
                }

                tex2d.SetData(colorData);

                return new PositionedTexture(tex2d.Width, tex2d.Height, 0, 0, tex2d);
            }
        }

        public int GetTileSetId(int uniqueTileIndex)
        {
            return GetTileGraphics(uniqueTileIndex).TileSetId;
        }
    }
}