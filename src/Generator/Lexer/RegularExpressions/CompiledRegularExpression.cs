namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;
    using System.Linq;

    public class CompiledRegularExpression
    {
        private List<LeafCharacterClass> atoms;
        private Dfa dfa;

        public CompiledRegularExpression(List<LeafCharacterClass> atoms, Dfa dfa)
        {
            this.atoms = atoms;
            this.dfa = dfa;
        }

        public bool Match(string s)
        {
            DfaNode state = dfa.StartNode;
            bool rejected = false;
            foreach (char c in s)
            {
                var atom = atoms.First(a => a.Contains(c));
                DfaEdge found = null;
                foreach (var edge in dfa.Edges)
                {
                    if (edge.SourceNode == state && edge.Symbol == atom)
                    {
                        found = edge;
                        break;
                    }
                }
                if (found != null)
                {
                    state = found.TargetNode;
                }
                else
                {
                    rejected = true;
                    break;
                }
            }

            return rejected ? false : state.IsFinal;
        }

        public string LongestMatch(string s)
        {
            DfaNode state = dfa.StartNode;
            int bestMatch = -1;
            int count = 0;
            bool rejected = false;
            foreach (char c in s)
            {
                count++;
                var atom = atoms.First(a => a.Contains(c));
                DfaEdge found = null;
                foreach (var edge in dfa.Edges)
                {
                    if (edge.SourceNode == state && edge.Symbol == atom)
                    {
                        found = edge;
                        break;
                    }
                }
                if (found != null)
                {
                    state = found.TargetNode;
                    if (state.IsFinal)
                    {
                        bestMatch = count;
                    }
                }
                else
                {
                    rejected = true;
                    break;
                }
            }
            if (!rejected && state.IsFinal)
            {
                bestMatch = count;
            }
            if (bestMatch == -1)
            {
                return null;
            }
            else
            {
                return s.Substring(0, bestMatch);
            }
        }
    }
}