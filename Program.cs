namespace ParsingLab
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            NonTerminal e = new NonTerminal("Expr");
            NonTerminal t = new NonTerminal("Term");
            Terminal num = new Terminal("n");
            Terminal times = new Terminal("*");
            Terminal plus = new Terminal("+");
            Terminal op = new Terminal("(");
            Terminal cp = new Terminal(")");

            Grammar g = new Grammar(e);
            g.Add(new Production(e, new List<Symbol> { t }));
            g.Add(new Production(e, new List<Symbol> { t, plus, e }));
            g.Add(new Production(t, new List<Symbol> { num }));
            //g.Add(new Production(t, new List<Symbol> { t, times, e }));
            //g.Add(new Production(t, new List<Symbol> { op, e, cp }));

            Console.WriteLine("Grammar");
            Console.WriteLine(string.Join("\n", g));
            Console.WriteLine();

            foreach (var item in g.FirstSets())
            {
                Console.WriteLine("First({0}) \t= [{1}]", item.Key, string.Join(", ", item.Value));
            }
            Console.WriteLine();

            foreach (var item in g.FollowSets())
            {
                Console.WriteLine("Follow({0}) \t= [{1}]", item.Key, string.Join(", ", item.Value));
            }
            Console.WriteLine();

            Parser generatedParser = g.GenerateParser();

            Console.WriteLine("Parsing trace");
            generatedParser.Parse(new List<Terminal> { new Terminal("n"), new Terminal("+"), new Terminal("n"), new Terminal("+"), new Terminal("n"), Eof.Instance });
        }
    }

    class Symbol
    {
        public readonly string name;

        public Symbol(string name)
        {
            this.name = name;
        }

        public override bool Equals(object obj)
        {
            Symbol that = obj as Symbol;
            return that != null && this.name.Equals(that.name);
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override string ToString()
        {
            return name;
        }
    }

    class Epsilon : Terminal
    {
        private Epsilon()
            : base("Epsilon")
        {
        }

        private static Epsilon instance = new Epsilon();
        public static Epsilon Instance
        {
            get { return instance; }
        }
    }

    class Eof : Terminal
    {
        private Eof()
            : base("Eof")
        {
        }

        private static Eof instance = new Eof();
        public static Eof Instance
        {
            get { return instance; }
        }
    }

    class Terminal : Symbol
    {
        public Terminal(string name)
            : base(name)
        {
        }
    }

    class NonTerminal : Symbol
    {
        public NonTerminal(string name)
            : base(name)
        {
        }
    }

    class Production
    {
        public readonly NonTerminal Left;
        public readonly List<Symbol> Right;
        public Production(NonTerminal left, List<Symbol> right)
        {
            this.Left = left;
            this.Right = right;
        }

        public override string ToString()
        {
            return Left + "->" + string.Join(", ", Right);
        }
    }

    class Item
    {
        public readonly Production Production;
        public readonly List<Symbol> DotLeft;
        public readonly List<Symbol> DotRight;
        public readonly Terminal LookAhead;
        public Item(Production production, List<Symbol> dotLeft, List<Symbol> dotRight, Terminal lookAhead)
        {
            this.Production = production;
            this.DotLeft = dotLeft;
            this.DotRight = dotRight;
            this.LookAhead = lookAhead;
        }

        public override bool Equals(object obj)
        {
            Item that = obj as Item;
            return that != null &&
                (this.DotLeft.SequenceEqual(that.DotLeft) &&
                this.DotRight.SequenceEqual(that.DotRight) &&
                this.LookAhead.Equals(that.LookAhead));
        }

        public override int GetHashCode()
        {
            // TODO: Implement a good hash code function for performance
            return 1;
        }

        public override string ToString()
        {
            return "[" + this.Production.Left + "->" + string.Join(" ", DotLeft.Select(t => t.ToString()).Concat(new List<string> { "." }).Concat(DotRight.Select(t => t.ToString()))) + " : " + LookAhead + "]";
        }
    }

    class ItemSet : HashSet<Item>
    {
        public override int GetHashCode()
        {
            // TODO: Implement a good hash code function for performance
            return 1;
        }

        public override bool Equals(object obj)
        {
            ItemSet that = obj as ItemSet;
            return that != null && this.SequenceEqual(that);
        }
    }

    class Grammar : List<Production>
    {
        private HashSet<Terminal> terminals;
        private HashSet<NonTerminal> nonTerminals;
        private Dictionary<Symbol, HashSet<Terminal>> firstSets;
        private Dictionary<Symbol, HashSet<Terminal>> followSets;

        private NonTerminal start;
        private NonTerminal goal;
        private Production goalProduction;

        public Grammar(NonTerminal start)
        {
            this.goal = new NonTerminal("Goal");
            this.start = start;
            this.goalProduction = new Production(goal, new List<Symbol> { start, Eof.Instance });
            this.Add(goalProduction);
        }

        public HashSet<Terminal> Terminals()
        {
            if (this.terminals == null)
            {
                this.terminals = new HashSet<Terminal>(GenerateTerminals());
            }

            return this.terminals;
        }

        public HashSet<NonTerminal> NonTerminals()
        {
            if (this.nonTerminals == null)
            {
                this.nonTerminals = new HashSet<NonTerminal>(GenerateNonTerminals());
            }

            return this.nonTerminals;
        }

        public Dictionary<Symbol, HashSet<Terminal>> FirstSets()
        {
            if (this.firstSets == null)
            {
                this.GenerateFirstSets();
            }

            return this.firstSets;
        }

        public Dictionary<Symbol, HashSet<Terminal>> FollowSets()
        {
            if (this.followSets == null)
            {
                this.GenerateFollowSets();
            }

            return this.followSets;
        }

        private IEnumerable<Terminal> GenerateTerminals()
        {
            foreach (var production in this)
            {
                foreach (var symbol in production.Right)
                {
                    if (symbol is Terminal)
                    {
                        yield return (symbol as Terminal);
                    }
                }
            }
        }

        private IEnumerable<NonTerminal> GenerateNonTerminals()
        {
            foreach (var production in this)
            {
                yield return production.Left;
            }
        }

        private void GenerateFirstSets()
        {
            this.firstSets = new Dictionary<Symbol, HashSet<Terminal>>();
            foreach (var terminal in this.Terminals())
            {
                this.firstSets.Add(terminal, new HashSet<Terminal>(new List<Terminal> { terminal }));
            }
            foreach (var nonTerminal in this.NonTerminals())
            {
                this.firstSets.Add(nonTerminal, new HashSet<Terminal>());
            }
            bool updated = false;
            do
            {
                updated = false;
                foreach (var production in this)
                {
                    HashSet<Terminal> leftFirstSet = this.firstSets[production.Left];
                    foreach (var right in production.Right)
                    {
                        if (right is Terminal)
                        {
                            // A -> T X => First(A) contains T
                            var rightTerminal = right as Terminal;
                            if (!leftFirstSet.Contains(rightTerminal))
                            {
                                leftFirstSet.Add(rightTerminal);
                                updated = true;
                            }

                            break;
                        }
                        else
                        {
                            // A -> NT X => First(A) contains First(NT)
                            var rightNonTerminal = right as NonTerminal;
                            var rightFirstSet = this.firstSets[rightNonTerminal];
                            foreach (var rightFirstSetElement in rightFirstSet)
                            {
                                if (!leftFirstSet.Contains(rightFirstSetElement))
                                {
                                    leftFirstSet.Add(rightFirstSetElement);
                                    updated = true;
                                }
                            }

                            // If First(NT) Contains Epsilon then First(A) contains First(X)
                            if (!rightFirstSet.Contains(Epsilon.Instance))
                            {
                                break;
                            }
                        }
                    }
                }
            } while (updated);
        }

        private void GenerateFollowSets()
        {
            this.followSets = new Dictionary<Symbol, HashSet<Terminal>>();
            foreach (var terminal in this.Terminals())
            {
                this.followSets.Add(terminal, new HashSet<Terminal>());
            }

            foreach (var nonTerminal in this.NonTerminals())
            {
                this.followSets.Add(nonTerminal, new HashSet<Terminal>());
            }

            bool updated = false;
            do
            {
                updated = false;
                foreach (var production in this)
                {
                    var currentShouldFollowSet = this.followSets[production.Left];
                    List<Symbol> rightReversed = production.Right.Reverse<Symbol>().ToList();
                    while (rightReversed.Count > 0)
                    {
                        var currentItem = rightReversed.First();
                        var currentItemFollowSet = this.followSets[currentItem];
                        foreach (var currentShouldFollow in currentShouldFollowSet)
                        {
                            if (!currentItemFollowSet.Contains(currentShouldFollow))
                            {
                                currentItemFollowSet.Add(currentShouldFollow);
                                updated = true;
                            }
                        }

                        rightReversed.RemoveAt(0);
                        if (currentItem is Terminal)
                        {
                            currentShouldFollowSet = new HashSet<Terminal>(new List<Terminal> { currentItem as Terminal });
                        }
                        else
                        {
                            NonTerminal currentItemAsNonTerminal = currentItem as NonTerminal;
                            var currentItemFirstSet = this.firstSets[currentItemAsNonTerminal];
                            if (currentItemFirstSet.Contains(Epsilon.Instance))
                            {
                                foreach (var currentItemFirstSetItem in currentItemFirstSet)
                                {
                                    if (!currentItemFirstSetItem.Equals(Epsilon.Instance))
                                    {
                                        currentShouldFollowSet.Add(currentItemFirstSetItem);
                                    }
                                }
                            }
                            else
                            {
                                currentShouldFollowSet = new HashSet<Terminal>(currentItemFirstSet);
                            }
                        }
                    }
                }
            } while (updated);
        }

        private HashSet<Terminal> FirstSetOfSequence(List<Symbol> symbols)
        {
            HashSet<Terminal> result = new HashSet<Terminal>();
            bool breakCalled = false;
            foreach (var symbol in symbols)
            {
                var symbolFirstSet = this.FirstSets()[symbol];
                bool hasEpsilon = false;
                foreach (var symbolFirstSetElement in symbolFirstSet)
                {
                    if (symbolFirstSetElement.Equals(Epsilon.Instance))
                    {
                        hasEpsilon = true;
                    }
                    else
                    {
                        result.Add(symbolFirstSetElement);
                    }
                }

                if (!hasEpsilon)
                {
                    breakCalled = true;
                    break;
                }
            }

            if (!breakCalled)
            {
                result.Add(Epsilon.Instance);
            }

            return result;
        }

        public Parser GenerateParser()
        {
            HashSet<Item> reachableItems = new HashSet<Item>();
            List<Tuple<Item, Symbol, Item>> links = new List<Tuple<Item, Symbol, Item>>();
            HashSet<Item> enqueued = new HashSet<Item>();
            Queue<Item> queue = new Queue<Item>();

            // Step 1: Generate the seed.
            foreach (var startTerminal in this.FirstSets()[goal])
            {
                var startItem = new Item(goalProduction, new List<Symbol>(), new List<Symbol>(goalProduction.Right), startTerminal);
                enqueued.Add(startItem);
                queue.Enqueue(startItem);
            }

            // Step 2: BFS - record all nodes and links
            while (queue.Count > 0)
            {
                var currentItem = queue.Dequeue();
                reachableItems.Add(currentItem);
                // Find reduction move and push to queue
                foreach (var reducedItem in this.ReduceMoves(currentItem))
                {
                    links.Add(Tuple.Create<Item, Symbol, Item>(currentItem, Epsilon.Instance, reducedItem));
                    if (!enqueued.Contains(reducedItem))
                    {
                        enqueued.Add(reducedItem);
                        queue.Enqueue(reducedItem);
                    }
                }
                foreach (var shiftMove in this.ShiftMoves(currentItem))
                {
                    var shiftItem = shiftMove.Item2;
                    links.Add(Tuple.Create<Item, Symbol, Item>(currentItem, shiftMove.Item1, shiftItem));
                    if (!enqueued.Contains(shiftItem))
                    {
                        enqueued.Add(shiftItem);
                        queue.Enqueue(shiftItem);
                    }
                }
            }

            Console.WriteLine("reachable items");
            foreach (var item in reachableItems)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine();

            Console.WriteLine("links");
            foreach (var link in links)
            {
                Console.WriteLine(link.Item1 + "\t-" + link.Item2 + "->\t" + link.Item3);
            }
            Console.WriteLine();

            // Step 3: Subset Construction
            HashSet<Item> seed = new HashSet<Item>();
            foreach (var startTerminal in this.FirstSets()[goal])
            {
                var startItem = new Item(goalProduction, new List<Symbol>(), new List<Symbol>(goalProduction.Right), startTerminal);
                seed.Add(startItem);
            }

            var subsetNodes = new HashSet<ItemSet>();
            var subsetLinks = new List<Tuple<ItemSet, Symbol, ItemSet>>();
            var subsetConstructionEnqueued = new HashSet<ItemSet>();
            var subsetConstructionQueue = new Queue<ItemSet>();
            var startSubset = EpsilonClosure(seed, links);
            subsetConstructionEnqueued.Add(startSubset);
            subsetConstructionQueue.Enqueue(startSubset);

            while (subsetConstructionQueue.Count > 0)
            {
                var currentSubset = subsetConstructionQueue.Dequeue();
                subsetNodes.Add(currentSubset);

                Dictionary<Symbol, HashSet<Item>> nonEpsilonNeighbors = new Dictionary<Symbol, HashSet<Item>>();
                foreach (var nonEpsilonLink in links.Where(l => currentSubset.Contains(l.Item1) && !l.Item2.Equals(Epsilon.Instance)))
                {
                    HashSet<Item> temp;
                    if (!nonEpsilonNeighbors.TryGetValue(nonEpsilonLink.Item2, out temp))
                    {
                        temp = new HashSet<Item>();
                        nonEpsilonNeighbors.Add(nonEpsilonLink.Item2, temp);
                    }

                    temp.Add(nonEpsilonLink.Item3);
                }

                foreach (var nonEpsilonNeighbor in nonEpsilonNeighbors)
                {
                    var nonEpsilonNeighborClosure = EpsilonClosure(nonEpsilonNeighbor.Value, links);
                    subsetLinks.Add(Tuple.Create(currentSubset, nonEpsilonNeighbor.Key, nonEpsilonNeighborClosure));
                    if (!subsetConstructionEnqueued.Contains(nonEpsilonNeighborClosure))
                    {
                        subsetConstructionEnqueued.Add(nonEpsilonNeighborClosure);
                        subsetConstructionQueue.Enqueue(nonEpsilonNeighborClosure);
                    }
                }
            }

            // Step 4: Translate this graph into tables for efficient parsing
            int subsetNodeNumber = 0;
            var numberedSubsetNodes = new Dictionary<ItemSet, int>();
            foreach (var subsetNode in subsetNodes)
            {
                numberedSubsetNodes.Add(subsetNode, subsetNodeNumber++);
            }
            var numberedSubsetLinks = new List<Tuple<int, Symbol, int>>(subsetLinks.Select(t => Tuple.Create(numberedSubsetNodes[t.Item1], t.Item2, numberedSubsetNodes[t.Item3])));

            Parser result = new Parser(this);

            Console.WriteLine("Subset nodes");
            foreach (var numberedSubsetNode in numberedSubsetNodes)
            {
                var subsetNode = numberedSubsetNode.Key;
                int subsetNumber = numberedSubsetNode.Value;
                Console.WriteLine(subsetNumber);
                foreach (var item in subsetNode)
                {
                    Console.WriteLine("  " + item);
                    if (item.DotRight.Count > 0)
                    {
                        if (item.DotRight.First() is Terminal)
                        {
                            Terminal toShift = item.DotRight.First() as Terminal;
                            int shiftTo = numberedSubsetLinks.Single(t => t.Item1 == subsetNumber && t.Item2.Equals(toShift)).Item3;
                            Console.WriteLine("    action({0},{1}) = shift {2}", subsetNumber, toShift, shiftTo);
                            var actionTableKey = Tuple.Create(subsetNumber, toShift);
                            if (!result.actionTable.ContainsKey(actionTableKey))
                            {
                                if (item.Production == goalProduction)
                                {
                                    result.actionTable.Add(actionTableKey, Tuple.Create(Action.Accept, 0)); 
                                }
                                else
                                {
                                    result.actionTable.Add(actionTableKey, Tuple.Create(Action.Shift, shiftTo));
                                }
                            }
                            else
                            {
                                if (result.actionTable[actionTableKey].Item1 != Action.Shift)
                                {
                                    throw new Exception("Shift Reduce Conflict");
                                }
                            }
                        }
                    }
                    else
                    {
                        int reducingProductionNumber = this.IndexOf(item.Production);
                        Console.WriteLine("    action({0},{1}) = reduce {2}", subsetNumber, item.LookAhead, reducingProductionNumber);
                        var actionTableKey = Tuple.Create(subsetNumber, item.LookAhead);
                        if (!result.actionTable.ContainsKey(actionTableKey))
                        {
                            result.actionTable.Add(actionTableKey, Tuple.Create(Action.Reduce, reducingProductionNumber));
                        }
                        else
                        {
                            if (result.actionTable[actionTableKey].Item1 == Action.Shift)
                            {
                                throw new Exception("Shift Reduce Conflict");
                            }
                            else if (result.actionTable[actionTableKey].Item1 == Action.Reduce)
                            {
                                throw new Exception("Reduce Reduce Conflict");
                            }
                        }                           
                    }
                }
            }
            Console.WriteLine();
            
            Console.WriteLine("Subset links");
            foreach (var numberedSubsetLink in numberedSubsetLinks)
            {
                if (numberedSubsetLink.Item2 is NonTerminal)
                {
                    result.gotoTable.Add(Tuple.Create(numberedSubsetLink.Item1, numberedSubsetLink.Item2 as NonTerminal), numberedSubsetLink.Item3);
                }
                Console.WriteLine(numberedSubsetLink);
            }
            Console.WriteLine();

            Console.WriteLine("Action table");
            Console.WriteLine(string.Join("\n", result.actionTable));
            Console.WriteLine();
            Console.WriteLine("Goto table");
            Console.WriteLine(string.Join("\n", result.gotoTable));
            Console.WriteLine();
            return result;
        }

        private ItemSet EpsilonClosure(HashSet<Item> seed, List<Tuple<Item, Symbol, Item>> links)
        {
            ItemSet result = new ItemSet();
            HashSet<Item> enqueued = new HashSet<Item>();
            Queue<Item> queue = new Queue<Item>();
            foreach (var item in seed)
            {
                enqueued.Add(item);
                queue.Enqueue(item);
            }

            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                result.Add(item);
                foreach (var epsilonNeighbor in links.Where(l => l.Item1.Equals(item) && l.Item2.Equals(Epsilon.Instance)).Select(t => t.Item3))
                {
                    if (!enqueued.Contains(epsilonNeighbor))
                    {
                        enqueued.Add(epsilonNeighbor);
                        queue.Enqueue(epsilonNeighbor);
                    }
                }
            }

            return result;
        }

        private IEnumerable<Item> ReduceMoves(Item currentItem)
        {
            if (currentItem.DotRight.Count > 0)
            {
                var firstRightSymbol = currentItem.DotRight.First();
                if (firstRightSymbol is NonTerminal)
                {
                    var firstRightNonTerminal = firstRightSymbol as NonTerminal;
                    var afterReducingNonTerminalWithLookahead = new List<Symbol>(currentItem.DotRight.Skip(1));
                    afterReducingNonTerminalWithLookahead.Add(currentItem.LookAhead);
                    foreach (var reducingProduction in this.Where(t => t.Left.Equals(firstRightNonTerminal)))
                    {
                        foreach (var terminal in FirstSetOfSequence(afterReducingNonTerminalWithLookahead))
                        {
                            var reducingItem = new Item(reducingProduction, new List<Symbol>(), new List<Symbol>(reducingProduction.Right), terminal);
                            yield return reducingItem;
                        }
                    }
                }
            }
        }

        private IEnumerable<Tuple<Symbol, Item>> ShiftMoves(Item currentItem)
        {
            if (currentItem.DotRight.Count > 0)
            {
                var toShift = currentItem.DotRight.First();
                var newLeft = new List<Symbol>(currentItem.DotLeft);
                newLeft.Add(toShift);
                var newRight = new List<Symbol>(currentItem.DotRight.Skip(1));
                var shiftItem = new Item(currentItem.Production, newLeft, newRight, currentItem.LookAhead);
                yield return Tuple.Create(toShift, shiftItem);
            }
        }
    }

    class Parser
    {
        public Dictionary<Tuple<int, Terminal>, Tuple<Action, int>> actionTable;
        public Dictionary<Tuple<int, NonTerminal>, int> gotoTable;
        private Grammar grammar;

        public Parser(Grammar grammar)
        {
            this.grammar = grammar;
            this.actionTable = new Dictionary<Tuple<int, Terminal>, Tuple<Action, int>>();
            this.gotoTable = new Dictionary<Tuple<int, NonTerminal>, int>();
        }

        public void Parse(List<Terminal> source)
        {
            Stack<Tuple<int, Symbol>> stack = new Stack<Tuple<int, Symbol>>();
            stack.Push(Tuple.Create<int, Symbol>(0, Eof.Instance));
            while (true)
            {
                Terminal lookAhead = source.First();
                var action = actionTable[Tuple.Create(stack.Peek().Item1, lookAhead)];
                Console.WriteLine(action);
                if (action.Item1 == Action.Shift)
                {
                    source.RemoveAt(0);
                    stack.Push(Tuple.Create(action.Item2, lookAhead as Symbol));
                }
                else if (action.Item1 == Action.Reduce)
                {
                    Production reducingProduction = this.grammar[action.Item2];
                    int countToPop = reducingProduction.Right.Count;
                    for (int i = 0; i < countToPop; i++) { stack.Pop(); }
                    int reduceTo = gotoTable[Tuple.Create(stack.Peek().Item1, reducingProduction.Left)];
                    stack.Push(Tuple.Create(reduceTo, reducingProduction.Left as Symbol));
                }
                else
                {
                    break;
                }
            }
        }
    }

    enum Action
    {
        Shift,
        Reduce,
        Accept
    }
}
