using System.Collections.Generic;

using JetBrains.Annotations;

namespace GroboXsd.Parser
{
    public class SchemaComplexTypeSequenceItem : SchemaComplexTypeItem
    {
        public SchemaComplexTypeSequenceItem([NotNull] List<SchemaComplexTypeItem> items,
                                             int minOccurs,
                                             int? maxOccurs // null means unbounded
            )
            : base(minOccurs, maxOccurs)
        {
            Items = items;
        }

        [NotNull]
        public List<SchemaComplexTypeItem> Items { get; private set; }
    }
}