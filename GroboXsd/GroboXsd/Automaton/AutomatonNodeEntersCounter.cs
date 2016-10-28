using System.Collections.Generic;

using JetBrains.Annotations;

namespace GroboXsd.Automaton
{
    public class AutomatonNodeEntersCounter
    {
        public AutomatonNodeEntersCounter(int id, int minOccurs, int? maxOccurs)
        {
            Id = id;
            MinOccurs = minOccurs;
            MaxOccurs = maxOccurs;
            ElementNames = new HashSet<string>();
        }

        public int Id { get; private set; }
        public int MinOccurs { get; private set; }
        public int? MaxOccurs { get; private set; }

        [NotNull]
        public HashSet<string> ElementNames { get; private set; }
    }
}