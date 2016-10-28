using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GroboXsd.Automaton.SimpleTypeExecutors;
using GroboXsd.Errors;
using GroboXsd.Parser;
using GroboXsd.ReadonlyCollections;

using JetBrains.Annotations;

namespace GroboXsd.Automaton
{
    public class SchemaAutomatonFactoryBuilder : ISchemaAutomatonFactoryBuilder
    {
        public SchemaAutomatonFactoryBuilder(ISchemaSimpleTypeExecutorFactory schemaSimpleTypeExecutorFactory)
        {
            this.schemaSimpleTypeExecutorFactory = schemaSimpleTypeExecutorFactory;
        }

        private const string epsilon = "";

        /* 
		 * We are assuming that schema doesn't contain 'all' items.
		 * In such a case schema can be rather simply converted into a classic FDA.
		 */

        [NotNull]
        public CreateSchemaAutomatonDelegate Build([CanBeNull] SchemaTypeBase schemaRootType)
        {
            // Build a non-deterministic automaton first
            var ids = new Ids();
            var nda = BuildNDA(schemaRootType, 0, ids);
            /*
			 * Remove all empty jumps in order to build a FDA.
			 * Here we're making an assumption that each subitem of a 'choice' item starts with a different letter.
			 * This makes building FDA from NDA an easier task because we don't need to unite some vertices as for the general case.
			 */
            var mapping = new Dictionary<NDANode, FDANode>();
            //System.IO.File.WriteAllText(@"c:\temp\nda.gml", PrintNDA(nda.Start));
            var fdaStart = MakeClosure(nda.Start, mapping);
            var nextConsistentStates = new ConcurrentDictionary<FDANode, JumpToNextConsistentState>();
            //System.IO.File.WriteAllText(@"c:\temp\fda.gml", PrintFDA(fdaStart));
            SetCorrenspondingOpeningNodes(fdaStart);
            return () => new SchemaAutomaton(fdaStart, nextConsistentStates, new int[ids.counterId], new bool[ids.requiredAttributeId]);
        }

        private FDANode MakeClosure([NotNull] NDANode node, Dictionary<NDANode, FDANode> mapping)
        {
            FDANode result;
            if(mapping.TryGetValue(node, out result))
                return result;
            mapping.Add(node, result = new FDANode(node, schemaSimpleTypeExecutorFactory));
            var visitedByEmptyJumps = new Dictionary<NDANode, VisitedEntersCounters>();
            var visitedByNotEmptyJumps = new Dictionary<NDANode, VisitedEntersCounters>();
            var queue = new Queue<KeyValuePair<NDANode, VisitedEntersCounters>>();
            var prevNode = new Dictionary<NDANode, Tuple<NDANode, NDAEmptyJump>>();
            var startVisitedCounters = new VisitedEntersCounters();
            startVisitedCounters.AddEntersCounterToUpdate(node.entersCounterToUpdate);
            queue.Enqueue(new KeyValuePair<NDANode, VisitedEntersCounters>(node, startVisitedCounters));
            visitedByEmptyJumps.Add(node, new VisitedEntersCounters(startVisitedCounters));
            while(queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentCounters = current.Value;
                foreach(var emptyJump in current.Key.emptyJumps)
                {
                    var copy = new VisitedEntersCounters(currentCounters);
                    if(emptyJump.Direction == EdgeDirection.Forward && !copy.entersCountersToUpdate.Contains(current.Key.entersCounterToCheck))
                        copy.AddEntersCounterToCheck(current.Key.entersCounterToCheck);
                    result.ignoreWhitespaces |= emptyJump.Node.ignoreWhitespaces;
                    copy.AddEntersCounterToUpdate(emptyJump.Node.entersCounterToUpdate);
                    VisitedEntersCounters entersCounters;
                    if(visitedByEmptyJumps.TryGetValue(emptyJump.Node, out entersCounters))
                    {
                        if(!copy.Equals(entersCounters))
                            throw new InvalidOperationException("Ambiguous paths encountered");
                        continue;
                    }
                    queue.Enqueue(new KeyValuePair<NDANode, VisitedEntersCounters>(emptyJump.Node, copy));
                    prevNode.Add(emptyJump.Node, new Tuple<NDANode, NDAEmptyJump>(current.Key, emptyJump));
                    visitedByEmptyJumps.Add(emptyJump.Node, copy);
                }
                foreach(var notEmptyJump in current.Key.notEmptyJumps)
                {
                    var copy = new VisitedEntersCounters(currentCounters);
                    if(notEmptyJump.Key.Direction == EdgeDirection.Forward && !copy.entersCountersToUpdate.Contains(current.Key.entersCounterToCheck))
                        copy.AddEntersCounterToCheck(current.Key.entersCounterToCheck);
                    copy.AddEntersCounterToUpdate(notEmptyJump.Value.entersCounterToUpdate);
                    VisitedEntersCounters entersCounters;
                    if(visitedByNotEmptyJumps.TryGetValue(notEmptyJump.Value, out entersCounters))
                    {
                        if(!copy.Equals(entersCounters))
                            throw new InvalidOperationException("Ambiguous paths encountered");
                        continue;
                    }
                    visitedByNotEmptyJumps.Add(notEmptyJump.Value, copy);
                }
            }
            foreach(var notEmptyJump in node.notEmptyJumps)
            {
                var endNode = notEmptyJump.Value;
                var letter = notEmptyJump.Key.Letter;
                var jump = new FDAJump(MakeClosure(endNode, mapping));
                if(endNode.entersCounterToUpdate != null)
                {
                    jump.AddEntersCounterForUpdate(endNode.entersCounterToUpdate);
                    endNode.entersCounterToUpdate.ElementNames.Add(letter.Substring(1));
                }
                result.jumps.Add(letter, jump);
            }
            foreach(var reachableNode in visitedByEmptyJumps.Keys)
            {
                var path = new List<NDAEmptyJump>();
                var current = reachableNode;
                while(current != node)
                {
                    var jump = prevNode[current];
                    path.Add(jump.Item2);
                    current = jump.Item1;
                }
                var entersCountersToUpdate = new List<AutomatonNodeEntersCounter>();
                var entersCountersToCheck = new List<AutomatonNodeEntersCounter>();
                current = node;
                for(var i = path.Count - 1; i >= 0; --i)
                {
                    var nextReachable = path[i].Node;
                    if(nextReachable.entersCounterToUpdate != null)
                        entersCountersToUpdate.Add(nextReachable.entersCounterToUpdate);
                    if(current.entersCounterToCheck != null && path[i].Direction == EdgeDirection.Forward)
                        entersCountersToCheck.Add(current.entersCounterToCheck);
                    foreach(var notEmptyJump in nextReachable.notEmptyJumps)
                    {
                        var endNode = notEmptyJump.Value;
                        var fdaEndNode = MakeClosure(endNode, mapping);
                        FDAJump jump;
                        var letter = notEmptyJump.Key.Letter;
                        if(result.jumps.TryGetValue(letter, out jump))
                        {
                            if(jump.Node.Id != fdaEndNode.Id)
                                throw new InvalidOperationException(string.Format("Unsupported schema. Element '{0}' leads to different automaton nodes", letter));
                        }
                        else
                            result.jumps.Add(letter, jump = new FDAJump(fdaEndNode));
                        foreach(var entersCounter in entersCountersToUpdate)
                        {
                            jump.AddEntersCounterForUpdate(entersCounter);
                            entersCounter.ElementNames.Add(letter.Substring(1));
                        }
                        foreach(var enterCounter in entersCountersToCheck)
                            jump.AddEntersCounterForCheck(enterCounter);
                        if(endNode.entersCounterToUpdate != null)
                        {
                            jump.AddEntersCounterForUpdate(endNode.entersCounterToUpdate);
                            endNode.entersCounterToUpdate.ElementNames.Add(letter.Substring(1));
                        }
                    }
                    current = nextReachable;
                }
            }
            return result;
        }

