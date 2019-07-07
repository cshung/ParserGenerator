namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;
    using System.Linq;

    public class SetComparer<T> : IEqualityComparer<HashSet<T>>
    {
        public bool Equals(HashSet<T> set1, HashSet<T> set2)
        {
            return set1.Except(set2).Count() == 0 && set2.Except(set1).Count() == 0;
        }

        public int GetHashCode(HashSet<T> obj)
        {
            // This guarantee same hash code for same set
            return obj.Select(t => t.GetHashCode()).Aggregate(0, (x, y) => x ^ y);
        }
    }
}
