using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    public class DepthFilter : IFilter
    {
        public string Element { get; }
        public string Operator { get; }
        public string Value { get; }

        public DepthFilter(int value)
        {
            Element = "depth";
            Value = value.ToString(CultureInfo.InvariantCulture);
        }

        public string GetQueryStringParameter()
        {
            return String.Format("{0}={1}", Uri.EscapeDataString(Element), Uri.EscapeDataString(Value));
        }
    }
}
