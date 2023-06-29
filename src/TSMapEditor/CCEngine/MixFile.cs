using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TSMapEditor.CCEngine
{
    /// <summary>
    /// A Tiberian Sun / Red Alert 2 type MIX file.
    /// </summary>
    class MixFile
    {

        private const int INDEX_POSITION = 10;


        public MixFile() { }

        public MixFile(MixFile masterMix, int startOffset)
        {
            this.masterMix = masterMix;
            this.mixStartOffset = startOffset;
        }


        public string FilePath { get; private set; }

        private List<MixFileEntry> entries;

        private int bodyOffset;

        private Stream stream;

        /// <summary>
        /// The MIX file that this MIX file resides in, if any.
        /// </summary>
        private MixFile masterMix;

        /// <summary>
        /// The start offset for this MIX file when it's inside another MIX file.
        /// Should be zero if this MIX file is not inside another MIX file.
        /// </summary>
        private int mixStartOffset = 0;


        private readonly object locker = new object();

        /// <summary>
        /// Reads MIX file information from a MIX file in the given file system path.
        /// </summary>
        /// <param name="path">The path to the MIX file.</param>
        public void Parse(string path)
        {
            if (masterMix != null)
                throw new InvalidOperationException("Can't parse from a file when the MIX file is inside another MIX file.");

            FilePath = path;

            using (FileStream fileStream = File.OpenRead(path))
            {
                Parse(fileStream);
            }
        }

        /// <summary>
        /// Reads MIX file entry information from a stream.
        /// </summary>
        /// <param name="stream">The stream. Can be null for MIX files that
        /// reside inside another MIX file.</param>
        public void Parse(Stream stream = null)
        {
            if (masterMix != null)
            {
                masterMix.OpenFile();
                stream = this.stream = masterMix.stream;
                stream.Position = mixStartOffset;
            }

            if (stream.Length < INDEX_POSITION)
                return;

            entries = new List<MixFileEntry>();

            byte[] buffer = new byte[256];
            stream.Read(buffer, 0, 4);
            MixType mixType = (MixType)BitConverter.ToInt32(buffer, 0);
            
            var isEncrypted = (mixType & MixType.ENCRYPTED) != 0;

            if (isEncrypted)
            {
                // Read and decrypt the Blowfish associated with this MIX.
                stream.Read(buffer, 0, KeyDecryptor.SIZE_OF_ENCRYPTED_KEY);
                stream = new BlowfishStream(stream, KeyDecryptor.DecryptBlowfishKey(buffer));
            }

            stream.Read(buffer, 0, MixFileHeader.SIZE_OF_HEADER);

            MixFileHeader header = new MixFileHeader(buffer);

            bodyOffset = INDEX_POSITION + MixFileEntry.SIZE_OF_FILE_ENTRY * header.FileCount;

            if (isEncrypted)
            {
                // Account for Blowfish key and padding.
                bodyOffset += KeyDecryptor.SIZE_OF_ENCRYPTED_KEY;
                bodyOffset += (header.FileCount % 2) == 0 ? 2 : 6;
            }

            for (int i = 0; i < header.FileCount; i++)
            {
                if (stream.Position + MixFileEntry.SIZE_OF_FILE_ENTRY >= stream.Length)
                    throw new MixParseException("Invalid MIX file.");

                stream.Read(buffer, 0, MixFileEntry.SIZE_OF_FILE_ENTRY);
                entries.Add(new MixFileEntry(buffer));
            }

            if (masterMix != null)
                masterMix.CloseFile();
        }

        /// <summary>
        /// Returns a list of file entries in this MIX file.
        /// </summary>
        public List<MixFileEntry> GetEntries()
        {
            // Return a copy of the list so the callee can't modify our original list
            return new List<MixFileEntry>(entries);
        }

        /// <summary>
        /// Opens the MIX file for performing one or more read operations.
        /// </summary>
        public void OpenFile()
        {
            if (masterMix != null)
            {
                masterMix.OpenFile();
                this.stream = masterMix.stream;
                return;
            }

            if (FilePath == null)
                throw new MixParseException("No MIX file path defined!");

            if (stream == null || !stream.CanRead)
                stream = File.OpenRead(FilePath);
        }

        public MixFileEntry? GetEntry(uint id)
        {
            int index = entries.FindIndex(e => e.Identifier == id);
            if (index < 0)
                return null;

            return entries[index];
        }

        /// <summary>
        /// Gets file data from the MIX file.
        /// </summary>
        /// <param name="offset">The start offset from the MIX body.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>A byte array.</returns>
        public byte[] GetFileData(int offset, int count)
        {
            byte[] buffer = new byte[count];

            stream.Position = mixStartOffset + bodyOffset + offset;
            stream.Read(buffer, 0, count);

            return buffer;
        }

        /// <summary>
        /// Closes the MIX file.
        /// </summary>
        public void CloseFile()
        {
            stream.Close();
        }

        /// <summary>
        /// Gets data for a single file from the MIX file.
        /// </summary>
        /// <param name="offset">The start offset from the MIX body.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>A byte array.</returns>
        public byte[] GetSingleFileData(int offset, int count)
        {
            lock (locker)
            {
                OpenFile();
                byte[] buffer = GetFileData(offset, count);
                CloseFile();
                return buffer;
            }
        }

        /// <summary>
        /// Calculates and returns the internal ID of a file based on its name.
        /// The ID is needed when finding files from inside MIX files.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        /// <returns></returns>
        public static uint GetFileID(string fileName)
        {
            fileName = fileName.ToUpperInvariant();
            int a = fileName.Length >> 2;
            if ((fileName.Length & 3) > 0)
            {
                fileName += (char)(fileName.Length - (a << 2));
                int i = 3 - ((fileName.Length - 1) & 3);
                while (i-- > 0)
                    fileName = fileName + fileName[a << 2];
            }

            return MixCRC.GetCRC(Encoding.ASCII.GetBytes(fileName));
        }
    }

    /// <summary>
    /// The type of a MIX file.
    /// </summary>
    [Flags]
    public enum MixType
    {
        DEFAULT = 0,
        CHECKSUMMED = 0x00010000,
        ENCRYPTED = 0x00020000
    }

    /// <summary>
    /// A MIX file header. Contains information on the number of files and the size
    /// of the body of the MIX file.
    /// </summary>
    public struct MixFileHeader
    {
        public const int SIZE_OF_HEADER = 6;

        public MixFileHeader(byte[] buffer)
        {
            if (buffer.Length < SIZE_OF_HEADER)
                throw new ArgumentException("buffer is not long enough");

            FileCount = BitConverter.ToInt16(buffer, 0);
            BodySize = BitConverter.ToInt32(buffer, 2);
        }

        /// <summary>
        /// The number of files in the MIX file.
        /// </summary>
        public short FileCount { get; private set; }

        /// <summary>
        /// The size of the MIX file, excluding the header and index.
        /// </summary>
        public int BodySize { get; private set; }
    }

    /// <summary>
    /// Contains information on a file stored inside a MIX file.
    /// </summary>
    public struct MixFileEntry
    {
        public const int SIZE_OF_FILE_ENTRY = 12;

        public MixFileEntry(byte[] buffer)
        {
            if (buffer.Length < SIZE_OF_FILE_ENTRY)
                throw new ArgumentException("buffer is not long enough");

            Identifier = BitConverter.ToUInt32(buffer, 0);
            Offset = BitConverter.ToInt32(buffer, 4);
            Size = BitConverter.ToInt32(buffer, 8);
        }

        /// <summary>
        /// The identifier used to identify the file instead of a normal name.
        /// </summary>
        public uint Identifier { get; private set; }

        /// <summary>
        /// The offset of the file, from the start of the body.
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        /// The size of the file.
        /// </summary>
        public int Size { get; private set; }
    }

    public struct FileOffsetInfo
    {

    }

    public class MixParseException : Exception
    {
        public MixParseException(string message) : base(message)
        {
        }
    }
}
