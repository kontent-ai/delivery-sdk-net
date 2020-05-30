namespace Kentico.Kontent.Delivery.Abstractions.Responses
{
    public interface IDeliveryTaxonomyResponse : IResponse
    {
        ITaxonomyGroup Taxonomy { get; }
    }
}