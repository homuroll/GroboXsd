using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

using JetBrains.Annotations;

namespace GroboXsd.Parser
{
    public class SchemaParser : ISchemaParser
    {
        [NotNull]
        public SchemaTypeBase Parse([NotNull] XmlDocument schema)
        {
            var root = schema.DocumentElement;
            if(root == null)
                throw new InvalidOperationException("Schema root element is null");
            var schemaNamespaceAttr = root.Attributes.Cast<XmlAttribute>().SingleOrDefault(attr => attr.Name.StartsWith("xmlns:") && attr.Value == NamespaceManager.Schema);
            var prefix = schemaNamespaceAttr == null ? "" : schemaNamespaceAttr.LocalName + ":";
            return ParseSchema(root, new Context(prefix));
        }

        [NotNull]
        private static SchemaComplexType ParseSchema([NotNull] XmlElement element, [NotNull] Context context)
        {
            var children = new List<SchemaComplexTypeItem>();
            var attributes = new List<SchemaComplexTypeAttribute>();
            var childNodes = GetSchemaChildNodes(element);
            foreach(var child in childNodes)
            {
                switch(child.LocalName)
                {
                case "simpleType":
                case "complexType":
                    context.DeclareType(GetName(child), child);
                    break;
                case "element":
                    context.DeclareElement(GetName(child), child);
                    break;
                case "attributeGroup":
                    context.DeclareAttributeGroup(GetName(child), child);
                    break;
                }
            }
            foreach(var child in childNodes)
            {
                switch(child.LocalName)
                {
                case "attribute":
                    var attribute = ParseComplexTypeAttribute(child, context);
                    if(attribute != null)
                        attributes.Add(attribute);
                    break;
                case "attributeGroup":
                    attributes.AddRange(context.GetAttributeGroupDefinition(GetName(child)));
                    break;
                case "simpleType":
                case "complexType":
                    context.GetTypeDefinition(GetName(child));
                    break;
                case "element":
                    children.Add(context.GetElementDefinition(GetName(child)));
                    break;
                default:
                    children.Add(ParseComplexTypeItem(child, context));
                    break;
                }
            }
            return new SchemaComplexType(null, "schema", children, attributes, null);
        }

        [CanBeNull]
        private static string[] ParseAnnotation([NotNull] XmlElement element)
        {
            var annotation = GetSingleSchemaChildNode(element, "annotation");
            if(annotation == null)
                return null;
            var documentations = GetSchemaChildNodes(annotation).Where(child => child.LocalName == "documentation").ToArray();
            return documentations.Length == 0 ? null : documentations.Select(documentation => documentation.InnerText).ToArray();
        }

        [NotNull]
        private static SchemaTypeBase ParseComplexType([NotNull] XmlElement element, [NotNull] Context context, [NotNull] string name)
        {
            var mixed = element.GetAttribute("mixed");
            if(mixed == "true" || mixed == "1")
                throw new InvalidOperationException("Mixed content is not supported");
            var baseTypeName = element.GetAttribute("base");
            var baseType = string.IsNullOrEmpty(baseTypeName) ? null : context.GetTypeDefinition(baseTypeName);
            var children = new List<SchemaComplexTypeItem>();
            var attributes = new List<SchemaComplexTypeAttribute>();
            var childNodes = GetSchemaChildNodes(element);
            if(childNodes.Count == 1 && childNodes[0].LocalName == "simpleContent")
                return ParseSimpleContent(childNodes[0], context, name);
            if(childNodes.Count == 1 && childNodes[0].LocalName == "complexContent")
                return ParseComplexContent(childNodes[0], context, name);
            foreach(var child in childNodes)
            {
                switch(child.LocalName)
                {
                case "attribute":
                    var attribute = ParseComplexTypeAttribute(child, context);
                    if(attribute != null)
                        attributes.Add(attribute);
                    break;
                case "attributeGroup":
                    attributes.AddRange(context.GetAttributeGroupDefinition(GetReference(child)));
                    break;
                default:
                    children.Add(ParseComplexTypeItem(child, context));
                    break;
                }
            }
            return new SchemaComplexType(baseType, name, children, attributes, ParseAnnotation(element));
        }

