using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliverSDK
{
    public class ElementsFilter : IFilter
    {
        public string Element { get; }
        public string Operator { get; }
        public string Value { get; }

        public ElementsFilter(params string[] values)
        {
            Element = "elements";
            Value = String.Join(",", values);
        }

        public string GetQueryStringParameter()
        {
            return String.Format("{0}={1}", Uri.EscapeDataString(Element), Uri.EscapeDataString(Value));
        }
    }
}
