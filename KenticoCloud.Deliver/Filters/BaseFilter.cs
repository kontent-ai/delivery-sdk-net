using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    public class BaseFilter : IFilter
    {
        public string Element { get; protected set; }
        public string Operator { get; protected set; }
        public string Value { get; protected set; }

        public BaseFilter(string element, string value)
        {
            Element = element;
            Value = value;
        }

        public string GetQueryStringParameter()
        {
            return String.Format("{0}{1}={2}", Uri.EscapeDataString(Element), Operator, Uri.EscapeDataString(Value));
        }
    }
}
