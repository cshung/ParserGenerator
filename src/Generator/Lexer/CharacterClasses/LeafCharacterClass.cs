namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;

    public abstract class LeafCharacterClass : CharacterClass
    {
        public LeafCharacterClass()
        {
            this.Origins = new List<LeafCharacterClass>();
        }

        public override void FindLeafCharacterClasses(List<LeafCharacterClass> solution)
        {
            solution.Add(this);
        }

        public static LeafCharacterClass Others = new OtherSet();

        public List<LeafCharacterClass> Origins { get; private set; }

        public void AddOrigin(LeafCharacterClass immediateParent)
        {
            this.Origins.AddRange(immediateParent.Origins);
        }

        public void SetOriginal()
        {
            this.Origins.Add(this);
        }

        public override void PickAtoms(List<LeafCharacterClass> allAtoms)
        {
            // no-op - computation of leaf set is done externally
        }

        public abstract bool Contains(char c);

        public class OtherSet : LeafCharacterClass
        {
            public override void FindLeafCharacterClasses(List<LeafCharacterClass> solution)
            {
                throw new NotImplementedException();
            }

            public override bool Contains(char c)
            {
                return true;
            }
        }
    }
}