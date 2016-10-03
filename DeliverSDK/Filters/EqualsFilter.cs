using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliverSDK
{
    public class EqualsFilter : BaseFilter, IFilter
    {
        public EqualsFilter(string element, string value)
            : base (element, value)
        {
        }
    }
}
