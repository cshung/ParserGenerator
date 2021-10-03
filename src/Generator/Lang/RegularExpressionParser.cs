namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;
    using System.Linq;

    public class RegularExpressionParser
    {
        Terminal c = new Terminal { DisplayName = "char" };
        Terminal pipe = new Terminal { DisplayName = "|" };
        Terminal star = new Terminal { DisplayName = "*" };
        Terminal lp = new Terminal { DisplayName = "(" };
        Terminal rp = new Terminal { DisplayName = ")" };
        Terminal lb = new Terminal { DisplayName = "[" };
        Terminal rb = new Terminal { DisplayName = "]" };
        Terminal hyphen = new Terminal { DisplayName = "-" };
        Terminal caret = new Terminal { DisplayName = "^" };

        NonTerminal re = new NonTerminal { DisplayName = "RE" };
        NonTerminal reu = new NonTerminal { DisplayName = "REu" };
        NonTerminal rec = new NonTerminal { DisplayName = "REc" };
        NonTerminal cs = new NonTerminal { DisplayName = "CS" };
        NonTerminal csu = new NonTerminal { DisplayName = "CSu" };
        Parser parser;

        public RegularExpressionParser()
        {
            Grammar grammar = new Grammar
            {
                Goal = re,
                Productions = new List<Production>
                {
                    new Production { From = re, To = new List<Symbol> { reu }, SemanticAction = (a) => a[0] },
                    new Production { From = re, To = new List<Symbol> { reu, pipe, re }, SemanticAction = (a) => new UnionRegularExpression { Left = (RegularExpression)a[0], Right = (RegularExpression)a[2] }},
                    new Production { From = reu, To = new List<Symbol> { rec }, SemanticAction = (a) => a[0] },
                    new Production { From = reu, To = new List<Symbol> { rec, reu }, SemanticAction = (a) => new ConcatenateRegularExpression { Left = (RegularExpression)a[0], Right = (RegularExpression)a[1] } },
                    new Production { From = rec, To = new List<Symbol> { c }, SemanticAction = (a) => new CharSetRegularExpression { CharSet = new ExplicitCharacterClass { Elements = { (char)a[0] } } } },
                    new Production { From = rec, To = new List<Symbol> { lb, cs, rb }, SemanticAction = (a) => new CharSetRegularExpression { CharSet = (CharacterClass)a[1] } },
                    new Production { From = rec, To = new List<Symbol> { lb, caret, cs, rb }, SemanticAction = (a) => new CharSetRegularExpression { CharSet = new Complement { Operand = (CharacterClass)a[2] } } },
                    new Production { From = rec, To = new List<Symbol> { rec, star }, SemanticAction = (a) => new KleeneStarRegularExpression { Operand = (RegularExpression) a[0] } },
                    new Production { From = rec, To = new List<Symbol> { lp, re, rp }, SemanticAction = (a) => a[1] },
                    new Production { From = cs, To = new List<Symbol> { csu }, SemanticAction = (a) => a[0] },
                    new Production { From = cs, To = new List<Symbol> { csu, cs }, SemanticAction = (a) => new Union { Left = (CharacterClass)a[0], Right = (CharacterClass)a[1] } },
                    new Production { From = csu, To = new List<Symbol> { c, hyphen, c }, SemanticAction = (a) => new RangeCharacterClass { From = (char)a[0], To = (char)a[2] } },
                    new Production { From = csu, To = new List<Symbol> { c }, SemanticAction = (a) => new ExplicitCharacterClass { Elements = { (char)a[0] } } },
                }
            };
            this.parser = new ParserGenerator(grammar, ParserMode.SLR).Generate();
        }

        public RegularExpression Parse(string s)
        {
            // A very simple scanner (which do not handle escape)
            IEnumerable<Token> tokens = s.Select(d =>
            {
                switch (d)
                {
                    case '(': return new Token { Symbol = lp };
                    case ')': return new Token { Symbol = rp };
                    case '*': return new Token { Symbol = star };
                    case '|': return new Token { Symbol = pipe };
                    case '[': return new Token { Symbol = lb };
                    case ']': return new Token { Symbol = rb };
                    case '-': return new Token { Symbol = hyphen };
                    case '^': return new Token { Symbol = caret };
                    default: return new Token { Symbol = c, SemanticValue = d };
                }
            });

            return (RegularExpression)parser.Parse(tokens);
        }
    }
}
