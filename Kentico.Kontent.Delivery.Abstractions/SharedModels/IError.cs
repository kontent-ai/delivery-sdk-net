﻿namespace Kentico.Kontent.Delivery.Abstractions.SharedModels
{
    /// <summary>
    /// Represents a faulty JSON response from Kontent Delivery API.
    /// </summary>
    public interface IError
    {
        /// <summary>
        /// Gets error Message.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets the ID of a request that can be used for troubleshooting.
        /// </summary>
        string RequestId { get; }

        /// <summary>
        /// Gets Kontent Delivery API error code. Check the Message property for more information
        /// </summary>
        int ErrorCode { get; }

        /// <summary>
        /// Gets specific code of error.
        /// </summary>
        int SpecificCode { get; }
    }
}
