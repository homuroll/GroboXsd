using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace GroboXsd.Parser
{
    public class SchemaSimpleTypeRestriction
    {
        public override bool Equals(object obj)
        {
            var other = obj as SchemaSimpleTypeRestriction;
            if(other == null)
                return false;
            if(Length != other.Length)
                return false;
            if(MinLength != other.MinLength)
                return false;
            if(MaxLength != other.MaxLength)
                return false;
            if(!Helpers.StringsEqual(MaxInclusive, other.MaxInclusive))
                return false;
            if(!Helpers.StringsEqual(MaxExclusive, other.MaxExclusive))
                return false;
            if(!Helpers.StringsEqual(MinInclusive, other.MinInclusive))
                return false;
            if(!Helpers.StringsEqual(MinExclusive, other.MinExclusive))
                return false;
            if(TotalDigits != other.TotalDigits)
                return false;
            if(FractionDigits != other.FractionDigits)
                return false;
            if(WhiteSpace != other.WhiteSpace)
                return false;
            if(!Helpers.StringsArraysEqual(Patterns, other.Patterns))
                return false;
            if(!Helpers.StringsArraysEqual(Values, other.Values))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            var coeffs = new List<int>
                {
                    Length == null ? -1 : Length.Value,
                    MinLength == null ? -1 : MinLength.Value,
                    MaxLength == null ? -1 : MaxLength.Value,
                    (MaxInclusive ?? "").GetHashCode(),
                    (MaxExclusive ?? "").GetHashCode(),
                    (MinInclusive ?? "").GetHashCode(),
                    (MinExclusive ?? "").GetHashCode(),
                    TotalDigits == null ? -1 : TotalDigits.Value,
                    FractionDigits == null ? -1 : FractionDigits.Value,
                    (int)WhiteSpace
                };
            coeffs.AddRange((Patterns ?? new string[0]).Select(s => s.GetHashCode()));
            coeffs.AddRange((Values ?? new string[0]).Select(s => s.GetHashCode()));
            return Helpers.Horner(coeffs, 1000000009);
        }

        public int? Length { get; set; }

        public int? MinLength { get; set; }

        public int? MaxLength { get; set; }

        [CanBeNull]
        public string[] Patterns { get; set; }

        [CanBeNull]
        public string PatternDescription { get; set; }

        [CanBeNull]
        public string[] Values { get; set; }

        [CanBeNull]
        public string MaxInclusive { get; set; }

        [CanBeNull]
        public string MaxExclusive { get; set; }

        [CanBeNull]
        public string MinInclusive { get; set; }

        [CanBeNull]
        public string MinExclusive { get; set; }

        [CanBeNull]
        public int? TotalDigits { get; set; }

        [CanBeNull]
        public int? FractionDigits { get; set; }

        public WhiteSpaceEnum WhiteSpace { get; set; }

        public enum WhiteSpaceEnum
        {
            Preserve,
            Replace,
            Collapse
        }
    }
}