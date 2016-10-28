using System.Text.RegularExpressions;

using GroboXsd.Errors;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public class Base64BinarySimpleTypeExecutor : SchemaSimpleTypeExecutorBase
    {
        public Base64BinarySimpleTypeExecutor()
            : base(new StringSimpleTypeExecutor())
        {
            regex = SchemaRegexParser.Parse(@"((([A-Za-z0-9+/] ?){4})*(([A-Za-z0-9+/] ?){3}[A-Za-z0-9+/]|([A-Za-z0-9+/] ?){2}[AEIMQUYcgkosw048] ?=|[A-Za-z0-9+/] ?[AQgw] ?= ?=))?");
        }

        protected override SchemaAutomatonError ExecuteInternal([CanBeNull] string value, [NotNull] string nodeType, [NotNull] string name, int lineNumber, int linePosition)
        {
            if(string.IsNullOrEmpty(value))
                return new SchemaAutomatonError.SchemaAutomatonError16(nodeType, name, lineNumber, linePosition);
            if(!regex.IsMatch(value))
                return new SchemaAutomatonError.SchemaAutomatonError5(nodeType, name, value, "данными в формате base64", lineNumber, linePosition);
            return null;
        }

        private readonly Regex regex;
    }
}