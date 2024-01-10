namespace TSMapEditor.Models
{
    public class CsfString
    {
        public string ID { get; }
        public string Value { get; }

        public CsfString(string id, string value)
        {
            ID = id;
            Value = value;
        }
    }
}
