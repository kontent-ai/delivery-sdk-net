using System.Text.Json;
using Kontent.Ai.Delivery.SharedModels;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.SharedModels;

public class PaginationTests
{
    [Fact]
    public void SerializePagination_AndThen_DeserializeToCheck_ValuesMatch_UsingStandardSerialization() => ExecuteTestsScenario(SerializeThenDeserializeUsingJson);
    private void ExecuteTestsScenario(Func<Pagination, Pagination> serializeThenDeserialize)
    {
        var pagination = new Pagination()
        {
            Skip = 2,
            Limit = 500,
            Count = 10,
            TotalCount = 65,
            NextPageUrl = "https://nextpageUrl"
        };
        var deserializedPagination = serializeThenDeserialize(pagination);
        Assert.Equal(pagination.Skip, deserializedPagination.Skip);
        Assert.Equal(pagination.Limit, deserializedPagination.Limit);
        Assert.Equal(pagination.Count, deserializedPagination.Count);
        Assert.Equal(pagination.TotalCount, deserializedPagination.TotalCount);
        Assert.Equal(pagination.NextPageUrl, deserializedPagination.NextPageUrl);
    }

    private static Pagination SerializeThenDeserializeUsingJson(Pagination pagination)
    {
        var json = JsonSerializer.Serialize(pagination);
        return JsonSerializer.Deserialize<Pagination>(json)!;
    }
}
