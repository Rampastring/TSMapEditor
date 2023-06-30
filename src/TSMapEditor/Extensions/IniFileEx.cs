using System.IO;
using System.Text;
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

    public IniFileEx() : base() { }

    public IniFileEx(string fileName) : base(fileName) { }

    public IniFileEx(string filePath, CCFileManager ccFileManager) : base(filePath)
    {
        Include(ccFileManager);
        Inherit(ccFileManager);
    }

    public IniFileEx(string filePath, Encoding encoding, CCFileManager ccFileManager) : base(filePath, encoding)
    {
        Include(ccFileManager);
        Inherit(ccFileManager);
    }

    public IniFileEx(Stream stream, CCFileManager ccFileManager) : base(stream)
    {
        Include(ccFileManager);
        Inherit(ccFileManager);
    }

    public IniFileEx(Stream stream, Encoding encoding, CCFileManager ccFileManager) : base(stream, encoding)
    {
        Include(ccFileManager);
        Inherit(ccFileManager);
    }

    /// <summary>
    /// Creates an INI file from a directory + path or from MIX file system.
    /// </summary>
    /// <param name="filePath">Path to file or file name inside MIX file system.</param>
    /// <param name="gameDirectory">The path to the game directory.</param>
    /// <param name="ccFileManager">File manager object holding MIXes.</param>
    /// <returns>Constructed INI file or empty INI file on failure.</returns>
    public static IniFileEx FromPathOrMix(string filePath, string gameDirectory, CCFileManager ccFileManager)
    {
        if (filePath.Length == 0)
            return new();

        string rulesPath = Path.Combine(gameDirectory, filePath);
        if (File.Exists(rulesPath))
            return new(rulesPath, ccFileManager);

        var rulesBytes = ccFileManager.LoadFile(filePath);
        if (rulesBytes != null)
            return new(new MemoryStream(rulesBytes), ccFileManager);

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
    public void Inherit(CCFileManager ccFileManager)
    {
        if (!Constants.EnableIniInheritance)
            return;

        foreach (var section in Sections)
        {
            if (!section.KeyExists(PhobosInheritsSection))
                continue;

            foreach (var parentName in section.GetListValue<string>(PhobosInheritsSection, ',', x => x))
            {
                var parent = Sections.Find(s => s.SectionName == parentName);
                if (parent == null)
                    continue;

                foreach (var pair in parent.Keys)
                {
                    if (!section.KeyExists(pair.Key))
                        section.AddKey(pair.Key, pair.Value);
                }
            }
        }
    }
}
