using JetBrains.Annotations;

namespace GroboXsd.Errors
{
    public abstract class SchemaAutomatonError
    {
        protected SchemaAutomatonError(int lineNumber, int linePosition)
        {
            LineNumber = lineNumber;
            LinePosition = linePosition;
        }

        public int LineNumber { get; private set; }
        public int LinePosition { get; private set; }

        public interface IAttributeError
        {
            [CanBeNull]
            string AttributeName { get; }
        }

        public interface IElementError
        {
            [CanBeNull]
            string Element { get; }
        }

        public class SchemaAutomatonErrorCommon : SchemaAutomatonError, IElementError
        {
            public SchemaAutomatonErrorCommon(string message, string element, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                this.message = message;
                Element = element;
            }

            public string Element { get; }

            public override string ToString()
            {
                return message;
            }

            private readonly string message;
        }

        public class SchemaAutomatonError0 : SchemaAutomatonError, IElementError
        {
            public SchemaAutomatonError0([NotNull] string element, [NotNull] string[] expectedElements, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                Element = element;
                ExpectedElements = expectedElements;
            }

            [NotNull]
            public string Element { get; private set; }

            [NotNull]
            public string[] ExpectedElements { get; private set; }

            public override string ToString()
            {
                return string.Format("Содержимое элемента '{0}' является неполным. Список ожидаемых элементов: '{1}'.",
                                     Element, string.Join(", ", ExpectedElements));
            }
        }

        public class SchemaAutomatonError1 : SchemaAutomatonError, IElementError
        {
            public SchemaAutomatonError1([CanBeNull] string element, [NotNull] string invalidElement, [NotNull] string[] expectedElements, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                Element = element;
                InvalidElement = invalidElement;
                ExpectedElements = expectedElements;
            }

            [CanBeNull]
            public string Element { get; private set; }

            [NotNull]
            public string InvalidElement { get; private set; }

            [NotNull]
            public string[] ExpectedElements { get; private set; }

            public override string ToString()
            {
                return string.Format("В элементе '{0}' содержится лишний дочерний элемент '{1}', отсутствует/не заполнен элемент '{2}' или нарушен их порядок. Список ожидаемых дочерних элементов: '{2}'.",
                                     Element, InvalidElement, string.Join(", ", ExpectedElements));
            }
        }

        public class SchemaAutomatonError2 : SchemaAutomatonError, IElementError
        {
            public SchemaAutomatonError2([CanBeNull] string element, [NotNull] string invalidElement, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                Element = element;
                InvalidElement = invalidElement;
            }

            [CanBeNull]
            public string Element { get; private set; }

            [NotNull]
            public string InvalidElement { get; private set; }

            public override string ToString()
            {
                return string.Format("В элементе '{0}' содержится лишний дочерний элемент '{1}'.", Element, InvalidElement);
            }
        }

        public class SchemaAutomatonError3 : SchemaAutomatonError, IAttributeError
        {
            public SchemaAutomatonError3([NotNull] string attributeName, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                AttributeName = attributeName;
            }

            [NotNull]
            public string AttributeName { get; private set; }

            public override string ToString()
            {
                return string.Format("Атрибут '{0}' является лишним.", AttributeName);
            }
        }

        public class SchemaAutomatonError4 : SchemaAutomatonError, IAttributeError
        {
            public SchemaAutomatonError4([NotNull] string attributeName, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                AttributeName = attributeName;
            }

            [NotNull]
            public string AttributeName { get; private set; }

            public override string ToString()
            {
                return string.Format("Отсутствует обязательный атрибут '{0}'.", AttributeName);
            }
        }

        public class SchemaAutomatonError16 : SchemaAutomatonError, IAttributeError, IElementError
        {
            public SchemaAutomatonError16([NotNull] string nodeType, [NotNull] string name, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                NodeType = nodeType;
                Name = name;
            }

            [NotNull]
            public string NodeType { get; private set; }

            [NotNull]
            public string Name { get; private set; }

            [CanBeNull]
            string IAttributeError.AttributeName { get { return NodeType == "Атрибут" ? Name : null; } }