        private void SetCorrenspondingOpeningNodes(FDANode start)
        {
            SetCorrenspondingOpeningNodes(start, new Stack<FDANode>(), new HashSet<FDANode>());
        }

        private void SetCorrenspondingOpeningNodes(FDANode node, Stack<FDANode> path, HashSet<FDANode> visited)
        {
            if(visited.Contains(node))
                return;
            visited.Add(node);
            foreach(var jump in node.jumps)
            {
                var letter = jump.Key;
                var next = jump.Value.Node;
                if(letter.StartsWith("+"))
                {
                    path.Push(next);
                    SetCorrenspondingOpeningNodes(next, path, visited);
                    path.Pop();
                }
                else
                {
                    var prev = path.Pop();
                    if(node.correspondingOpeningNode != null && node.correspondingOpeningNode != prev)
                        throw new InvalidOperationException();
                    node.correspondingOpeningNode = prev;
                    SetCorrenspondingOpeningNodes(next, path, visited);
                    path.Push(prev);
                }
            }
        }

        private SchemaSimpleType Zzz([NotNull] SchemaComplexType nodeType, List<SchemaComplexTypeAttribute> attributes, List<SchemaComplexTypeItem> children)
        {
            SchemaSimpleType result;
            if(nodeType.BaseType == null)
                result = null;
            else if(nodeType.BaseType is SchemaSimpleType)
                result = (SchemaSimpleType)nodeType.BaseType;
            else
                result = Zzz((SchemaComplexType)nodeType.BaseType, attributes, children);
            attributes.AddRange(nodeType.Attributes);
            children.AddRange(nodeType.Children);
            return result;
        }

        [NotNull]
        private NonDeterministicAutomaton BuildNDA([CanBeNull] SchemaTypeBase nodeType, int depth, Ids ids)
        {
            var complexType = nodeType as SchemaComplexType;
            if(complexType == null)
            {
                var staRt = new NDANode(ids.nodeId++, depth)
                    {
                        anyType = nodeType == null,
                        innerTextType = (SchemaSimpleType)nodeType
                    };
                var finiSh = new NDANode(ids.nodeId++, depth);
                staRt.AddJump(epsilon, EdgeDirection.Forward, finiSh);
                return new NonDeterministicAutomaton(staRt, finiSh);
            }
            var attributes = new List<SchemaComplexTypeAttribute>();
            var children = new List<SchemaComplexTypeItem>();
            var innerTextType = Zzz(complexType, attributes, children);
            // TODO handle FixedValue
            var start = new NDANode(ids.nodeId++, depth)
                {
                    allowedAttributes = attributes.ToDictionary(attribute => attribute.Name, attribute => attribute.Type),
                    requiredAttributes = attributes.Where(attr => attr.Required).ToDictionary(attr => attr.Name, attr => ids.requiredAttributeId++),
                    innerTextType = innerTextType,
                };
            var node = start;
            foreach(var child in children.Select(child => BuildNDA(child, depth, ids)))
            {
                node.AddJump(epsilon, EdgeDirection.Forward, child.Start);
                node = child.Finish;
            }
            var finish = new NDANode(ids.nodeId++, depth);
            node.AddJump(epsilon, EdgeDirection.Forward, finish);
            return new NonDeterministicAutomaton(start, finish);
        }

