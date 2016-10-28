using System.Globalization;

using GroboXsd.Errors;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public class IntSimpleTypeExecutor : SchemaSimpleTypeExecutorBase
    {
        public IntSimpleTypeExecutor()
            : base(new IntegerSimpleTypeExecutor())
        {
        }

        [CanBeNull]
        protected override SchemaAutomatonError ExecuteInternal([CanBeNull] string value, [NotNull] string nodeType, [NotNull] string name, int lineNumber, int linePosition)
        {
            if(string.IsNullOrWhiteSpace(value))
                return new SchemaAutomatonError.SchemaAutomatonError16(nodeType, name, lineNumber, linePosition);
            int temp;
            if(!int.TryParse(value, NumberStyles.AllowLeadingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out temp))
                return new SchemaAutomatonError.SchemaAutomatonError5(nodeType, name, value, "целым 32-битным числом", lineNumber, linePosition);
            return null;
        }
    }
}