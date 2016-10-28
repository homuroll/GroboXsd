using GroboXsd.Errors;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public class BooleanSimpleTypeExecutor : SchemaSimpleTypeExecutorBase
    {
        public BooleanSimpleTypeExecutor()
            : base(new StringSimpleTypeExecutor())
        {
        }

        [CanBeNull]
        protected override SchemaAutomatonError ExecuteInternal([CanBeNull] string value, [NotNull] string nodeType, [NotNull] string name, int lineNumber, int linePosition)
        {
            if(string.IsNullOrEmpty(value))
                return new SchemaAutomatonError.SchemaAutomatonError16(nodeType, name, lineNumber, linePosition);
            if(value != "1" && value != "0" && value != "true" && value != "false")
                return new SchemaAutomatonError.SchemaAutomatonError5(nodeType, name, value, "0 или 1", lineNumber, linePosition);
            return null;
        }
    }
}