        [NotNull]
        private NonDeterministicAutomaton BuildNDA([NotNull] SchemaComplexTypeItem nodeItemType, int depth, Ids ids)
        {
            var start = new NDANode(ids.nodeId++, depth);
            NDANode finish;
            var element = nodeItemType as SchemaComplexTypeElementItem;
            var sequence = nodeItemType as SchemaComplexTypeSequenceItem;
            var choice = nodeItemType as SchemaComplexTypeChoiceItem;
            if(element != null)
            {
                AutomatonNodeEntersCounter counter = null;
                if(element.MinOccurs > 1 || element.MaxOccurs > 1)
                    counter = new AutomatonNodeEntersCounter(ids.counterId++, element.MinOccurs, element.MaxOccurs);
                var child = BuildNDA(element.Type, depth + 1, ids);
                finish = new NDANode(ids.nodeId++, depth);
                var opening = "+" + element.Name;
                var closing = "-" + element.Name;
                start.ignoreWhitespaces = true;
                start.AddJump(opening, EdgeDirection.Forward, child.Start);
                child.Finish.AddJump(closing, EdgeDirection.Forward, finish);
                finish.ignoreWhitespaces = true;
                if(element.MinOccurs == 0)
                    start.AddJump(epsilon, EdgeDirection.Forward, finish);
                if(element.MaxOccurs != 1)
                    finish.AddJump(opening, EdgeDirection.Backward, child.Start);
                if(counter != null)
                {
                    child.Start.entersCounterToUpdate = counter;
                    finish.entersCounterToCheck = counter;
                }
            }
            else if(sequence != null)
            {
                AutomatonNodeEntersCounter counter = null;
                if(sequence.MinOccurs > 1 || sequence.MaxOccurs > 1)
                    counter = new AutomatonNodeEntersCounter(ids.counterId++, sequence.MinOccurs, sequence.MaxOccurs);
                var node = start;
                var childrenAutomata = sequence.Items.Select(item => BuildNDA(item, depth, ids)).ToArray();
                finish = new NDANode(ids.nodeId++, depth);
                foreach(var item in childrenAutomata)
                {
                    node.AddJump(epsilon, EdgeDirection.Forward, item.Start);
                    node = item.Finish;
                }
                node.AddJump(epsilon, EdgeDirection.Forward, finish);
                if(sequence.MinOccurs == 0)
                    start.AddJump(epsilon, EdgeDirection.Forward, finish);
                if(sequence.MaxOccurs != 1)
                    finish.AddJump(epsilon, EdgeDirection.Backward, childrenAutomata[0].Start);
                if(counter != null)
                {
                    childrenAutomata[0].Start.entersCounterToUpdate = counter;
                    finish.entersCounterToCheck = counter;
                }
            }
            else if(choice != null)
            {
                AutomatonNodeEntersCounter counter = null;
                if(choice.MinOccurs > 1 || choice.MaxOccurs > 1)
                    counter = new AutomatonNodeEntersCounter(ids.counterId++, choice.MinOccurs, choice.MaxOccurs);
                var childrenAutomata = choice.Items.Select(item => BuildNDA(item, depth, ids)).ToArray();
                finish = new NDANode(ids.nodeId++, depth);
                if(counter != null)
                    finish.entersCounterToCheck = counter;
                foreach(var item in childrenAutomata)
                {
                    start.AddJump(epsilon, EdgeDirection.Forward, item.Start);
                    item.Finish.AddJump(epsilon, EdgeDirection.Forward, finish);
                    if(choice.MaxOccurs != 1)
                        finish.AddJump(epsilon, EdgeDirection.Backward, item.Start);
                    if(counter != null)
                        item.Start.entersCounterToUpdate = counter;
                }
                if(choice.MinOccurs == 0)
                    start.AddJump(epsilon, EdgeDirection.Forward, finish);
            }
            else
                throw new InvalidOperationException();
            return new NonDeterministicAutomaton(start, finish);
        }

        private enum EdgeDirection
        {
            Forward,
            Backward
        }

        private readonly ISchemaSimpleTypeExecutorFactory schemaSimpleTypeExecutorFactory;

        private class EntersCountersSet : IEnumerable<AutomatonNodeEntersCounter>
        {
            public EntersCountersSet(EntersCountersSet other)
            {
                if(other == null)
                    return;
                foreach(var counter in other.set)
                    set.Add(counter);
            }

            public EntersCountersSet(params AutomatonNodeEntersCounter[] counters)
            {
                foreach(var counter in counters.Where(counter => counter != null))
                    set.Add(counter);
            }

            public IEnumerator<AutomatonNodeEntersCounter> GetEnumerator()
            {
                return set.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(AutomatonNodeEntersCounter counter)
            {
                if(counter != null)
                    set.Add(counter);
            }

            public void Remove(AutomatonNodeEntersCounter counter)
            {
                if(counter != null)
                    set.Remove(counter);
            }

            public bool Contains(AutomatonNodeEntersCounter counter)
            {
                return counter != null && set.Contains(counter);
            }

            public bool Equals(EntersCountersSet other)
            {
                if(other == null)
                    return false;
                return set.Count == other.set.Count && set.All(counter => other.set.Contains(counter));
            }

            public static bool Equals(EntersCountersSet first, EntersCountersSet second)
            {
                if(first == null)
                    return second == null || second.set.Count == 0;
                if(second == null)
                    return first.set.Count == 0;
                return first.Equals(second);
            }

            private readonly HashSet<AutomatonNodeEntersCounter> set = new HashSet<AutomatonNodeEntersCounter>();
        }

