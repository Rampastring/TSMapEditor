using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;

namespace TSMapEditor.CCEngine
{
    public class CCFileManager
    {
        public string GameDirectory { get; set; }

        /// <summary>
        /// Contains information on which MIX file each found game file can be loaded from.
        /// </summary>
        private Dictionary<uint, FileLocationInfo> fileLocationInfos = new Dictionary<uint, FileLocationInfo>();

        /// <summary>
        /// List of all MIX files that have been registered to the file manager.
        /// </summary>
        private List<MixFile> mixFiles = new List<MixFile>();

        /// <summary>
        /// List of all CSF files that have been registered to the file manager.
        /// </summary>
        private List<CsfFile> csfFiles = new();

        private List<string> searchDirectories = new List<string>();


        public void ReadConfig()
        {
            var iniFile = new IniFile(Environment.CurrentDirectory + "/Config/FileManagerConfig.ini");

            AddSearchDirectory(Environment.CurrentDirectory);
            iniFile.DoForEveryValueInSection("SearchDirectories", v => AddSearchDirectory(Path.Combine(GameDirectory, v)));
            iniFile.DoForEveryValueInSection("PrimaryMIXFiles", v => LoadPrimaryMixFile(v));
            iniFile.DoForEveryValueInSection("SecondaryMIXFiles", v => LoadSecondaryMixFile(v));
            iniFile.DoForEveryValueInSection("StringTables", v => LoadStringTable(v));
        }

        /// <summary>
        /// Adds a directory to the list of directories where files will be
        /// searched from.
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        public void AddSearchDirectory(string path)
        {
            char lastChar = path[path.Length - 1];
            if (lastChar != '/' && lastChar != '\\')
                path = path + "/";
            searchDirectories.Add(path);
        }

        /// <summary>
        /// Loads a MIX file.
        /// Searches for it from both the search directories
        /// as well as already loaded MIX files.
        /// </summary>
        /// <param name="name">The name of the MIX file.</param>
        /// <returns>True if the MIX file was successfully loaded, otherwise false.</returns>
        private bool LoadMIXFile(string name)
        {
            uint identifier = MixFile.GetFileID(name);

            // First check from game directory, if not found then check from already loaded MIX files
            if (!LoadMixFromDirectories(name))
            {
                if (fileLocationInfos.TryGetValue(identifier, out FileLocationInfo value))
                {
                    var mixFile = new MixFile(value.MixFile, value.Offset);
                    mixFile.Parse();
                    AddMix(mixFile);
                    return true;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to search for and load a MIX file from the search directories.
        /// Returns true if loading the MIX file succeeds, otherwise false.
        /// </summary>
        /// <param name="name">The name of the MIX file.</param>
        /// <returns>True if the MIX file was successfully loaded, otherwise false.</returns>
        private bool LoadMixFromDirectories(string name)
        {
            string searchDir = null;

            foreach (string dir in searchDirectories)
            {
                if (File.Exists(dir + name))
                {
                    searchDir = dir;
                    break;
                }
            }

            if (searchDir == null)
                return false;

            var mixFile = new MixFile();
            mixFile.Parse(searchDir + name);
            AddMix(mixFile);

            return true;
        }

        /// <summary>
        /// Registers a MIX file to the file system.
        /// Adds all file entries from the MIX file to the file location tracking system.
        /// </summary>
        /// <param name="mixFile">The MIX file to register.</param>
        private void AddMix(MixFile mixFile)
        {
            mixFiles.Add(mixFile);

            foreach (MixFileEntry fileEntry in mixFile.GetEntries())
            {
                fileLocationInfos[fileEntry.Identifier] = new FileLocationInfo(mixFile, fileEntry.Offset, fileEntry.Size);
            }
        }

        /// <summary>
        /// Loads a required MIX file.
        /// Throws a FileNotFoundException if the MIX file isn't found.
        /// </summary>
        /// <param name="name">The name of the MIX file.</param>
        public void LoadPrimaryMixFile(string name)
        {
            if (!LoadMIXFile(name))
            {
                throw new FileNotFoundException("Primary MIX file not found: " + name);
            }
        }

        /// <summary>
        /// Loads an optional MIX file.
        /// Does not throw an exception if the MIX file is not found.
        /// </summary>
        /// <param name="name">The name of the MIX file.</param>
        public void LoadSecondaryMixFile(string name)
        {
            if (!LoadMIXFile(name))
            {
                Logger.Log("Secondary MIX file not found: " + name);
            }
        }

        /// <summary>
        /// Loads a required CSF file.
        /// Throws a FileNotFoundException if the CSF file isn't found.
        /// </summary>
        /// <param name="name">The name of the CSf file.</param>
        public void LoadStringTable(string name)
        {
            var data = LoadFile(name);
            if (data == null)
                throw new FileNotFoundException("CSF file not found: " + name);
            var file = new CsfFile(name);
            file.ParseFromBuffer(data);
            csfFiles.Add(file);
        }

        /// <summary>
        /// Drain loaded stringtable list and return it.
        /// </summary>
        public List<CsfFile> DrainStringTables()
        {
            var list = csfFiles;
            csfFiles = new();
            return list;
        }

        public byte[] LoadFile(string name)
        {
            if (File.Exists(Path.Combine(Environment.CurrentDirectory, name)))
                return File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, name));

            uint id = MixFile.GetFileID(name);

            if (fileLocationInfos.TryGetValue(id, out FileLocationInfo value))
            {
                return value.MixFile.GetSingleFileData(value.Offset, value.Size);
            }

            return null;
        }
    }

    /// <summary>
    /// Struct for holding data on which MIX file a file exists in,
    /// and where the file exists within the MIX file.
    /// </summary>
    internal struct FileLocationInfo
    {
        public FileLocationInfo(MixFile mixFile, int offset, int size)
        {
            MixFile = mixFile;
            Offset = offset;
            Size = size;
        }

        public MixFile MixFile { get; private set; }
        public int Offset { get; private set; }
        public int Size { get; private set; }
    }
}
