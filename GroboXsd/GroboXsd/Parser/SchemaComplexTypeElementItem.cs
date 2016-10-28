using JetBrains.Annotations;

namespace GroboXsd.Parser
{
    public class SchemaComplexTypeElementItem : SchemaComplexTypeItem
    {
        public SchemaComplexTypeElementItem([NotNull] string name,
                                            [CanBeNull] SchemaTypeBase type,
                                            int minOccurs,
                                            int? maxOccurs, // null means unbounded
                                            [CanBeNull] string fixedValue)
            : base(minOccurs, maxOccurs)
        {
            Name = name;
            Type = type;
            FixedValue = fixedValue;
        }

        [NotNull]
        public string Name { get; private set; }

        [CanBeNull]
        public SchemaTypeBase Type { get; private set; }

        [CanBeNull]
        public string FixedValue { get; private set; }
    }
}