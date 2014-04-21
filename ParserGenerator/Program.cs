//
// Practicing subset construction algorithm for regular expression
// Practicing the LR(1) and SLR(1) Parsing Algorithm
//
// TODO: Separation of tool set and runtime
// TODO: Better error reporting in conflicts
// TODO: Abstract subset construction algorithm implementation
// 
namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;

    internal static class Program
    {
        private static void Main(string[] args)
        {
            LexicalIdentifierSample();
            ExpressionGrammarSample();
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
                    Tuple.Create<RegularExpression, Action<string>>(identifier, (s) => {Console.WriteLine(s);}),
                    Tuple.Create<RegularExpression, Action<string>>(whitespace, (s) => {} ), /* Ignore whitespaces */
                    
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
    }
}
