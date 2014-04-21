namespace Andrew.ParserGenerator
{
    using System.Diagnostics;

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
}