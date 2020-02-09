namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    // TODO: Right associative operator
    // TODO: Operator Precedence
    internal class OperatorConflictResolver : IConflictResolver
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
