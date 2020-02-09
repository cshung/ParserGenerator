namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;

    public abstract class RegularExpression
    {
        public abstract void FindCharacterSets(List<CharacterClass> solution);
        public abstract Nfa Build();

        public CompiledRegularExpression Compile(bool dumpAutomatas = false)
        {
            // Step 1: Find all sets and breakdown into atoms
            List<CharacterClass> sets = new List<CharacterClass>();
            this.FindCharacterSets(sets);
            List<LeafCharacterClass> atoms = CharacterClassCalculator.BreakDown(sets);

            // Step 2: Build NFA
            Nfa nfa = this.Build();
            if (dumpAutomatas)
            {
                Console.WriteLine(nfa.ToGraphViz());
            }

            // Step 3: Build DFA
            Dfa dfa = nfa.Build();
            if (dumpAutomatas)
            {
                Console.WriteLine(dfa.ToGraphViz());
            }

            // Step 4: Bundle and return
            return new CompiledRegularExpression(atoms, dfa);
        }
    }
}