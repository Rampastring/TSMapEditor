using System;
using System.Collections.Generic;
using System.IO;
using CNCMaps.FileFormats.VirtualFileSystem;
using Microsoft.Xna.Framework;

namespace TSMapEditor.CCEngine
{
    public class HvaLoadException : Exception
    {
        public HvaLoadException(string message) : base(message) { }
    }

    /// <summary>
    /// .hva file format
    /// Based on the CNCMaps Renderer code
    /// https://github.com/zzattack/ccmaps-net
    /// </summary>

    public class HvaFile : VirtualFile
    {
        public int NumFrames { get; set; }
        public List<Section> Sections { get; set; }

        public class Section
        {
            public string Name;
            public List<float[]> Matrices;
            public Section(int numMatrices)
            {
                Matrices = new List<float[]>(numMatrices);
            }
        }

        public HvaFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = true)
            : base(baseStream, filename, baseOffset, fileSize, isBuffered)
        {
            Initialize();
        }

        public HvaFile(Stream baseStream, string filename = "", bool isBuffered = true)
            : base(baseStream, filename, isBuffered)
        {
            Initialize();
        }

        public HvaFile(byte[] buffer, string filename = "") : base(new MemoryStream(buffer), filename, true)
        {
            Initialize();
        }

        private void Initialize()
        {
            Seek(0, SeekOrigin.Begin);
            ReadCString(16); // filename
            NumFrames = ReadInt32();
            int numSections = ReadInt32();
            Sections = new List<Section>(numSections);

            for (int i = 0; i < numSections; i++)
            {
                Sections.Add(new Section(NumFrames)
                {
                    Name = ReadCString(16)
                });
            }

            for (int frame = 0; frame < NumFrames; frame++)
            {
                for (int section = 0; section < Sections.Count; section++)
                    Sections[section].Matrices.Add(ReadMatrix());
            }
        }

        private float[] ReadMatrix()
        {
            var ret = new float[12];
            for (int i = 0; i < 12; i++)
            {
                ret[i] = ReadFloat();
            }
            return ret;
        }

        public Matrix LoadMatrix(string section, int frame = 0)
        {
            return ToMatrix(Sections.Find(s => s.Name == section).Matrices[frame]);
        }

        public Matrix LoadMatrix(int section, int frame = 0)
        {
            return ToMatrix(Sections[section].Matrices[frame]);
        }

        private static Matrix ToMatrix(float[] hvaMatrix)
        {
            return new Matrix(
                hvaMatrix[0], hvaMatrix[4], hvaMatrix[8], 0,
                hvaMatrix[1], hvaMatrix[5], hvaMatrix[9], 0,
                hvaMatrix[2], hvaMatrix[6], hvaMatrix[10], 0,
                hvaMatrix[3], hvaMatrix[7], hvaMatrix[11], 1);
        }
    }
}