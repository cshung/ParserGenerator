namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Parser
    {
        private readonly Grammar grammar;
        private readonly Dictionary<int, Dictionary<Terminal, ParseAction>> actionTable;
        private readonly Dictionary<int, Dictionary<NonTerminal, int>> gotoTable;
        private readonly Stack<ParserState> parserStack;
        
        public Parser(Grammar grammar, Dictionary<int, Dictionary<Terminal, ParseAction>> actionTable, Dictionary<int, Dictionary<NonTerminal, int>> gotoTable)
        {
            this.grammar = grammar;
            this.actionTable = actionTable;
            this.gotoTable = gotoTable;
            this.parserStack = new Stack<ParserState>();
        }

        public object Parse(IEnumerable<Token> terminals)
        {
            bool dumpParserTrace = false;

            parserStack.Push(new ParserState { Token = null, State = 0 });
            foreach (var token in terminals.Concat(new List<Token> { new Token { Symbol = Terminal.eof, SemanticValue = null } }))
            {
                var terminal = token.Symbol as Terminal;
                bool shifted = false;
                while (!shifted)
                {
                    int currentState = parserStack.Peek().State;
                    Dictionary<Terminal, ParseAction> actionMap;
                    if (this.actionTable.TryGetValue(currentState, out actionMap))
                    {
                        ParseAction nextAction;
                        if (actionMap.TryGetValue(terminal, out nextAction))
                        {
                            if (nextAction is ShiftAction)
                            {
                                ShiftAction shiftAction = (ShiftAction)nextAction;
                                parserStack.Push(new ParserState { Token = token, State = shiftAction.ToState });
                                shifted = true;
                                if (dumpParserTrace)
                                {
                                    Console.WriteLine("Shift: " + string.Join(",", this.parserStack.Reverse()));
                                }
                            }
                            else if (nextAction is ReduceAction)
                            {
                                ReduceAction reduceAction = (ReduceAction)nextAction;
                                Production reductionProduction = this.grammar.Productions[reduceAction.Production];
                                object[] semanticValues = new object[reductionProduction.To.Count];
                                for (int i = 0; i < reductionProduction.To.Count; i++)
                                {
                                    semanticValues[reductionProduction.To.Count - i - 1] = parserStack.Pop().Token.SemanticValue;
                                    if (dumpParserTrace)
                                    {
                                        Console.WriteLine("Reduce pop: " + string.Join(",", this.parserStack.Reverse()));
                                    }
                                }
                                currentState = parserStack.Peek().State;
                                Dictionary<NonTerminal, int> gotoMap;
                                if (this.gotoTable.TryGetValue(currentState, out gotoMap))
                                {
                                    int nextState;
                                    if (gotoMap.TryGetValue(reductionProduction.From, out nextState))
                                    {
                                        object semanticValue = null;
                                        if (reductionProduction.SemanticAction != null)
                                        {
                                            semanticValue = reductionProduction.SemanticAction(semanticValues);
                                        }
                                        parserStack.Push(new ParserState { Token = new Token { Symbol = reductionProduction.From, SemanticValue = semanticValue }, State = nextState });
                                        if (dumpParserTrace)
                                        {
                                            Console.WriteLine("Reduce: " + string.Join(",", this.parserStack.Reverse()));
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception();
                                    }
                                }
                                else
                                {
                                    throw new Exception();
                                }
                            }
                            else
                            {
                                return this.parserStack.Peek().Token.SemanticValue;
                            }

                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }
            throw new Exception();
        }

        class ParserState
        {
            public Token Token { get; set; }
            public int State { get; set; }
            public override string ToString()
            {
                return string.Format("{0}({1})", this.Token == null ? "empty" : this.Token.Symbol.DisplayName, this.State);
            }
        };
    }
}