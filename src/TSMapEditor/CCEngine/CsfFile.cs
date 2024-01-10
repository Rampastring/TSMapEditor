using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using TSMapEditor.Models;

namespace TSMapEditor.CCEngine
{

    public class CsfLoadException : Exception
    {
        public CsfLoadException(string message) : base(message)
        {
        }
    }

    public enum CsfVersion
    {
        Nox = 2,
        Cnc = 3,
    }

    public enum CsfLanguage
    {
        EnglishAmerican = 0,
        EnglishBritish = 1,
        German = 2,
        French = 3,
        Spanish = 4,
        Italian = 5,
        Japanese = 6,
        Jabberwockie = 7,
        Korean = 8,
        Chinese = 9,
    }

    /// <summary>
    /// Represents a CSF file header. Most of the informations here are not useful.
    /// </summary>
    struct CsfFileHeader
    {
        public const int SizeOf = 24;
        private const uint CsfPrefix = 0x43534620;  // " FSC" as an uint32

        public CsfFileHeader(byte[] buffer)
        {
            if (buffer.Length < SizeOf)
                throw new CsfLoadException(nameof(CsfFileHeader) + ": buffer is not long enough");

            if (BitConverter.ToUInt32(buffer, 0) != CsfPrefix)
                throw new CsfLoadException(nameof(CsfFileHeader) + ": FSC prefix not found");

            Version = (CsfVersion)BitConverter.ToUInt32(buffer, 4);
            NumberOfLabels = BitConverter.ToUInt32(buffer, 8);
            NumberOfStrings = BitConverter.ToUInt32(buffer, 12);
            Unused = BitConverter.ToUInt32(buffer, 16);
            Language = (CsfLanguage)BitConverter.ToUInt32(buffer, 20);
        }

        public CsfVersion Version;
        public uint NumberOfLabels;
        public uint NumberOfStrings;
        public uint Unused;
        public CsfLanguage Language;
    }

    /// <summary>
    /// Represents a CSF (stringtable) file. Contains several string labels: text key-value pairs.
    /// </summary>
    public class CsfFile
    {
        private const uint LblPrefix = 0x4C424C20;  // " LBL" as an uint32
        private const uint StrPrefix = 0x53545220;  // " RTS" as an uint32
        private const uint StrwPrefix = 0x53545257;  // "WRTS" as an uint32

        public CsfFile() { }

        public CsfFile(string fileName)
        {
            this.fileName = fileName;
        }

        private readonly string fileName;
        private CsfFileHeader csfFileHeader;

        public CsfString[] Strings { get; private set; }

        /// <summary>
        /// Creates a CSf file from a directory + path or from MIX file system.
        /// </summary>
        /// <param name="filePath">Path to file or file name inside MIX file system.</param>
        /// <param name="gameDirectory">The path to the game directory.</param>
        /// <param name="ccFileManager">File manager object holding MIXes.</param>
        /// <returns>Loaded CSF file, or empty CSF file object if the file was not found.</returns>
        public static CsfFile FromPathOrMix(string filePath, string gameDirectory, CCFileManager ccFileManager)
        {
            if (filePath.Length == 0)
                return new();

            string path = Path.Combine(gameDirectory, filePath);
            if (File.Exists(path))
            {
                var file = new CsfFile(path);
                file.ParseFromFile(path);
                return file;
            }

            var bytes = ccFileManager.LoadFile(filePath);
            if (bytes != null)
            {
                var file = new CsfFile(filePath);
                file.ParseFromBuffer(bytes);
            }

            return new();
        }

        public void ParseFromFile(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                Parse(stream);
            }
        }

        public void Parse(Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(buffer, 0, buffer.Length);
            ParseFromBuffer(buffer);
        }

        public void ParseFromBuffer(byte[] buffer)
        {
            try
            {
                csfFileHeader = new CsfFileHeader(buffer);
                var strings = new List<CsfString>((int)csfFileHeader.NumberOfLabels);

                using (var memoryStream = new MemoryStream(buffer))
                {
                    memoryStream.Position = CsfFileHeader.SizeOf;
                    for (int i = 0; i < csfFileHeader.NumberOfLabels; i++)
                        strings.Add(ParseLabel(memoryStream));
                }

                Strings = strings.ToArray();
            }
            catch (CsfLoadException ex)
            {
                throw new CsfLoadException("Failed to load CSF file. Make sure that the file is not corrupted. Filename: " + fileName + ", original exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Parse a CsfString from a stream.
        /// </summary>
        /// <param name="memoryStream">Input stream.</param>
        /// <exception cref="CsfLoadException"></exception>
        public CsfString ParseLabel(MemoryStream memoryStream)
        {
            var buffer = new byte[4];
            memoryStream.Read(buffer, 0, 4);

            if (BitConverter.ToUInt32(buffer) != LblPrefix)
                throw new CsfLoadException(nameof(CsfFile) + ": LBL prefix not found");

            memoryStream.Read(buffer, 0, 4);
            uint numberOfPairs = BitConverter.ToUInt32(buffer, 0);
            memoryStream.Read(buffer, 0, 4);
            uint labelLength = BitConverter.ToUInt32(buffer, 0);

            var labelBuffer = new byte[labelLength];
            memoryStream.Read(labelBuffer, 0, labelBuffer.Length);
            var csfLabel = System.Text.Encoding.ASCII.GetString(labelBuffer);

            var csfString = ParseString(memoryStream, buffer);
            for (uint i = 1; i < numberOfPairs; i++)
                ParseString(memoryStream, buffer, skip: true);

            return new CsfString(csfLabel, csfString);
        }

        /// <summary>
        /// Parse a single string from a stream.
        /// </summary>
        /// <param name="memoryStream">Input stream.</param>
        /// <param name="buffer">4 byte long temporary buffer.</param>
        /// <param name="skip">Only advance stream and return null.</param>
        /// <returns>Read string or null if `skip` was true.</returns>
        /// <exception cref="CsfLoadException"></exception>
        public string ParseString(MemoryStream memoryStream, byte[] buffer, bool skip = false)
        {
            memoryStream.Read(buffer, 0, 4);
            uint prefix = BitConverter.ToUInt32(buffer);
            bool hasExtra = prefix == StrwPrefix;

            if ((prefix != StrPrefix) && !hasExtra)
                throw new CsfLoadException(nameof(CsfFile) + ": STR/STRW prefix not found");

            memoryStream.Read(buffer, 0, 4);
            uint stringLength = BitConverter.ToUInt32(buffer, 0);
            string csfString = null;

            if (!skip)
            {
                var stringBuffer = new byte[stringLength * 2];
                memoryStream.Read(stringBuffer, 0, stringBuffer.Length);
                // Westwood's "encoding".
                for (uint i = 0; i < stringBuffer.Length; i++)
                    stringBuffer[i] = (byte)~stringBuffer[i];

                csfString = System.Text.Encoding.Unicode.GetString(stringBuffer);
            } else
            {
                memoryStream.Position += stringLength;
            }

            if (hasExtra)
            {
                memoryStream.Read(buffer, 0, 4);
                uint extraLength = BitConverter.ToUInt32(buffer, 0);
                // Skip the extra data, as it's unused by the game.
                memoryStream.Position += extraLength;
            }

            return csfString;
        }
    }
}
