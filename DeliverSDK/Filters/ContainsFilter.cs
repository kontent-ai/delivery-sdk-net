using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliverSDK
{
    public class ContainsFilter : BaseFilter, IFilter
    {
        public ContainsFilter(string element, string value)
            : base (element, value)
        {
            Operator = "[contains]";
        }
    }
}
