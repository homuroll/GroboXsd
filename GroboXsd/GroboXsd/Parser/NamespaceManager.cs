using System.Xml;

namespace GroboXsd.Parser
{
    public class NamespaceManager
    {
        public static XmlNamespaceManager Manager
        {
            get
            {
                if(manager == null)
                {
                    manager = new XmlNamespaceManager(new NameTable());
                    manager.AddNamespace("ns", "file://C:/bl/WORK/Languages/ФНС 3.0/draf/FNS_3.0.xsd");
                    manager.AddNamespace("types", "file://C:/bl/WORK/Languages/ФНС 3.0/draf/types.xsd");
                    manager.AddNamespace("math", Math);
                    manager.AddNamespace("xs", Schema);
                    manager.AddNamespace("dt", DatabaseType);
                    manager.AddNamespace("td", "http://www.kontur-extern.ru/TypeDescription.xsd");
                    manager.AddNamespace("fat", "http://www.kontur-extern.ru/FormAttachmentType.xsd");
                }
                return manager;
            }
        }

        public const string DatabaseType = "http://www.kontur-extern.ru/DataBaseType.xsd";
        public const string Math = "http://www.kontur-extern.ru/ФНС 4.0/math.xsd";
        public const string Schema = "http://www.w3.org/2001/XMLSchema";
        public const string XmlNamespace = "http://www.w3.org/2000/xmlns/";
        public const string SchemaInstance = "http://www.w3.org/2001/XMLSchema-instance";

        private static XmlNamespaceManager manager;
    }
}