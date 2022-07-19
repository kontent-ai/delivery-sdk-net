using System.Linq;
using FluentAssertions;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Caching.Tests.ContentTypes;
using Kentico.Kontent.Urls.Delivery.QueryParameters;
using Kentico.Kontent.Urls.Delivery.QueryParameters.Filters;
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
                CacheHelpers.GetItemKey<object>("codename", Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemKey<object>("other_codename", Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemKey<object>("codename", new []{new DepthParameter(1)}),
                CacheHelpers.GetItemKey<object>("codename", new [] {new DepthParameter(2) }),
                CacheHelpers.GetItemKey<object>("codename", new []{new SystemTypeEqualsFilter("article") }),
                CacheHelpers.GetItemKey<Article>("codename", new []{new SystemTypeEqualsFilter("article") })
            };

            keys.Distinct().Count().Should().Be(keys.Length);
        }

        [Fact]
        public void GetItemsKey_WithDifferentValues_AreUnique()
        {
            var keys = new[]
            {
                CacheHelpers.GetItemsKey<object>(Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemsKey<object>(new []{new DepthParameter(1)}),
                CacheHelpers.GetItemsKey<object>(new [] {new DepthParameter(2) }),
                CacheHelpers.GetItemsKey<object>(new []{new SystemTypeEqualsFilter("article") }) ,
                CacheHelpers.GetItemsKey<Article>(new []{new SystemTypeEqualsFilter("article") })
            };

            keys.Distinct().Count().Should().Be(keys.Length);
        }

        [Fact]
        public void GetTypeKey_WithDifferentValues_AreUnique()
        {
            var keys = new[]
            {
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

