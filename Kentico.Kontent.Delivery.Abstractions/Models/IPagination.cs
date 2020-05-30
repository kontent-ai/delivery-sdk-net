namespace Kentico.Kontent.Delivery.Abstractions
{
    public interface IPagination
    {
        int Count { get; }
        int Limit { get; }
        string NextPageUrl { get; }
        int Skip { get; }
        int? TotalCount { get; }
    }
}