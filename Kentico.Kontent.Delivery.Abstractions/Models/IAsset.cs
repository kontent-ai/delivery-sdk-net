namespace Kentico.Kontent.Delivery.Abstractions
{
    public interface IAsset: IImage
    {
        string Description { get; set; }
        int Height { get; set; }
        string Name { get; }
        int Size { get; }
        string Type { get; }
        string Url { get; set; }
        int Width { get; set; }
    }
}