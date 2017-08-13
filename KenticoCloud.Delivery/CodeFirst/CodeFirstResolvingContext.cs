using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Context of the current resolving process
    /// </summary>
    public class CodeFirstResolvingContext
    {
        /// <summary>
        /// Gets the content item within current resolving context
        /// </summary>
        public Func<string, object> GetModularContentItem { get; internal set; }

        /// <summary>
        /// Gets the Delivery client used for retrieving the data
        /// </summary>
        public IDeliveryClient Client { get; internal set; }
    }
}
