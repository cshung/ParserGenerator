namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;

    public static class HashSetExtensions
    {
        public static void AddRange<T>(this HashSet<T> thisPtr, IEnumerable<T> elements)
        {
            foreach (var elem in elements)
            {
                thisPtr.Add(elem);
            }
        }
    }
}