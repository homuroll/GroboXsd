using System.Collections.Generic;

using JetBrains.Annotations;

namespace GroboXsd.Parser
{
    public class SchemaComplexType : SchemaTypeBase
    {
        public SchemaComplexType([CanBeNull] SchemaTypeBase baseType,
                                 [NotNull] string name,
                                 [NotNull] List<SchemaComplexTypeItem> children,
                                 [NotNull] List<SchemaComplexTypeAttribute> attributes,
                                 [CanBeNull] string[] description)
            : base(baseType, name, description)
        {
            Children = children;
            Attributes = attributes;
        }

        [NotNull]
        public List<SchemaComplexTypeItem> Children { get; private set; }

        [NotNull]
        public List<SchemaComplexTypeAttribute> Attributes { get; private set; }
    }
}