        private class FDAJump
        {
            public FDAJump([NotNull] FDANode node)
            {
                if(node == null)
                    throw new ArgumentNullException("node");
                Node = node;
            }

            [NotNull]
            public FDANode Node { get; private set; }

            public void AddEntersCounterForUpdate(AutomatonNodeEntersCounter counter)
            {
                entersCountersToUpdate.Add(counter);
            }

            public void AddEntersCounterForCheck(AutomatonNodeEntersCounter counter)
            {
                entersCountersToCheck.Add(counter);
            }

            public readonly HashSet<AutomatonNodeEntersCounter> entersCountersToCheck = new HashSet<AutomatonNodeEntersCounter>();
            public readonly HashSet<AutomatonNodeEntersCounter> entersCountersToUpdate = new HashSet<AutomatonNodeEntersCounter>();
        }

        private class FDANode
        {
            public FDANode(NDANode ndaNode, ISchemaSimpleTypeExecutorFactory schemaSimpleTypeExecutorFactory)
            {
                Id = ndaNode.Id;
                Depth = ndaNode.Depth;
                allowedAttributes = ReadonlyHashtable.Create(ndaNode.allowedAttributes ?? new Dictionary<string, SchemaSimpleType>(),
                                                             type => type == null ? null : schemaSimpleTypeExecutorFactory.Build(type));
                requiredAttributes = ReadonlyHashtable.Create(ndaNode.requiredAttributes ?? new Dictionary<string, int>());
                innerTextSchemaSimpleTypeExecutor = ndaNode.innerTextType == null ? null : schemaSimpleTypeExecutorFactory.Build(ndaNode.innerTextType);
                anyType = ndaNode.anyType;
                ignoreWhitespaces = ndaNode.ignoreWhitespaces;
            }

            public int Id { get; private set; }
            public int Depth { get; private set; }
            public readonly IReadonlyHashtable<ISchemaSimpleTypeExecutor> allowedAttributes;
            public readonly bool anyType;
            public bool ignoreWhitespaces;
            public readonly ISchemaSimpleTypeExecutor innerTextSchemaSimpleTypeExecutor;
            public readonly Dictionary<string, FDAJump> jumps = new Dictionary<string, FDAJump>();
            public readonly IReadonlyHashtable<int> requiredAttributes;
            public FDANode correspondingOpeningNode;
        }

        private class Ids
        {
            public int counterId;
            public int nodeId;
            public int requiredAttributeId;
        }

        private class JumpToNextConsistentState
        {
            public JumpToNextConsistentState([NotNull] FDANode node, [NotNull] List<AutomatonNodeEntersCounter> countersToReset)
            {
                Node = node;
                CountersToReset = countersToReset;
            }

            [NotNull]
            public FDANode Node { get; private set; }

            [NotNull]
            public List<AutomatonNodeEntersCounter> CountersToReset { get; private set; }
        }

        private class NDAEdge
        {
            public NDAEdge([NotNull] string letter, EdgeDirection direction)
            {
                Letter = letter;
                Direction = direction;
            }

            [NotNull]
            public string Letter { get; private set; }

            public EdgeDirection Direction { get; private set; }

            public override bool Equals(object obj)
            {
                var other = obj as NDAEdge;
                if(other == null)
                    return false;
                return other.Letter == Letter && other.Direction == Direction;
            }

            public override int GetHashCode()
            {
                return unchecked ((int)Direction * 314159265 + Letter.GetHashCode());
            }
        }

        private class NDAEmptyJump
        {
            public NDAEmptyJump(EdgeDirection direction, [NotNull] NDANode node)
            {
                Direction = direction;
                Node = node;
            }

            public EdgeDirection Direction { get; private set; }

            [NotNull]
            public NDANode Node { get; private set; }
        }

        private class NDANode
        {
            public NDANode(int id, int depth)
            {
                Id = id;
                Depth = depth;
            }

            public int Id { get; private set; }
            public int Depth { get; private set; }

            public void AddJump([NotNull] string letter, EdgeDirection direction, [NotNull] NDANode node)
            {
                if(string.IsNullOrEmpty(letter))
                    emptyJumps.Add(new NDAEmptyJump(direction, node));
                else
                    notEmptyJumps.Add(new NDAEdge(letter, direction), node);
            }

            public readonly List<NDAEmptyJump> emptyJumps = new List<NDAEmptyJump>();
            public readonly Dictionary<NDAEdge, NDANode> notEmptyJumps = new Dictionary<NDAEdge, NDANode>();
            public Dictionary<string, SchemaSimpleType> allowedAttributes;
            public bool anyType;
            public bool ignoreWhitespaces;

            public AutomatonNodeEntersCounter entersCounterToCheck;
            public AutomatonNodeEntersCounter entersCounterToUpdate;
            public SchemaSimpleType innerTextType;
            public Dictionary<string, int> requiredAttributes;
        }

        private class NonDeterministicAutomaton
        {
            public NonDeterministicAutomaton([NotNull] NDANode start, [NotNull] NDANode finish)
            {
                Start = start;
                Finish = finish;
            }

            [NotNull]
            public NDANode Start { get; private set; }

            [NotNull]
            public NDANode Finish { get; private set; }
        }

        private class SchemaAutomaton : ISchemaAutomaton
        {
            public SchemaAutomaton(FDANode start, ConcurrentDictionary<FDANode, JumpToNextConsistentState> nextConsistentStates, int[] counters, bool[] existances)
            {
                this.start = start;
                this.nextConsistentStates = nextConsistentStates;
                this.counters = counters;
                this.existances = existances;
                Reset();
            }

