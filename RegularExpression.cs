namespace ParsingLab2
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    class LexicalAnalyzer
    {
        public List<Tuple<RegularExpression, Action<string>>> Specification { get; set; }

        public void Analyze(string s)
        {
            while (s.Length > 0)
            {
                List<Tuple<CompiledRegularExpression, Action<string>>> compiledSpecification = this.Specification.Select(spec => Tuple.Create(spec.Item1.Compile(), spec.Item2)).ToList();
                List<Tuple<string, Action<string>>> matches = compiledSpecification.Select(c => Tuple.Create(c.Item1.LongestMatch(s), c.Item2)).Where(m => m.Item1 != null).ToList();
                if (matches.Count != 0)
                {
                    int maximalLength = matches.Select(m => m.Item1.Length).Max();
                    Tuple<string, Action<string>> match = matches.First(m => m.Item1.Length == maximalLength);
                    match.Item2(match.Item1);
                    s = s.Substring(maximalLength);
                }
                else
                {
                    Console.WriteLine("Input matches no rule");
                    break;
                }
            }
        }
    }

    class SetComparer<T> : IEqualityComparer<HashSet<T>>
    {
        public bool Equals(HashSet<T> set1, HashSet<T> set2)
        {
            return set1.Except(set2).Count() == 0 && set2.Except(set1).Count() == 0;
        }

        public int GetHashCode(HashSet<T> obj)
        {
            // TODO: Don't defeat hashing?
            return 0;
        }
    }

    public class Dfa
    {
        // TODO: Index the edges by node
        public DfaNode StartNode;
        public List<DfaNode> Nodes;
        public List<DfaEdge> Edges;
    }

    [DebuggerDisplay("{id}")]
    public class DfaNode
    {
        private int id;
        private static int counter = 0;
        public DfaNode()
        {
            this.id = counter++;
        }

        public bool IsFinal { get; set; }
    }

    [DebuggerDisplay("{SourceNode.id} -{Symbol}-> {TargetNode.id}")]
    public class DfaEdge
    {
        public DfaNode SourceNode { get; set; }
        public LeafSet Symbol { get; set; }
        public DfaNode TargetNode { get; set; }
    }

    public class Nfa
    {
        // TODO: Index the edges by node
        public NfaNode StartNode { get; set; }
        public NfaNode EndNode { get; set; }
        public List<NfaNode> Nodes { get; set; }
        public List<NfaEdge> Edges { get; set; }

        private HashSet<NfaNode> EpsilonClosure(HashSet<NfaNode> input)
        {
            HashSet<NfaNode> result = new HashSet<NfaNode>();
            Queue<NfaNode> pending = new Queue<NfaNode>();
            foreach (var i in input)
            {
                pending.Enqueue(i);
                result.Add(i);
            }
            while (pending.Count() > 0)
            {
                NfaNode current = pending.Dequeue();
                foreach (var edge in Edges)
                {
                    if (edge.Symbol == null && edge.SourceNode == current)
                    {
                        if (!result.Contains(edge.TargetNode))
                        {
                            result.Add(edge.TargetNode);
                            pending.Enqueue(edge.TargetNode);
                        }
                    }
                }
            }

            return result;
        }

        private List<Tuple<LeafSet, HashSet<NfaNode>>> DfaNeighbors(HashSet<NfaNode> dfaStart)
        {
            var outgoingNonEpsilonEdges = new Dictionary<LeafSet, HashSet<NfaNode>>();
            foreach (var node in dfaStart)
            {
                foreach (var edge in this.Edges)
                {
                    if (edge.Symbol != null && edge.SourceNode == node)
                    {
                        HashSet<NfaNode> outgoingNodes;
                        if (!outgoingNonEpsilonEdges.TryGetValue(edge.Symbol, out outgoingNodes))
                        {
                            outgoingNodes = new HashSet<NfaNode>();
                            outgoingNonEpsilonEdges.Add(edge.Symbol, outgoingNodes);
                        }

                        outgoingNodes.Add(edge.TargetNode);
                    }
                }
            }

            List<Tuple<LeafSet, HashSet<NfaNode>>> result = new List<Tuple<LeafSet, HashSet<NfaNode>>>();
            foreach (var outgoingNonEpsilonEdge in outgoingNonEpsilonEdges)
            {
                result.Add(Tuple.Create(outgoingNonEpsilonEdge.Key, EpsilonClosure(outgoingNonEpsilonEdge.Value)));
            }
            return result;
        }

        public Dfa Build()
        {
            HashSet<NfaNode> dfaStart = this.EpsilonClosure(new HashSet<NfaNode> { this.StartNode });

            Dictionary<HashSet<NfaNode>, DfaNode> dfaNodes = new Dictionary<HashSet<NfaNode>, DfaNode>(new SetComparer<NfaNode>());
            List<DfaEdge> dfaEdges = new List<DfaEdge>();
            Queue<HashSet<NfaNode>> pending = new Queue<HashSet<NfaNode>>();

            dfaNodes.Add(dfaStart, new DfaNode());
            pending.Enqueue(dfaStart);
            while (pending.Count > 0)
            {
                HashSet<NfaNode> current = pending.Dequeue();
                var neighbors = this.DfaNeighbors(current);
                foreach (var neighbor in neighbors)
                {
                    DfaNode targetNode;
                    if (!dfaNodes.TryGetValue(neighbor.Item2, out targetNode))
                    {
                        targetNode = new DfaNode { IsFinal = neighbor.Item2.Contains(this.EndNode) };
                        dfaNodes.Add(neighbor.Item2, targetNode);
                        pending.Enqueue(neighbor.Item2);
                    }
                    dfaEdges.Add(new DfaEdge { SourceNode = dfaNodes[current], Symbol = neighbor.Item1, TargetNode = targetNode });
                }
            }

            Dfa result = new Dfa
            {
                StartNode = dfaNodes[dfaStart],
                Nodes = new List<DfaNode>(dfaNodes.Values),
                Edges = dfaEdges
            };
            return result;
        }
    }

    [DebuggerDisplay("{id}")]
    public class NfaNode
    {
        private int id;
        private static int counter = 0;
        public NfaNode()
        {
            this.id = counter++;
        }
    }

    [DebuggerDisplay("{SourceNode.id} -{Symbol}-> {TargetNode.id}")]
    public class NfaEdge
    {
        public NfaNode SourceNode { get; set; }
        public LeafSet Symbol { get; set; } // null mean epsilon
        public NfaNode TargetNode { get; set; }
    }

    public class CompiledRegularExpression
    {
        private List<LeafSet> atoms;
        private Dfa dfa;

        public CompiledRegularExpression(List<LeafSet> atoms, Dfa dfa)
        {
            this.atoms = atoms;
            this.dfa = dfa;
        }

        public bool Match(string s)
        {
            DfaNode state = dfa.StartNode;
            bool rejected = false;
            foreach (char c in s)
            {
                var atom = atoms.First(a => a.Contains(c));
                DfaEdge found = null;
                foreach (var edge in dfa.Edges)
                {
                    if (edge.SourceNode == state && edge.Symbol == atom)
                    {
                        found = edge;
                        break;
                    }
                }
                if (found != null)
                {
                    state = found.TargetNode;
                }
                else
                {
                    rejected = true;
                    break;
                }
            }

            return rejected ? false : state.IsFinal;
        }

        public string LongestMatch(string s)
        {
            DfaNode state = dfa.StartNode;
            int bestMatch = -1;
            int count = 0;
            bool rejected = false;
            foreach (char c in s)
            {
                count++;
                var atom = atoms.First(a => a.Contains(c));
                DfaEdge found = null;
                foreach (var edge in dfa.Edges)
                {
                    if (edge.SourceNode == state && edge.Symbol == atom)
                    {
                        found = edge;
                        break;
                    }
                }
                if (found != null)
                {
                    state = found.TargetNode;
                    if (state.IsFinal)
                    {
                        bestMatch = count;
                    }
                }
                else
                {
                    rejected = true;
                    break;
                }
            }
            if (!rejected && state.IsFinal)
            {
                bestMatch = count;
            }
            if (bestMatch == -1)
            {
                return null;
            }
            else
            {
                return s.Substring(0, bestMatch);
            }
        }
    }

    public abstract class RegularExpression
    {
        public abstract void FindSets(List<Set> solution);
        public abstract Nfa Build();

        public CompiledRegularExpression Compile()
        {
            // Step 1: Find all sets and breakdown into atoms
            List<Set> sets = new List<Set>();
            this.FindSets(sets);
            List<LeafSet> atoms = SetManager.BreakDown(sets);

            // Step 2: Build NFA
            Nfa nfa = this.Build();

            // Step 3: Build DFA
            Dfa dfa = nfa.Build();

            // Step 4: Bundle and return
            return new CompiledRegularExpression(atoms, dfa);
        }
    }

    public class CharSetRegularExpression : RegularExpression
    {
        public Set CharSet { get; set; }

        public override void FindSets(List<Set> solution)
        {
            solution.Add(this.CharSet);
        }

        public override Nfa Build()
        {
            NfaNode startNode = new NfaNode();
            NfaNode endNode = new NfaNode();
            return new Nfa
            {
                StartNode = startNode,
                EndNode = endNode,
                Nodes = new List<NfaNode> { startNode, endNode },
                Edges = new List<NfaEdge>(this.CharSet.Atoms.Select(a => new NfaEdge { SourceNode = startNode, Symbol = a, TargetNode = endNode })),
            };
        }
    }

    public class UnionRegularExpression : RegularExpression
    {
        public RegularExpression Left { get; set; }
        public RegularExpression Right { get; set; }

        public override void FindSets(List<Set> solution)
        {
            this.Left.FindSets(solution);
            this.Right.FindSets(solution);
        }

        public override Nfa Build()
        {
            NfaNode startNode = new NfaNode();
            NfaNode endNode = new NfaNode();
            Nfa left = this.Left.Build();
            Nfa right = this.Right.Build();
            Nfa result = new Nfa();
            result.StartNode = startNode;
            result.EndNode = endNode;
            result.Nodes = new List<NfaNode>();
            result.Nodes.Add(startNode);
            result.Nodes.Add(endNode);
            result.Nodes.AddRange(left.Nodes);
            result.Nodes.AddRange(right.Nodes);
            result.Edges = new List<NfaEdge>();
            result.Edges.Add(new NfaEdge { SourceNode = startNode, TargetNode = left.StartNode });
            result.Edges.Add(new NfaEdge { SourceNode = startNode, TargetNode = right.StartNode });
            result.Edges.Add(new NfaEdge { SourceNode = left.EndNode, TargetNode = endNode });
            result.Edges.Add(new NfaEdge { SourceNode = right.EndNode, TargetNode = endNode });
            result.Edges.AddRange(left.Edges);
            result.Edges.AddRange(right.Edges);
            return result;
        }
    }

    public class ConcatenateRegularExpression : RegularExpression
    {
        public RegularExpression Left { get; set; }
        public RegularExpression Right { get; set; }

        public override void FindSets(List<Set> solution)
        {
            this.Left.FindSets(solution);
            this.Right.FindSets(solution);
        }

        public override Nfa Build()
        {
            Nfa left = this.Left.Build();
            Nfa right = this.Right.Build();
            Nfa result = new Nfa();
            result.StartNode = left.StartNode;
            result.EndNode = right.EndNode;
            result.Nodes = new List<NfaNode>();
            result.Nodes.AddRange(left.Nodes);
            result.Nodes.AddRange(right.Nodes);
            result.Edges = new List<NfaEdge>();
            result.Edges.Add(new NfaEdge { SourceNode = left.EndNode, TargetNode = right.StartNode });
            result.Edges.AddRange(left.Edges);
            result.Edges.AddRange(right.Edges);
            return result;
        }
    }

    public class KleeneStarRegularExpression : RegularExpression
    {
        public RegularExpression Operand { get; set; }

        public override void FindSets(List<Set> solution)
        {
            this.Operand.FindSets(solution);
        }

        public override Nfa Build()
        {
            Nfa operand = this.Operand.Build();
            Nfa result = new Nfa();
            NfaNode startNode = new NfaNode();
            NfaNode endNode = new NfaNode();
            result.StartNode = startNode;
            result.EndNode = endNode;
            result.Nodes = new List<NfaNode>();
            result.Nodes.Add(startNode);
            result.Nodes.Add(endNode);
            result.Nodes.AddRange(operand.Nodes);
            result.Edges = new List<NfaEdge>();
            result.Edges.Add(new NfaEdge { SourceNode = startNode, TargetNode = operand.StartNode });
            result.Edges.Add(new NfaEdge { SourceNode = operand.EndNode, TargetNode = endNode });
            result.Edges.Add(new NfaEdge { SourceNode = operand.EndNode, TargetNode = operand.StartNode });
            result.Edges.AddRange(operand.Edges);
            return result;
        }
    }
}