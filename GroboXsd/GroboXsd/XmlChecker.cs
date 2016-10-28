using System;
using System.IO;
using System.Threading;
using System.Xml;

using GroboXsd.Errors;

namespace GroboXsd
{
    internal class XmlChecker
    {
        public XmlChecker(Stream file, ISchemaTree schemaTree)
        {
            stream = file;
            schemaValidator = schemaTree;
            schemaValidator.ErrorEventHandler += SchemaErrorEventHandler;
        }

        public void Check()
        {
            schemaValidator = schemaValidator.ToRoot();
            stream.Seek(0, SeekOrigin.Begin);

            xmlValidatingReader = XmlReader.Create(stream);
            xmlLineInfo = (xmlValidatingReader as IXmlLineInfo) ?? new DummyLineInfo();

            while(xmlValidatingReader.Read())
            {
                Interlocked.Exchange(ref streamPosition, stream.Position);
                switch(xmlValidatingReader.NodeType)
                {
                case XmlNodeType.Element:
                    schemaValidator = schemaValidator.StartElement(xmlValidatingReader.Name, xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
                    while(xmlValidatingReader.MoveToNextAttribute())
                    {
                        var attrValue = xmlValidatingReader.Value;
                        var attrName = xmlValidatingReader.Name;
                        schemaValidator.ReadAttribute(attrName, attrValue, xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
                    }
                    schemaValidator.DoneAttributes();
                    xmlValidatingReader.MoveToElement();
                    if(xmlValidatingReader.IsEmptyElement)
                        schemaValidator = schemaValidator.EndElement(xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
                    break;
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    schemaValidator.ReadWhitespace(xmlValidatingReader.Value, xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
                    break;

                case XmlNodeType.Text:
                    schemaValidator.ReadText(xmlValidatingReader.Value, xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
                    break;
                case XmlNodeType.EndElement:
                    schemaValidator = schemaValidator.EndElement(xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
                    break;
                }
            }
        }

        private static void SchemaErrorEventHandler(object sender, SchemaAutomatonError e)
        {
            Console.WriteLine("Error at line {0}, position {1}: {2}", e.LineNumber, e.LinePosition, e);
        }

        private readonly Stream stream;
        private ISchemaTree schemaValidator;
        private XmlReader xmlValidatingReader;
        private long streamPosition;
        private IXmlLineInfo xmlLineInfo;

        private class DummyLineInfo : IXmlLineInfo
        {
            public bool HasLineInfo()
            {
                return false;
            }

            public int LineNumber { get { return 0; } }
            public int LinePosition { get { return 0; } }
        }
    }
}