            public bool InAnyTypeState { get { return !inFatalState && current.anyType; } }

            public bool HasText { get { return !inFatalState && current.innerTextSchemaSimpleTypeExecutor != null; } }

            public void SetLineInfo(int lineNumber, int linePosition)
            {
                currentLineNumber = lineNumber;
                currentLinePosition = linePosition;
            }

            [CanBeNull]
            public SchemaAutomatonError StartElement([NotNull] string name)
            {
                SchemaAutomatonError result = null;
                if(!inFatalState)
                {
                    result = MakeJump("+" + name, path.Count == 0 ? null : path.Peek());
                    if(current != null)
                        current.requiredAttributes.ForEach(requiredAttribute => existances[requiredAttribute] = false);
                }
                path.Push(name);
                return result;
            }

            [CanBeNull]
            public SchemaAutomatonError EndElement()
            {
                var top = path.Pop();
                var result = inFatalState ? null : MakeJump("-" + top, top);
                if(inFatalState && current != null && path.Count == current.Depth)
                    inFatalState = false;
                return result;
            }

            public SchemaAutomatonError ReadAttribute([NotNull] string name, [NotNull] string value)
            {
                if(inFatalState)
                    return null;
                ISchemaSimpleTypeExecutor attributeTypeExecutor;
                if(!current.allowedAttributes.TryGetValue(name, out attributeTypeExecutor))
                    return new SchemaAutomatonError.SchemaAutomatonError3(name, currentLineNumber, currentLinePosition);
                int requiredAttributeId;
                if(current.requiredAttributes.TryGetValue(name, out requiredAttributeId))
                    existances[requiredAttributeId] = true;
                return attributeTypeExecutor == null ? null : attributeTypeExecutor.Execute(value, "Атрибут", name, currentLineNumber, currentLinePosition);
            }

            public SchemaAutomatonError ReadText([NotNull] string text)
            {
                if(inFatalState)
                    return null;
                return current.innerTextSchemaSimpleTypeExecutor == null
                           ? new SchemaAutomatonError.SchemaAutomatonError10(path.Peek(), currentLineNumber, currentLinePosition)
                           : current.innerTextSchemaSimpleTypeExecutor.Execute(text, "Элемент", path.Peek(), currentLineNumber, currentLinePosition);
            }

            public SchemaAutomatonError ReadWhitespace([NotNull] string whitespace)
            {
                if(inFatalState)
                    return null;
                if(current.innerTextSchemaSimpleTypeExecutor != null)
                    return current.innerTextSchemaSimpleTypeExecutor.Execute(whitespace, "Элемент", path.Peek(), currentLineNumber, currentLinePosition);
                if(current.ignoreWhitespaces)
                    return null;
                return new SchemaAutomatonError.SchemaAutomatonError13(path.Peek(), currentLineNumber, currentLinePosition);
            }

            public IEnumerable<SchemaAutomatonError> CheckRequiredAttributes()
            {
                if(inFatalState)
                    return new List<SchemaAutomatonError>();
                var result = new List<SchemaAutomatonError>();
                current.requiredAttributes.ForEach((name, id) =>
                    {
                        if(!existances[id])
                            result.Add(new SchemaAutomatonError.SchemaAutomatonError4(name, currentLineNumber, currentLinePosition));
                    });
                return result;
            }

            public void Reset()
            {
                current = start;
                ResetCounters(start);
                inFatalState = false;
            }

            private static JumpToNextConsistentState ComputeJumpToNextConsistentState(FDANode current)
            {
                var desiredDepth = current.Depth - 1;
                FDANode nextConsistentState = null;
                var countersToReset = new List<AutomatonNodeEntersCounter>();
                // Run BFS to find the closing node for the current's parent
                var queue = new Queue<FDANode>();
                var visited = new HashSet<FDANode> {current};
                queue.Enqueue(current);
                while(queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    foreach(var jump in node.jumps.Values)
                    {
                        countersToReset.AddRange(jump.entersCountersToUpdate);
                        countersToReset.AddRange(jump.entersCountersToCheck);
                        var next = jump.Node;
                        if(next.Depth < desiredDepth)
                            throw new InvalidOperationException();
                        if(next.Depth > desiredDepth)
                        {
                            if(!visited.Contains(next))
                            {
                                queue.Enqueue(next);
                                visited.Add(next);
                            }
                        }
                        else
                        {
                            if(nextConsistentState != null && nextConsistentState != next)
                                throw new InvalidOperationException();
                            nextConsistentState = next;
                        }
                    }
                }
                if(nextConsistentState == null)
                    return null;
                return new JumpToNextConsistentState(nextConsistentState, countersToReset);
            }

            private void GotoNextConsistentState()
            {
                var jumpToNextConsistentState = nextConsistentStates.GetOrAdd(current, ComputeJumpToNextConsistentState);
                if(jumpToNextConsistentState == null)
                {
                    current = null;
                    return;
                }
                foreach(var counter in jumpToNextConsistentState.CountersToReset)
                    counters[counter.Id] = 0;
                current = jumpToNextConsistentState.Node;
            }

            private void ResetCounters(FDANode root)
            {
                var queue = new Queue<FDANode>();
                queue.Enqueue(root);
                var visited = new HashSet<FDANode> {root};
                while(queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    foreach(var jump in node.jumps.Values)
                    {
                        foreach(var counter in jump.entersCountersToUpdate)
                            counters[counter.Id] = 0;
                        foreach(var counter in jump.entersCountersToCheck)
                            counters[counter.Id] = 0;
                        if(!visited.Contains(jump.Node))
                        {
                            queue.Enqueue(jump.Node);
                            visited.Add(jump.Node);
                        }
                    }
                }
            }

