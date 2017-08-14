using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Core.Contracts.Resolvers
{
    public interface IRichTextResolver
    {
        /// <summary>
        /// Resolves the input string assuming it contains HTML. The return value depends on the implementation so it's defined as 'object' here.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        object Resolve(string input, string pageUri = null);
    }
}
