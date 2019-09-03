using System;
using System.Threading;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a content type.
    /// </summary>
    public sealed class DeliveryTypeResponse : AbstractResponse
    {
        private readonly Lazy<ContentType> _type;

        /// <summary>
        /// Gets the content type.
        /// </summary>
        public ContentType Type => _type.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Cloud Delivery API that contains a content type.</param>
        internal DeliveryTypeResponse(ApiResponse response) : base(response)
        {
            _type = new Lazy<ContentType>(() => new ContentType(_response.Content), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Implicitly converts the specified <paramref name="response"/> to a content type.
        /// </summary>
        /// <param name="response">The response to convert.</param>
        public static implicit operator ContentType(DeliveryTypeResponse response) => response.Type;
    }
}
