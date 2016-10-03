using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliverSDK
{
    public class LimitFilter
    {
        public string Element { get; }
        public string Operator { get; }
        public string Value { get; }

        public LimitFilter(int value)
        {
            Element = "limit";
            Value = value.ToString(CultureInfo.InvariantCulture);
        }

        public string GetQueryStringParameter()
        {
            return String.Format("{0}={1}", Uri.EscapeDataString(Element), Uri.EscapeDataString(Value));
        }
    }
}
