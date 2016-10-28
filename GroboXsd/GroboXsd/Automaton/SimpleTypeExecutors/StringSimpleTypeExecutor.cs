using GroboXsd.Errors;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public class StringSimpleTypeExecutor : SchemaSimpleTypeExecutorBase
    {
        public StringSimpleTypeExecutor()
            : base(null)
        {
        }

        [CanBeNull]
        protected override SchemaAutomatonError ExecuteInternal([CanBeNull] string value, [NotNull] string nodeType, [NotNull] string name, int lineNumber, int linePosition)
        {
            return null;
        }
    }
}