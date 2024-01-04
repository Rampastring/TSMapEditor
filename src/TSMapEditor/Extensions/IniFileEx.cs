using System.IO;
using System.Text;
using System.Collections.Generic;
using Rampastring.Tools;
using TSMapEditor.CCEngine;

namespace TSMapEditor.Extensions;

/// <summary>
/// IniFile with support for Ares #include and Phobos $Include and $Inherits.
/// </summary>
public class IniFileEx: IniFile
{
    private static readonly string AresIncludeSection = "#include";
    private static readonly string PhobosIncludeSection = "$Include";
    private static readonly string PhobosInheritsSection = "$Inherits";

    private readonly HashSet<IniSection> alreadyInheritedSections = new();

    public IniFileEx() : base() { }

    public IniFileEx(string fileName) : base(fileName) { }

    public IniFileEx(string filePath, CCFileManager ccFileManager) : base(filePath)
    {
        Include(ccFileManager);
        Inherit();
    }

    public IniFileEx(string filePath, Encoding encoding, CCFileManager ccFileManager) : base(filePath, encoding)
    {
        Include(ccFileManager);
        Inherit();
    }

    public IniFileEx(Stream stream, CCFileManager ccFileManager) : base(stream)
    {
        Include(ccFileManager);
        Inherit();
    }

    public IniFileEx(Stream stream, Encoding encoding, CCFileManager ccFileManager) : base(stream, encoding)
    {
        Include(ccFileManager);
        Inherit();
    }

    /// <summary>
    /// Creates an INI file from a directory + path or from MIX file system.
    /// </summary>
    /// <param name="filePath">Path to file or file name inside MIX file system.</param>
    /// <param name="gameDirectory">The path to the game directory.</param>
    /// <param name="ccFileManager">File manager object holding MIXes.</param>
    /// <returns>Loaded INI file, or empty INI file object if the file was not found.</returns>
    public static IniFileEx FromPathOrMix(string filePath, string gameDirectory, CCFileManager ccFileManager)
    {
        if (filePath.Length == 0)
            return new();

        string iniPath = Path.Combine(gameDirectory, filePath);
        if (File.Exists(iniPath))
            return new(iniPath, ccFileManager);

        var iniBytes = ccFileManager.LoadFile(filePath);
        if (iniBytes != null)
            return new(new MemoryStream(iniBytes), ccFileManager);

        return new();
    }

    /// <summary>
    /// Includes all base INI files into this file.
    /// </summary>
    public void Include(CCFileManager ccFileManager)
    {
        if (!Constants.EnableIniInclude)
            return;

        string sectionName;
        if (SectionExists(PhobosIncludeSection))
            sectionName = PhobosIncludeSection;
        else if (SectionExists(AresIncludeSection))
            sectionName = AresIncludeSection;
        else
            return;

        foreach (var pair in GetSection(sectionName).Keys)
        {
            string directory = FileName != null ? SafePath.CombineFilePath(SafePath.GetFileDirectoryName(FileName)) : "";
            IniFileEx includedIni = FromPathOrMix(pair.Value, directory, ccFileManager);
            ConsolidateIniFiles(includedIni, this);
            Sections = includedIni.Sections;
        }
    }

    /// <summary>
    /// For each section, inherits all missing keys from listed parents.
    /// </summary>
    public void Inherit()
    {
        if (!Constants.EnableIniInheritance)
            return;

        foreach (var section in Sections)
            InheritFromParent(section);

        alreadyInheritedSections.Clear();
    }

    private void InheritFromParent(IniSection section)
    {
        // If this section has already been processed, exit.
        if (alreadyInheritedSections.Contains(section))
            return;

        // If this section has no parents, mark as processed and exit.
        if (!section.KeyExists(PhobosInheritsSection))
        {
            alreadyInheritedSections.Add(section);
            return;
        }

        // Run recursively.
        foreach (var parentName in section.GetListValue<string>(PhobosInheritsSection, ',', x => x))
        {
            var parent = Sections.Find(s => s.SectionName == parentName);
            if (parent == null)
                continue;

            InheritFromParent(parent);

            foreach (var pair in parent.Keys)
            {
                if (!section.KeyExists(pair.Key))
                    section.AddKey(pair.Key, pair.Value);
            }
        }
        alreadyInheritedSections.Add(section);
    }
}
