namespace Andrew.ParserGenerator
{
    using System.Diagnostics;

    [DebuggerDisplay("{SourceNode.id} -{Symbol}-> {TargetNode.id}")]
    public class DfaEdge
    {
        public DfaNode SourceNode { get; set; }
        public LeafCharacterClass Symbol { get; set; }
        public DfaNode TargetNode { get; set; }
    }
}