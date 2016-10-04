using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    public class InFilter : BaseFilter, IFilter
    {
        public InFilter(string element, string value)
            : base(element, value)
        {
            Operator = "[in]";
        }
    }
}
