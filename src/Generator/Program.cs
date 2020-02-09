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
            OperatorSample();
            GrammarSample();
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
            Dictionary<String, Terminal> terminals = new Dictionary<String, Terminal>();
            foreach (Production p in grammar.Productions)
            {
                foreach (Symbol s in p.To)
                {
                    Terminal t = s as Terminal;
                    if (t != null)
                    {
                        if (!terminals.ContainsKey(t.DisplayName))
                        {
                            terminals.Add(t.DisplayName, t);
                        }
                    }
                }
            }
            parser.Parse("param_tag param_name".Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(n => new Token { Symbol = terminals[n] }));
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
                Specification = new List<Tuple<CompiledRegularExpression, Terminal, Action<Token>>>
                {
                    Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(identifier.Compile(), id, null),
                    Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(whitespace.Compile(), null, null)
                }
            };

            foreach (var token in lexicalAnalyzer.Analyze("Hello World Compiler001 error"))
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

        
    }
}
