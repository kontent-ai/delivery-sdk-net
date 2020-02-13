using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kentico.Kontent.Delivery.Cache
{
    /// <summary>
    /// A helper for creating cache dependency keys
    /// </summary>
    public static class CacheHelpers
    {
        #region Constants

        private const int MAX_DEPENDENCY_ITEMS = 50;

        private const string CONTENT_ITEM_IDENTIFIER = "content_item";
        private const string CONTENT_ITEM_TYPED_IDENTIFIER = "content_item_typed";
        private const string CONTENT_ITEM_JSON_IDENTIFIER = "content_item_json";
        private const string CONTENT_ITEM_LISTING_IDENTIFIER = "content_item_listing";
        private const string CONTENT_ITEM_LISTING_TYPED_IDENTIFIER = "content_item_listing_typed";
        private const string CONTENT_ITEM_LISTING_JSON_IDENTIFIER = "content_item_listing_json";

        private const string CONTENT_TYPE_IDENTIFIER = "content_type";
        private const string CONTENT_TYPE_JSON_IDENTIFIER = "content_type_json";
        private const string CONTENT_TYPE_LISTING_IDENTIFIER = "content_type_listing";
        private const string CONTENT_TYPE_LISTING_JSON_IDENTIFIER = "content_type_listing_json";

        private const string TAXONOMY_GROUP_IDENTIFIER = "taxonomy_group";
        private const string TAXONOMY_GROUP_JSON_IDENTIFIER = "taxonomy_group_json";
        private const string TAXONOMY_GROUP_LISTING_IDENTIFIER = "taxonomy_group_listing";
        private const string TAXONOMY_GROUP_LISTING_JSON_IDENTIFIER = "taxonomy_group_listing_json";

        private const string CONTENT_TYPE_ELEMENT_IDENTIFIER = "content_type_element";

        private const string DEPENDENCY_ITEM = "dependency_item";
        private const string DEPENDENCY_ITEM_LISTING = "dependency_item_listing";
        private const string DEPENDENCY_TYPE_LISTING = "dependency_type_listing";
        private const string DEPENDENCY_TAXONOMY_GROUP = "dependency_taxonomy_group";
        private const string DEPENDENCY_TAXONOMY_GROUP_LISTING = "dependency_taxonomy_group_listing";

        #endregion

        #region API keys

        public static string GetItemJsonKey(string codename, params string[] parameters)
        {
            return StringHelpers.Join(new[] { CONTENT_ITEM_JSON_IDENTIFIER, codename }.Concat(parameters ?? Enumerable.Empty<string>()));
        }

        public static string GetItemKey(string codename, IEnumerable<IQueryParameter> parameters)
        {
            return StringHelpers.Join(new[] { CONTENT_ITEM_IDENTIFIER, codename }.Concat(parameters?.Select(x => x.GetQueryStringParameter()) ?? Enumerable.Empty<string>()));
        }

        public static string GetItemTypedKey(string codename, IEnumerable<IQueryParameter> parameters)
        {
            return StringHelpers.Join(new[] { CONTENT_ITEM_TYPED_IDENTIFIER, codename }.Concat(parameters?.Select(x => x.GetQueryStringParameter()) ?? Enumerable.Empty<string>()));
        }

        public static string GetItemsJsonKey(params string[] parameters)
        {
            return StringHelpers.Join(new[] { CONTENT_ITEM_LISTING_JSON_IDENTIFIER }.Concat(parameters));
        }

        public static string GetItemsKey(IEnumerable<IQueryParameter> parameters)
        {
            return StringHelpers.Join(new[] { CONTENT_ITEM_LISTING_IDENTIFIER }.Concat(parameters?.Select(x => x.GetQueryStringParameter()) ?? Enumerable.Empty<string>()));
        }

        public static string GetItemsTypedKey(IEnumerable<IQueryParameter> parameters)
        {
            return StringHelpers.Join(new[] { CONTENT_ITEM_LISTING_TYPED_IDENTIFIER }.Concat(parameters?.Select(x => x.GetQueryStringParameter()) ?? Enumerable.Empty<string>()));
        }

        public static string GetTypeJsonKey(string codename)
        {
            return StringHelpers.Join(CONTENT_TYPE_JSON_IDENTIFIER, codename);
        }

        public static string GetTypeKey(string codename)
        {
            return StringHelpers.Join(CONTENT_TYPE_IDENTIFIER, codename);
        }

        public static string GetTypesJsonKey(params string[] parameters)
        {
            return StringHelpers.Join(new[] { CONTENT_TYPE_LISTING_JSON_IDENTIFIER }.Concat(parameters ?? Enumerable.Empty<string>()));
        }

        public static string GetTypesKey(IEnumerable<IQueryParameter> parameters)
        {
            return StringHelpers.Join(new[] { CONTENT_TYPE_LISTING_IDENTIFIER }.Concat(parameters?.Select(x => x.GetQueryStringParameter()) ?? Enumerable.Empty<string>()));
        }

        public static string GetTaxonomyJsonKey(string codename)
        {
            return StringHelpers.Join(TAXONOMY_GROUP_JSON_IDENTIFIER, codename);
        }

        public static string GetTaxonomyKey(string codename)
        {
            return StringHelpers.Join(TAXONOMY_GROUP_IDENTIFIER, codename);
        }

        public static string GetTaxonomiesJsonKey(params string[] parameters)
        {
            return StringHelpers.Join(new[] { TAXONOMY_GROUP_LISTING_JSON_IDENTIFIER }.Concat(parameters ?? Enumerable.Empty<string>()));
        }

        public static string GetTaxonomiesKey(IEnumerable<IQueryParameter> parameters)
        {
            return StringHelpers.Join(new[] { TAXONOMY_GROUP_LISTING_IDENTIFIER }.Concat(parameters?.Select(x => x.GetQueryStringParameter()) ?? Enumerable.Empty<string>()));
        }

        public static string GetContentElementKey(string contentTypeCodename, string contentElementCodename)
        {
            return StringHelpers.Join(CONTENT_TYPE_ELEMENT_IDENTIFIER, contentTypeCodename, contentElementCodename);
        }

        #endregion

        #region Dependency keys

        public static string GetItemDependencyKey(string codename)
        {
            return StringHelpers.Join(DEPENDENCY_ITEM, codename);
        }

        public static string GetItemsDependencyKey()
        {
            return DEPENDENCY_ITEM_LISTING;
        }

        public static string GetTypesDependencyKey()
        {
            return DEPENDENCY_TYPE_LISTING;
        }

        public static string GetTaxonomyDependencyKey(string codename)
        {
            return StringHelpers.Join(DEPENDENCY_TAXONOMY_GROUP, codename);
        }

        public static string GetTaxonomiesDependencyKey()
        {
            return DEPENDENCY_TAXONOMY_GROUP_LISTING;
        }

        #endregion

        #region Dependecies

        public static IEnumerable<string> GetItemJsonDependencies(JObject response)
        {
            var dependencies = new HashSet<string>();

            if (IsItemResponse(response))
            {
                if (TryExtractCodename(response["item"] as JObject, out var codename))
                {
                    var dependencyKey = GetItemDependencyKey(codename);
                    dependencies.Add(dependencyKey);
                }

                foreach (var property in response["modular_content"]?.Children<JProperty>() ?? Enumerable.Empty<JProperty>())
                {
                    if (!IsComponent(property) && TryExtractCodename(property.Value as JObject, out var linkedItemCodename))
                    {
                        var dependencyKey = GetItemDependencyKey(linkedItemCodename);
                        dependencies.Add(dependencyKey);
                    }
                }
            }

            return dependencies.Count > MAX_DEPENDENCY_ITEMS
                ? new[] { GetItemsDependencyKey() }
                : dependencies.AsEnumerable();

            bool TryExtractCodename(JObject item, out string codename)
            {
                codename = item?["system"]?["codename"]?.Value<string>();
                return codename != null;
            }
        }

        public static IEnumerable<string> GetItemDependencies(dynamic response)
        {
            var dependencies = new HashSet<string>();

            if (!IsItemResponse(response))
            {
                return dependencies;
            }

            var codename = response.Item?.System?.Codename?.ToString();
            if (codename != null)
            {
                var dependencyKey = GetItemDependencyKey(codename);
                dependencies.Add(dependencyKey);
            }

            foreach (var modularItem in response.LinkedItems)
            {
                if (modularItem is JProperty property && !IsComponent(property))
                {
                    var linkedItemCodename = property.Value?["system"]?["codename"]?.Value<string>();
                    if (linkedItemCodename != null)
                    {
                        var dependencyKey = GetItemDependencyKey(linkedItemCodename);
                        dependencies.Add(dependencyKey);
                    }
                }
            }


            return dependencies.Count > MAX_DEPENDENCY_ITEMS
                ? new[] { GetItemsDependencyKey() }
                : dependencies.AsEnumerable();
        }

        public static IEnumerable<string> GetItemsJsonDependencies(JObject response)
        {
            return IsItemListingResponse(response)
                ? new[] { GetItemsDependencyKey() }
                : Enumerable.Empty<string>();
        }

        public static IEnumerable<string> GetItemsDependencies(dynamic response)
        {
            return IsItemListingResponse(response)
                ? new[] { GetItemsDependencyKey() }
                : Enumerable.Empty<string>();
        }

        public static IEnumerable<string> GetTypeJsonDependencies(JObject response)
        {
            return response?["system"]?["codename"] != null
                ? new[] { GetTypesDependencyKey() }
                : Enumerable.Empty<string>();
        }

        public static IEnumerable<string> GetTypeDependencies(DeliveryTypeResponse response)
        {
            return response?.Type?.System?.Codename != null
                ? new[] { GetTypesDependencyKey() }
                : Enumerable.Empty<string>();
        }

        public static IEnumerable<string> GetTypesJsonDependencies(JObject response)
        {
            return response?["types"] != null
                ? new[] { GetTypesDependencyKey() }
                : Enumerable.Empty<string>();
        }

        public static IEnumerable<string> GetTypesDependencies(DeliveryTypeListingResponse response)
        {
            return response?.Types != null
                ? new[] { GetTypesDependencyKey() }
                : Enumerable.Empty<string>();
        }

        public static IEnumerable<string> GetContentElementDependencies(DeliveryElementResponse response)
        {
            return response?.Element?.Codename != null
                ? new[] { GetTypesDependencyKey() }
                : Enumerable.Empty<string>();
        }

        public static IEnumerable<string> GetTaxonomyJsonDependencies(JObject response)
        {
            return response?["system"]?["codename"] != null
                ? new[] { GetTaxonomyDependencyKey(response["system"]["codename"].Value<string>()) }
                : Enumerable.Empty<string>();
        }

        public static IEnumerable<string> GetTaxonomyDependencies(DeliveryTaxonomyResponse response)
        {
            return response?.Taxonomy?.System?.Codename != null
                ? new[] { GetTaxonomyDependencyKey(response.Taxonomy.System.Codename) }
                : Enumerable.Empty<string>();
        }

        public static IEnumerable<string> GetTaxonomiesJsonDependencies(JObject response)
        {
            return response?["taxonomies"] != null
                ? new[] { GetTaxonomiesDependencyKey() }
                : Enumerable.Empty<string>();
        }

        public static IEnumerable<string> GetTaxonomiesDependencies(DeliveryTaxonomyListingResponse response)
        {
            return response?.Taxonomies != null
                ? new[] { GetTaxonomiesDependencyKey() }
                : Enumerable.Empty<string>();
        }

        #endregion

        private static bool IsItemResponse(JObject response)
        {
            return response?["item"] != null;
        }

        private static bool IsItemResponse(dynamic response)
        {
            return response is DeliveryItemResponse
                   || response.GetType().IsGenericType
                   && response.GetType().GetGenericTypeDefinition() == typeof(DeliveryItemResponse<>)
                   && response.Item != null;
        }

        private static bool IsItemListingResponse(JObject response)
        {
            return response?["items"] != null;
        }

        private static bool IsItemListingResponse(dynamic response)
        {
            return response is DeliveryItemListingResponse ||
                   response.GetType().IsGenericType &&
                   response.GetType().GetGenericTypeDefinition() == typeof(DeliveryItemListingResponse<>);
        }

        private static bool IsComponent(JProperty property)
        {
            // Components have substring 01 in its id starting at position 14.
            // xxxxxxxx-xxxx-01xx-xxxx-xxxxxxxxxxxx
            var id = property?.Value?["system"]?["id"]?.Value<string>();
            return Guid.TryParse(id, out _) && id.Substring(14, 2).Equals("01", StringComparison.Ordinal);
        }
    }
}
