namespace Andrew.ParserGenerator
{
    public class Terminal : Symbol
    {
        public static Terminal eof = new Terminal { DisplayName = "eof" };
        public static Terminal epsilon = new Terminal { DisplayName = "epsilon" };
    }
}
