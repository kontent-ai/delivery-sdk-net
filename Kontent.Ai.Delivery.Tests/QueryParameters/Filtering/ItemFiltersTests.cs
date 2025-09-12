using Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.QueryParameters.Filtering
{
    public class ItemFiltersTests
    {
        private readonly ItemFilters _filters = new();

        [Fact]
        public void Equals_BuildsCorrectFilter()
        {
            var filter = _filters.Equals(SystemPath.Type(), Scalar.From("article"));

            Assert.Equal("system.type[eq]=\"article\"", filter.ToQueryParameter());
        }

        [Fact]
        public void All_BuildsCorrectFilter()
        {
            var filter = _filters.All(ElementsPath.Element("tags"), "a", "b");

            Assert.Equal("elements.tags[all]=\"a\",\"b\"", filter.ToQueryParameter());
        }

        [Fact]
        public void Empty_BuildsCorrectFilter()
        {
            var filter = _filters.Empty(ElementsPath.Element("title"));

            Assert.Equal("elements.title[empty]", filter.ToQueryParameter());
        }
    }
}


