using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Xml;

using GrEmit;

using GroboXsd.Errors;
using GroboXsd.Parser;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public class SchemaSimpleTypeExecutor : SchemaSimpleTypeExecutorBase
    {
        public SchemaSimpleTypeExecutor([CanBeNull] ISchemaSimpleTypeExecutor baseExecutor, [NotNull] SchemaSimpleType schemaSimpleType)
            : base(baseExecutor)
        {
            this.schemaSimpleType = schemaSimpleType;
            var restriction = schemaSimpleType.Restriction;
            if(restriction != null)
            {
                length = restriction.Length;
                minLength = restriction.MinLength;
                maxLength = restriction.MaxLength;
                var atomicBaseType = schemaSimpleType.AtomicBaseType;
                if(ReferenceEquals(atomicBaseType, SchemaSimpleType.Integer)
                   || ReferenceEquals(atomicBaseType, SchemaSimpleType.Int)
                   || ReferenceEquals(atomicBaseType, SchemaSimpleType.Decimal))
                {
                    minInclusiveDecimal = ParseDecimal(restriction.MinInclusive);
                    minExclusiveDecimal = ParseDecimal(restriction.MinExclusive);
                    maxInclusiveDecimal = ParseDecimal(restriction.MaxInclusive);
                    maxExclusiveDecimal = ParseDecimal(restriction.MaxExclusive);
                    isNumber = true;
                }
                if(ReferenceEquals(atomicBaseType, SchemaSimpleType.Date)
                   || ReferenceEquals(atomicBaseType, SchemaSimpleType.GYear)
                   || ReferenceEquals(atomicBaseType, SchemaSimpleType.GMonth))
                {
                    if(ReferenceEquals(atomicBaseType, SchemaSimpleType.Date))
                        typeCode = DateTimeTypeCode.Date;
                    else if(ReferenceEquals(atomicBaseType, SchemaSimpleType.GYear))
                        typeCode = DateTimeTypeCode.GYear;
                    else if(ReferenceEquals(atomicBaseType, SchemaSimpleType.GMonth))
                        typeCode = DateTimeTypeCode.GMonth;
                    else
                        throw new InvalidOperationException();
                    minInclusiveDate = ParseDate(restriction.MinInclusive, typeCode);
                    minExclusiveDate = ParseDate(restriction.MinExclusive, typeCode);
                    maxInclusiveDate = ParseDate(restriction.MaxInclusive, typeCode);
                    maxExclusiveDate = ParseDate(restriction.MaxExclusive, typeCode);
                }
                totalDigits = restriction.TotalDigits;
                fractionDigits = restriction.FractionDigits;
                if(fractionDigits != null && totalDigits == null)
                    throw new InvalidOperationException("The 'totalDigits' facet must be specified along with the 'fractionDigits' facet");
                regexes = (restriction.Patterns ?? new string[0]).Select(SchemaRegexParser.Parse).ToArray();
                if(restriction.Patterns != null && restriction.Patterns.Length > 0)
                {
                    if(!string.IsNullOrEmpty(restriction.PatternDescription))
                        patternDescription = restriction.PatternDescription;
                    else if(schemaSimpleType.Description != null && schemaSimpleType.Description.Length > 0)
                        patternDescription = string.Join("; ", schemaSimpleType.Description);
                    else
                        patternDescription = GetPatternsDescription(restriction.Patterns);
                }
                if(!isNumber)
                {
                    if(typeCode == DateTimeTypeCode.None)
                    {
                        stringValues = restriction.Values == null || restriction.Values.Length == 0
                                           ? null
                                           : new HashSet<string>(restriction.Values);
                    }
                    else
                    {
                        xsdDateTimeValues = restriction.Values == null || restriction.Values.Length == 0
                                                ? null
                                                : restriction.Values.Select(s => string.IsNullOrEmpty(s) ? null : XsdDateTimeWrapper.Parse(s, typeCode)).ToList();
                    }
                }
                else
                {
                    decimalValues = restriction.Values == null || restriction.Values.Length == 0
                                        ? null
                                        : restriction.Values.Select(s => decimal.Parse(s, decimalStyle, CultureInfo.InvariantCulture)).ToList();
                }
                switch(restriction.WhiteSpace)
                {
                case SchemaSimpleTypeRestriction.WhiteSpaceEnum.Replace:
                    prepareValue = CDataNormalize;
                    break;
                case SchemaSimpleTypeRestriction.WhiteSpaceEnum.Collapse:
                    prepareValue = NonCDataNormalize;
                    break;
                }
            }
        }

        private const NumberStyles decimalStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowTrailingWhite;

        private static Func<string, string> BuildCDataNormalize()
        {
            var method = new DynamicMethod("CDataNormalize_" + Guid.NewGuid(), typeof(string), new[] {typeof(string)}, typeof(string), true);
            var xmlComplianceUtilType = typeof(XmlReader).Assembly.GetTypes().FirstOrDefault(type => type.Name == "XmlComplianceUtil");
            if(xmlComplianceUtilType == null)
                throw new InvalidOperationException("The type 'XmlComplianceUtil' is not found");
            var cDataNormalizeMethod = xmlComplianceUtilType.GetMethod("CDataNormalize", BindingFlags.Public | BindingFlags.Static);
            if(cDataNormalizeMethod == null)
                throw new InvalidOperationException("The method 'XmlComplianceUtil.CDataNormalize' is not found");
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Call(cDataNormalizeMethod);
                il.Ret();
            }
            return (Func<string, string>)method.CreateDelegate(typeof(Func<string, string>));
        }

        private static Func<string, string> BuildNonCDataNormalize()
        {
            var method = new DynamicMethod("CDataNormalize_" + Guid.NewGuid(), typeof(string), new[] {typeof(string)}, typeof(string), true);
            var xmlComplianceUtilType = typeof(XmlReader).Assembly.GetTypes().FirstOrDefault(type => type.Name == "XmlComplianceUtil");
            if(xmlComplianceUtilType == null)
                throw new InvalidOperationException("The type 'XmlComplianceUtil' is not found");
            var nonCDataNormalizeMethod = xmlComplianceUtilType.GetMethod("NonCDataNormalize", BindingFlags.Public | BindingFlags.Static);
            if(nonCDataNormalizeMethod == null)
                throw new InvalidOperationException("The method 'XmlComplianceUtil.NonCDataNormalize' is not found");
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Call(nonCDataNormalizeMethod);
                il.Ret();
            }
            return (Func<string, string>)method.CreateDelegate(typeof(Func<string, string>));
        }

        [NotNull]
        private static string GetPatternsDescription([NotNull] string[] patterns)
        {
            if(patterns.Length == 1)
            {
                var pattern = patterns[0];
                if(pattern == @"^(19|2\d)\d{2}$")
                    return "Значение должно быть в диапазоне от 1900 до 2999";
                var match = regex0.Match(pattern);
                if(match.Success)
                    return string.Format("Значение должно быть набором цифр длины не более {0} знаков", match.Groups["maxLength"]);
                match = regex1.Match(pattern);
                if(match.Success)
                {
                    var values = "";
                    var delimeter = " или ";
                    foreach(var capture in match.Groups["length"].Captures)
                        values = values + capture + delimeter;
                    if(values.Substring(values.Length - delimeter.Length) == delimeter)
                        values = values.Substring(0, values.Length - delimeter.Length);
                    return string.Format("Значение должно быть набором цифр длины ровно {0} знаков", values);
                }
            }
            else
            {
                var values = "";
                const string delimeter = " или ";
                var ok = true;
                foreach(var pattern in patterns)
                {
                    var match = regex1.Match(pattern);
                    if(match.Success)
                    {
                        foreach(var capture in match.Groups["length"].Captures)
                            values = values + capture + delimeter;
                    }
                    else
                    {
                        ok = false;
                        break;
                    }
                }
                if(ok)
                {
                    if(values.Substring(values.Length - delimeter.Length) == delimeter)
                        values = values.Substring(0, values.Length - delimeter.Length);
                    return string.Format("Значение должно быть набором цифр длины ровно {0} знаков", values);
                }
            }
            return string.Format("Значение должно соответствовать хотя бы одному шаблону из списка: '{0}'",
                                 string.Join(" или ", patterns.Select(pattern => "'" + pattern + "'")));
        }

        private static decimal? ParseDecimal(string value)
        {
            if(string.IsNullOrEmpty(value))
                return null;
            int totalDigits, fractionDigits;
            if(!DecimalSimpleTypeExecutor.Check(value, out totalDigits, out fractionDigits))
                throw new InvalidOperationException(string.Format("Unable to parse decimal from '{0}'", value));
            decimal result;
            if(!decimal.TryParse(value, decimalStyle, CultureInfo.InvariantCulture, out result))
                throw new InvalidOperationException(string.Format("Integer value '{0}' is too large for a decimal", value));
            return result;
        }

        private static XsdDateTimeWrapper ParseDate(string value, DateTimeTypeCode typeCode)
        {
            if(string.IsNullOrEmpty(value))
                return null;
            XsdDateTimeWrapper result;
            if(!XsdDateTimeWrapper.TryParse(value, typeCode, out result))
                throw new InvalidOperationException(string.Format("Unable to parse XsdDateTime from '{0}'", value));
            return result;
        }

        [CanBeNull]
        protected override SchemaAutomatonError ExecuteInternal([CanBeNull] string value, [NotNull] string nodeType, [NotNull] string name, int lineNumber, int linePosition)
        {
            string preparedValue = value;
            if(prepareValue != null && value != null)
                preparedValue = prepareValue(value);
            preparedValue = preparedValue ?? "";
            if(regexes != null && regexes.Length > 0)
            {
                if(!regexes.Any(t => t.IsMatch(preparedValue)))
                    return new SchemaAutomatonError.SchemaAutomatonError9(nodeType, name, value, "pattern", patternDescription, lineNumber, linePosition);
            }
            if(typeCode == DateTimeTypeCode.None)
            {
                if(isNumber)
                {
                    if(string.IsNullOrEmpty(preparedValue))
                        throw new InvalidOperationException();
                    var parsed = false;
                    decimal parsedDecimal = 0;
                    // Number
                    if(maxInclusiveDecimal != null)
                    {
                        parsedDecimal = decimal.Parse(preparedValue, decimalStyle, CultureInfo.InvariantCulture);
                        parsed = true;
                        if(parsedDecimal.CompareTo(maxInclusiveDecimal.Value) > 0)
                            return new SchemaAutomatonError.SchemaAutomatonError9(nodeType, name, value, "maxInclusive", schemaSimpleType.Restriction.MaxInclusive, lineNumber, linePosition);
                    }
                    if(maxExclusiveDecimal != null)
                    {
                        if(!parsed)
                        {
                            parsedDecimal = decimal.Parse(preparedValue, decimalStyle, CultureInfo.InvariantCulture);
                            parsed = true;
                        }
                        if(parsedDecimal.CompareTo(maxExclusiveDecimal.Value) >= 0)
                            return new SchemaAutomatonError.SchemaAutomatonError9(nodeType, name, value, "maxExclusive", schemaSimpleType.Restriction.MaxExclusive, lineNumber, linePosition);
                    }
                    if(minInclusiveDecimal != null)
                    {
                        if(!parsed)
                        {
                            parsedDecimal = decimal.Parse(preparedValue, decimalStyle, CultureInfo.InvariantCulture);
                            parsed = true;
                        }
                        if(parsedDecimal.CompareTo(minInclusiveDecimal.Value) < 0)
                            return new SchemaAutomatonError.SchemaAutomatonError9(nodeType, name, value, "minInclusive", schemaSimpleType.Restriction.MinInclusive, lineNumber, linePosition);
                    }
                    if(minExclusiveDecimal != null)
                    {
                        if(!parsed)
                        {
                            parsedDecimal = decimal.Parse(preparedValue, decimalStyle, CultureInfo.InvariantCulture);
                            parsed = true;
                        }
                        if(parsedDecimal.CompareTo(minExclusiveDecimal.Value) <= 0)
                            return new SchemaAutomatonError.SchemaAutomatonError9(nodeType, name, value, "minExclusive", schemaSimpleType.Restriction.MinExclusive, lineNumber, linePosition);
                    }
                    if(totalDigits != null)
                    {
                        int actualTotalDigits, actualFractionDigits;
                        DecimalSimpleTypeExecutor.Check(preparedValue, out actualTotalDigits, out actualFractionDigits);
                        if(actualTotalDigits > totalDigits.Value)
                            return new SchemaAutomatonError.SchemaAutomatonError9(nodeType, name, value, "totalDigits", totalDigits.Value, lineNumber, linePosition);
                        if(fractionDigits != null && actualFractionDigits > fractionDigits.Value)
                            return new SchemaAutomatonError.SchemaAutomatonError9(nodeType, name, value, "fractionDigits", fractionDigits.Value, lineNumber, linePosition);
                    }
                    if(decimalValues != null && decimalValues.Count > 0)
                    {
                        if(!parsed)
                        {
                            parsedDecimal = decimal.Parse(preparedValue, decimalStyle, CultureInfo.InvariantCulture);
                            parsed = true;
                        }
                        if(!decimalValues.Contains(parsedDecimal))
                            return new SchemaAutomatonError.SchemaAutomatonError9(nodeType, name, value, "enumeration", schemaSimpleType.Restriction.Values, lineNumber, linePosition);
                    }
                }
            }
            else
            {
                if(string.IsNullOrEmpty(preparedValue))
                    throw new InvalidOperationException();
                // Date
                XsdDateTimeWrapper parsed = null;
                if(maxInclusiveDate != null)
                {
                    if(!XsdDateTimeWrapper.TryParse(preparedValue, typeCode, out parsed))
                        throw new InvalidOperationException();
                    if(parsed.CompareTo(maxInclusiveDate) > 0)
                        return new SchemaAutomatonError.SchemaAutomatonError9(nodeType, name, value, "maxInclusive", schemaSimpleType.Restriction.MaxInclusive, lineNumber, linePosition);
                }
                if(maxExclusiveDate != null)
                {
                    if(parsed == null)
                    {
                        if(!XsdDateTimeWrapper.TryParse(preparedValue, typeCode, out parsed))
                            throw new InvalidOperationException();
                    }
                    if(parsed.CompareTo(maxExclusiveDate) >= 0)
                        return new SchemaAutomatonError.SchemaAutomatonError9(nodeType, name, value, "maxExclusive", schemaSimpleType.Restriction.MaxExclusive, lineNumber, linePosition);
                }
                if(minInclusiveDate != null)
                {
                    if(parsed == null)
                    {
                        if(!XsdDateTimeWrapper.TryParse(preparedValue, typeCode, out parsed))
                            throw new InvalidOperationException();
                    }
                    if(parsed.CompareTo(minInclusiveDate) < 0)
                        return new SchemaAutomatonError.SchemaAutomatonError9(nodeType, name, value, "minInclusive", schemaSimpleType.Restriction.MinInclusive, lineNumber, linePosition);
                }
                if(minExclusiveDate != null)
                {
                    if(parsed == null)
                    {
                        if(!XsdDateTimeWrapper.TryParse(preparedValue, typeCode, out parsed))
                            throw new InvalidOperationException();
                    }
                    if(parsed.CompareTo(minExclusiveDate) <= 0)
                        return new SchemaAutomatonError.SchemaAutomatonError9(nodeType, name, value, "minExclusive", schemaSimpleType.Restriction.MinExclusive, lineNumber, linePosition);
                }
                if(xsdDateTimeValues != null)
                {
                    if(parsed == null)
                    {
                        if(!XsdDateTimeWrapper.TryParse(preparedValue, typeCode, out parsed))
                            throw new InvalidOperationException();
                    }
                    if(!xsdDateTimeValues.Any(z => z != null && z.CompareTo(parsed) == 0))
                        return new SchemaAutomatonError.SchemaAutomatonError9(nodeType, name, value, "enumeration", schemaSimpleType.Restriction.Values, lineNumber, linePosition);
                }
            }
            if(typeCode == DateTimeTypeCode.None && !isNumber)
            {
                if(length != null)
                {
                    if(string.IsNullOrEmpty(preparedValue))
                        return new SchemaAutomatonError.SchemaAutomatonError16(nodeType, name, lineNumber, linePosition);
                    if(preparedValue.Length != length.Value)
                        return new SchemaAutomatonError.SchemaAutomatonError6(nodeType, name, value, length.Value, lineNumber, linePosition);
                }
                if(maxLength != null)
                {
                    if(preparedValue.Length > maxLength.Value)
                        return new SchemaAutomatonError.SchemaAutomatonError7(nodeType, name, value, maxLength.Value, lineNumber, linePosition);
                }
                if(minLength != null)
                {
                    if(minLength.Value > 0 && string.IsNullOrEmpty(preparedValue))
                        return new SchemaAutomatonError.SchemaAutomatonError16(nodeType, name, lineNumber, linePosition);
                    if(!string.IsNullOrEmpty(preparedValue) && preparedValue.Length < minLength.Value)
                        return new SchemaAutomatonError.SchemaAutomatonError8(nodeType, name, value, minLength.Value, lineNumber, linePosition);
                }
                if(stringValues != null && stringValues.Count > 0 && !stringValues.Contains(preparedValue))
                    return new SchemaAutomatonError.SchemaAutomatonError9(nodeType, name, value, "enumeration", schemaSimpleType.Restriction.Values, lineNumber, linePosition);
            }
            return null;
        }

        internal static decimal Power(int x, int y)
        {
            //Returns X raised to the power Y
            var returnValue = 1m;
            var decimalValue = (decimal)x;
            if(y > 28)
            {
                //CLR decimal cannot handle more than 29 digits (10 power 28.)
                return decimal.MaxValue;
            }
            for(var i = 0; i < y; i++)
                returnValue = returnValue * decimalValue;
            return returnValue;
        }

        public static readonly Func<string, string> CDataNormalize = BuildCDataNormalize();

        public static readonly Func<string, string> NonCDataNormalize = BuildNonCDataNormalize();
        private static readonly Regex regex0 = new Regex(@"^\[0\-9\]\{\d+\,(?<maxLength>\d+)\}$", RegexOptions.Compiled);
        private static readonly Regex regex1 = new Regex(@"^\[0\-9\]\{(?<length>\d+)\}(\|\[0\-9\]\{(?<length>\d+)\})*$", RegexOptions.Compiled);
        private readonly List<decimal> decimalValues;
        private readonly List<XsdDateTimeWrapper> xsdDateTimeValues;
        private readonly int? fractionDigits;
        private readonly bool isNumber;
        private readonly int? length;
        private readonly XsdDateTimeWrapper maxExclusiveDate;
        private readonly decimal? maxExclusiveDecimal;
        private readonly XsdDateTimeWrapper maxInclusiveDate;
        private readonly decimal? maxInclusiveDecimal;
        private readonly int? maxLength;
        private readonly XsdDateTimeWrapper minExclusiveDate;
        private readonly decimal? minExclusiveDecimal;
        private readonly XsdDateTimeWrapper minInclusiveDate;
        private readonly decimal? minInclusiveDecimal;
        private readonly int? minLength;
        private readonly string patternDescription;
        private readonly Func<string, string> prepareValue;
        private readonly Regex[] regexes;
        private readonly SchemaSimpleType schemaSimpleType;
        private readonly HashSet<string> stringValues;
        private readonly int? totalDigits;
        private readonly DateTimeTypeCode typeCode;
    }
}