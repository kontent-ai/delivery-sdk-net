using Kontent.Ai.Delivery.SharedModels;
using System.Text.Json.Serialization;
using IApiResponse = Kontent.Ai.Delivery.Abstractions.IApiResponse; // TODO: Remove this once we adopt ApiResponse from Refit

namespace Kontent.Ai.Delivery.Sync;

internal sealed class DeliverySyncInitResponse : AbstractResponse, IDeliverySyncInitResponse
{
    /// <inheritdoc/>
    public IList<ISyncItem> SyncItems { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliverySyncInitResponse"/>
    /// </summary>
    /// <param name="response"></param>
    /// <param name="syncItems"></param>
    [JsonConstructor]
    public DeliverySyncInitResponse(IApiResponse response, IList<ISyncItem> syncItems)
        : base(response)
    {
        SyncItems = syncItems;
    }

    public DeliverySyncInitResponse(IApiResponse response) : base(response)
    {
    }
}