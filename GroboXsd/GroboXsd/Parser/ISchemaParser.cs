using System.Xml;

using JetBrains.Annotations;

namespace GroboXsd.Parser
{
    public interface ISchemaParser
    {
        [NotNull]
        SchemaTypeBase Parse([NotNull] XmlDocument schema);
    }
}