using System.Linq;
using FakeItEasy;
using FluentAssertions;
using Kontent.Ai.Delivery.Abstractions;
using Xunit;

namespace Kontent.Ai.Delivery.Caching.Tests
{
    public class CacheHelpersTests
    {
        private const string SampleCodename = "sample_code";

        private static IContentItem<TModel> CreateContentItem<TModel>(string codename)
            where TModel : IElementsModel
        {
            var system = A.Fake<IContentItemSystemAttributes>();
            A.CallTo(() => system.Codename).Returns(codename);

            var item = A.Fake<IContentItem<TModel>>();
            A.CallTo(() => item.System).Returns(system);
            return item;
        }

        private static IContentItem<IElementsModel> CreateDynamicContentItem(string codename)
        {
            var system = A.Fake<IContentItemSystemAttributes>();
            A.CallTo(() => system.Codename).Returns(codename);

            var item = A.Fake<IContentItem<IElementsModel>>();
            A.CallTo(() => item.System).Returns(system);
            return item;
        }

        private static IContentType CreateContentType(string codename)
        {
            var system = A.Fake<IContentTypeSystemAttributes>();
            A.CallTo(() => system.Codename).Returns(codename);

            var type = A.Fake<IContentType>();
            A.CallTo(() => type.System).Returns(system);
            return type;
        }

        private static IContentElement CreateContentElement(string codename)
        {
            var element = A.Fake<IContentElement>();
            A.CallTo(() => element.Codename).Returns(codename);
            return element;
        }

        private static ITaxonomyGroup CreateTaxonomyGroup(string codename)
        {
            var system = A.Fake<ITaxonomyGroupSystemAttributes>();
            A.CallTo(() => system.Codename).Returns(codename);

            var taxonomy = A.Fake<ITaxonomyGroup>();
            A.CallTo(() => taxonomy.System).Returns(system);
            return taxonomy;
        }

        private static ILanguage CreateLanguage(string codename)
        {
            var system = A.Fake<ILanguageSystemAttributes>();
            A.CallTo(() => system.Codename).Returns(codename);

            var language = A.Fake<ILanguage>();
            A.CallTo(() => language.System).Returns(system);
            return language;
        }

        // Dependency key methods

        [Fact]
        public void GetItemDependencyKey_Returns_Expected_Format()
        {
            CacheHelpers.GetItemDependencyKey(SampleCodename)
                .Should().Be($"dependency_item|{SampleCodename}");
        }

        [Fact]
        public void GetItemsDependencyKey_Returns_Constant()
        {
            CacheHelpers.GetItemsDependencyKey().Should().Be("dependency_item_listing");
        }

        [Fact]
        public void GetTypesDependencyKey_Returns_Constant()
        {
            CacheHelpers.GetTypesDependencyKey().Should().Be("dependency_type_listing");
        }

        [Fact]
        public void GetTaxonomyDependencyKey_Returns_Expected_Format()
        {
            CacheHelpers.GetTaxonomyDependencyKey(SampleCodename)
                .Should().Be($"dependency_taxonomy_group|{SampleCodename}");
        }

        [Fact]
        public void GetTaxonomiesDependencyKey_Returns_Constant()
        {
            CacheHelpers.GetTaxonomiesDependencyKey().Should().Be("dependency_taxonomy_group_listing");
        }

        [Fact]
        public void GetLanguagesDependencyKey_Returns_Constant()
        {
            CacheHelpers.GetLanguagesDependencyKey().Should().Be("dependency_language_listing");
        }

        // Item dependencies (generic)

        public class DummyElements : IElementsModel { }

        [Fact]
        public void GetItemDependencies_Generic_NullItem_Returns_Empty()
        {
            CacheHelpers.GetItemDependencies<IElementsModel>(null).Should().BeEmpty();
        }

        [Fact]
        public void GetItemDependencies_Generic_NullSystem_Returns_Empty()
        {
            var item = A.Fake<IContentItem<DummyElements>>();
            A.CallTo(() => item.System).Returns(null);

            CacheHelpers.GetItemDependencies(item).Should().BeEmpty();
        }

        [Fact]
        public void GetItemDependencies_Generic_NullCodename_Returns_Empty()
        {
            var system = A.Fake<IContentItemSystemAttributes>();
            A.CallTo(() => system.Codename).Returns(null);
            var item = A.Fake<IContentItem<DummyElements>>();
            A.CallTo(() => item.System).Returns(system);

            CacheHelpers.GetItemDependencies(item).Should().BeEmpty();
        }

        [Fact]
        public void GetItemDependencies_Generic_Valid_Returns_Single_Item_Key()
        {
            var item = CreateContentItem<DummyElements>(SampleCodename);
            var result = CacheHelpers.GetItemDependencies(item).ToArray();

            result.Should().HaveCount(1);
            result[0].Should().Be($"dependency_item|{SampleCodename}");
        }

        // Item dependencies (dynamic overload)

        [Fact]
        public void GetItemDependencies_Dynamic_NullItem_Returns_Empty()
        {
            CacheHelpers.GetItemDependencies((IContentItem<IElementsModel>)null).Should().BeEmpty();
        }

        [Fact]
        public void GetItemDependencies_Dynamic_NullSystem_Returns_Empty()
        {
            var item = A.Fake<IContentItem<IElementsModel>>();
            A.CallTo(() => item.System).Returns(null);

            CacheHelpers.GetItemDependencies(item).Should().BeEmpty();
        }

