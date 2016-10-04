using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    public class RangeFilter : BaseFilter, IFilter
    {
        public RangeFilter(string element, string value)
            : base(element, value)
        {
            Operator = "[range]";
        }
    }
}
