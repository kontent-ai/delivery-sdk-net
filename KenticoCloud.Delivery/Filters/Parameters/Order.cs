using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents "order" query parameter.
    /// </summary>
    public class Order : IFilter
    {
        /// <summary>
        /// Order direction.
        /// </summary>
        public string OrderDirection { get; }

        /// <summary>
        /// Codename of the element for order.
        /// </summary>
        public string OrderElement { get; }

        /// <summary>
        /// Constructs the order filter.
        /// </summary>
        /// <param name="element">Element codename.</param>
        /// <param name="orderDirection">Order direction.</param>
        public Order(string element, OrderDirection orderDirection = Delivery.OrderDirection.Ascending)
        {
            OrderElement = element;
            OrderDirection = orderDirection == Delivery.OrderDirection.Ascending ? "[asc]" : "[desc]";
        }

        /// <summary>
        /// Returns the query string representation of the filter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            return String.Format("order={0}{1}", Uri.EscapeDataString(OrderElement), OrderDirection);
        }
    }
}