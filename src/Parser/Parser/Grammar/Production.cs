namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;

    public class Production
    {
        public NonTerminal From { get; set; }
        public List<Symbol> To { get; set; }
        public Func<object[], object> SemanticAction { get; set; }
    }
}
