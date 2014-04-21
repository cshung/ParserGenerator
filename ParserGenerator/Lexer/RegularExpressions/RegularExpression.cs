namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;

    public abstract class RegularExpression
    {
        public abstract void FindCharacterSets(List<CharacterClass> solution);
        public abstract Nfa Build();

        public CompiledRegularExpression Compile()
        {
            // Step 1: Find all sets and breakdown into atoms
            List<CharacterClass> sets = new List<CharacterClass>();
            this.FindCharacterSets(sets);
            List<LeafCharacterClass> atoms = CharacterClassCalculator.BreakDown(sets);

            // Step 2: Build NFA
            Nfa nfa = this.Build();

            // Step 3: Build DFA
            Dfa dfa = nfa.Build();

            // Step 4: Bundle and return
            return new CompiledRegularExpression(atoms, dfa);
        }
    }
}