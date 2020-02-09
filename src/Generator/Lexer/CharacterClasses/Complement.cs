namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;
    using System.Linq;

    public class Complement : CharacterClass
    {
        public CharacterClass Operand { get; set; }

        public override void FindLeafCharacterClasses(List<LeafCharacterClass> solution)
        {
            this.Operand.FindLeafCharacterClasses(solution);
        }

        public override void PickAtoms(List<LeafCharacterClass> allAtoms)
        {
            this.Atoms.AddRange(allAtoms.Except(this.Operand.Atoms));
        }
    }
}