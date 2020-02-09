namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;
    using System.Linq;

    public class Union : CharacterClass
    {
        public CharacterClass Left { get; set; }
        public CharacterClass Right { get; set; }

        public override void FindLeafCharacterClasses(List<LeafCharacterClass> solution)
        {
            this.Left.FindLeafCharacterClasses(solution);
            this.Right.FindLeafCharacterClasses(solution);
        }

        public override void PickAtoms(List<LeafCharacterClass> allAtoms)
        {
            this.Atoms.AddRange(this.Left.Atoms.Union(this.Right.Atoms));
        }
    }
}