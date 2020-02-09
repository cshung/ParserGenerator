namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    
    internal class GrammarParser
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
                Specification = new List<Tuple<CompiledRegularExpression, Terminal, Action<Token>>>
                    {
                        Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(regularExpressionParser.Parse("[A-Z](_|[a-z]|[A-Z])*").Compile(), nonTerminalIdentifier, null),
                        Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(regularExpressionParser.Parse("[a-z](_|[a-z]|[A-Z])*").Compile(), terminalIdentifier, null),
                        Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(regularExpressionParser.Parse(">").Compile(), arrow, null),
                        Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(regularExpressionParser.Parse(" ").Compile(), null, null),
                        Tuple.Create<CompiledRegularExpression, Terminal, Action<Token>>(regularExpressionParser.Parse("\r\n").Compile(), endline, null),
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
}