        [Fact]
        public void GetItemDependencies_Dynamic_NullCodename_Returns_Empty()
        {
            var system = A.Fake<IContentItemSystemAttributes>();
            A.CallTo(() => system.Codename).Returns(null);
            var item = A.Fake<IContentItem<IElementsModel>>();
            A.CallTo(() => item.System).Returns(system);

            CacheHelpers.GetItemDependencies(item).Should().BeEmpty();
        }

        [Fact]
        public void GetItemDependencies_Dynamic_Valid_Returns_Single_Item_Key()
        {
            var item = CreateDynamicContentItem(SampleCodename);
            var result = CacheHelpers.GetItemDependencies(item).ToArray();

            result.Should().HaveCount(1);
            result[0].Should().Be($"dependency_item|{SampleCodename}");
        }

        // Content type dependencies

        [Fact]
        public void GetTypeDependencies_NullType_Returns_Empty()
        {
            CacheHelpers.GetTypeDependencies(null).Should().BeEmpty();
        }

        [Fact]
        public void GetTypeDependencies_NullSystem_Returns_Empty()
        {
            var type = A.Fake<IContentType>();
            A.CallTo(() => type.System).Returns(null);

            CacheHelpers.GetTypeDependencies(type).Should().BeEmpty();
        }

        [Fact]
        public void GetTypeDependencies_NullCodename_Returns_Empty()
        {
            var system = A.Fake<IContentTypeSystemAttributes>();
            A.CallTo(() => system.Codename).Returns(null);
            var type = A.Fake<IContentType>();
            A.CallTo(() => type.System).Returns(system);

            CacheHelpers.GetTypeDependencies(type).Should().BeEmpty();
        }

        [Fact]
        public void GetTypeDependencies_Valid_Returns_Single_Types_Key()
        {
            var type = CreateContentType(SampleCodename);
            var result = CacheHelpers.GetTypeDependencies(type).ToArray();

            result.Should().HaveCount(1);
            result[0].Should().Be("dependency_type_listing");
        }

        // Content element dependencies

        [Fact]
        public void GetContentElementDependencies_NullElement_Returns_Empty()
        {
            CacheHelpers.GetContentElementDependencies(null).Should().BeEmpty();
        }

        [Fact]
        public void GetContentElementDependencies_NullCodename_Returns_Empty()
        {
            var element = A.Fake<IContentElement>();
            A.CallTo(() => element.Codename).Returns(null);

            CacheHelpers.GetContentElementDependencies(element).Should().BeEmpty();
        }

        [Fact]
        public void GetContentElementDependencies_Valid_Returns_Single_Types_Key()
        {
            var element = CreateContentElement(SampleCodename);
            var result = CacheHelpers.GetContentElementDependencies(element).ToArray();

            result.Should().HaveCount(1);
            result[0].Should().Be("dependency_type_listing");
        }

        // Taxonomy dependencies

        [Fact]
        public void GetTaxonomyDependencies_NullTaxonomy_Returns_Empty()
        {
            CacheHelpers.GetTaxonomyDependencies(null).Should().BeEmpty();
        }

        [Fact]
        public void GetTaxonomyDependencies_NullSystem_Returns_Empty()
        {
            var taxonomy = A.Fake<ITaxonomyGroup>();
            A.CallTo(() => taxonomy.System).Returns(null);

            CacheHelpers.GetTaxonomyDependencies(taxonomy).Should().BeEmpty();
        }

        [Fact]
        public void GetTaxonomyDependencies_NullCodename_Returns_Empty()
        {
            var system = A.Fake<ITaxonomyGroupSystemAttributes>();
            A.CallTo(() => system.Codename).Returns(null);
            var taxonomy = A.Fake<ITaxonomyGroup>();
            A.CallTo(() => taxonomy.System).Returns(system);

            CacheHelpers.GetTaxonomyDependencies(taxonomy).Should().BeEmpty();
        }

        [Fact]
        public void GetTaxonomyDependencies_Valid_Returns_Single_Taxonomy_Key()
        {
            var taxonomy = CreateTaxonomyGroup(SampleCodename);
            var result = CacheHelpers.GetTaxonomyDependencies(taxonomy).ToArray();

            result.Should().HaveCount(1);
            result[0].Should().Be($"dependency_taxonomy_group|{SampleCodename}");
        }

        // Language dependencies

        [Fact]
        public void GetLanguagesDependencies_NullLanguage_Returns_Empty()
        {
            CacheHelpers.GetLanguagesDependencies(null).Should().BeEmpty();
        }

        [Fact]
        public void GetLanguagesDependencies_NullSystem_Returns_Empty()
        {
            var language = A.Fake<ILanguage>();
            A.CallTo(() => language.System).Returns(null);

            CacheHelpers.GetLanguagesDependencies(language).Should().BeEmpty();
        }

        [Fact]
        public void GetLanguagesDependencies_NullCodename_Returns_Empty()
        {
            var system = A.Fake<ILanguageSystemAttributes>();
            A.CallTo(() => system.Codename).Returns(null);
            var language = A.Fake<ILanguage>();
            A.CallTo(() => language.System).Returns(system);

            CacheHelpers.GetLanguagesDependencies(language).Should().BeEmpty();
        }

        [Fact]
        public void GetLanguagesDependencies_Valid_Returns_Single_Languages_Key()
        {
            var language = CreateLanguage(SampleCodename);
            var result = CacheHelpers.GetLanguagesDependencies(language).ToArray();

            result.Should().HaveCount(1);
            result[0].Should().Be("dependency_language_listing");
        }
    }
}

