using System.Collections.Generic;

namespace GroboXsd
{
    public static class Helpers
    {
        public static bool StringsEqual(string first, string second)
        {
            return (first ?? "") == (second ?? "");
        }

        public static bool StringsArraysEqual(string[] first, string[] second)
        {
            first = first ?? new string[0];
            second = second ?? new string[0];
            if(first.Length != second.Length)
                return false;
            for(var i = 0; i < first.Length; ++i)
            {
                if(!StringsEqual(first[i], second[i]))
                    return false;
            }
            return true;
        }

        public static int Horner(IEnumerable<int> coeffs, int x)
        {
            var result = 0;
            foreach(var coef in coeffs)
            {
                unchecked
                {
                    result = result * x + coef;
                }
            }
            return result;
        }
    }
}