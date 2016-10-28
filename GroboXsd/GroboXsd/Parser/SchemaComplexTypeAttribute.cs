using JetBrains.Annotations;

namespace GroboXsd.Parser
{
    public class SchemaComplexTypeAttribute
    {
        public SchemaComplexTypeAttribute([NotNull] string name, [CanBeNull] SchemaSimpleType type, bool required, [CanBeNull] string fixedValue)
        {
            Name = name;
            Required = required;
            FixedValue = fixedValue;
            Type = type;
        }

        [NotNull]
        public string Name { get; private set; }

        [CanBeNull]
        public SchemaSimpleType Type { get; private set; }

        public bool Required { get; private set; }

        [CanBeNull]
        public string FixedValue { get; private set; }
    }
}