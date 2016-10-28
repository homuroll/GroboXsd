using System.Text;

using GroboXsd.Parser;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public class SchemaRegularExpressionBuilder
    {
        public string Build(SchemaTypeBase schemaRootType)
        {
            var result = new StringBuilder();
            Build(schemaRootType, 0, result);
            return result.ToString();
        }

        private static string[] BuildMargins()
        {
            var result = new string[1024];
            result[0] = "";
            for(var i = 1; i < 1024; ++i)
                result[i] = new string(' ', i);
            return result;
        }

        private void Build(SchemaComplexTypeItem nodeItemType, int margin, StringBuilder result)
        {
            var element = nodeItemType as SchemaComplexTypeElementItem;
            var sequence = nodeItemType as SchemaComplexTypeSequenceItem;
            var choice = nodeItemType as SchemaComplexTypeChoiceItem;
            if(element != null)
            {
                result.Append(margins[margin]);
                if(element.MinOccurs == 1 && element.MaxOccurs == 1)
                    result.Append("<" + element.Name + ">");
                else
                    result.Append("(<" + element.Name + ">");
                if(element.Type is SchemaComplexType && ((SchemaComplexType)element.Type).Children.Count > 0)
                {
                    result.AppendLine();
                    Build(element.Type, margin + 4, result);
                    result.AppendLine();
                    result.Append(margins[margin]);
                }
                if(element.MinOccurs == 1 && element.MaxOccurs == 1)
                    result.Append("</" + element.Name + ">");
                else if(element.MinOccurs == 0 && element.MaxOccurs == null)
                    result.Append("</" + element.Name + ">)[*]");
                else
                    result.Append("</" + element.Name + ">)[" + element.MinOccurs + ", " + (element.MaxOccurs == null ? "*" : element.MaxOccurs.ToString()) + "]");
            }
            if(sequence != null)
            {
                if(sequence.MinOccurs != 1 || sequence.MaxOccurs != 1)
                    result.Append("(");
                for(var i = 0; i < sequence.Items.Count; i++)
                {
                    Build(sequence.Items[i], margin, result);
                    if(i < sequence.Items.Count - 1)
                        result.AppendLine();
                }
                if(sequence.MinOccurs != 1 || sequence.MaxOccurs != 1)
                {
                    if(sequence.MinOccurs == 0 && sequence.MaxOccurs == null)
                        result.Append(")[*]");
                    else
                        result.Append(")[" + sequence.MinOccurs + ", " + (sequence.MaxOccurs == null ? "*" : sequence.MaxOccurs.ToString()) + "]");
                }
            }
            if(choice != null)
            {
                result.Append(margins[margin]);
                result.AppendLine("(");
                for(var i = 0; i < choice.Items.Count; i++)
                {
                    Build(choice.Items[i], margin, result);
                    if(i < choice.Items.Count - 1)
                    {
                        result.AppendLine();
                        result.Append(margins[margin]);
                        result.Append("|");
                        result.AppendLine();
                    }
                }
                result.AppendLine();
                result.Append(margins[margin]);
                result.Append(")");
                if(choice.MinOccurs != 1 || choice.MaxOccurs != 1)
                {
                    if(choice.MinOccurs == 0 && choice.MaxOccurs == null)
                        result.Append("[*]");
                    else
                        result.Append("[" + choice.MinOccurs + ", " + (choice.MaxOccurs == null ? "*" : choice.MaxOccurs.ToString()) + "]");
                }
            }
        }

        private void Build(SchemaTypeBase nodeType, int margin, StringBuilder result)
        {
            var complexType = nodeType as SchemaComplexType;
            if(complexType == null)
                return;
            if(complexType.BaseType != null)
                Build(complexType.BaseType, margin, result);
            foreach(var child in complexType.Children)
                Build(child, margin, result);
        }

        private static readonly string[] margins = BuildMargins();
    }
}