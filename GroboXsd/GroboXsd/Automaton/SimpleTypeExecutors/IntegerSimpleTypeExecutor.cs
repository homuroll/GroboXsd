using GroboXsd.Errors;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public class IntegerSimpleTypeExecutor : SchemaSimpleTypeExecutorBase
    {
        public IntegerSimpleTypeExecutor()
            : base(new StringSimpleTypeExecutor())
        {
        }

        [CanBeNull]
        protected override SchemaAutomatonError ExecuteInternal([CanBeNull] string value, [NotNull] string nodeType, [NotNull] string name, int lineNumber, int linePosition)
        {
            if(string.IsNullOrWhiteSpace(value))
                return new SchemaAutomatonError.SchemaAutomatonError16(nodeType, name, lineNumber, linePosition);
            if(!Check(value))
                return new SchemaAutomatonError.SchemaAutomatonError5(nodeType, name, value, "целым числом", lineNumber, linePosition);
            return null;
        }

        public static unsafe bool Check([NotNull] string value)
        {
            fixed(char* c = value)
            {
                var p = c;
                var end = c + value.Length;
                while(p < end && char.IsWhiteSpace(*p))
                    ++p;
                if(p >= end)
                    return true;
                if(*p == '-' || *p == '+')
                    ++p;
                if(p >= end || !char.IsDigit(*p))
                    return false;
                while(p < end && char.IsDigit(*p))
                    ++p;
                while(p < end && char.IsWhiteSpace(*p))
                    ++p;
                return p >= end;
            }
        }
    }
}