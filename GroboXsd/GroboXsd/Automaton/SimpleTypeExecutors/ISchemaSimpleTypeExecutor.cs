using GroboXsd.Errors;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public interface ISchemaSimpleTypeExecutor
    {
        [CanBeNull]
        SchemaAutomatonError Execute([CanBeNull] string value, [NotNull] string nodeType, [NotNull] string name, int lineNumber, int linePosition);
    }
}