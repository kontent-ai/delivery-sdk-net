using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    public class SkipFilter
    {
        public string Element { get; }
        public string Operator { get; }
        public string Value { get; }

        public SkipFilter(int value)
        {
            Element = "skip";
            Value = value.ToString(CultureInfo.InvariantCulture);
        }

        public string GetQueryStringParameter()
        {
            return String.Format("{0}={1}", Uri.EscapeDataString(Element), Uri.EscapeDataString(Value));
        }
    }
}
