namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;

    public class Dfa
    {
        // TODO: Index the edges by node
        public DfaNode StartNode { get; set; }
        public List<DfaNode> Nodes { get; set; }
        public List<DfaEdge> Edges { get; set; }
    }
}