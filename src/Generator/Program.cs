// Practicing subset construction algorithm for regular expression
// Practicing the LR(1) and SLR(1) Parsing Algorithm
// TODO: Top down recursive parser generation
// TODO: Separation of tool set and runtime
// TODO: Abstract subset construction algorithm implementation
namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class Program
    {
        public static void Main(string[] args)
        {
            AnalyzeDefines();
        }

        public abstract class Node
        {
            public abstract void Show(StringBuilder sb);
        }

        public class And : Node
        {
            public Node Left { get; set; }
            public Node Right { get; set; }

            public override void Show(StringBuilder sb)
            {
                sb.Append("(");
                this.Left.Show(sb);
                sb.Append(" && ");
                this.Right.Show(sb);
                sb.Append(")");
            }
        }

        public class Or : Node
        {
            public Node Left { get; set; }
            public Node Right { get; set; }

            public override void Show(StringBuilder sb)
            {
                sb.Append("(");
                this.Left.Show(sb);
                sb.Append(" || ");
                this.Right.Show(sb);
                sb.Append(")");
            }
        }

        public class Not : Node
        {
            public Node Op { get; set; }

            public override void Show(StringBuilder sb)
            {
                sb.Append("!");
                this.Op.Show(sb);
            }
        }

        public class Sym : Node
        {
            public string Symbol { get; set; }

            public override void Show(StringBuilder sb)
            {
                sb.Append(this.Symbol);
            }
        }

        private static void AnalyzeDefines()
        {
            LexicalAnalyzer lexicalAnalyzer;
            Parser parser;
            CreateDefinesExpressionParser(out lexicalAnalyzer, out parser);

            string[] lines = File.ReadAllLines(@"C:\dev\runtime\src\coreclr\gc\gc.cpp");
            Stack<Node> define = new Stack<Node>();
            HashSet<string> preprocessors = new HashSet<string>();
            List<Tuple<string, string>> pairs = new List<Tuple<string, string>>();
            int popcount = 1;
            foreach (var l in lines)
            {
                string line = l;
                Regex comment = new Regex(" *//.*");
                line = comment.Replace(line, "");
                if (line.StartsWith("#"))
                {
                    string[] tokens = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    string preprocessor = tokens[0];
                    preprocessors.Add(preprocessor);
                    string parse;
                    Node top = null;
                    Node expression = null;
                    switch (preprocessor)
                    {
                        case "#if":
                            parse = line.Replace("#if", "").Replace(" ", "");
                            expression = (Node)parser.Parse(lexicalAnalyzer.Analyze(parse));
                            define.Push(expression);
                            break;
                        case "#ifdef":
                            define.Push(new Sym { Symbol = tokens[1] });
                            break;
                        case "#ifndef":
                            define.Push(new Not { Op = new Sym { Symbol = tokens[1] } });
                            break;
                        case "#else":
                            top = define.Pop();
                            define.Push(Negate(top));
                            break;
                        case "#elif":
                            parse = line.Replace("#elif", "").Replace(" ", "");
                            expression = (Node)parser.Parse(lexicalAnalyzer.Analyze(parse));
                            top = define.Pop();
                            define.Push(Negate(top));
                            define.Push(expression);
                            popcount++;
                            break;

                        case "#endif":
                            for (int count = 0; count < popcount; count++)
                            {
                                define.Pop();
                            }
                            popcount = 1;
                            break;
                    }
                }
                StringBuilder sb = new StringBuilder();
                foreach (var node in define)
                {
                    node.Show(sb);
                    sb.Append(",");
                }
                pairs.Add(Tuple.Create(sb.ToString(), l));
            }
            int max = 0;
            foreach (var pair in pairs)
            {
                max = Math.Max(pair.Item1.Length, max);
            }
            max = max + 1;
            StringBuilder outputBuilder = new StringBuilder();
            foreach (var pair in pairs)
            {
                outputBuilder.Append("/*");
                outputBuilder.Append(pair.Item1);
                int pad = max - pair.Item1.Length;
                outputBuilder.Append(new string(' ', pad));
                outputBuilder.Append("*/");
                outputBuilder.Append(pair.Item2);
                outputBuilder.AppendLine();
                /*
                if (pair.Item1.Length == max - 1)
                {
                    outputBuilder.AppendLine(pair.Item1);
                }
                */
            }
            Console.WriteLine(outputBuilder);
        }

        private static Node Negate(Node op)
        {
            Not not = op as Not;
            if (not == null)
            {
                return new Not { Op = op };
            }
            else
            {
                return not.Op;
            }
        }

        private static void CreateDefinesExpressionParser(out LexicalAnalyzer lexicalAnalyzer, out Parser parser)
        {
            RegularExpressionParser regularExpressionParser = new RegularExpressionParser();
            RegularExpression zeroExpression = regularExpressionParser.Parse("0");
            RegularExpression oneExpression = regularExpressionParser.Parse("1");
            RegularExpression definedExpression = regularExpressionParser.Parse("defined");
            RegularExpression bangExpression = regularExpressionParser.Parse("!");
            RegularExpression andExpression = regularExpressionParser.Parse("&&");
            RegularExpression orExpression = new ConcatenateRegularExpression
            {
                Left = new CharSetRegularExpression
                {
                    CharSet = new ExplicitCharacterClass { Elements = { '|' } },
                },
                Right = new CharSetRegularExpression
                {
                    CharSet = new ExplicitCharacterClass { Elements = { '|' } },
                },
            };
            RegularExpression lpExpression = new CharSetRegularExpression
            {
                CharSet = new ExplicitCharacterClass { Elements = { '(' } },
            };
            RegularExpression rpExpression = new CharSetRegularExpression
            {
                CharSet = new ExplicitCharacterClass { Elements = { ')' } },
            };
            RegularExpression symbolExpression = regularExpressionParser.Parse("([A-Z]|[a-z]|_|[0-9])*");

            Terminal zero = new Terminal { DisplayName = "Zero" };
            Terminal one = new Terminal { DisplayName = "One" };
            Terminal defined = new Terminal { DisplayName = "Defined" };
            Terminal bang = new Terminal { DisplayName = "Bang" };
            Terminal and = new Terminal { DisplayName = "And" };
            Terminal or = new Terminal { DisplayName = "Or" };
            Terminal lp = new Terminal { DisplayName = "Lp" };
            Terminal rp = new Terminal { DisplayName = "Rp" };
            Terminal symbol = new Terminal { DisplayName = "Symbol" };

            lexicalAnalyzer = new LexicalAnalyzer
            {
                Specification = new List<Tuple<CompiledRegularExpression, Terminal, Action<Token>>>
                {
                    Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(zeroExpression.Compile(), zero, null),
                    Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(oneExpression.Compile(), one, null),
                    Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(definedExpression.Compile(), defined, null),
                    Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(bangExpression.Compile(), bang, null),
                    Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(andExpression.Compile(), and, null),
                    Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(orExpression.Compile(), or, null),
                    Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(lpExpression.Compile(), lp, null),
                    Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(rpExpression.Compile(), rp, null),
                    Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(symbolExpression.Compile(), symbol, null),
                }
            };
            Grammar grammar = null;

            NonTerminal expr = new NonTerminal { DisplayName = "Expr" };
            NonTerminal term = new NonTerminal { DisplayName = "Term" };

            Grammar exprGrammar = new Grammar
            {
                Goal = expr,
                Productions = new List<Production>
                    {
                        new Production { From = expr, To = new List<Symbol> { term }, SemanticAction = (tokens) => tokens[0] },
                        new Production { From = expr, To = new List<Symbol> { expr, or, term }, SemanticAction = (tokens) => new Or { Left = (Node)tokens[0], Right = (Node)tokens[2] } },
                        new Production { From = expr, To = new List<Symbol> { expr, and, term }, SemanticAction = (tokens) => new And { Left = (Node)tokens[0], Right = (Node)tokens[2] } },
                        new Production { From = term, To = new List<Symbol> { lp, expr, rp }, SemanticAction = (tokens) => tokens[1] },
                        new Production { From = term, To = new List<Symbol> { zero }, SemanticAction = (tokens) => new Sym { Symbol = "0" } },
                        new Production { From = term, To = new List<Symbol> { one }, SemanticAction = (tokens) => new Sym { Symbol = "1" } },
                        new Production { From = term, To = new List<Symbol> { defined, lp, symbol, rp }, SemanticAction = (tokens) => new Sym { Symbol = (string)tokens[2] } },
                        new Production { From = term, To = new List<Symbol> { bang, defined, lp, symbol, rp }, SemanticAction = (tokens) => new Not { Op = new Sym { Symbol = (string)tokens[3] } } },
                        new Production { From = term, To = new List<Symbol> { symbol }, SemanticAction = (tokens) => new Sym { Symbol = (string)tokens[0] } },
                    }
            };

            grammar = exprGrammar;

            parser = new ParserGenerator(grammar, ParserMode.SLR).Generate();
        }
    }
}
