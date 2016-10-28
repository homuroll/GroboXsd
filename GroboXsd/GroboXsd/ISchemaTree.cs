using JetBrains.Annotations;

namespace GroboXsd
{
    public interface ISchemaTree
    {
        event SchemaErrorEventHandler ErrorEventHandler;

        [NotNull]
        ISchemaTree StartElement([NotNull] string name, int lineNumber, int linePosition);

        [NotNull]
        ISchemaTree ReadAttribute([NotNull] string name, [NotNull] string value, int lineNumber, int linePosition);

        [NotNull]
        ISchemaTree DoneAttributes();

        [NotNull]
        ISchemaTree ReadText([NotNull] string text, int lineNumber, int linePosition);

        [NotNull]
        ISchemaTree ReadWhitespace([NotNull] string whitespace, int lineNumber, int linePosition);

        [NotNull]
        ISchemaTree EndElement(int lineNumber, int linePosition);

        [NotNull]
        ISchemaTree ToRoot();
    }
}