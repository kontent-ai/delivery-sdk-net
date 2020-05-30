namespace Kentico.Kontent.Delivery.Abstractions.ContentLinks
{
    public interface IContentLink
    {
        string Codename { get; }
        string ContentTypeCodename { get; }
        string Id { get; }
        string UrlSlug { get; }
    }
}