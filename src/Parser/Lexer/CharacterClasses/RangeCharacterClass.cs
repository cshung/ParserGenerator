namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;
    using System.Diagnostics;

    [DebuggerDisplay("{ToString()}")]
    public class RangeCharacterClass : LeafCharacterClass
    {
        public char From { get; set; }

        public char To { get; set; }

        public override bool Contains(char c)
        {
            return c >= this.From && c <= this.To;
        }

        public override string ToString()
        {
            return string.Format("{0} -> {1}", From, To);
        }
    }
}