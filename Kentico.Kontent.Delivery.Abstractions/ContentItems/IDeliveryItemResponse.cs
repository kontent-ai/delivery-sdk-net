using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a content item.
    /// </summary>
    /// <typeparam name="T">The type of a content item in the response.</typeparam>
    public interface IDeliveryItemResponse<out T> : IResponse
    {
        /// <summary>
        /// Gets the content item.
        /// </summary>
        T Item { get; }

        /// <summary>
        /// Gets the linked items and their properties.
        /// </summary>
        dynamic LinkedItems { get; }
    }
}