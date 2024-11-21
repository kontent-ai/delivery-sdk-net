using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.Elements;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.ContentItems
{
    internal class AssetElementValueConverter : IPropertyValueConverter<IEnumerable<IAsset>>
    {
        public IOptionsMonitor<DeliveryOptions> Options { get; }

        public AssetElementValueConverter(IOptionsMonitor<DeliveryOptions> options)
        {
            Options = options;
        }

        public Task<object> GetPropertyValueAsync<TElement>(PropertyInfo property, TElement contentElement, ResolvingContext context) where TElement : IContentElementValue<IEnumerable<IAsset>>
        {
            if (!typeof(IEnumerable<IAsset>).IsAssignableFrom(property.PropertyType))
            {
                throw new InvalidOperationException($"Type of property {property.Name} must implement {nameof(IEnumerable<IAsset>)} in order to receive asset content.");
            }

            if (!(contentElement is AssetElementValue assetElementValue))
            {
                return null;
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

            return Task.FromResult((object)assets);
        }

        private string ResolveAssetUrl(IAsset asset)
        {
            var url = ReplaceAssetUrlWIthCustomAssetUrl(asset.Url);
            var renditionPresetToBeApplied = Options.CurrentValue.DefaultRenditionPreset;
            if (renditionPresetToBeApplied == null || asset.Renditions == null)
                return url;

            return asset.Renditions.TryGetValue(renditionPresetToBeApplied, out var renditionToBeApplied)
                ? $"{url}?{renditionToBeApplied.Query}"
                : url;
        }

        /// <summary>
        /// Replace the beginning part of the asset URL with the AssetUrlReplacement value.
        /// </summary>
        /// <param name="url">Original Asset Url</param>
        /// <returns>New URL with the CDN URL replaces with AssetUrlReplacement</returns>
        private string ReplaceAssetUrlWIthCustomAssetUrl(string url)
        {
            if (!string.IsNullOrEmpty(Options.CurrentValue.AssetUrlReplacement))
            {
                // Replace the beginning part of the asset URL with the AssetUrlReplacement value by taking the third forward slash as the ending point for the string replacement
                var endOfUrlIndex = url.IndexOf("/", url.IndexOf("/", url.IndexOf("/", 0) + 1) + 1);
                if (endOfUrlIndex > 0)
                {
                    return Options.CurrentValue.AssetUrlReplacement + url.Substring(endOfUrlIndex);
                }
            }
            return url;
        }
    }
}
