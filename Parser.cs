namespace ParsingLab2
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    public interface ParseAction
    {
    }

    public class ShiftAction : ParseAction
    {
        public int ToState { get; set; }
    }

    public class ReduceAction : ParseAction
    {
        public int Production { get; set; }
    }

    public class AcceptAction : ParseAction
    {
    }

    class Parser
    {
        class ParserState
        {
            public Token Token { get; set; }
            public int State { get; set; }
        };

        private Dictionary<int, Dictionary<Terminal, ParseAction>> actionTable;
        private Dictionary<int, Dictionary<NonTerminal, int>> gotoTable;
        private Stack<ParserState> parserStack;
        private Grammar grammar;

        public Parser(Grammar grammar, Dictionary<int, Dictionary<Terminal, ParseAction>> actionTable, Dictionary<int, Dictionary<NonTerminal, int>> gotoTable)
        {
            this.grammar = grammar;
            this.actionTable = actionTable;
            this.gotoTable = gotoTable;
            this.parserStack = new Stack<ParserState>();
        }

        public object Parse(IEnumerable<Token> terminals)
        {
            parserStack.Push(new ParserState { Token = null, State = 0 });
            foreach (var token in terminals.Concat(new List<Token> { new Token { Symbol = Terminal.eof, SemanticValue = null} }))
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
                            }
                            else if (nextAction is ReduceAction)
                            {
                                ReduceAction reduceAction = (ReduceAction)nextAction;
                                Production reductionProduction = this.grammar.Productions[reduceAction.Production];
                                object[] semanticValues = new object[reductionProduction.To.Count];
                                for (int i = 0; i < reductionProduction.To.Count; i++)
                                {
                                    semanticValues[reductionProduction.To.Count - i - 1] = parserStack.Pop().Token.SemanticValue;
                                }
                                currentState = parserStack.Peek().State;
                                Dictionary<NonTerminal, int> gotoMap;
                                if (this.gotoTable.TryGetValue(currentState, out gotoMap))
                                {
                                    int nextState;
                                    if (gotoMap.TryGetValue(reductionProduction.From, out nextState))
                                    {
                                        object semanticValue = reductionProduction.SemanticAction(semanticValues);
                                        parserStack.Push(new ParserState { Token = new Token { Symbol = reductionProduction.From, SemanticValue = semanticValue }, State = nextState });
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
    }

    class Token
    {
        public Symbol Symbol { get; set; }
        public object SemanticValue { get; set; }
    }
}