        [NotNull]
        private static SchemaTypeBase ParseSimpleContent([NotNull] XmlElement element, [NotNull] Context context, [NotNull] string name)
        {
            var child = GetSingleSchemaChildNode(element);
            switch(child.LocalName)
            {
            case "restriction":
                return ParseRestriction(child, context, name, ParseAnnotation(element));
            case "extension":
                return ParseExtension(child, context, name);
            default:
                throw new InvalidOperationException(string.Format("Unexpected content of 'simpleContent' element: '{0}'; expected 'extension' or 'restriction'", child.LocalName));
            }
        }

        [NotNull]
        private static SchemaTypeBase ParseComplexContent([NotNull] XmlElement element, [NotNull] Context context, [NotNull] string name)
        {
            var mixed = element.GetAttribute("mixed");
            if(mixed == "true" || mixed == "1")
                throw new InvalidOperationException("Mixed content is not supported");
            var child = GetSingleSchemaChildNode(element);
            switch(child.LocalName)
            {
            case "extension":
                return ParseComplexType(child, context, name);
            default:
                throw new InvalidOperationException(string.Format("Unexpected content of 'complexContent' element: '{0}'; expected 'extension'", child.LocalName));
            }
        }

        [NotNull]
        private static SchemaComplexType ParseExtension([NotNull] XmlElement element, [NotNull] Context context, [NotNull] string name)
        {
            var baseTypeName = element.GetAttribute("base");
            var baseType = string.IsNullOrEmpty(baseTypeName) ? null : context.GetTypeDefinition(baseTypeName);
            var attributes = ParseAttributeGroup(element, context);
            return new SchemaComplexType(baseType, name, new List<SchemaComplexTypeItem>(), attributes, ParseAnnotation(element));
        }

        [NotNull]
        private static List<SchemaComplexTypeAttribute> ParseAttributeGroup([NotNull] XmlElement element, [NotNull] Context context)
        {
            var childNodes = GetSchemaChildNodes(element);
            var attributes = new List<SchemaComplexTypeAttribute>();
            foreach(var child in childNodes)
            {
                switch(child.LocalName)
                {
                case "attribute":
                    var attribute = ParseComplexTypeAttribute(child, context);
                    if(attribute != null)
                        attributes.Add(attribute);
                    break;
                case "attributeGroup":
                    attributes.AddRange(context.GetAttributeGroupDefinition(GetReference(child)));
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Unexpected content: '{0}'; expected 'attribute' or 'attributeGroup'", child.LocalName));
                }
            }
            return attributes;
        }

        [NotNull]
        private static SchemaComplexTypeItem ParseComplexTypeItem([NotNull] XmlElement element, [NotNull] Context context)
        {
            switch(element.LocalName)
            {
            case "element":
                return ParseElementItem(element, context);
            case "sequence":
                return ParseSequenceItem(element, context);
            case "choice":
                return ParseChoiceItem(element, context);
            default:
                throw new NotSupportedException(string.Format("Schema complex type item '{0}' is not supported", element.LocalName));
            }
        }

        [NotNull]
        private static SchemaComplexTypeChoiceItem ParseChoiceItem([NotNull] XmlElement element, [NotNull] Context context)
        {
            var minOccurs = ParseMinOccursAttribute(element);
            var maxOccurs = ParseMaxOccursAttribute(element);
            return new SchemaComplexTypeChoiceItem(GetSchemaChildNodes(element).Select(x => ParseComplexTypeItem(x, context)).ToList(), minOccurs, maxOccurs);
        }

        [NotNull]
        private static SchemaComplexTypeSequenceItem ParseSequenceItem([NotNull] XmlElement element, [NotNull] Context context)
        {
            var minOccurs = ParseMinOccursAttribute(element);
            var maxOccurs = ParseMaxOccursAttribute(element);
            return new SchemaComplexTypeSequenceItem(GetSchemaChildNodes(element).Select(x => ParseComplexTypeItem(x, context)).ToList(), minOccurs, maxOccurs);
        }