            private bool SomeHowCheckWhetherTheCorrespondingOpeningNodeAllowsElements()
            {
                if(current.correspondingOpeningNode == null)
                    throw new InvalidOperationException("Corresponding opening node is not set");
                return current.correspondingOpeningNode.jumps.Keys.Any(key => key[0] == '+');
            }

            private SchemaAutomatonError MakeJump([NotNull] string letter, [CanBeNull] string element)
            {
                SchemaAutomatonError result;
                FDAJump jump;
                if(!current.jumps.TryGetValue(letter, out jump))
                {
                    if(letter[0] == '-') // Closing tag
                        result = new SchemaAutomatonError.SchemaAutomatonError0(element, current.jumps.Keys.Select(key => key.Substring(1)).ToArray(), currentLineNumber, currentLinePosition);
                    else
                    {
                        // Opening tag
                        var expectedOpeningTags = current.jumps.Keys.Where(key => key[0] == '+').Select(key => key.Substring(1)).ToArray();
                        if(expectedOpeningTags.Length > 0)
                            result = new SchemaAutomatonError.SchemaAutomatonError1(element, letter.Substring(1), expectedOpeningTags, currentLineNumber, currentLinePosition);
                        else
                        {
                            var some_how_check_whether_the_corresponding_opening_node_allows_elements = SomeHowCheckWhetherTheCorrespondingOpeningNodeAllowsElements();
                            if(some_how_check_whether_the_corresponding_opening_node_allows_elements)
                                result = new SchemaAutomatonError.SchemaAutomatonError2(element, letter.Substring(1), currentLineNumber, currentLinePosition);
                            else
                            {
                                if(current.innerTextSchemaSimpleTypeExecutor != null)
                                    result = new SchemaAutomatonError.SchemaAutomatonError12(element, letter.Substring(1), currentLineNumber, currentLinePosition);
                                else
                                    result = new SchemaAutomatonError.SchemaAutomatonError11(element, letter.Substring(1), currentLineNumber, currentLinePosition);
                            }
                        }
                    }
                    inFatalState = true;
                }
                else
                {
                    result = null;
                    foreach(var counter in jump.entersCountersToUpdate)
                    {
                        var counterValue = ++counters[counter.Id];
                        if(counter.MaxOccurs != null && counterValue > counter.MaxOccurs)
                        {
                            var expectedElements = current.jumps.Keys.Where(key => key[0] == '+').Select(key => key.Substring(1))
                                                          .Where(elementName => !counter.ElementNames.Contains(elementName))
                                                          .ToArray();
                            if(expectedElements.Length > 0)
                                result = new SchemaAutomatonError.SchemaAutomatonError1(element, letter.Substring(1), expectedElements, currentLineNumber, currentLinePosition);
                            else
                                result = new SchemaAutomatonError.SchemaAutomatonError2(element, letter.Substring(1), currentLineNumber, currentLinePosition);
                            inFatalState = true;
                        }
                    }
                    foreach(var counter in jump.entersCountersToCheck)
                    {
                        if(counters[counter.Id] < counter.MinOccurs)
                        {
                            result = new SchemaAutomatonError.SchemaAutomatonError0(element, counter.ElementNames.ToArray(), currentLineNumber, currentLinePosition);
                            inFatalState = true;
                        }
                        counters[counter.Id] = 0;
                    }
                    if(!inFatalState)
                        current = jump.Node;
                }
                if(inFatalState)
                    GotoNextConsistentState();
                return result;
            }

            private readonly int[] counters;
            private readonly bool[] existances;
            private readonly ConcurrentDictionary<FDANode, JumpToNextConsistentState> nextConsistentStates;
            private readonly Stack<string> path = new Stack<string>();
            private readonly FDANode start;
            private FDANode current;
            private bool inFatalState;
            private int currentLineNumber;
            private int currentLinePosition;
        }

        private class VisitedEntersCounters
        {
            public VisitedEntersCounters()
            {
                entersCountersToUpdate = new EntersCountersSet();
                entersCountersToCheck = new EntersCountersSet();
            }

            public VisitedEntersCounters(VisitedEntersCounters other)
            {
                entersCountersToUpdate = new EntersCountersSet(other.entersCountersToUpdate);
                entersCountersToCheck = new EntersCountersSet(other.entersCountersToCheck);
            }

            public bool Equals(VisitedEntersCounters other)
            {
                if(!EntersCountersSet.Equals(entersCountersToUpdate, other.entersCountersToUpdate))
                    return false;
                foreach(var counter in entersCountersToCheck)
                {
                    if(entersCountersToUpdate.Contains(counter) && !(other.entersCountersToCheck.Contains(counter) && other.entersCountersToUpdate.Contains(counter)))
                        return false;
                }
                foreach(var counter in other.entersCountersToCheck)
                {
                    if(other.entersCountersToUpdate.Contains(counter) && !(entersCountersToCheck.Contains(counter) && entersCountersToUpdate.Contains(counter)))
                        return false;
                }
                return true;
            }

            public void AddEntersCounterToUpdate(AutomatonNodeEntersCounter counter)
            {
                entersCountersToUpdate.Add(counter);
            }

            public void AddEntersCounterToCheck(AutomatonNodeEntersCounter counter)
            {
                entersCountersToCheck.Add(counter);
            }

