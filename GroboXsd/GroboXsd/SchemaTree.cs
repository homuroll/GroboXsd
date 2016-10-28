using System.Collections.Generic;

using GroboXsd.Automaton;
using GroboXsd.Errors;

using JetBrains.Annotations;

namespace GroboXsd
{
    public class SchemaTree : ISchemaTree
    {
        public SchemaTree([NotNull] ISchemaAutomaton schemaAutomaton)
        {
            this.schemaAutomaton = schemaAutomaton;
        }

        [NotNull]
        public ISchemaTree StartElement([NotNull] string name, int lineNumber, int linePosition)
        {
            schemaAutomaton.SetLineInfo(lineNumber, linePosition);
            if(depth > 0)
            {
                depth++;
                return this;
            }
            if(path.Count > 0)
                CheckRequiredAttributes(path.Peek());
            var error = schemaAutomaton.StartElement(name);
            if(error != null)
                RaiseError(error);
            path.Push(new State());
            if(schemaAutomaton.InAnyTypeState)
                depth = 1;
            return this;
        }

        [NotNull]
        public ISchemaTree ReadAttribute([NotNull] string name, [NotNull] string value, int lineNumber, int linePosition)
        {
            schemaAutomaton.SetLineInfo(lineNumber, linePosition);
            if(depth > 0)
                return this;

            var error = schemaAutomaton.ReadAttribute(name, value);
            if(error != null)
                RaiseError(error);
            return this;
        }

        [NotNull]
        public ISchemaTree DoneAttributes()
        {
            if(depth > 0)
                return this;
            if(path.Count > 0)
                CheckRequiredAttributes(path.Peek());
            return this;
        }

        [NotNull]
        public ISchemaTree ReadText([NotNull] string text, int lineNumber, int linePosition)
        {
            schemaAutomaton.SetLineInfo(lineNumber, linePosition);
            if(depth > 0)
                return this;

            var state = path.Peek();
            var error = schemaAutomaton.ReadText(text);
            state.TextIsChecked = true;
            if(error != null)
                RaiseError(error);
            return this;
        }

        [NotNull]
        public ISchemaTree ReadWhitespace([NotNull] string whitespace, int lineNumber, int linePosition)
        {
            schemaAutomaton.SetLineInfo(lineNumber, linePosition);
            if(depth > 0 || path.Count == 0)
                return this;
            var state = path.Peek();
            var error = schemaAutomaton.ReadWhitespace(whitespace);
            state.TextIsChecked = true;
            if(error != null)
                RaiseError(error);
            return this;
        }

        [NotNull]
        public ISchemaTree EndElement(int lineNumber, int linePosition)
        {
            schemaAutomaton.SetLineInfo(lineNumber, linePosition);
            if(depth > 0 && --depth > 0)
                return this;
            var state = path.Pop();
            CheckRequiredAttributes(state);
            CheckText(state);
            var error = schemaAutomaton.EndElement();
            if(error != null)
                RaiseError(error);
            return this;
        }

        [NotNull]
        public ISchemaTree ToRoot()
        {
            schemaAutomaton.Reset();
            path.Clear();
            depth = 0;
            return this;
        }

        public event SchemaErrorEventHandler ErrorEventHandler;

        private void CheckRequiredAttributes([NotNull] State state)
        {
            if(!state.RequiredAttributesAreChecked)
            {
                var errors = schemaAutomaton.CheckRequiredAttributes();
                foreach(var error in errors)
                    RaiseError(error);
            }
            state.RequiredAttributesAreChecked = true;
        }

        private void CheckText([NotNull] State state)
        {
            if(!state.TextIsChecked)
            {
                if(schemaAutomaton.HasText)
                {
                    var error = schemaAutomaton.ReadText("");
                    if(error != null)
                        RaiseError(error);
                }
            }
            state.TextIsChecked = true;
        }

        private void RaiseError([NotNull] SchemaAutomatonError error)
        {
            if(ErrorEventHandler != null)
                ErrorEventHandler(this, error);
        }

        private readonly Stack<State> path = new Stack<State>();

        [NotNull]
        private readonly ISchemaAutomaton schemaAutomaton;

        private int depth;

        private class State
        {
            public bool RequiredAttributesAreChecked { get; set; }
            public bool TextIsChecked { get; set; }
        }
    }
}