            [CanBeNull]
            string IElementError.Element { get { return NodeType == "Элемент" ? Name : null; } }

            public override string ToString()
            {
                return string.Format("{0} '{1}' некорректный - Не заполнено значение.", NodeType, Name);
            }
        }

        public class SchemaAutomatonError11 : SchemaAutomatonError, IElementError
        {
            public SchemaAutomatonError11([NotNull] string element, [NotNull] string invalidElement, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                Element = element;
                InvalidElement = invalidElement;
            }

            [NotNull]
            public string Element { get; private set; }

            [NotNull]
            public string InvalidElement { get; private set; }

            public override string ToString()
            {
                return string.Format("Элемент '{0}' не может содержать элемент '{1}', поскольку элемент не может иметь содержимого.", Element, InvalidElement);
            }
        }

        public class SchemaAutomatonError12 : SchemaAutomatonError, IElementError
        {
            public SchemaAutomatonError12([NotNull] string element, [NotNull] string invalidElement, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                Element = element;
                InvalidElement = invalidElement;
            }

            [NotNull]
            public string Element { get; private set; }

            [NotNull]
            public string InvalidElement { get; private set; }

            public override string ToString()
            {
                return string.Format("Элемент '{0}' не может содержать элемент '{1}', поскольку элемент может содержать только текст.", Element, InvalidElement);
            }
        }

        public class SchemaAutomatonError10 : SchemaAutomatonError, IElementError
        {
            public SchemaAutomatonError10([NotNull] string element, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                Element = element;
            }

            [NotNull]
            public string Element { get; private set; }

            public override string ToString()
            {
                return string.Format("Элемент '{0}' не может содержать текста.", Element);
            }
        }

        public class SchemaAutomatonError13 : SchemaAutomatonError, IElementError
        {
            public SchemaAutomatonError13([NotNull] string element, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                Element = element;
            }

            [NotNull]
            public string Element { get; private set; }

            public override string ToString()
            {
                return string.Format("Элемент '{0}' не может содержать пробелы или перенос строки между открывающим и закрывающим тегом элемента.", Element);
            }
        }

        public class SchemaAutomatonError5 : SchemaAutomatonError, IAttributeError, IElementError
        {
            public SchemaAutomatonError5([NotNull] string nodeType, [NotNull] string name, [NotNull] string value, [NotNull] string valueType, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                NodeType = nodeType;
                Name = name;
                Value = value;
                ValueType = valueType;
            }

            [NotNull]
            public string NodeType { get; private set; }

            [NotNull]
            public string Name { get; private set; }

            [NotNull]
            public string Value { get; private set; }

            [NotNull]
            public string ValueType { get; private set; }

            [CanBeNull]
            string IAttributeError.AttributeName { get { return NodeType == "Атрибут" ? Name : null; } }

            [CanBeNull]
            string IElementError.Element { get { return NodeType == "Элемент" ? Name : null; } }

            public override string ToString()
            {
                return string.Format("{0} '{1}' некорректный - Значение '{2}' не является {3}.", NodeType, Name, Value, ValueType);
            }
        }

        public class SchemaAutomatonError6 : SchemaAutomatonError, IAttributeError, IElementError
        {
            public SchemaAutomatonError6([NotNull] string nodeType, [NotNull] string name, [NotNull] string value, int length, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                NodeType = nodeType;
                Name = name;
                Value = value;
                Length = length;
            }

            [NotNull]
            public string NodeType { get; private set; }

            [NotNull]
            public string Name { get; private set; }

            [NotNull]
            public string Value { get; private set; }

            public int Length { get; private set; }

            [CanBeNull]
            string IAttributeError.AttributeName { get { return NodeType == "Атрибут" ? Name : null; } }

            [CanBeNull]
            string IElementError.Element { get { return NodeType == "Элемент" ? Name : null; } }

            public override string ToString()
            {
                return string.Format("{0} '{1}' некорректный - Длина значения '{2}' должна быть равна {3}.", NodeType, Name, Value, Length);
            }
        }

