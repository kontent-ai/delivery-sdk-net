using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    public class OrderFilter : IFilter
    {
        public string Element { get; }
        public string Operator { get; }
        public string Value { get; }

        public OrderFilter(string element, OrderDirection orderDirection = OrderDirection.Ascending)
        {
            Element = "order";
            Value = element;
            Operator = orderDirection == OrderDirection.Ascending ? "[asc]" : "[desc]";
        }

        public string GetQueryStringParameter()
        {
            return String.Format("{0}={1}{2}", Uri.EscapeDataString(Element), Uri.EscapeDataString(Value), Operator);
        }
    }
}