        [NotNull]
        private static SchemaComplexTypeElementItem ParseElementItem([NotNull] XmlElement element, [NotNull] Context context)
        {
            var name = element.GetAttribute("name");
            SchemaTypeBase type;
            int minOccurs;
            int? maxOccurs;
            string fixedValue;
            if(!string.IsNullOrEmpty(name))
            {
                minOccurs = ParseMinOccursAttribute(element);
                maxOccurs = ParseMaxOccursAttribute(element);
                type = ParseType(element, context);
                fixedValue = element.GetAttribute("fixed");
            }
            else
            {
                var reference = element.GetAttribute("ref");
                if(string.IsNullOrEmpty(reference))
                    throw new InvalidOperationException("Either 'name' or 'ref' attribute must be specified");
                var parsedElement = context.GetElementDefinition(reference);
                name = parsedElement.Name;
                type = parsedElement.Type;
                minOccurs = element.Attributes["minOccurs"] == null
                                ? parsedElement.MinOccurs
                                : ParseMinOccursAttribute(element);
                maxOccurs = element.Attributes["maxOccurs"] == null
                                ? parsedElement.MaxOccurs
                                : ParseMaxOccursAttribute(element);
                fixedValue = element.Attributes["fixed"] == null
                                 ? parsedElement.FixedValue
                                 : element.GetAttribute("fixed");
            }
            var item = new SchemaComplexTypeElementItem(name, type, minOccurs, maxOccurs, fixedValue);
            return item;
        }

        [CanBeNull]
        private static SchemaComplexTypeAttribute ParseComplexTypeAttribute([NotNull] XmlElement element, [NotNull] Context context)
        {
            var name = GetName(element);
            var use = element.GetAttribute("use");
            var required = false;
            if(!string.IsNullOrEmpty(use))
            {
                switch(use)
                {
                case "prohibited":
                    return null; // No such attribute
                case "required":
                    required = true;
                    break;
                case "optional":
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Incorrect value for attribute usage: '{0}'", use));
                }
            }
            var type = ParseType(element, context);
            if(type is SchemaComplexType)
                throw new InvalidOperationException("An attribute cannot be of complex type");
            return new SchemaComplexTypeAttribute(name, (SchemaSimpleType)type, required, element.GetAttribute("fixed"));
        }

        [CanBeNull]
        private static SchemaTypeBase ParseType([NotNull] XmlElement element, [NotNull] Context context)
        {
            var typeName = element.GetAttribute("type");
            SchemaTypeBase type;
            if(!string.IsNullOrEmpty(typeName))
                type = context.GetTypeDefinition(typeName);
            else
            {
                var complexTypeElement = GetSingleSchemaChildNode(element, "complexType");
                var simpleTypeElement = GetSingleSchemaChildNode(element, "simpleType");
                if(complexTypeElement != null && simpleTypeElement != null)
                    throw new InvalidOperationException(string.Format("It is only allowed to define either a complex or a simple type not both. Element: '{0}'", element.LocalName));
                type = complexTypeElement != null
                           ? ParseComplexType(complexTypeElement, context, "anonymousType")
                           : (simpleTypeElement == null ? null : ParseSimpleType(simpleTypeElement, context, "anonymousType"));
            }
            return type;
        }

        [NotNull]
        private static SchemaSimpleType ParseSimpleType([NotNull] XmlElement element, [NotNull] Context context, [NotNull] string name)
        {
            var restrictionElement = GetSingleSchemaChildNode(element, "restriction");
            if(restrictionElement == null)
                throw new InvalidOperationException("'restriction' element required in order to declare a simple type");

            return ParseRestriction(restrictionElement, context, name, ParseAnnotation(element));
        }

