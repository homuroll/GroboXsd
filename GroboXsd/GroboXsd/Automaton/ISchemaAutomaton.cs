using System.Collections.Generic;

using GroboXsd.Errors;

using JetBrains.Annotations;

namespace GroboXsd.Automaton
{
    public interface ISchemaAutomaton
    {
        bool InAnyTypeState { get; }
        bool HasText { get; }
        void Reset();
        void SetLineInfo(int lineNumber, int linePosition);
        SchemaAutomatonError StartElement([NotNull] string name);
        SchemaAutomatonError EndElement();
        SchemaAutomatonError ReadAttribute([NotNull] string name, [NotNull] string value);
        SchemaAutomatonError ReadText([NotNull] string text);
        SchemaAutomatonError ReadWhitespace([NotNull] string whitespace);
        IEnumerable<SchemaAutomatonError> CheckRequiredAttributes();
    }
}