            public readonly EntersCountersSet entersCountersToCheck;
            public readonly EntersCountersSet entersCountersToUpdate;
        }

        #region Debug Output

        private void CountVerticesAndEdges(FDANode node, HashSet<int> visited, out int nodesCount, out int edgesCount)
        {
            visited.Add(node.Id);
            nodesCount = 1;
            edgesCount = 0;
            foreach(var jump in node.jumps.Values)
            {
                int localNodesCount, localEdgesCount;
                //edgesCount += jump.entersCountersToCheck.Count;
                //edgesCount += jump.entersCountersToUpdate.Count;
                //foreach(var counter in jump.entersCountersToCheck.Where(counter => !visited.Contains(counter.Id)))
                //{
                //    ++nodesCount;
                //    visited.Add(counter.Id);
                //}
                //foreach(var counter in jump.entersCountersToUpdate.Where(counter => !visited.Contains(counter.Id)))
                //{
                //    ++nodesCount;
                //    visited.Add(counter.Id);
                //}
                edgesCount++;
                if(visited.Contains(jump.Node.Id))
                    continue;
                CountVerticesAndEdges(jump.Node, visited, out localNodesCount, out localEdgesCount);
                nodesCount += localNodesCount;
                edgesCount += localEdgesCount;
            }
        }

        private void CountVerticesAndEdges(NDANode node, HashSet<int> visited, out int nodesCount, out int edgesCount)
        {
            visited.Add(node.Id);
            nodesCount = 1;
            edgesCount = 0;
            foreach(var emptyJump in node.emptyJumps)
            {
                int localNodesCount, localEdgesCount;
                //edgesCount += jump.entersCountersToCheck.Count;
                //edgesCount += jump.entersCountersToUpdate.Count;
                //foreach(var counter in jump.entersCountersToCheck.Where(counter => !visited.Contains(counter.Id)))
                //{
                //    ++nodesCount;
                //    visited.Add(counter.Id);
                //}
                //foreach(var counter in jump.entersCountersToUpdate.Where(counter => !visited.Contains(counter.Id)))
                //{
                //    ++nodesCount;
                //    visited.Add(counter.Id);
                //}
                edgesCount++;
                if(visited.Contains(emptyJump.Node.Id))
                    continue;
                CountVerticesAndEdges(emptyJump.Node, visited, out localNodesCount, out localEdgesCount);
                nodesCount += localNodesCount;
                edgesCount += localEdgesCount;
            }
            foreach(var jump in node.notEmptyJumps)
            {
                int localNodesCount, localEdgesCount;
                //edgesCount += jump.entersCountersToCheck.Count;
                //edgesCount += jump.entersCountersToUpdate.Count;
                //foreach(var counter in jump.entersCountersToCheck.Where(counter => !visited.Contains(counter.Id)))
                //{
                //    ++nodesCount;
                //    visited.Add(counter.Id);
                //}
                //foreach(var counter in jump.entersCountersToUpdate.Where(counter => !visited.Contains(counter.Id)))
                //{
                //    ++nodesCount;
                //    visited.Add(counter.Id);
                //}
                edgesCount++;
                if(visited.Contains(jump.Value.Id))
                    continue;
                CountVerticesAndEdges(jump.Value, visited, out localNodesCount, out localEdgesCount);
                nodesCount += localNodesCount;
                edgesCount += localEdgesCount;
            }
        }

        private string PrintNDA(NDANode start)
        {
            var visited = new HashSet<int>();
            int nodesCount, edgesCount;
            CountVerticesAndEdges(start, visited, out nodesCount, out edgesCount);
            var result = new StringBuilder();
            result.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
            result.AppendLine(@"<graphml xmlns=""http://graphml.graphdrawing.org/xmlns"">");
            result.AppendLine(string.Format(@"<graph id=""zzz"" edgedefault=""directed"" parse.nodes=""{0}"" parse.edges=""{1}"" parse.order=""nodesfirst"" parse.nodeids=""free"" parse.edgeids=""free"">", nodesCount, edgesCount));
            visited.Clear();
            PrintVertices(start, visited, result);
            visited.Clear();
            PrintEdges(start, visited, result);
            result.AppendLine("</graph>");
            result.AppendLine("</graphml>");
            return result.ToString();
        }

        private string PrintFDA(FDANode start)
        {
            var visited = new HashSet<int>();
            int nodesCount, edgesCount;
            CountVerticesAndEdges(start, visited, out nodesCount, out edgesCount);
            var result = new StringBuilder();
            result.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
            result.AppendLine(@"<graphml xmlns=""http://graphml.graphdrawing.org/xmlns"">");
            result.AppendLine(string.Format(@"<graph id=""zzz"" edgedefault=""directed"" parse.nodes=""{0}"" parse.edges=""{1}"" parse.order=""nodesfirst"" parse.nodeids=""free"" parse.edgeids=""free"">", nodesCount, edgesCount));
            visited.Clear();
            PrintVertices(start, visited, result);
            visited.Clear();
            PrintEdges(start, visited, result);
            result.AppendLine("</graph>");
            result.AppendLine("</graphml>");
            return result.ToString();
        }

