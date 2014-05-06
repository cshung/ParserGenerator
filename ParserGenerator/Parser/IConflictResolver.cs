namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal interface IConflictResolver
    {
        bool? ShouldFirstOverrideSecond(ParserItem first, ParserItem second);
    }
}
