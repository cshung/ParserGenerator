﻿namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;
    using System.Linq;

    public class Intersect : CharacterClass
    {
        public CharacterClass Left { get; set; }
        public CharacterClass Right { get; set; }

        public override void FindLeafCharacterClasses(List<LeafCharacterClass> solution)
        {
            this.Left.FindLeafCharacterClasses(solution);
            this.Right.FindLeafCharacterClasses(solution);
        }

        public override void Compute(List<LeafCharacterClass> allAtoms)
        {
            this.Atoms.AddRange(this.Left.Atoms.Intersect(this.Right.Atoms));
        }
    }
}