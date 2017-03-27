namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class ParserGenerator
    {
        private Grammar grammar;
        private ParserMode parserMode;
        private IConflictResolver conflictResolver;
        private NonTerminal goal;
        private List<Terminal> terminals;
        private List<NonTerminal> nonTerminals;
        private List<RewrittenProduction> rewrittenGrammar;
        private Dictionary<int, HashSet<int>> firstSets;
        private Dictionary<int, HashSet<int>> followSets;
        private List<HashSet<Item>> canonicalSets;
        private List<CanonicalSetTransition> canonicalSetTransitions;
        private Dictionary<int, Dictionary<Terminal, ParseAction>> actionTable;
        private Dictionary<int, Dictionary<NonTerminal, int>> gotoTable;
        private Dictionary<int, Dictionary<Terminal, Item>> actionItemTable;

        internal ParserGenerator(Grammar grammar, ParserMode parserMode, IConflictResolver conflictResolver = null)
        {
            this.grammar = grammar;
            this.parserMode = parserMode;
            this.conflictResolver = conflictResolver;
        }

        internal Parser Generate()
        {
            bool dumpFirstSet = false;
            bool dumpFollowSet = false;
            bool dumpCanonicalSet = false;
            bool dumpTables = false;

            // Step 1: Rewrite the grammar so that it is based on numbers only, not the objects, so it is easier to work with in code.
            //         Also add a unique production from the "added" goal to goal of the grammar      
            this.RewriteGrammar();

            // Step 2: Compute the first sets
            this.BuildFirstSets();

            if (dumpFirstSet)
            {
                this.DumpFirstSet();
            }

            if (parserMode == ParserMode.SLR)
            {
                this.BuildFollowSets();
                if (dumpFollowSet)
                {
                    this.DumpFollowSet();
                }
            }

            // Step 3: Build the canonical sets
            this.BuildCanonicalSets();

            if (dumpCanonicalSet)
            {
                this.DumpCanonicalSets();
            }

            // Step 4: Build the tables

            this.BuildTables();

            if (dumpTables)
            {
                this.DumpTables();
            }

            return new Parser(this.grammar, this.actionTable, this.gotoTable);
        }

        #region Build Tables

        private void DumpTables()
        {
            var terminalColumn = new List<Terminal>();
            foreach (var state in actionTable)
            {
                foreach (var action in state.Value)
                {
                    if (!terminalColumn.Contains(action.Key))
                    {
                        terminalColumn.Add(action.Key);
                    }
                }
            }

            var nonTerminalColumn = new List<NonTerminal>();
            foreach (var state in gotoTable)
            {
                foreach (var elem in state.Value)
                {
                    if (!nonTerminalColumn.Contains(elem.Key))
                    {
                        nonTerminalColumn.Add(elem.Key);
                    }
                }
            }

            var states = new HashSet<int>(actionTable.Keys.Union(this.gotoTable.Keys)).ToArray();
            Array.Sort(states);

            Console.Write("<tr><td>&nbsp;</td>");
            for (int j = 0; j < terminalColumn.Count; j++)
            {
                var terminal = terminalColumn[j];
                Console.Write("<td>");
                Console.Write(terminal.DisplayName);
                Console.Write("</td>");
            }
            for (int j = 0; j < nonTerminalColumn.Count; j++)
            {
                var nonTerminal = nonTerminalColumn[j];
                Console.Write("<td>");
                Console.Write(nonTerminal.DisplayName);
                Console.Write("</td>");
            }
            Console.WriteLine("</tr>");

            for (int i = 0; i < states.Length; i++)
            {
                int state = states[i];
                Console.Write("<tr><td>");
                Console.Write(state);
                Console.Write("</td>");
                for (int j = 0; j < terminalColumn.Count; j++)
                {
                    Console.Write("<td>");
                    var terminal = terminalColumn[j];
                    if (actionTable.ContainsKey(state))
                    {
                        var actions = actionTable[state];
                        if (actions.ContainsKey(terminal))
                        {
                            Console.Write(ToActionString(actions[terminal]));
                        }
                        else
                        {
                            Console.Write("&nbsp;");
                        }
                    }
                    else
                    {
                        Console.Write("&nbsp;");
                    }
                    Console.Write("</td>");
                }

                for (int j = 0; j < nonTerminalColumn.Count; j++)
                {
                    Console.Write("<td>");
                    var nonTerminal = nonTerminalColumn[j];
                    if (gotoTable.ContainsKey(state))
                    {
                        var gotos = gotoTable[state];
                        if (gotos.ContainsKey(nonTerminal))
                        {
                            Console.Write(gotos[nonTerminal]);
                        }
                        else
                        {
                            Console.Write("&nbsp;");
                        }
                    }
                    else
                    {
                        Console.Write("&nbsp;");
                    }
                    Console.Write("</td>");
                }

                Console.WriteLine("</tr>");
            }
        }

        private string ToActionString(ParseAction parseAction)
        {
            if (parseAction is ShiftAction)
            {
                return "Shift " + ((ShiftAction)parseAction).ToState;
            }
            else if (parseAction is ReduceAction)
            {
                return "Reduce " + ((ReduceAction)parseAction).Production;
            }
            else
            {
                return "Accept";
            }
        }

        private void BuildTables()
        {
            this.actionTable = new Dictionary<int, Dictionary<Terminal, ParseAction>>();
            this.actionItemTable = new Dictionary<int, Dictionary<Terminal, Item>>();
            this.gotoTable = new Dictionary<int, Dictionary<NonTerminal, int>>();
            foreach (var canonicalSet in this.canonicalSets)
            {
                var thisCanonicalSetIndex = IndexOf(canonicalSet);
                foreach (var item in canonicalSet)
                {
                    var itemProduction = this.rewrittenGrammar[item.Production];
                    if (item.StackTopPosition != itemProduction.To.Count)
                    {
                        int nextElement = itemProduction.To[item.StackTopPosition];
                        if (nextElement > 0)
                        {
                            Terminal shiftingTerminal = ToSymbol(nextElement) as Terminal;
                            int nextCanonicalSetIndex = this.canonicalSetTransitions.Single(t => t.From == thisCanonicalSetIndex && t.Symbol == nextElement).To;

                            Dictionary<Terminal, ParseAction> actionMap;
                            if (!actionTable.TryGetValue(thisCanonicalSetIndex, out actionMap))
                            {
                                actionMap = new Dictionary<Terminal, ParseAction>();
                                actionTable.Add(thisCanonicalSetIndex, actionMap);
                            }
                            Dictionary<Terminal, Item> actionItemMap;
                            if (!actionItemTable.TryGetValue(thisCanonicalSetIndex, out actionItemMap))
                            {
                                actionItemMap = new Dictionary<Terminal, Item>();
                                actionItemTable.Add(thisCanonicalSetIndex, actionItemMap);
                            }
                            ParseAction currentAction;
                            ParseAction actionToAdd = new ShiftAction { ToState = nextCanonicalSetIndex };
                            if (actionMap.TryGetValue(shiftingTerminal, out currentAction))
                            {
                                if (currentAction is ReduceAction)
                                {
                                    ParserItem item1 = ToParserItem(item);
                                    ParserItem item2 = ToParserItem(actionItemMap[shiftingTerminal]);
                                    bool? which = (this.conflictResolver == null) ? null : this.conflictResolver.ShouldFirstOverrideSecond(item1, item2);
                                    if (which == null)
                                    {
                                        Console.Error.WriteLine("Shift/Reduce conflict");
                                        Console.Error.WriteLine("Shift  : " + item1.ToString());
                                        Console.Error.WriteLine("Reduce : " + item2.ToString());
                                    }
                                    else if (which.Value)
                                    {
                                        actionMap.Remove(shiftingTerminal);
                                        actionItemMap.Remove(shiftingTerminal);
                                        actionMap.Add(shiftingTerminal, actionToAdd);
                                        actionItemMap.Add(shiftingTerminal, item);
                                    }
                                    else
                                    {
                                        // the existing item wins - no-op
                                    }
                                }
                            }
                            else
                            {
                                actionMap.Add(shiftingTerminal, actionToAdd);
                                actionItemMap.Add(shiftingTerminal, item);
                            }
                        }
                    }
                    else
                    {
                        IEnumerable<Terminal> reduceSymbols;
                        if (parserMode == ParserMode.LR)
                        {
                            Terminal reduceSymbol = ToSymbol(item.Lookahead) as Terminal;
                            reduceSymbols = new List<Terminal> { reduceSymbol };
                        }
                        else
                        {
                            reduceSymbols = this.followSets[this.rewrittenGrammar[item.Production].From].Select(t => ToSymbol(t) as Terminal);
                        }

                        foreach (var reduceSymbol in reduceSymbols)
                        {
                            Dictionary<Terminal, ParseAction> actionMap;
                            if (!actionTable.TryGetValue(thisCanonicalSetIndex, out actionMap))
                            {
                                actionMap = new Dictionary<Terminal, ParseAction>();
                                actionTable.Add(thisCanonicalSetIndex, actionMap);
                            }
                            Dictionary<Terminal, Item> actionItemMap;
                            if (!actionItemTable.TryGetValue(thisCanonicalSetIndex, out actionItemMap))
                            {
                                actionItemMap = new Dictionary<Terminal, Item>();
                                actionItemTable.Add(thisCanonicalSetIndex, actionItemMap);
                            }
                            ParseAction currentAction;
                            actionMap.TryGetValue(reduceSymbol, out currentAction);

                            ParseAction actionToAdd = null;
                            if (item.Production == 0)
                            {
                                actionToAdd = new AcceptAction();
                            }
                            else
                            {
                                // The index is -1 to account for the fact we have an extra production
                                actionToAdd = new ReduceAction { Production = item.Production - 1 };
                            }

                            if (currentAction != null)
                            {
                                ParserItem item1 = ToParserItem(item);
                                ParserItem item2 = ToParserItem(actionItemMap[reduceSymbol]);

                                bool? which = (this.conflictResolver == null) ? null : this.conflictResolver.ShouldFirstOverrideSecond(item1, item2);
                                if (which == null)
                                {
                                    if (currentAction is ShiftAction)
                                    {
                                        Console.Error.WriteLine("Shift/Reduce conflict");
                                        Console.Error.WriteLine("Shift  : " + item2.ToString());
                                        Console.Error.WriteLine("Reduce : " + item1.ToString());
                                    }
                                    else if (currentAction is ReduceAction)
                                    {
                                        Console.Error.WriteLine("Reduce/Reduce conflict");
                                        Console.Error.WriteLine("Reduce : " + item1.ToString());
                                        Console.Error.WriteLine("Reduce : " + item2.ToString());
                                    }
                                }
                                else if (which.Value)
                                {
                                    actionMap.Remove(reduceSymbol);
                                    actionItemMap.Remove(reduceSymbol);
                                    actionMap.Add(reduceSymbol, actionToAdd);
                                    actionItemMap.Add(reduceSymbol, item);
                                }
                                else
                                {
                                    // the existing item wins - no-op
                                }
                            }
                            else
                            {
                                actionMap.Add(reduceSymbol, actionToAdd);
                                actionItemMap.Add(reduceSymbol, item);
                            }
                        }
                    }
                }
            }

            foreach (var transition in this.canonicalSetTransitions)
            {
                if (transition.Symbol < 0)
                {
                    Dictionary<NonTerminal, int> gotoMap;
                    if (!this.gotoTable.TryGetValue(transition.From, out gotoMap))
                    {
                        gotoMap = new Dictionary<NonTerminal, int>();
                        gotoTable.Add(transition.From, gotoMap);
                    }
                    gotoMap.Add(ToSymbol(transition.Symbol) as NonTerminal, transition.To);
                }
            }
        }

        #endregion

        #region Compute Canonical Set

        private void DumpCanonicalSets()
        {
            Console.WriteLine("digraph");
            Console.WriteLine("{");
            int i = 0;
            foreach (var canonicalSet in canonicalSets)
            {
                Console.Write("CanonicalSet{0} [label=\"", i++);
                foreach (var item in canonicalSet)
                {
                    Console.Write(ToParserItem(item).ToString());
                    if (parserMode == ParserMode.LR)
                    {
                        Console.Write(" , " +
                        ToSymbol(item.Lookahead).DisplayName
                        );
                    }
                    Console.Write("\\n");
                }
                Console.WriteLine("\"];");
            }
            foreach (var transition in this.canonicalSetTransitions)
            {
                Console.WriteLine("CanonicalSet{0} -> CanonicalSet{2} [label=\"{1}\"];", transition.From, ToSymbol(transition.Symbol).DisplayName, transition.To);
            }
            Console.WriteLine("}");
        }

        private void BuildCanonicalSets()
        {
            this.canonicalSets = new List<HashSet<Item>>();
            this.canonicalSetTransitions = new List<CanonicalSetTransition>();
            Item startItem = new Item { Production = 0, StackTopPosition = 0, Lookahead = ToIndex(Terminal.eof), };
            HashSet<Item> startCanonicalSet = this.Closure(new List<Item> { startItem });
            this.canonicalSets.Add(startCanonicalSet);
            Queue<HashSet<Item>> unmarked = new Queue<HashSet<Item>>();
            unmarked.Enqueue(startCanonicalSet);
            while (unmarked.Count > 0)
            {
                HashSet<Item> currentCanonicalSet = unmarked.Dequeue();
                HashSet<int> nextElements = new HashSet<int>();
                foreach (var currentItem in currentCanonicalSet)
                {
                    var currentItemProduction = this.rewrittenGrammar[currentItem.Production];
                    if (currentItem.StackTopPosition != currentItemProduction.To.Count)
                    {
                        int currentItemNextElement = currentItemProduction.To[currentItem.StackTopPosition];
                        nextElements.Add(currentItemNextElement);
                    }
                }
                int currentCanonicalSetIndex = IndexOf(currentCanonicalSet);
                foreach (var nextElement in nextElements)
                {
                    var nextCanonicalSet = Goto(currentCanonicalSet, nextElement);
                    int nextCanonicalSetIndex = IndexOf(nextCanonicalSet);
                    if (nextCanonicalSetIndex == -1)
                    {
                        canonicalSetTransitions.Add(new CanonicalSetTransition { From = currentCanonicalSetIndex, To = canonicalSets.Count, Symbol = nextElement });
                        this.canonicalSets.Add(nextCanonicalSet);
                        unmarked.Enqueue(nextCanonicalSet);
                    }
                    else
                    {
                        canonicalSetTransitions.Add(new CanonicalSetTransition { From = currentCanonicalSetIndex, To = nextCanonicalSetIndex, Symbol = nextElement });
                    }
                }
            }
        }

        private int IndexOf(HashSet<Item> nextCanonicalSet)
        {
            var match = this.canonicalSets.Select((elem, index) => Tuple.Create(elem, index)).SingleOrDefault(tuple => SetEqual(tuple.Item1, nextCanonicalSet));
            return match == null ? -1 : match.Item2;
        }

        private static bool SetEqual(HashSet<Item> cc1, HashSet<Item> cc2)
        {
            return (cc1.Except(cc2).Count() == 0 && cc2.Except(cc1).Count() == 0);
        }

        private HashSet<Item> Closure(IEnumerable<Item> items)
        {
            HashSet<Item> closure = new HashSet<Item>(items);
            bool closureChanged = true;
            while (closureChanged)
            {
                HashSet<Item> toAdd = new HashSet<Item>();
                foreach (var item in closure)
                {
                    var itemProduction = this.rewrittenGrammar[item.Production];
                    // if there is a symbol after the dot
                    if (item.StackTopPosition != itemProduction.To.Count)
                    {
                        int nextElement = itemProduction.To[item.StackTopPosition];
                        // if the symbol is a non-terminal
                        if (nextElement < 0)
                        {
                            foreach (var toExpandProduction in this.rewrittenGrammar.Select((p, i) => Tuple.Create(p, i)).Where(p => p.Item1.From == nextElement))
                            {
                                if (parserMode == ParserMode.LR)
                                {
                                    foreach (var possibleLookahead in FirstSetOfSequence(itemProduction.To.Skip(item.StackTopPosition + 1).Concat(new List<int> { item.Lookahead })))
                                    {
                                        toAdd.Add(new Item { Production = toExpandProduction.Item2, StackTopPosition = 0, Lookahead = possibleLookahead });
                                    }
                                }
                                else
                                {
                                    toAdd.Add(new Item { Production = toExpandProduction.Item2, StackTopPosition = 0, Lookahead = 1 });
                                }
                            }
                        }
                    }
                }
                int closureOriginalCount = closure.Count;
                foreach (var elem in toAdd)
                {
                    closure.Add(elem);
                }
                closureChanged = closure.Count != closureOriginalCount;
            }
            return closure;
        }

        private HashSet<Item> Goto(IEnumerable<Item> items, int symbol)
        {
            var moved = new HashSet<Item>();
            foreach (var item in items)
            {
                var itemProduction = this.rewrittenGrammar[item.Production];
                // if there is a symbol after the dot
                if (item.StackTopPosition != itemProduction.To.Count)
                {
                    int nextElement = itemProduction.To[item.StackTopPosition];
                    if (nextElement == symbol)
                    {
                        moved.Add(new Item { Production = item.Production, StackTopPosition = item.StackTopPosition + 1, Lookahead = item.Lookahead });
                    }
                }
            }
            return Closure(moved);
        }

        private ParserItem ToParserItem(Item item)
        {
            var itemProduction = this.rewrittenGrammar[item.Production];
            return new ParserItem
            {
                From = ToSymbol(itemProduction.From),
                SeenSymbols = itemProduction.To.Take(item.StackTopPosition).Select(t => ToSymbol(t)).ToList(),
                ExpectedSymbols = itemProduction.To.Skip(item.StackTopPosition).Select(t => ToSymbol(t)).ToList(),
                Lookahead = ToSymbol(item.Lookahead),
            };
        }

        class Item
        {
            public int Production { get; set; }
            public int StackTopPosition { get; set; }
            public int Lookahead { get; set; }

            public override bool Equals(object obj)
            {
                Item that = obj as Item;
                return that != null && this.Production == that.Production && this.StackTopPosition == that.StackTopPosition && this.Lookahead == that.Lookahead;
            }

            public override int GetHashCode()
            {
                return Production ^ StackTopPosition ^ Lookahead;
            }
        }

        class CanonicalSetTransition
        {
            public int From { get; set; }
            public int To { get; set; }
            public int Symbol { get; set; }
        }

        #endregion

        #region Compute First Sets

        private void BuildFirstSets()
        {
            this.firstSets = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < terminals.Count; i++)
            {
                // The first set of any terminal is itself, period.
                firstSets.Add(i + 1, new HashSet<int> { i + 1 });
            }
            for (int i = 0; i < nonTerminals.Count; i++)
            {
                firstSets.Add(-i - 1, new HashSet<int>());
            }
            bool firstSetChanged = true;
            while (firstSetChanged)
            {
                firstSetChanged = false;
                foreach (var rewrittenProduction in this.rewrittenGrammar)
                {
                    // Obtain a reference to update
                    var firstSet = this.firstSets[rewrittenProduction.From];
                    int originalFirstSetCount = firstSet.Count;
                    foreach (var elem in FirstSetOfSequence(rewrittenProduction.To))
                    {
                        firstSet.Add(elem);
                    }
                    if (firstSet.Count > originalFirstSetCount)
                    {
                        firstSetChanged = true;
                    }
                }
            }
        }

        private void BuildFollowSets()
        {
            this.followSets = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < terminals.Count; i++)
            {
                followSets.Add(i + 1, new HashSet<int>());
            }
            for (int i = 0; i < nonTerminals.Count; i++)
            {
                followSets.Add(-i - 1, new HashSet<int>());
            }
            this.followSets[ToIndex(this.goal)].Add(ToIndex(Terminal.eof));
            bool followSetsChanged = true;
            while (followSetsChanged)
            {
                followSetsChanged = false;
                foreach (var rewrittenProduction in this.rewrittenGrammar)
                {
                    int lastSymbol = rewrittenProduction.To[rewrittenProduction.To.Count - 1];
                    var followSetsOfLastSymbol = followSets[lastSymbol];
                    var followSetOfFrom = followSets[rewrittenProduction.From];

                    foreach (var elem in followSetOfFrom)
                    {
                        if (!followSetsOfLastSymbol.Contains(elem))
                        {
                            followSetsOfLastSymbol.Add(elem);
                            followSetsChanged = true;
                        }
                    }
                    HashSet<int> trailer = followSetOfFrom;
                    for (int i = rewrittenProduction.To.Count - 1; i > 0; i--)
                    {
                        var computingFollowSet = this.followSets[rewrittenProduction.To[i - 1]];
                        var nextFollowSet = this.followSets[rewrittenProduction.To[i]];
                        var nextFirstSet = this.firstSets[rewrittenProduction.To[i]];
                        if (nextFollowSet.Contains(ToIndex(Terminal.epsilon)))
                        {
                            foreach (var elem in nextFirstSet)
                            {
                                if (elem != ToIndex(Terminal.epsilon))
                                {
                                    if (!computingFollowSet.Contains(elem))
                                    {
                                        computingFollowSet.Add(elem);
                                        followSetsChanged = true;
                                    }
                                }
                            }
                            foreach (var elem in trailer)
                            {
                                if (!computingFollowSet.Contains(elem))
                                {
                                    computingFollowSet.Add(elem);
                                    followSetsChanged = true;
                                }
                            }
                        }
                        else
                        {
                            foreach (var elem in nextFirstSet)
                            {
                                if (elem != ToIndex(Terminal.epsilon))
                                {
                                    if (!computingFollowSet.Contains(elem))
                                    {
                                        computingFollowSet.Add(elem);
                                        followSetsChanged = true;
                                    }
                                }
                            }
                            trailer = new HashSet<int>();
                        }
                    }
                }
            }
        }

        private void DumpFirstSet()
        {
            foreach (var kvp in this.firstSets)
            {
                if (kvp.Key < 0)
                {
                    Console.WriteLine(ToSymbol(kvp.Key).DisplayName + "\t\t" + string.Join(",", kvp.Value.Select(t => ToSymbol(t).DisplayName)));
                }
            }
        }

        private void DumpFollowSet()
        {
            Console.WriteLine("Follow Sets");
            foreach (var kvp in this.followSets)
            {
                Console.WriteLine(ToSymbol(kvp.Key).DisplayName + "\t\t" + string.Join(",", kvp.Value.Select(t => ToSymbol(t).DisplayName)));
            }
        }

        private HashSet<int> FirstSetOfSequence(IEnumerable<int> symbols)
        {
            HashSet<int> result = new HashSet<int>();
            bool nonTerminalCouldBeEmpty = true;
            foreach (var symbol in symbols)
            {
                var currentFirstSet = this.firstSets[symbol];
                bool containEpsilon = false;
                foreach (var elem in currentFirstSet)
                {
                    if (elem == ToIndex(Terminal.epsilon))
                    {
                        containEpsilon = true;
                    }
                    else
                    {
                        result.Add(elem);
                    }
                }

                if (!containEpsilon)
                {
                    nonTerminalCouldBeEmpty = false;
                    break;
                }
            }
            if (nonTerminalCouldBeEmpty)
            {
                result.Add(ToIndex(Terminal.epsilon));
            }
            return result;
        }

        #endregion

        #region Rewriting grammar

        private void RewriteGrammar()
        {
            this.terminals = new List<Terminal>();
            this.terminals.Add(Terminal.epsilon);
            this.terminals.Add(Terminal.eof);

            this.nonTerminals = new List<NonTerminal>();
            this.goal = new NonTerminal { DisplayName = "Goal" };
            this.nonTerminals.Add(this.goal);
            this.nonTerminals.Add(this.grammar.Goal);

            foreach (var production in grammar.Productions)
            {
                CheckAdd(production.From);
                foreach (var toElem in production.To)
                {
                    CheckAdd(toElem);
                }
            }

            this.rewrittenGrammar = new List<RewrittenProduction>();
            this.rewrittenGrammar.Add(new RewrittenProduction { From = ToIndex(this.goal), To = new List<int> { ToIndex(this.grammar.Goal) } });

            foreach (var production in grammar.Productions)
            {
                this.rewrittenGrammar.Add(new RewrittenProduction { From = ToIndex(production.From), To = production.To.Select(s => ToIndex(s)).ToList() });
            }
        }

        private int ToIndex(Symbol symbol)
        {
            if (symbol is Terminal)
            {
                return this.terminals.IndexOf(symbol as Terminal) + 1;
            }
            else
            {
                return -this.nonTerminals.IndexOf(symbol as NonTerminal) - 1;
            }
        }

        private Symbol ToSymbol(int index)
        {
            if (index > 0)
            {
                return terminals[index - 1];
            }
            else
            {
                return nonTerminals[-index - 1];
            }
        }

        private void CheckAdd(Symbol symbol)
        {

            if (symbol is Terminal)
            {
                if (!terminals.Contains(symbol))
                {
                    terminals.Add(symbol as Terminal);
                }
            }
            else if (symbol is NonTerminal)
            {
                if (!nonTerminals.Contains(symbol))
                {
                    nonTerminals.Add(symbol as NonTerminal);
                }
            }
        }

        class RewrittenProduction
        {
            public int From { get; set; }
            public List<int> To { get; set; }
        }

        #endregion
    }
}