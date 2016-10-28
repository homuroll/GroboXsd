using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public static class SchemaRegexParser
    {
        public static Regex Parse([NotNull] string pattern)
        {
            var regex = (Regex)regexes[pattern];
            if(regex == null)
            {
                lock(lockObject)
                {
                    regex = (Regex)regexes[pattern];
                    if(regex == null)
                        regexes[pattern] = regex = new Regex(Preprocess(pattern), RegexOptions.Compiled);
                }
            }
            return regex;
        }

        private static string Preprocess(string pattern)
        {
            var bufBld = new StringBuilder();
            bufBld.Append("^(");

            var source = pattern.ToCharArray();
            var length = pattern.Length;
            var copyPosition = 0;
            for(var position = 0; position < length - 2; position++)
            {
                if(source[position] == '\\')
                {
                    if(source[position + 1] == '\\')
                        position++; // skip it 
                    else
                    {
                        var ch = source[position + 1];
                        for(var i = 0; i < c_map.Length; i++)
                        {
                            if(c_map[i].match == ch)
                            {
                                if(copyPosition < position)
                                    bufBld.Append(source, copyPosition, position - copyPosition);
                                bufBld.Append(c_map[i].replacement);
                                position++;
                                copyPosition = position + 1;
                                break;
                            }
                        }
                    }
                }
            }
            if(copyPosition < length)
                bufBld.Append(source, copyPosition, length - copyPosition);

            bufBld.Append(")$");
            return bufBld.ToString();
        }

        private struct Map
        {
            internal Map(char m, string r)
            {
                match = m;
                replacement = r;
            }

            internal readonly char match;
            internal readonly string replacement;
        };

        private static readonly Hashtable regexes = new Hashtable();
        private static readonly object lockObject = new object();

        private static readonly Map[] c_map =
            {
                new Map('c', "\\p{_xmlC}"),
                new Map('C', "\\P{_xmlC}"),
                new Map('d', "\\p{_xmlD}"),
                new Map('D', "\\P{_xmlD}"),
                new Map('i', "\\p{_xmlI}"),
                new Map('I', "\\P{_xmlI}"),
                new Map('w', "\\p{_xmlW}"),
                new Map('W', "\\P{_xmlW}"),
            };
    }
}