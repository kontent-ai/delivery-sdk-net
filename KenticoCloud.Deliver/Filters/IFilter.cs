using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents a query parameter filter.
    /// </summary>
    public interface IFilter
    {
        /// <summary>
        /// Returns the query string representation of the filter.
        /// </summary>
        string GetQueryStringParameter();
    }
}
