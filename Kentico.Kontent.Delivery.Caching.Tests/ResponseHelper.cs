using System;
using System.Collections.Generic;
using System.Linq;

namespace Kentico.Kontent.Delivery.Caching.Tests
{
    public static class ResponseHelper
    {
        internal static object CreateItemResponse(object item, IEnumerable<(string codename, object item)> modularContent = null) => new
        {
            item,
            modular_content = modularContent?.ToDictionary(x => x.codename, x => x.item) ?? new Dictionary<string, object>()
        };

        internal static object CreateItemsResponse(ICollection<object> items, IEnumerable<(string codename, object item)> modularContent = null) => new
        {
            items,
            modular_content = modularContent?.ToDictionary(x => x.codename, x => x.item) ?? new Dictionary<string, object>(),
            pagination = new
            {
                skip = 0,
                limit = 0,
                count = items.Count,
                next_page = ""
            }
        };

        /// <summary>
        /// Creates a response based on passed data.
        /// </summary>
        /// <param name="items">Items to be used in response.</param>
        /// <param name="modularContent">Modular content to be used in response.</param>
        /// <param name="pagination">Pagination to be used in response.</param>
        /// <returns>Response model based on parameters items, modular_content and pagination. If <c>modularContent</c> is null or not passed, response will contain empty modular content data. If <c>pagination</c> is null or not passed, response will contain empty paging data with proper items count value.</returns>
        internal static object CreatePagedItemsResponse(ICollection<object> items, IEnumerable<(string codename, object item)> modularContent = null, object pagination= null) => new
        {
            items,
            modular_content = modularContent?.ToDictionary(x => x.codename, x => x.item) ?? new Dictionary<string, object>(),
            pagination = pagination ?? new
            {
                skip = 0,
                limit = 0,
                count = items.Count,
                next_page = ""
            }
        };

        internal static object CreateItemsFeedResponse(ICollection<object> items, IEnumerable<(string codename, object item)> modularContent = null) => new
        {
            items,
            modular_content = modularContent?.ToDictionary(x => x.codename, x => x.item) ?? new Dictionary<string, object>()
        };

        internal static object CreateTypesResponse(ICollection<object> types) => new
        {
            types,
            pagination = new
            {
                skip = 0,
                limit = 0,
                count = types.Count,
                next_page = ""
            }
        };

        internal static object CreateTaxonomiesResponse(ICollection<object> taxonomies) => new
        {
            taxonomies,
            pagination = new
            {
                skip = 0,
                limit = 0,
                count = taxonomies.Count,
                next_page = ""
            }
        };

        internal static object CreateLanguagesResponse(ICollection<object> languages) => new
        {
            languages,
            pagination = new
            {
                skip = 0,
                limit = 0,
                count = languages.Count,
                next_page = ""
            }
        };

        internal static object CreateItem(string codename, string value = null) => new
        {
            elements = new Dictionary<string, object>
            {
                {
                    "title",
                    new
                    {
                        type = "text",
                        name= "Title",
                        value= value ?? string.Empty
                    }
                }
            },
            system = new
            {
                id = Guid.NewGuid().ToString(),
                codename,
                type = "test_item",
                last_modified = "2019-03-27T13:10:01.791Z"
            }
        };

        internal static (string codename, object item) CreateComponent()
        {
            // Components have substring 01 in its id starting at position 14.
            // xxxxxxxx-xxxx-01xx-xxxx-xxxxxxxxxxxx
            var id = Guid.NewGuid().ToString();
            id = $"{id.Substring(0, 14)}01{id.Substring(16)}";
            var codename = $"n{id}";
            return (
                codename,
                new
                {
                    elements = new Dictionary<string, object>(),
                    system = new
                    {
                        id,
                        codename
                    }
                });
        }

        internal static object CreateType(string codename, string elementName = "Title") => new
        {
            elements = new Dictionary<string, object>
            {
                {
                    elementName.ToLowerInvariant(),
                    new
                    {
                        type = "text",
                        name= elementName
                    }
                }
            },
            system = new
            {
                id = Guid.NewGuid().ToString(),
                codename,
                last_modified = "2019-03-27T13:10:01.791Z"
            }
        };

        internal static object CreateTaxonomy(string codename, IEnumerable<string> terms) => new
        {
            terms = (terms ?? Enumerable.Empty<string>()).Select(t => new
            {
                codename = t,
                name = t,
                terms = Enumerable.Empty<object>()
            }),
            system = new
            {
                id = Guid.NewGuid().ToString(),
                codename,
                last_modified = "2019-03-27T13:10:01.791Z"
            }
        };

        internal static object CreateLanguage(string codename, string name) => new
        {
            system = new
            {
                id = Guid.NewGuid().ToString(),
                codename,
                name
            }
        };

        internal static object CreateContentElement(string codename, string name) => new
        {
            type = "text",
            name,
            codename
        };
    }
}
