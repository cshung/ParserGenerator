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
            ParseRegularExpression();
            OperatorSample();
            GrammarSample();
        }

        private class GrammarParser
        {
            private LexicalAnalyzer lexer;
            private Parser parser;

            public GrammarParser()
            {
                Terminal nonTerminalIdentifier = new Terminal { DisplayName = "NT" };
                Terminal terminalIdentifier = new Terminal { DisplayName = "T" };
                Terminal arrow = new Terminal { DisplayName = ">" };
                Terminal endline = new Terminal { DisplayName = "endl" };

                NonTerminal grammar = new NonTerminal { DisplayName = "Grammar" };
                NonTerminal completeSymbolList = new NonTerminal { DisplayName = "CompleteSymbolList" };
                NonTerminal symbolList = new NonTerminal { DisplayName = "SymbolList" };
                NonTerminal symbol = new NonTerminal { DisplayName = "Symbol" };
                NonTerminal rules = new NonTerminal { DisplayName = "Rules" };
                NonTerminal rule = new NonTerminal { DisplayName = "Rule" };
                NonTerminal ruleElementList = new NonTerminal { DisplayName = "RuleElementList" };
                NonTerminal ruleElement = new NonTerminal { DisplayName = "RuleElement" };

                RegularExpressionParser regularExpressionParser = new RegularExpressionParser();

                this.lexer = new LexicalAnalyzer
                {
                    Specification = new List<Tuple<RegularExpression, Terminal, Action<Token>>>
                    {
                        Tuple.Create<RegularExpression, Terminal, Action<Token>>(regularExpressionParser.Parse("[A-Z](_|[a-z]|[A-Z])*"), nonTerminalIdentifier, null),
                        Tuple.Create<RegularExpression, Terminal, Action<Token>>(regularExpressionParser.Parse("[a-z](_|[a-z]|[A-Z])*"), terminalIdentifier, null),
                        Tuple.Create<RegularExpression, Terminal, Action<Token>>(regularExpressionParser.Parse(">"), arrow, null),
                        Tuple.Create<RegularExpression, Terminal, Action<Token>>(regularExpressionParser.Parse(" "), null, null),
                        Tuple.Create<RegularExpression, Terminal, Action<Token>>(
                        new ConcatenateRegularExpression {
                            Left = new CharSetRegularExpression { CharSet = new ExplicitCharacterClass { Elements = { '\r' } } },
                            Right = new CharSetRegularExpression { CharSet = new ExplicitCharacterClass { Elements = { '\n' } } }
                        }
                        , endline, null),
                    }
                };

                Dictionary<string, Symbol> table = new Dictionary<string, Symbol>();

                Grammar grammarGrammar = new Grammar
                {
                    Goal = grammar,
                    Productions = new List<Production>
                    {
                        new Production { From = grammar, To = new List<Symbol>  { completeSymbolList, endline, nonTerminalIdentifier, endline, rules }, SemanticAction = args => new Grammar { Goal = ((NonTerminal)table[(string)args[2]]), Productions = ((IEnumerable<Production>)args[4]).ToList() } },
                        new Production { From = completeSymbolList, To = new List<Symbol> { symbolList }, SemanticAction = (args) => { table.Clear(); foreach (Symbol s in (IEnumerable<Symbol>)args[0]) { table.Add(s.DisplayName, s); } return null; }},
                        new Production { From = symbolList, To = new List<Symbol>  { symbol }, SemanticAction = args => new List<Symbol> { (Symbol)args[0] } },
                        new Production { From = symbolList, To = new List<Symbol>  { symbol, symbolList }, SemanticAction = args => new List<Symbol> { (Symbol)args[0] }.Concat((IEnumerable<Symbol>)args[1]) },
                        new Production { From = symbol, To = new List<Symbol> { nonTerminalIdentifier, endline }, SemanticAction = (args) => new NonTerminal { DisplayName = (string) args[0] } },
                        new Production { From = symbol, To = new List<Symbol> { terminalIdentifier, endline }, SemanticAction = (args) => new Terminal { DisplayName = (string) args[0] } },
                        new Production { From = rules, To = new List<Symbol> { rule }, SemanticAction = (args) => new List<Production> { (Production)args[0] } },
                        new Production { From = rules, To = new List<Symbol> { rule, rules }, SemanticAction = (args) => (new List<Production> { (Production)args[0] }).Concat((IEnumerable<Production>)args[1])},
                        new Production { From = rule,  To = new List<Symbol> { nonTerminalIdentifier, arrow, ruleElementList, endline }, SemanticAction = args => new Production { From = ((NonTerminal)table[(string)args[0]]), To = ((IEnumerable<Symbol>)args[2]).ToList() }},
                        new Production { From = ruleElementList, To = new List<Symbol> { ruleElement }, SemanticAction = (args) => new List<Symbol> { (Symbol)args[0] } },
                        new Production { From = ruleElementList, To = new List<Symbol> { ruleElement, ruleElementList }, SemanticAction = (args) => (new List<Symbol> { (Symbol)args[0] }).Concat((IEnumerable<Symbol>)args[1]) },
                        new Production { From = ruleElement, To = new List<Symbol> { nonTerminalIdentifier }, SemanticAction = (args) => table[(string)args[0]] },
                        new Production { From = ruleElement, To = new List<Symbol> { terminalIdentifier }, SemanticAction = (args) => table[(string)args[0]] },
                    }
                };
                
                this.parser = new ParserGenerator(grammarGrammar, ParserMode.SLR).Generate();
            }

            public Grammar Parse(string grammarText)
            {
                return (Grammar)this.parser.Parse(lexer.Analyze(grammarText));
            }
        }

        private static void GrammarSample()
        {
            string grammarText = @"
close_brace
close_paren
close_square_bracket
dotdotdot
equal_sign
open_brace
open_paren
open_square_bracket
Param_desc
Param_doc
param_name
Param_name_value
param_tag
param_type
Param_type_desc
param_value
pipe
Pipe_param_type_list
star

Param_doc
Param_doc            > param_tag Param_name_value
Param_doc            > param_tag open_brace Param_type_desc close_brace Param_name_value Param_desc 
Param_type_desc      > param_type
Param_type_desc      > param_type equal_sign
Param_type_desc      > open_paren param_type Pipe_param_type_list close_paren
Pipe_param_type_list > pipe param_type 
Pipe_param_type_list > pipe param_type Pipe_param_type_list
Param_type_desc      > star
Param_type_desc      > dotdotdot param_type
Param_name_value     > param_name
Param_name_value     > open_square_bracket param_name close_square_bracket
Param_name_value     > open_square_bracket param_name equal_sign param_value close_square_bracket
";
            grammarText = grammarText.Trim();
            grammarText = grammarText + "\r\n";
            Grammar grammar = new GrammarParser().Parse(grammarText);
            Parser parser = new ParserGenerator(grammar, ParserMode.SLR).Generate();
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

            Terminal id = new Terminal { DisplayName = "Ident" };

            LexicalAnalyzer lexicalAnalyzer = new LexicalAnalyzer
            {
                Specification = new List<Tuple<RegularExpression, Terminal, Action<Token>>>
                {
                    Tuple.Create<RegularExpression, Terminal, Action<Token>>(identifier, id, null),
                    Tuple.Create<RegularExpression, Terminal, Action<Token>>(whitespace, null, null)
                }
            };

            foreach (var token in lexicalAnalyzer.Analyze("Hello World error"))
            {
                Console.WriteLine(token.SemanticValue);
            }
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

        private class RegularExpressionParser
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

        private static void ParseRegularExpression()
        {
            string s = "Hel*o|W(or)[^0-9][b-d]";

            RegularExpression result = new RegularExpressionParser().Parse(s);

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
