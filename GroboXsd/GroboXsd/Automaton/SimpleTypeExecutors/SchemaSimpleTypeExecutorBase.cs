using GroboXsd.Errors;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public abstract class SchemaSimpleTypeExecutorBase : ISchemaSimpleTypeExecutor
    {
        protected SchemaSimpleTypeExecutorBase([CanBeNull] ISchemaSimpleTypeExecutor baseExecutor)
        {
            this.baseExecutor = baseExecutor;
        }

        [CanBeNull]
        public SchemaAutomatonError Execute([CanBeNull] string value, [NotNull] string nodeType, [NotNull] string name, int lineNumber, int linePosition)
        {
            if(baseExecutor != null)
            {
                var baseResult = baseExecutor.Execute(value, nodeType, name, lineNumber, linePosition);
                if(baseResult != null)
                    return baseResult;
            }
            return ExecuteInternal(value, nodeType, name, lineNumber, linePosition);
        }

        [CanBeNull]
        protected abstract SchemaAutomatonError ExecuteInternal([CanBeNull] string value, [NotNull] string nodeType, [NotNull] string name, int lineNumber, int linePosition);

        [CanBeNull]
        private readonly ISchemaSimpleTypeExecutor baseExecutor;
    }
}