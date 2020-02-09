namespace Andrew.ParserGenerator
{
    using System.Diagnostics;

    [DebuggerDisplay("{SourceNode.id} -{Symbol}-> {TargetNode.id}")]
    public class NfaEdge
    {
        public NfaNode SourceNode { get; set; }
        public LeafCharacterClass Symbol { get; set; } // null mean epsilon
        public NfaNode TargetNode { get; set; }
    }
}