        [NotNull]
        private static SchemaSimpleType ParseRestriction([NotNull] XmlElement element, [NotNull] Context context, [NotNull] string name,
                                                         [CanBeNull] string[] description)
        {
            var baseTypeName = element.GetAttribute("base");
            var baseType = string.IsNullOrEmpty(baseTypeName) ? null : context.GetTypeDefinition(baseTypeName);
            if(baseType == null)
            {
                var subType = GetSingleSchemaChildNode(element, "simpleType");
                if(subType != null)
                    baseType = ParseSimpleType(subType, context, "anonymousType");
            }
            var restriction = new SchemaSimpleTypeRestriction();
            var values = new HashSet<string>();
            var patterns = new List<string>();
            string patternDescription = null;
            foreach(var child in GetSchemaChildNodes(element))
            {
                var value = child.GetAttribute("value");
                switch(child.LocalName)
                {
                case "simpleType":
                    break;
                case "enumeration":
                    if(values.Contains(value))
                        throw new InvalidOperationException(string.Format("Duplicate enumeration value: '{0}'", value));
                    values.Add(value);
                    break;
                case "length":
                    if(restriction.Length != null)
                        throw new InvalidOperationException("Duplicate 'length' facet");
                    int length;
                    if(!int.TryParse(value, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out length))
                        throw new InvalidOperationException(string.Format("'length' attribute is an integer but was '{0}'", value));
                    restriction.Length = length;
                    break;
                case "minLength":
                    if(restriction.MinLength != null)
                        throw new InvalidOperationException("Duplicate 'minLength' facet");
                    int minLength;
                    if(!int.TryParse(value, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out minLength))
                        throw new InvalidOperationException(string.Format("'minLength' attribute is an integer but was '{0}'", value));
                    restriction.MinLength = minLength;
                    break;
                case "maxLength":
                    if(restriction.MaxLength != null)
                        throw new InvalidOperationException("Duplicate 'maxLength' facet");
                    int maxLength;
                    if(!int.TryParse(value, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out maxLength))
                        throw new InvalidOperationException(string.Format("'maxLength' attribute is an integer but was '{0}'", value));
                    restriction.MaxLength = maxLength;
                    break;
                case "pattern":
                    patterns.Add(value);
                    if(string.IsNullOrEmpty(patternDescription))
                        patternDescription = child.GetAttribute("descriptionError", NamespaceManager.DatabaseType);
                    break;
                case "minInclusive":
                    if(restriction.MinInclusive != null)
                        throw new InvalidOperationException("Duplicate 'minInclusive' facet");
                    restriction.MinInclusive = value;
                    break;
                case "minExclusive":
                    if(restriction.MinExclusive != null)
                        throw new InvalidOperationException("Duplicate 'minExclusive' facet");
                    restriction.MinExclusive = value;
                    break;
                case "maxInclusive":
                    if(restriction.MaxInclusive != null)
                        throw new InvalidOperationException("Duplicate 'maxInclusive' facet");
                    restriction.MaxInclusive = value;
                    break;
                case "maxExclusive":
                    if(restriction.MaxExclusive != null)
                        throw new InvalidOperationException("Duplicate 'maxExclusive' facet");
                    restriction.MaxExclusive = value;
                    break;
                case "totalDigits":
                    int totalDigits;
                    if(!int.TryParse(value, out totalDigits))
                        throw new InvalidOperationException(string.Format("'totalDigits' attribute is an integer but was '{0}'", value));
                    if(restriction.TotalDigits != null)
                        throw new InvalidOperationException("Duplicate 'totalDigits' facet");
                    restriction.TotalDigits = totalDigits;
                    break;
                case "fractionDigits":
                    int fractionalDigits;
                    if(!int.TryParse(value, out fractionalDigits))
                        throw new InvalidOperationException(string.Format("'fractionDigits' attribute is an integer but was '{0}'", value));
                    if(restriction.FractionDigits != null)
                        throw new InvalidOperationException("Duplicate 'fractionDigits' facet");
                    restriction.FractionDigits = fractionalDigits;
                    break;
                case "whiteSpace":
                    switch(value)
                    {
                    case "preserve":
                        restriction.WhiteSpace = SchemaSimpleTypeRestriction.WhiteSpaceEnum.Preserve;
                        break;
                    case "replace":
                        restriction.WhiteSpace = SchemaSimpleTypeRestriction.WhiteSpaceEnum.Replace;
                        break;
                    case "collapse":
                        restriction.WhiteSpace = SchemaSimpleTypeRestriction.WhiteSpaceEnum.Collapse;
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Invalid 'whiteSpace' facet value: '{0}'", value));
                    }
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Facet '{0}' is not supported", child.LocalName));
                }
            }
            if(values.Count > 0)
                restriction.Values = values.ToArray();
            if(patterns.Count > 0)
                restriction.Patterns = patterns.ToArray();
            restriction.PatternDescription = patternDescription;
            if(restriction.MinLength != null && restriction.MaxLength != null && restriction.MinLength > restriction.MaxLength)
                throw new InvalidOperationException("The value of 'minLength' facet cannot be greater than the value of 'maxLength' facet");
            if(restriction.Length != null && (restriction.MinLength != null || restriction.MaxLength != null))
                throw new InvalidOperationException("The 'length' facet cannot be filled along with the either of the 'minLength' or the 'maxLength' facets");
            return new SchemaSimpleType(baseType, name, restriction, description);
        }

