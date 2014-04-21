namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class LexicalAnalyzer
    {
        public List<Tuple<RegularExpression, Action<string>>> Specification { get; set; }

        public void Analyze(string s)
        {
            while (s.Length > 0)
            {
                List<Tuple<CompiledRegularExpression, Action<string>>> compiledSpecification = this.Specification.Select(spec => Tuple.Create(spec.Item1.Compile(), spec.Item2)).ToList();
                List<Tuple<string, Action<string>>> matches = compiledSpecification.Select(c => Tuple.Create(c.Item1.LongestMatch(s), c.Item2)).Where(m => m.Item1 != null).ToList();
                if (matches.Count != 0)
                {
                    int maximalLength = matches.Select(m => m.Item1.Length).Max();
                    Tuple<string, Action<string>> match = matches.First(m => m.Item1.Length == maximalLength);
                    match.Item2(match.Item1);
                    s = s.Substring(maximalLength);
                }
                else
                {
                    Console.WriteLine("Input matches no rule");
                    break;
                }
            }
        }
    }
}