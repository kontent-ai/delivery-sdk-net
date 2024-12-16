using System.Linq;
using FluentAssertions;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching.Tests.ContentTypes;
using Kontent.Ai.Urls.Delivery.QueryParameters;
using Kontent.Ai.Urls.Delivery.QueryParameters.Filters;
using Xunit;

namespace Kontent.Ai.Delivery.Caching.Tests
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
                CacheHelpers.GetItemKey<Article>("codename", new []{new SystemTypeEqualsFilter("article") }),
                CacheHelpers.GetItemKey<SomeClass>("codename", Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemKey<someclass>("codename", Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemKey<AnotherNamespace.SomeClass>("codename", Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemKey<AnotherNamespace.someclass>("codename", Enumerable.Empty<IQueryParameter>()),
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
                CacheHelpers.GetItemsKey<Article>(new []{new SystemTypeEqualsFilter("article") }),
                CacheHelpers.GetItemsKey<SomeClass>(Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemsKey<someclass>(Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemsKey<AnotherNamespace.SomeClass>(Enumerable.Empty<IQueryParameter>()),
                CacheHelpers.GetItemsKey<AnotherNamespace.someclass>(Enumerable.Empty<IQueryParameter>()),
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

    internal class SomeClass { }
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
    internal class someclass { }
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
}

namespace Kontent.Ai.Delivery.Caching.Tests.AnotherNamespace
{
    internal class SomeClass { }
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
    internal class someclass { }
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
}

