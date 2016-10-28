using GroboXsd.Parser;

using JetBrains.Annotations;

namespace GroboXsd.Automaton
{
    public interface ISchemaAutomatonFactoryBuilder
    {
        [NotNull]
        CreateSchemaAutomatonDelegate Build([CanBeNull] SchemaTypeBase schemaRootType);
    }

    public delegate ISchemaAutomaton CreateSchemaAutomatonDelegate();
}