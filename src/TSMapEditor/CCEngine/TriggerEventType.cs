using TSMapEditor.Models;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.CCEngine
{
    public class TriggerEventType : INIDefineable
    {
        public TriggerEventType(int id)
        {
            ID = id;
        }

        public int ID { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public TriggerParamType P1Type { get; set; } = TriggerParamType.Unused;
        public TriggerParamType P2Type { get; set; } = TriggerParamType.Unused;
        public bool Available { get; set; } = true;
    }
}
