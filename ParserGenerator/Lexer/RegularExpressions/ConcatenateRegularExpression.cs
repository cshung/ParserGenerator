namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;

    public class ConcatenateRegularExpression : RegularExpression
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
}