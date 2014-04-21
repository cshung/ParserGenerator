namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;

    public class Grammar
    {
        public NonTerminal Goal { get; set; }
        public List<Production> Productions { get; set; }
    }
}
