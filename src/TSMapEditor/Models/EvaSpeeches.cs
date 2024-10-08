using System.Collections.Generic;
using System.Collections.Immutable;
using TSMapEditor.Extensions;
using TSMapEditor.Misc;

namespace TSMapEditor.Models
{
    public class EvaSpeech
    {
        public EvaSpeech(int index, string name, string text)
        {
            Index = index;
            Name = name;
            Text = text;
        }

        public override string ToString()
        {
            return $"{Name} {Text}";
        }

        public int Index { get; }
        public string Name { get; }
        public string Text { get; }
    }

    public class EvaSpeeches
    {
        public EvaSpeeches(IniFileEx evaIni)
        {
            Initialize(evaIni);
        }

        public EvaSpeeches(EvaSpeech[] speeches)
        {
            List = ImmutableList.Create(speeches);
        }

        public ImmutableList<EvaSpeech> List { get; private set; }

        public EvaSpeech Get(int index)
        {
            return List.GetElementIfInRange(index);
        }

        public EvaSpeech Get(string name)
        {
            return List.Find(speech => speech.Name == name);
        }

        private void Initialize(IniFileEx evaIni)
        {
            var speeches = new List<EvaSpeech>();

            const string speechSectionName = "DialogList";

            evaIni.DoForEveryValueInSection(speechSectionName, name =>
            {
                if (string.IsNullOrEmpty(name))
                    return;

                var speechSection = evaIni.GetSection(name);
                string text = string.Empty;

                if (speechSection != null)
                {
                    text = speechSection.GetStringValue("Text", text);
                }

                speeches.Add(new EvaSpeech(speeches.Count, name, text));
            });

            List = ImmutableList.Create(speeches.ToArray());
        }
    }
}
