using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    public interface IElementsFilter : IFilter
    {
        /// <summary>
        /// Element codename.
        /// </summary>
        string Element { get; }

        /// <summary>
        /// Parameter value.
        /// </summary>
        string Value { get; }

        /// <summary>
        /// Query operator.
        /// </summary>
        string Operator { get; }
    }
}
