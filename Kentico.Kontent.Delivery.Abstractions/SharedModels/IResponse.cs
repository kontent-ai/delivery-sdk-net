﻿namespace Kentico.Kontent.Delivery.Abstractions.SharedModels
{
    /// <summary>
    /// A base interface for all response envelopes.
    /// </summary>
    public interface IResponse
    {
        /// <summary>
        /// A successful JSON response from the Kontent Delivery API.
        /// </summary>
        public IApiResponse ApiResponse { get; }
    }
}
