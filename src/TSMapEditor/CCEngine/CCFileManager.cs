using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using TSMapEditor.UI;

namespace TSMapEditor.CCEngine
{
    public class CCFileManager
    {
        public string GameDirectory { get; set; }
        
        private List<string> searchDirectories = new();

        /// <summary>
        /// Contains information on which MIX file each found game file can be loaded from.
        /// </summary>
        private Dictionary<uint, FileLocationInfo> fileLocationInfos = new();

        /// <summary>
        /// List of all MIX files that have been registered with the file manager.
        /// </summary>
        private List<MixFile> mixFiles = new();

        /// <summary>
        /// List of all CSF files that have been registered with the file manager.
        /// </summary>
        public List<CsfFile> CsfFiles { get; } = new();

        public void ReadConfig()
        {
            string configPath =
                Helpers.NormalizePath(Path.Combine(Environment.CurrentDirectory, "Config", "FileManagerConfig.ini"));
            var iniFile = new IniFile(configPath);

            AddSearchDirectory(Environment.CurrentDirectory);
            iniFile.DoForEveryValueInSection("SearchDirectories", v => AddSearchDirectory(Path.Combine(GameDirectory, v)));
            iniFile.DoForEveryValueInSection("MIXFiles", ProcessMixFileEntry);
            iniFile.DoForEveryValueInSection("StringTables", LoadStringTable);
        }

        /// <summary>
        /// Adds a directory to the list of directories where files will be
        /// searched from.
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        public void AddSearchDirectory(string path)
        {
            searchDirectories.Add(Helpers.NormalizePath(path));
        }

        /// <summary>
        /// Processes an entry in the MIXFiles list.
        /// Loads a required or optional MIX file
        /// or handles a special entry.
        /// </summary>
        /// <param name="entry">Contents of an entry of the MIXFiles list.</param>
        private void ProcessMixFileEntry(string entry)
        {
            var parts = entry.Split(',', StringSplitOptions.TrimEntries);
            string mixName = parts[0];

            if (IsSpecialMixName(mixName))
            {
                HandleSpecialMixName(mixName);
                return;
            }

            bool isRequired = false;

            if (parts.Length > 1)
                isRequired = Conversions.BooleanFromString(parts[1], isRequired);

            if (isRequired)
                LoadRequiredMixFile(mixName);
            else
                LoadOptionalMixFile(mixName);
        }

        /// <summary>
        /// Loads a MIX file.
        /// Searches for it from both the search directories
        /// as well as already loaded MIX files.
        /// </summary>
        /// <param name="name">The name of the MIX file.</param>
        /// <returns>True if the MIX file was successfully loaded, otherwise false.</returns>
        private bool LoadMixFile(string name)
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
                if (File.Exists(Path.Combine(dir, name)))
                {
                    searchDir = dir;
                    break;
                }
            }

            if (searchDir == null)
                return false;

            var mixFile = new MixFile();
            mixFile.Parse(Path.Combine(searchDir, name));
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
                if (fileLocationInfos.ContainsKey(fileEntry.Identifier))
                    continue;

                fileLocationInfos[fileEntry.Identifier] = new FileLocationInfo(mixFile, fileEntry.Offset, fileEntry.Size);
            }
        }

        /// <summary>
        /// Loads a required MIX file.
        /// Throws a FileNotFoundException if the MIX file isn't found.
        /// </summary>
        /// <param name="name">The name of the MIX file.</param>
        public void LoadRequiredMixFile(string name)
        {
            if (!LoadMixFile(name))
            {
                throw new FileNotFoundException("Required MIX file not found: " + name);
            }
        }

        /// <summary>
        /// Loads an optional MIX file.
        /// Does not throw an exception if the MIX file is not found.
        /// </summary>
        /// <param name="name">The name of the MIX file.</param>
        public void LoadOptionalMixFile(string name)
        {
            if (!LoadMixFile(name))
            {
                Logger.Log("Optional MIX file not found: " + name);
            }
        }

        /// <summary>
        /// Loads MIX files of the format NAME##.
        /// </summary>
        /// <param name="name">The common name of the MIX files.</param>
        public void LoadIndexedMixFiles(string name)
        {
            for (int i = 99; i >= 0; i--)
                LoadMixFile($"{name}{i:00}.mix");
        }

        /// <summary>
        /// Loads MIX files with a wildcard.
        /// </summary>
        /// <param name="name">The common name of the MIX files.</param>
        public void LoadWildcardMixFiles(string name)
        {
            foreach (string searchDirectory in searchDirectories)
            {
                if (!Directory.Exists(searchDirectory))
                    continue;

                var files = Directory.GetFiles(searchDirectory, name);
                foreach (string file in files)
                    LoadMixFile(Path.GetFileName(file));
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
            CsfFiles.Add(file);
        }

        public byte[] LoadFile(string name)
        {
            foreach (string searchDirectory in searchDirectories)
            {
                string looseFilePath = Path.Combine(searchDirectory, name);
                if (File.Exists(looseFilePath))
                    return File.ReadAllBytes(looseFilePath);
            }

            uint id = MixFile.GetFileID(name);

            if (fileLocationInfos.TryGetValue(id, out FileLocationInfo value))
            {
                return value.MixFile.GetSingleFileData(value.Offset, value.Size);
            }

            return null;
        }

        private bool IsSpecialMixName(string name)
        {
            name = name.ToUpper();
            switch (name)
            {
                case "$TSECACHE":
                case "$RA2ECACHE":
                case "$TSELOCAL":
                case "$RA2ELOCAL":
                case "$EXPAND":
                case "$EXPANDMD":
                    return true;
                default:
                    return false;
            }
        }

        private void HandleSpecialMixName(string name)
        {
            name = name.ToUpper();
            switch (name)
            {
                case "$TSECACHE":
                    LoadIndexedMixFiles("ecache");
                    break;
                case "$RA2ECACHE":
                    LoadWildcardMixFiles("ecache*.mix");
                    break;
                case "$TSELOCAL":
                    LoadIndexedMixFiles("elocal");
                    break;
                case "$RA2ELOCAL":
                    LoadWildcardMixFiles("elocal*.mix");
                    break;
                case "$EXPAND":
                    LoadIndexedMixFiles("expand");
                    break;
                case "$EXPANDMD":
                    LoadIndexedMixFiles("expandmd");
                    break;
            }
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
