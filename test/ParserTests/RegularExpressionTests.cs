namespace Andrew.Parser.Tests
{
    using System;
    using Andrew.ParserGenerator;
    using Xunit;

    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            string s = "Hel*o|W(or)[^0-9][b-d]";
            RegularExpression result = new RegularExpressionParser().Parse(s);
            CompiledRegularExpression compiled = result.Compile(dumpAutomatas: false);
            Assert.True(compiled.Match("Hello"));
            Assert.True(compiled.Match("World"));
        }
    }
}