        private void PrintEdges(FDANode node, HashSet<int> visited, StringBuilder result)
        {
            visited.Add(node.Id);
            foreach(var jump in node.jumps)
            {
                //foreach(var counter in jump.Value.entersCountersToUpdate)
                //    result.AppendLine(string.Format(@"    <edge id=""{0}"" source=""S{1}"" target=""C{2}"" />", "update", node.Id, counter.Id));
                //foreach(var counter in jump.Value.entersCountersToCheck)
                //    result.AppendLine(string.Format(@"    <edge id=""{0}"" source=""S{1}"" target=""C{2}"" />", "check", node.Id, counter.Id));
                result.AppendLine(string.Format(@"    <edge id=""{0}"" source=""S{1}"" target=""S{2}"" />", jump.Key, node.Id, jump.Value.Node.Id));
                if(!visited.Contains(jump.Value.Node.Id))
                    PrintEdges(jump.Value.Node, visited, result);
            }
        }

        private void PrintEdges(NDANode node, HashSet<int> visited, StringBuilder result)
        {
            visited.Add(node.Id);
            foreach(var emptyJump in node.emptyJumps)
            {
                //foreach(var counter in jump.Value.entersCountersToUpdate)
                //    result.AppendLine(string.Format(@"    <edge id=""{0}"" source=""S{1}"" target=""C{2}"" />", "update", node.Id, counter.Id));
                //foreach(var counter in jump.Value.entersCountersToCheck)
                //    result.AppendLine(string.Format(@"    <edge id=""{0}"" source=""S{1}"" target=""C{2}"" />", "check", node.Id, counter.Id));
                result.AppendLine(string.Format(@"    <edge id=""{0}"" source=""S{1}"" target=""S{2}"" direction=""{3}"" />", "epsilon", node.Id, emptyJump.Node.Id, emptyJump.Direction));
                if(!visited.Contains(emptyJump.Node.Id))
                    PrintEdges(emptyJump.Node, visited, result);
            }
            foreach(var jump in node.notEmptyJumps)
            {
                //foreach(var counter in jump.Value.entersCountersToUpdate)
                //    result.AppendLine(string.Format(@"    <edge id=""{0}"" source=""S{1}"" target=""C{2}"" />", "update", node.Id, counter.Id));
                //foreach(var counter in jump.Value.entersCountersToCheck)
                //    result.AppendLine(string.Format(@"    <edge id=""{0}"" source=""S{1}"" target=""C{2}"" />", "check", node.Id, counter.Id));
                result.AppendLine(string.Format(@"    <edge id=""{0}"" source=""S{1}"" target=""S{2}"" direction=""{3}"" />", jump.Key.Letter, node.Id, jump.Value.Id, jump.Key.Direction));
                if(!visited.Contains(jump.Value.Id))
                    PrintEdges(jump.Value, visited, result);
            }
        }

        private void PrintVertices(FDANode node, HashSet<int> visited, StringBuilder result)
        {
            visited.Add(node.Id);
            result.AppendLine(string.Format(@"    <node id=""S{0}"" depth=""{1}"" ignoreWhitespaces=""{2}""/>", node.Id, node.Depth, node.ignoreWhitespaces));
            foreach(var jump in node.jumps.Values)
            {
                //foreach (var counter in jump.entersCountersToCheck.Where(counter => !visited.Contains(counter.Id)))
                //{
                //    result.AppendLine(string.Format(@"    <node id=""C{0}"" />", counter.Id));
                //    visited.Add(counter.Id);
                //}
                //foreach (var counter in jump.entersCountersToUpdate.Where(counter => !visited.Contains(counter.Id)))
                //{
                //    result.AppendLine(string.Format(@"    <node id=""C{0}"" />", counter.Id));
                //    visited.Add(counter.Id);
                //}
                if(!visited.Contains(jump.Node.Id))
                    PrintVertices(jump.Node, visited, result);
            }
        }

        private void PrintVertices(NDANode node, HashSet<int> visited, StringBuilder result)
        {
            visited.Add(node.Id);
            result.AppendLine(string.Format(@"    <node id=""S{0}"" depth=""{1}"" ignoreWhitespaces=""{2}"" {3}{4}/>", node.Id, node.Depth, node.ignoreWhitespaces, node.entersCounterToUpdate == null ? "" : @"entersCounterToUpdate=""" + node.entersCounterToUpdate.Id + @"""", node.entersCounterToCheck == null ? "" : @" entersCounterToCheck=""" + node.entersCounterToCheck.Id + @""""));
            foreach(var emptyJump in node.emptyJumps)
            {
                //foreach (var counter in jump.entersCountersToCheck.Where(counter => !visited.Contains(counter.Id)))
                //{
                //    result.AppendLine(string.Format(@"    <node id=""C{0}"" />", counter.Id));
                //    visited.Add(counter.Id);
                //}
                //foreach (var counter in jump.entersCountersToUpdate.Where(counter => !visited.Contains(counter.Id)))
                //{
                //    result.AppendLine(string.Format(@"    <node id=""C{0}"" />", counter.Id));
                //    visited.Add(counter.Id);
                //}
                if(!visited.Contains(emptyJump.Node.Id))
                    PrintVertices(emptyJump.Node, visited, result);
            }
            foreach(var jump in node.notEmptyJumps)
            {
                //foreach (var counter in jump.entersCountersToCheck.Where(counter => !visited.Contains(counter.Id)))
                //{
                //    result.AppendLine(string.Format(@"    <node id=""C{0}"" />", counter.Id));
                //    visited.Add(counter.Id);
                //}
                //foreach (var counter in jump.entersCountersToUpdate.Where(counter => !visited.Contains(counter.Id)))
                //{
                //    result.AppendLine(string.Format(@"    <node id=""C{0}"" />", counter.Id));
                //    visited.Add(counter.Id);
                //}
                if(!visited.Contains(jump.Value.Id))
                    PrintVertices(jump.Value, visited, result);
            }
        }

        #endregion
    }
}