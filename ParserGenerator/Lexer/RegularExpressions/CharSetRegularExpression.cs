namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;
    using System.Linq;

    public class CharSetRegularExpression : RegularExpression
    {
        public CharacterClass CharSet { get; set; }

        public override void FindCharacterSets(List<CharacterClass> solution)
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
}