namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;

    public class UnionRegularExpression : RegularExpression
    {
        public RegularExpression Left { get; set; }
        public RegularExpression Right { get; set; }

        public override void FindCharacterSets(List<CharacterClass> solution)
        {
            this.Left.FindCharacterSets(solution);
            this.Right.FindCharacterSets(solution);
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
}