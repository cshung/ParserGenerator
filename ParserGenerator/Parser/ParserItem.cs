namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;
    using System.Linq;

    internal class ParserItem
    {
        public Symbol From { get; set; }
        public List<Symbol> SeenSymbols { get; set; }
        public List<Symbol> ExpectedSymbols { get; set; }
        public Symbol Lookahead { get; set; }

        public override string ToString()
        {
            return From.DisplayName + " -> " + string.Join(" ", SeenSymbols.Select(t => t.DisplayName)) + " . " + string.Join(" ", ExpectedSymbols.Select(t => t.DisplayName));
        }
    }
}
