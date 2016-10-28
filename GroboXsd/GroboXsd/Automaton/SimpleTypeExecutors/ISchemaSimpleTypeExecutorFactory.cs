using GroboXsd.Parser;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public interface ISchemaSimpleTypeExecutorFactory
    {
        [NotNull]
        ISchemaSimpleTypeExecutor Build([NotNull] SchemaSimpleType schemaSimpleType);
    }
}