        private static int? ParseOccursAttribute([NotNull] XmlElement element, [NotNull] string name)
        {
            var attr = element.Attributes[name];
            if(attr == null)
                return 1; // Default value is 1
            if(attr.Value == "unbounded")
                return null; // No boundaries
            int result;
            if(!int.TryParse(attr.Value, out result) || result < 0)
                throw new InvalidOperationException(string.Format("Attribute '{0}' must be a non-negative 32-bit integer", name));
            return result;
        }

        private static int? ParseMaxOccursAttribute([NotNull] XmlElement element)
        {
            return ParseOccursAttribute(element, "maxOccurs");
        }

        private static int ParseMinOccursAttribute([NotNull] XmlElement element)
        {
            var result = ParseOccursAttribute(element, "minOccurs");
            if(result == null)
                throw new InvalidOperationException("minOccurs attribute cannot be 'unbounded'");
            return result.Value;
        }

        [NotNull]
        private static string GetName([NotNull] XmlElement element)
        {
            var name = element.GetAttribute("name");
            if(string.IsNullOrEmpty(name))
                throw new InvalidOperationException(string.Format("Attribute 'name' must be specified for '{0}'", element.LocalName));
            return name;
        }

        [NotNull]
        private static string GetReference([NotNull] XmlElement element)
        {
            var name = element.GetAttribute("ref");
            if(string.IsNullOrEmpty(name))
                throw new InvalidOperationException(string.Format("Attribute 'ref' must be specified for '{0}'", element.LocalName));
            return name;
        }

        [CanBeNull]
        private static XmlElement GetSingleSchemaChildNode([NotNull] XmlElement element, [NotNull] string name)
        {
            var childNodes = GetSchemaChildNodes(element, ignoreAnnotation : false).Where(child => child.LocalName == name).ToList();
            switch(childNodes.Count)
            {
            case 0:
                return null;
            case 1:
                return childNodes[0];
            default:
                throw new InvalidOperationException(string.Format("Too many child nodes with name '{0}' found in element '{1}'", name, element.LocalName));
            }
        }

        [NotNull]
        private static XmlElement GetSingleSchemaChildNode([NotNull] XmlElement element)
        {
            var childNodes = GetSchemaChildNodes(element);
            switch(childNodes.Count)
            {
            case 1:
                return childNodes[0];
            case 0:
                throw new InvalidOperationException(string.Format("Element '{0}' contains no child nodes", element.LocalName));
            default:
                throw new InvalidOperationException(string.Format("Too many child nodes in element '{0}'", element.LocalName));
            }
        }

        [NotNull]
        private static List<XmlElement> GetSchemaChildNodes([NotNull] XmlElement element, bool ignoreAnnotation = true)
        {
            var result = element.ChildNodes.OfType<XmlElement>().Where(x => x.NamespaceURI == NamespaceManager.Schema);
            if(ignoreAnnotation)
                result = result.Where(child => child.LocalName != "annotation");
            return result.ToList();
        }

        private class Context
        {
            public Context([NotNull] string prefix)
            {
                libraryTypes = new Dictionary<string, SchemaTypeBase>
                    {
                        {prefix + "anyType", null},
                        {prefix + "string", SchemaSimpleType.String},
                        {prefix + "integer", SchemaSimpleType.Integer},
                        {prefix + "int", SchemaSimpleType.Int},
                        {prefix + "decimal", SchemaSimpleType.Decimal},
                        {prefix + "boolean", SchemaSimpleType.Boolean},
                        {prefix + "date", SchemaSimpleType.Date},
                        {prefix + "gMonth", SchemaSimpleType.GMonth},
                        {prefix + "gYear", SchemaSimpleType.GYear},
                        {prefix + "anyURI", SchemaSimpleType.AnyURI},
                        {prefix + "base64Binary", SchemaSimpleType.Base64Binary},
                    };
            }

            public void DeclareType([NotNull] string name, [NotNull] XmlElement element)
            {
                if(types.ContainsKey(name))
                    throw new InvalidOperationException(string.Format("Type '{0}' is already declared", name));
                types.Add(name, new TypeBeingParsed(element));
            }

