namespace Andrew.ParserGenerator
{
    using System.Diagnostics;

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
}