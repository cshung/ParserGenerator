// Practicing subset construction algorithm for regular expression
// Practicing the LR(1) and SLR(1) Parsing Algorithm
// TODO: Top down recursive parser generation
// TODO: Separation of tool set and runtime
// TODO: Abstract subset construction algorithm implementation
namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Program
    {
        public static void Main(string[] args)
        {
            LexicalIdentifierSample();
            ExpressionGrammarSample();
            RegularExpressionSample();
            OperatorSample();
        }

        private static void LexicalIdentifierSample()
        {
            RegularExpression identifier = new ConcatenateRegularExpression
            {
                Left = new CharSetRegularExpression { CharSet = new RangeCharacterClass { From = 'A', To = 'Z' } },
                Right = new KleeneStarRegularExpression
                {
                    Operand = new CharSetRegularExpression
                    {
                        CharSet = new Union
                        {
                            Left = new RangeCharacterClass { From = 'a', To = 'z' },
                            Right = new RangeCharacterClass { From = '0', To = '9' },
                        }
                    }
                }
            };
            RegularExpression whitespace = new KleeneStarRegularExpression
            {
                Operand = new CharSetRegularExpression
                {
                    CharSet = new ExplicitCharacterClass
                    {
                        Elements = { ' ' },
                    },
                },
            };

            LexicalAnalyzer lexicalAnalyzer = new LexicalAnalyzer
            {
                Specification = new List<Tuple<RegularExpression, Action<string>>>
                {
                    Tuple.Create<RegularExpression, Action<string>>(identifier, (s) => { Console.WriteLine(s); }),
                    Tuple.Create<RegularExpression, Action<string>>(whitespace, (s) => { } ), /* Ignore whitespaces */
                    
                }
            };
            lexicalAnalyzer.Analyze("Hello World error");
        }

        private static void ExpressionGrammarSample()
        {
            Grammar grammar = null;

            NonTerminal expr = new NonTerminal { DisplayName = "Expr" };
            NonTerminal term = new NonTerminal { DisplayName = "Term" };
            NonTerminal factor = new NonTerminal { DisplayName = "Factor" };
            Terminal intTerm = new Terminal { DisplayName = "int" };

            Terminal lp = new Terminal { DisplayName = "(" };
            Terminal rp = new Terminal { DisplayName = ")" };
            Terminal add = new Terminal { DisplayName = "+" };
            Terminal sub = new Terminal { DisplayName = "-" };
            Terminal mul = new Terminal { DisplayName = "*" };

            Grammar exprGrammar = new Grammar
            {
                Goal = expr,
                Productions = new List<Production>
                    {
                        new Production { From = expr, To = new List<Symbol> { expr, add, term }, SemanticAction = a => (int)a[0] + (int)a[2] },
                        new Production { From = expr, To = new List<Symbol> { expr, sub, term }, SemanticAction = a => (int)a[0] - (int)a[2] },
                        new Production { From = expr, To = new List<Symbol> { term }, SemanticAction = a => (int)a[0] },
                        new Production { From = term, To = new List<Symbol> { term, mul, factor}, SemanticAction = a => (int)a[0] * (int)a[2] },
                        new Production { From = term, To = new List<Symbol> { factor}, SemanticAction = a => (int)a[0] },
                        new Production { From = factor, To = new List<Symbol> { lp, expr, rp }, SemanticAction = a => (int)a[1] },
                        new Production { From = factor, To = new List<Symbol> { intTerm }, SemanticAction = a => (int)a[0] }
                    }
            };

            grammar = exprGrammar;

            Parser parser = new ParserGenerator(grammar, ParserMode.SLR).Generate();
            object result = parser.Parse(new List<Token> { 
                new Token { Symbol = intTerm, SemanticValue = 1 },
                new Token { Symbol = add, SemanticValue = null },
                new Token { Symbol = intTerm, SemanticValue = 2 },
                new Token { Symbol = mul, SemanticValue = null },
                new Token { Symbol = intTerm, SemanticValue = 3 },
                new Token { Symbol = sub, SemanticValue = null },
                new Token { Symbol = intTerm, SemanticValue = 4 },
            });
            Console.WriteLine(result);
        }

        private static void RegularExpressionSample()
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
            Parser parser = new ParserGenerator(grammar, ParserMode.SLR).Generate();

            string s = "Hel*o|W(or)[^0-9][b-d]";

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

            RegularExpression result = (RegularExpression)parser.Parse(tokens);

            CompiledRegularExpression compiled = result.Compile();
            Console.WriteLine(compiled.Match("Hello"));
            Console.WriteLine(compiled.Match("World"));
        }

        private static void OperatorSample()
        {
            // Step 1: Create a grammar with associativity issue and try to solve it
            Terminal num = new Terminal { DisplayName = "num" };
            Terminal minus = new Terminal { DisplayName = "-" };
            NonTerminal expr = new NonTerminal { DisplayName = "Expr" };
            Grammar grammar = new Grammar
            {
                Goal = expr,
                Productions = new List<Production>
                {
                    new Production { From = expr, To = new List<Symbol> { num }, SemanticAction = (args) => args[0] },
                    new Production { From = expr, To = new List<Symbol> { expr, minus, expr }, SemanticAction = (args) => ((int)args[0]) - ((int)args[2]) } 
                }
            };
            var parser = new ParserGenerator(grammar, ParserMode.LR, new OperatorConflictResolver { Left = { minus } }).Generate();
            var answer = parser.Parse(new List<Token>
            {
                new Token { Symbol = num, SemanticValue = 7 },
                new Token { Symbol = minus, SemanticValue = null },
                new Token { Symbol = num, SemanticValue = 3 },
                new Token { Symbol = minus, SemanticValue = null },
                new Token { Symbol = num, SemanticValue = 2 },
            });
            Console.WriteLine(answer);
        }

        // TODO: Right associative operator
        // TODO: Operator Precedence
        class OperatorConflictResolver : IConflictResolver
        {
            public OperatorConflictResolver()
            {
                this.Left = new HashSet<Terminal>();
            }

            public HashSet<Terminal> Left { get; private set; }

            public bool? ShouldFirstOverrideSecond(ParserItem first, ParserItem second)
            {
                bool isFirstReduce = first.ExpectedSymbols.Count() == 0;
                bool isSecondReduce = second.ExpectedSymbols.Count() == 0;
                if (isFirstReduce && !isSecondReduce)
                {
                    return PreferShiftOrReduce(second, first);
                }
                else if (!isFirstReduce && isSecondReduce)
                {
                    return PreferShiftOrReduce(first, second);
                }
                else
                {
                    // Cannot handle reduce/reduce conflict
                    return null;
                }
            }

            private bool? PreferShiftOrReduce(ParserItem first, ParserItem second)
            {
                // Prefer (T op T ., op) to (T . op T, op) for left associative operator
                foreach (var op in this.Left)
                {
                    // TODO: Make pattern matching more declarative
                    bool firstMatch = first.SeenSymbols.Count() == 1 && first.ExpectedSymbols.Count() == 2 && first.ExpectedSymbols[0] == op && first.ExpectedSymbols[1] == first.SeenSymbols[0];
                    bool secondMatch = second.SeenSymbols.Count() == 3 && second.SeenSymbols[1] == op && second.SeenSymbols[0] == second.SeenSymbols[2];
                    if (firstMatch && secondMatch)
                    {
                        return false;
                    }
                }

                // Other case you don't know
                return null;
            }
        }
    }
}
