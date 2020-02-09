namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;

    public abstract class CharacterClass
    {
        public CharacterClass()
        {
            this.Atoms = new List<LeafCharacterClass>();
        }

        public abstract void FindLeafCharacterClasses(List<LeafCharacterClass> solution);

        public List<LeafCharacterClass> Atoms { get; private set; }

        public abstract void PickAtoms(List<LeafCharacterClass> allAtoms);
    }
}