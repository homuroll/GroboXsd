using System.Text.RegularExpressions;

using GroboXsd.Errors;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public class GMonthSimpleTypeExecutor : SchemaSimpleTypeExecutorBase
    {
        public GMonthSimpleTypeExecutor()
            : base(new StringSimpleTypeExecutor())
        {
            regex = SchemaRegexParser.Parse(@"--(0[1-9]|1[0-2])(Z|(\+|-)((0[0-9]|1[0-3]):[0-5][0-9]|14:00))?");
        }

        protected override SchemaAutomatonError ExecuteInternal([CanBeNull] string value, [NotNull] string nodeType, [NotNull] string name, int lineNumber, int linePosition)
        {
            if(string.IsNullOrEmpty(value))
                return new SchemaAutomatonError.SchemaAutomatonError16(nodeType, name, lineNumber, linePosition);
            if(!regex.IsMatch(value))
                return new SchemaAutomatonError.SchemaAutomatonError5(nodeType, name, value, "номером месяца", lineNumber, linePosition);
            return null;
        }

        private readonly Regex regex;
    }
}