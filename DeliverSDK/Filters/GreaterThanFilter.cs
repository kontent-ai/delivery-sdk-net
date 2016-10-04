using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    public class GreaterThanFilter : BaseFilter, IFilter
    {
        public GreaterThanFilter(string element, string value)
            : base(element, value)
        {
            Operator = "[gt]";
        }
    }
}