        public class SchemaAutomatonError7 : SchemaAutomatonError, IAttributeError, IElementError
        {
            public SchemaAutomatonError7([NotNull] string nodeType, [NotNull] string name, [NotNull] string value, int maxLength, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                NodeType = nodeType;
                Name = name;
                Value = value;
                MaxLength = maxLength;
            }

            [NotNull]
            public string NodeType { get; private set; }

            [NotNull]
            public string Name { get; private set; }

            [NotNull]
            public string Value { get; private set; }

            public int MaxLength { get; private set; }

            [CanBeNull]
            string IAttributeError.AttributeName { get { return NodeType == "Атрибут" ? Name : null; } }

            [CanBeNull]
            string IElementError.Element { get { return NodeType == "Элемент" ? Name : null; } }

            public override string ToString()
            {
                return string.Format("{0} '{1}' некорректный - Длина значения '{2}' должна быть не больше чем {3}.", NodeType, Name, Value, MaxLength);
            }
        }

        public class SchemaAutomatonError8 : SchemaAutomatonError, IAttributeError, IElementError
        {
            public SchemaAutomatonError8([NotNull] string nodeType, [NotNull] string name, [NotNull] string value, int minLength, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                NodeType = nodeType;
                Name = name;
                Value = value;
                MinLength = minLength;
            }

            [NotNull]
            public string NodeType { get; private set; }

            [NotNull]
            public string Name { get; private set; }

            [NotNull]
            public string Value { get; private set; }

            public int MinLength { get; private set; }

            [CanBeNull]
            string IAttributeError.AttributeName { get { return NodeType == "Атрибут" ? Name : null; } }

            [CanBeNull]
            string IElementError.Element { get { return NodeType == "Элемент" ? Name : null; } }

            public override string ToString()
            {
                return string.Format("{0} '{1}' некорректный - Длина значения '{2}' должна быть не меньше чем {3}.", NodeType, Name, Value, MinLength);
            }
        }

        public class SchemaAutomatonError9 : SchemaAutomatonError, IAttributeError, IElementError
        {
            public SchemaAutomatonError9([NotNull] string nodeType, [NotNull] string name, [CanBeNull] string value, [NotNull] string restriction, [NotNull] object restrictionValue, int lineNumber, int linePosition)
                : base(lineNumber, linePosition)
            {
                NodeType = nodeType;
                Name = name;
                Value = value;
                Restriction = restriction;
                RestrictionValue = restrictionValue;
            }

            [NotNull]
            public string NodeType { get; private set; }

            [NotNull]
            public string Name { get; private set; }

            [CanBeNull]
            public string Value { get; private set; }

            [NotNull]
            public string Restriction { get; private set; }

            [NotNull]
            public object RestrictionValue { get; private set; }

            private string GetError()
            {
                switch(Restriction)
                {
                case "enumeration":
                    return string.Format("Значение должно быть из списка допустимых значений: {0}", string.Join(", ", (string[])RestrictionValue));
                case "pattern":
                    return string.Format("Нарушен формат: {0}", RestrictionValue);
                case "totalDigits":
                    return string.Format("Общее количество цифр числа должно быть не больше чем {0}", RestrictionValue);
                case "fractionDigits":
                    return string.Format("Количество цифр дробной части числа должно быть не больше чем {0}", RestrictionValue);
                case "maxInclusive":
                    return string.Format("Значение должно быть не больше чем {0}", RestrictionValue);
                case "maxExclusive":
                    return string.Format("Значение должно быть строго меньше чем {0}", RestrictionValue);
                case "minInclusive":
                    return string.Format("Значение должно быть не меньше чем {0}", RestrictionValue);
                case "minExclusive":
                    return string.Format("Значение должно быть строго больше чем {0}", RestrictionValue);
                default:
                    return Restriction;
                }
            }

            [CanBeNull]
            string IAttributeError.AttributeName { get { return NodeType == "Атрибут" ? Name : null; } }

            [CanBeNull]
            string IElementError.Element { get { return NodeType == "Элемент" ? Name : null; } }

            public override string ToString()
            {
                return string.Format("{0} '{1}' некорректный - Значение '{2}' не проходит по следующему ограничению: '{3}'.", NodeType, Name, Value, GetError());
            }
        }
    }
}