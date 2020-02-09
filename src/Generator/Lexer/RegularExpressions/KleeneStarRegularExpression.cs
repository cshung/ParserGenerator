namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;

    public class KleeneStarRegularExpression : RegularExpression
    {
        public RegularExpression Operand { get; set; }

        public override void FindCharacterSets(List<CharacterClass> solution)
        {
            this.Operand.FindCharacterSets(solution);
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
            result.Edges.Add(new NfaEdge { SourceNode = operand.StartNode, TargetNode = operand.EndNode });
            result.Edges.Add(new NfaEdge { SourceNode = operand.EndNode, TargetNode = operand.StartNode });
            result.Edges.AddRange(operand.Edges);
            return result;
        }
    }
}