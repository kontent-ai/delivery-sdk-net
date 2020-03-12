using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Kentico.Kontent.Delivery.Caching.Tests
{
    public class CacheHelpersTests
    {
        #region API keys

        [Fact]
        public void GetItemKey_WithDifferentValues_AreUnique()
        {
            var keys = new[]
            {
                CacheHelpers.GetItemJsonKey("codename"),
                CacheHelpers.GetItemJsonKey("other_codename"),
                CacheHelpers.GetItemJsonKey("codename", "depth=1"),
                CacheHelpers.GetItemJsonKey("codename", "depth=2"),
                CacheHelpers.GetItemJsonKey("codename", "system.type=article"),
                CacheHelpers.GetItemKey("codename", Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemKey("other_codename", Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemKey("codename", new []{new DepthParameter(1)}),
                CacheHelpers.GetItemKey("codename", new [] {new DepthParameter(2) }),
                CacheHelpers.GetItemKey("codename", new []{new SystemTypeEqualsFilter("article") }),
                CacheHelpers.GetItemTypedKey("codename", Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemTypedKey("other_codename", Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemTypedKey("codename", new []{new DepthParameter(1)}),
                CacheHelpers.GetItemTypedKey("codename", new [] {new DepthParameter(2) }),
                CacheHelpers.GetItemTypedKey("codename", new []{new SystemTypeEqualsFilter("article") })
            };

            keys.Distinct().Count().Should().Be(keys.Length);
        }

        [Fact]
        public void GetItemsKey_WithDifferentValues_AreUnique()
        {
            var keys = new[]
            {
                CacheHelpers.GetItemsJsonKey(),
                CacheHelpers.GetItemsJsonKey("depth=1"),
                CacheHelpers.GetItemsJsonKey("depth=2"),
                CacheHelpers.GetItemsJsonKey("system.type=article"),
                CacheHelpers.GetItemsKey(Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemsKey(new []{new DepthParameter(1)}),
                CacheHelpers.GetItemsKey(new [] {new DepthParameter(2) }),
                CacheHelpers.GetItemsKey(new []{new SystemTypeEqualsFilter("article") }),
                CacheHelpers.GetItemsTypedKey(Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemsTypedKey(new []{new DepthParameter(1)}),
                CacheHelpers.GetItemsTypedKey(new [] {new DepthParameter(2) }),
                CacheHelpers.GetItemsTypedKey(new []{new SystemTypeEqualsFilter("article") })
            };

            keys.Distinct().Count().Should().Be(keys.Length);
        }

        [Fact]
        public void GetTypeKey_WithDifferentValues_AreUnique()
        {
            var keys = new[]
            {
                CacheHelpers.GetTypeJsonKey("codename"),
                CacheHelpers.GetTypeJsonKey("other_codename"),
                CacheHelpers.GetTypeKey("codename"),
                CacheHelpers.GetTypeKey("other_codename")
            };

            keys.Distinct().Count().Should().Be(keys.Length);
        }

        [Fact]
        public void GetTypesKey_WithDifferentValues_AreUnique()
        {
            var keys = new[]
            {
                CacheHelpers.GetTypesJsonKey(),
                CacheHelpers.GetTypesJsonKey("skip=1"),
                CacheHelpers.GetTypesJsonKey("skip=2"),
                CacheHelpers.GetTypesJsonKey("limit=2"),
                CacheHelpers.GetTypesKey(Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetTypesKey(new []{new  SkipParameter(1)}),
                CacheHelpers.GetTypesKey(new [] {new SkipParameter(2)}),
                CacheHelpers.GetTypesKey(new []{new LimitParameter(2)})
            };

            keys.Distinct().Count().Should().Be(keys.Length);
        }

        [Fact]
        public void GetTaxonomyKey_WithDifferentValues_AreUnique()
        {
            var keys = new[]
            {
                CacheHelpers.GetTaxonomyJsonKey("codename"),
                CacheHelpers.GetTaxonomyJsonKey("other_codename"),
                CacheHelpers.GetTaxonomyKey("codename"),
                CacheHelpers.GetTaxonomyKey("other_codename")
            };

            keys.Distinct().Count().Should().Be(keys.Length);
        }

        [Fact]
        public void GetTaxonomiesKey_WithDifferentValues_AreUnique()
        {
            var keys = new[]
            {
                CacheHelpers.GetTaxonomiesJsonKey(),
                CacheHelpers.GetTaxonomiesJsonKey("skip=1"),
                CacheHelpers.GetTaxonomiesJsonKey("skip=2"),
                CacheHelpers.GetTaxonomiesJsonKey("limit=2"),
                CacheHelpers.GetTaxonomiesKey(Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetTaxonomiesKey(new []{new  SkipParameter(1)}),
                CacheHelpers.GetTaxonomiesKey(new [] {new SkipParameter(2)}),
                CacheHelpers.GetTaxonomiesKey(new []{new LimitParameter(2)})
            };

            keys.Distinct().Count().Should().Be(keys.Length);
        }

        [Fact]
        public void GetContentElementKey_WithDifferentValues_AreUnique()
        {
            var keys = new[]
            {
                CacheHelpers.GetContentElementKey("type_codename", "element_codename"),
                CacheHelpers.GetContentElementKey("type_codename", "other_element_codename"),
                CacheHelpers.GetContentElementKey("other_type_codename", "element_codename")
            };

            keys.Distinct().Count().Should().Be(keys.Length);
        }

        #endregion
    }
}

