namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Builds canonical cache dependency keys used for invalidation.
/// </summary>
internal static class CacheDependencyKeyBuilder
{
    private const string ItemPrefix = "item_";
    private const string AssetPrefix = "asset_";
    private const string TaxonomyPrefix = "taxonomy_";
    private const string TypePrefix = "type_";

    public static string? BuildItemDependencyKey(string? codename) => BuildWithPrefix(ItemPrefix, codename);

    public static string BuildAssetDependencyKey(Guid assetId) => $"{AssetPrefix}{assetId}";

    public static string? BuildTaxonomyDependencyKey(string? taxonomyCodename) =>
        BuildWithPrefix(TaxonomyPrefix, taxonomyCodename);

    public static string? BuildTypeDependencyKey(string? typeCodename) =>
        BuildWithPrefix(TypePrefix, typeCodename);

    private static string? BuildWithPrefix(string prefix, string? value) => string.IsNullOrWhiteSpace(value) ? null : $"{prefix}{value}";
}
