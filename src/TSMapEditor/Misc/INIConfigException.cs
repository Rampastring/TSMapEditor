using System;

namespace TSMapEditor
{
    /// <summary>
    /// The exception that is thrown when INI data is invalid.
    /// </summary>
    public class INIConfigException : Exception
    {
        public INIConfigException(string message) : base(message)
        {
        }
    }
}
