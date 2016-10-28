using System;

namespace GroboXsd.Parser
{
    public abstract class SchemaComplexTypeItem
    {
        protected SchemaComplexTypeItem(
            int minOccurs,
            int? maxOccurs // null means unbounded
            )
        {
            if(maxOccurs != null && minOccurs > maxOccurs)
                throw new InvalidOperationException("The 'minOccurs' attribute value cannot be greater than the 'maxOccurs' attribute value");
            MinOccurs = minOccurs;
            MaxOccurs = maxOccurs;
        }

        public int MinOccurs { get; private set; }

        public int? MaxOccurs { get; private set; }
    }
}