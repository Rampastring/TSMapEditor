using System.Collections.Generic;
using System.Collections.Immutable;
using TSMapEditor.Extensions;
using TSMapEditor.Misc;

namespace TSMapEditor.Models
{
    public class Sound
    {
        public Sound(int index, string name)
        {
            Index = index;
            Name = name;
        }

        public override string ToString()
        {
            return $"{Index} {Name}";
        }

        public int Index { get; }
        public string Name { get; }
    }

    public class Sounds
    {
        public Sounds(IniFileEx soundIni)
        {
            Initialize(soundIni);
        }

        public ImmutableList<Sound> List { get; private set; }

        public Sound Get(int index)
        {
            return List.GetElementIfInRange(index);
        }

        public Sound Get(string name)
        {
            return List.Find(sound => sound.Name == name);
        }

        private void Initialize(IniFileEx evaIni)
        {
            var sounds = new List<Sound>();

            const string speechSectionName = "SoundList";

            evaIni.DoForEveryValueInSection(speechSectionName, name =>
            {
                if (string.IsNullOrEmpty(name))
                    return;

                sounds.Add(new Sound(sounds.Count, name));
            });

            List = ImmutableList.Create(sounds.ToArray());
        }
    }
}