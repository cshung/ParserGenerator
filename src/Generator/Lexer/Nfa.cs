namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

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

        private List<Tuple<LeafCharacterClass, HashSet<NfaNode>>> DfaNeighbors(HashSet<NfaNode> dfaStart)
        {
            var outgoingNonEpsilonEdges = new Dictionary<LeafCharacterClass, HashSet<NfaNode>>();
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

            List<Tuple<LeafCharacterClass, HashSet<NfaNode>>> result = new List<Tuple<LeafCharacterClass, HashSet<NfaNode>>>();
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

            // Note that subset construction does not necessarily build a minimized DFA
            // TODO: Minimize the generated DFA

            dfaNodes.Add(dfaStart, new DfaNode { IsFinal = dfaStart.Contains(this.EndNode) });
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

        public string ToGraphViz()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"digraph finite_state_machine {
    rankdir=LR;
    size=""8,5""

    node[shape = doublecircle]; ");            
            sb.AppendLine("    node[shape = circle];");
            foreach (var edge in this.Edges)
            {
                sb.Append("    ");
                sb.Append("S");
                sb.Append(edge.SourceNode.Id);
                sb.Append(" -> ");
                sb.Append("S");
                sb.Append(edge.TargetNode.Id);
                sb.Append(@" [ label = """);
                sb.Append(edge.Symbol);
                sb.AppendLine(@""" ];");
            }
            sb.Append("}");

            return sb.ToString();
        }
    }
}