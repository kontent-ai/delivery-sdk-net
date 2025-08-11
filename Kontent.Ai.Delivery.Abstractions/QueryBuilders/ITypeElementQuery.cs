using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for retrieving a content type element.
/// </summary>
public interface ITypeElementQuery
{
    /// <summary>
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery element response.</returns>
    Task<IDeliveryElementResponse> ExecuteAsync();
}
