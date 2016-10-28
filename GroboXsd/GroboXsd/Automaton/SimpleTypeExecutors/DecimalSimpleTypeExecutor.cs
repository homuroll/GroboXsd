using GroboXsd.Errors;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public class DecimalSimpleTypeExecutor : SchemaSimpleTypeExecutorBase
    {
        public DecimalSimpleTypeExecutor()
            : base(new StringSimpleTypeExecutor())
        {
        }

        [CanBeNull]
        protected override SchemaAutomatonError ExecuteInternal([CanBeNull] string value, [NotNull] string nodeType, [NotNull] string name, int lineNumber, int linePosition)
        {
            if(string.IsNullOrWhiteSpace(value))
                return new SchemaAutomatonError.SchemaAutomatonError16(nodeType, name, lineNumber, linePosition);
            int totalDigits, fractionDigits;
            if(!Check(value, out totalDigits, out fractionDigits))
                return new SchemaAutomatonError.SchemaAutomatonError5(nodeType, name, value, "числом с дробной частью", lineNumber, linePosition);
            return null;
        }

        public static unsafe bool Check([NotNull] string value, out int totalDigits, out int fractionDigits)
        {
            totalDigits = 0;
            fractionDigits = 0;
            fixed(char* c = value)
            {
                var end = c + value.Length;
                var p = c;
                while(p < end && char.IsWhiteSpace(*p))
                    ++p;
                if(p >= end)
                    return true;
                if(*p == '-' || *p == '+')
                    ++p;
                if(p >= end || (!char.IsDigit(*p) && *p != '.'))
                    return false;
                var hasDigit = char.IsDigit(*p);
                while(p < end && *p == '0')
                    ++p;
                while(p < end && char.IsDigit(*p))
                {
                    ++p;
                    ++totalDigits;
                }
                if(p >= end)
                    return hasDigit;
                if(*p == '.')
                {
                    ++p;
                    if(p >= end)
                        return hasDigit;
                    end = c + value.Length - 1;
                    while(char.IsWhiteSpace(*end))
                        --end;
                    while(*end == '0')
                        --end;
                    ++end;
                    if(!char.IsDigit(*p))
                        return p >= end && hasDigit;
                    while(p < end && char.IsDigit(*p))
                    {
                        ++p;
                        ++totalDigits;
                        ++fractionDigits;
                    }
                    return p >= end;
                }
                while(p < end && char.IsWhiteSpace(*p))
                    ++p;
                return p >= end;
            }
        }
    }
}