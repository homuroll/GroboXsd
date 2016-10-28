using JetBrains.Annotations;

namespace GroboXsd.Parser
{
    public abstract class SchemaTypeBase
    {
        protected SchemaTypeBase([CanBeNull] SchemaTypeBase baseType, [NotNull] string name, [CanBeNull] string[] description)
        {
            BaseType = baseType;
            Name = name;
            Description = description;
        }

        [CanBeNull]
        public SchemaTypeBase BaseType { get; private set; }

        [NotNull]
        public string Name { get; private set; }

        [CanBeNull]
        public string[] Description { get; private set; }

        public override string ToString()
        {
            return Name;
        }
    }
}