            [NotNull]
            public SchemaTypeBase GetTypeDefinition([NotNull] string name)
            {
                SchemaTypeBase libraryType;
                if(libraryTypes.TryGetValue(name, out libraryType))
                    return libraryType;
                TypeBeingParsed type;
                if(!types.TryGetValue(name, out type))
                    throw new InvalidOperationException(string.Format("Type '{0}' is not declared", name));
                if(type.Parsed != null)
                    return type.Parsed;
                var xmlElement = type.XmlElement;
                switch(xmlElement.LocalName)
                {
                case "simpleType":
                    return type.Parsed = ParseSimpleType(xmlElement, this, name);
                case "complexType":
                    return type.Parsed = ParseComplexType(xmlElement, this, name);
                default:
                    throw new InvalidOperationException(string.Format("Unexpected element name: '{0}', expected 'simpleType' or 'complexType'", xmlElement.LocalName));
                }
            }

            public void DeclareElement([NotNull] string name, [NotNull] XmlElement element)
            {
                if(element.LocalName != "element")
                    throw new InvalidOperationException(string.Format("Unexpected element name: '{0}', expected 'element'", element.LocalName));
                if(elements.ContainsKey(name))
                    elements[name] = new ElementBeingParsed(element);
                elements.Add(name, new ElementBeingParsed(element));
            }

            [NotNull]
            public SchemaComplexTypeElementItem GetElementDefinition([NotNull] string name)
            {
                ElementBeingParsed element;
                if(!elements.TryGetValue(name, out element))
                    throw new InvalidOperationException(string.Format("Element '{0}' is not declared", name));
                if(element.Parsed != null)
                    return element.Parsed;
                var xmlElement = element.XmlElement;
                return element.Parsed = (SchemaComplexTypeElementItem)ParseComplexTypeItem(xmlElement, this);
            }

            public void DeclareAttributeGroup([NotNull] string name, [NotNull] XmlElement element)
            {
                if(attributeGroups.ContainsKey(name))
                    attributeGroups[name] = new AttributeGroupBeingParsed(element);
                attributeGroups.Add(name, new AttributeGroupBeingParsed(element));
            }

            [NotNull]
            public List<SchemaComplexTypeAttribute> GetAttributeGroupDefinition([NotNull] string name)
            {
                AttributeGroupBeingParsed attributeGroup;
                if(!attributeGroups.TryGetValue(name, out attributeGroup))
                    throw new InvalidOperationException(string.Format("Attribute group '{0}' is not declared", name));
                if(attributeGroup.Parsed != null)
                    return attributeGroup.Parsed;
                var xmlElement = attributeGroup.XmlElement;
                return attributeGroup.Parsed = ParseAttributeGroup(xmlElement, this);
            }

            [NotNull]
            private readonly Dictionary<string, AttributeGroupBeingParsed> attributeGroups = new Dictionary<string, AttributeGroupBeingParsed>();

            [NotNull]
            private readonly Dictionary<string, ElementBeingParsed> elements = new Dictionary<string, ElementBeingParsed>();

            [NotNull]
            private readonly Dictionary<string, SchemaTypeBase> libraryTypes;

            [NotNull]
            private readonly Dictionary<string, TypeBeingParsed> types = new Dictionary<string, TypeBeingParsed>();

            private class AttributeGroupBeingParsed
            {
                public AttributeGroupBeingParsed([NotNull] XmlElement element)
                {
                    XmlElement = element;
                }

                [NotNull]
                public XmlElement XmlElement { get; private set; }

                [CanBeNull]
                public List<SchemaComplexTypeAttribute> Parsed { get; set; }
            }

            private class ElementBeingParsed
            {
                public ElementBeingParsed([NotNull] XmlElement element)
                {
                    XmlElement = element;
                }

                [NotNull]
                public XmlElement XmlElement { get; private set; }

                [CanBeNull]
                public SchemaComplexTypeElementItem Parsed { get; set; }
            }

            private class TypeBeingParsed
            {
                public TypeBeingParsed([NotNull] XmlElement element)
                {
                    XmlElement = element;
                }

                [NotNull]
                public XmlElement XmlElement { get; private set; }

                [CanBeNull]
                public SchemaTypeBase Parsed { get; set; }
            }
        }
    }
}