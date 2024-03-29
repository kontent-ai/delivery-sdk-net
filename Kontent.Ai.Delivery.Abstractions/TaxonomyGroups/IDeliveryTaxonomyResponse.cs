﻿namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents a response from Kontent.ai Delivery API that contains a taxonomy group.
    /// </summary>
    public interface IDeliveryTaxonomyResponse : IResponse
    {
        /// <summary>
        /// Gets the taxonomy group.
        /// </summary>
        ITaxonomyGroup Taxonomy { get; }
    }
}