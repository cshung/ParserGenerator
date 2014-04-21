namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;
    using System.Diagnostics;

    [DebuggerDisplay("{ToString()}")]
    public class ExplicitCharacterClass : LeafCharacterClass
    {
        public HashSet<char> Elements { get; private set; }

        public ExplicitCharacterClass()
        {
            this.Elements = new HashSet<char>();
        }

        public override void FindLeafCharacterClasses(List<LeafCharacterClass> solution)
        {
            solution.Add(this);
        }

        public override bool Contains(char c)
        {
            return this.Elements.Contains(c);
        }

        public override string ToString()
        {
            return string.Format("{{{0}}}", string.Join(", ", Elements));
        }
    }
}