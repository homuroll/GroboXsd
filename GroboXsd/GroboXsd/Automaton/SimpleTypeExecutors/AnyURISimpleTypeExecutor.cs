using System;

using GroboXsd.Errors;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public class AnyURISimpleTypeExecutor : SchemaSimpleTypeExecutorBase
    {
        public AnyURISimpleTypeExecutor()
            : base(new StringSimpleTypeExecutor())
        {
        }

        [CanBeNull]
        protected override SchemaAutomatonError ExecuteInternal([CanBeNull] string value, [NotNull] string nodeType, [NotNull] string name, int lineNumber, int linePosition)
        {
            if(string.IsNullOrEmpty(value))
                return new SchemaAutomatonError.SchemaAutomatonError16(nodeType, name, lineNumber, linePosition);
            Uri temp;
            if(!Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out temp))
                return new SchemaAutomatonError.SchemaAutomatonError5(nodeType, name, value, "ссылкой", lineNumber, linePosition);
            return null;
        }
    }
}