using JetBrains.Annotations;

namespace GroboXsd.Parser
{
    public class SchemaSimpleType : SchemaTypeBase
    {
        public SchemaSimpleType([CanBeNull] SchemaTypeBase baseType, [NotNull] string name,
                                [CanBeNull] SchemaSimpleTypeRestriction restriction, [CanBeNull] string[] description)
            : base(baseType, name, description)
        {
            Restriction = restriction;
        }

        public SchemaSimpleType AtomicBaseType
        {
            get
            {
                SchemaTypeBase result = this;
                while(result.BaseType != null)
                    result = result.BaseType;
                return (SchemaSimpleType)result;
            }
        }

        [CanBeNull]
        public SchemaSimpleTypeRestriction Restriction { get; private set; }

        public override int GetHashCode()
        {
            return Helpers.Horner(new[]
                {
                    Name.GetHashCode(), BaseType == null ? 0 : BaseType.GetHashCode(), Restriction == null ? 0 : Restriction.GetHashCode()
                }, 223641479);
        }

        public override bool Equals(object obj)
        {
            var other = obj as SchemaSimpleType;
            if(other == null)
                return false;
            if(ReferenceEquals(this, other))
                return true;
            if(Name != other.Name)
                return false;
            if(BaseType != other.BaseType)
                return false;
            return Restriction == other.Restriction;
        }

        public static readonly SchemaSimpleType String = new SchemaSimpleType(null, "string", null, null);
        public static readonly SchemaSimpleType Integer = new SchemaSimpleType(null, "integer", null, null);
        public static readonly SchemaSimpleType Int = new SchemaSimpleType(null, "int", null, null);
        public static readonly SchemaSimpleType Decimal = new SchemaSimpleType(null, "decimal", null, null);
        public static readonly SchemaSimpleType Boolean = new SchemaSimpleType(null, "boolean", null, null);
        public static readonly SchemaSimpleType Date = new SchemaSimpleType(null, "date", null, null);
        public static readonly SchemaSimpleType GMonth = new SchemaSimpleType(null, "gMonth", null, null);
        public static readonly SchemaSimpleType GYear = new SchemaSimpleType(null, "gYear", null, null);
        public static readonly SchemaSimpleType AnyURI = new SchemaSimpleType(null, "anyURI", null, null);
        public static readonly SchemaSimpleType Base64Binary = new SchemaSimpleType(null, "base64Binary", null, null);
    }
}