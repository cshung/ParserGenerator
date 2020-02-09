namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class LexicalAnalyzer
    {
        public List<Tuple<CompiledRegularExpression, Terminal, Action<Token>>> Specification { get; set; }

        public IEnumerable<Token> Analyze(string s)
        {
            while (s.Length > 0)
            {   
                List<Tuple<string, Terminal, Action<Token>>> matches = Specification.Select(c => Tuple.Create(c.Item1.LongestMatch(s), c.Item2, c.Item3)).Where(m => m.Item1 != null).ToList();
                if (matches.Count != 0)
                {
                    int maximalLength = matches.Select(m => m.Item1.Length).Max();
                    var match = matches.First(m => m.Item1.Length == maximalLength);
                    if (match.Item2 != null)
                    {
                        Token token = new Token { Symbol = match.Item2, SemanticValue = match.Item1 };
                        if (match.Item3 != null)
                        {
                            match.Item3(token);
                        }

                        yield return token;
                    }
                    s = s.Substring(maximalLength);
                }
                else
                {
                    Console.Error.WriteLine("Input matches no rule");
                    yield break;
                }
            }
        }
    }
}