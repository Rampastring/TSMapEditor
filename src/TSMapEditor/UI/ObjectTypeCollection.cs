using System;
using System.Collections.Generic;

namespace TSMapEditor.UI
{
    public abstract class ObjectTypeCollection
    {
        public string Name { get; set; }

        public List<string> AllowedTheaters { get; set; }

        public bool IsValidForTheater(string theaterName)
        {
            if (AllowedTheaters == null || AllowedTheaters.Count == 0)
                return true;

            return AllowedTheaters.Exists(t => t.Equals(theaterName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
