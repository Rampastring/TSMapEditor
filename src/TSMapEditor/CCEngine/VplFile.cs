using System.Collections.Generic;
using System.IO;
using CNCMaps.FileFormats.VirtualFileSystem;

namespace TSMapEditor.CCEngine
{
    /// <summary>
    /// .vpl file format
    /// Based on the CNCMaps Renderer code
    /// https://github.com/zzattack/ccmaps-net
    /// </summary>
    public class VplFile : VirtualFile
    {
        public VplFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = false)
            : base(baseStream, filename, baseOffset, fileSize, isBuffered)
        {
            Parse();
        }

        public VplFile(Stream baseStream, string filename = "", bool isBuffered = true)
            : base(baseStream, filename, isBuffered)
        {
            Parse();
        }


        public VplFile(byte[] buffer, string filename = "") : base(new MemoryStream(buffer), filename, true)
        {
            Parse();
        }


        private uint firstRemap;
        private uint lastRemap;
        private uint numSections;
        private uint unknown;
        // private Palette _palette; // unused
        private List<byte[]> lookupSections = new();

        private void Parse()
        {
            firstRemap = ReadUInt32();
            lastRemap = ReadUInt32();
            numSections = ReadUInt32();
            unknown = ReadUInt32();
            var pal = Read(768);
            // palette = new Palette(pal, "voxels.vpl");
            for (uint i = 0; i < numSections; i++)
                lookupSections.Add(Read(256));
        }

        public byte GetPaletteIndex(byte page, byte color)
        {
            return lookupSections[page][color];
        }
    }
}