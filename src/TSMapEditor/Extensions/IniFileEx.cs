using System.IO;
using System.Text;
using Rampastring.Tools;

namespace TSMapEditor.Extensions;

public class IniFileEx: Rampastring.Tools.IniFile
{
    public IniFileEx() : base() { }

    public IniFileEx(string filePath) : base(filePath) { }

    public IniFileEx(string filePath, Encoding encoding) : base(filePath, encoding) { }

    public IniFileEx(Stream stream) : base(stream) { }

    public IniFileEx(Stream stream, Encoding encoding) : base(stream, encoding) { }

    public new void Parse()
    {
        base.Parse();

        foreach (var pair in GetSection("$Include").Keys)
        {
            string path = SafePath.CombineFilePath(SafePath.GetFileDirectoryName(FileName), pair.Value);
            IniFileEx includedIni = new(path);
            ConsolidateIniFiles(this, includedIni);
        }
    }
}
