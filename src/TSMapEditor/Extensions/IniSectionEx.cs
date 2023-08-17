using Rampastring.Tools;

namespace TSMapEditor.Extensions;

public class IniSectionEx : IniSection
{
    public bool AlreadyInherited { get; set; } = false;

    public IniSectionEx(IniSection section)
    {
        SectionName = section.SectionName;
        Keys = section.Keys;
    }
}
