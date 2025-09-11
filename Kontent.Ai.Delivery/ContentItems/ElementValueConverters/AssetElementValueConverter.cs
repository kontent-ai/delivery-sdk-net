using System.Reflection;
using Kontent.Ai.Delivery.ContentItems.Elements;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.ContentItems;

internal class AssetElementValueConverter(IOptionsMonitor<DeliveryOptions> options) : IPropertyValueConverter<IEnumerable<IAsset>, IList<IAsset>>
{
    public IOptionsMonitor<DeliveryOptions> Options { get; } = options;

    public Task<IList<IAsset>?> GetPropertyValueAsync<TElement>(PropertyInfo property, TElement contentElement, ResolvingContext context) where TElement : IContentElementValue<IEnumerable<IAsset>>
    {
        if (!typeof(IEnumerable<IAsset>).IsAssignableFrom(property.PropertyType))
        {
            throw new InvalidOperationException($"Type of property {property.Name} must implement {nameof(IEnumerable<IAsset>)} in order to receive asset content.");
        }

        if (!(contentElement is AssetElementValue assetElementValue))
        {
            return Task.FromResult<IList<IAsset>?>(null);
        }

        var assets = assetElementValue.Value
            .Select(asset => new Asset
            {
                Description = asset.Description,
                Name = asset.Name,
                Height = asset.Height,
                Width = asset.Width,
                Renditions = asset.Renditions,
                Size = asset.Size,
                Type = asset.Type,
                Url = ResolveAssetUrl(asset),
            })
            .Cast<IAsset>()
            .ToList();

        return Task.FromResult<IList<IAsset>?>(assets);
    }

    private string ResolveAssetUrl(IAsset asset)
    {
        var renditionPresetToBeApplied = Options.CurrentValue.DefaultRenditionPreset;
        if (renditionPresetToBeApplied == null || asset.Renditions == null)
            return asset.Url;

        return asset.Renditions.TryGetValue(renditionPresetToBeApplied, out var renditionToBeApplied)
            ? $"{asset.Url}?{renditionToBeApplied.Query}"
            : asset.Url;
    }
}
