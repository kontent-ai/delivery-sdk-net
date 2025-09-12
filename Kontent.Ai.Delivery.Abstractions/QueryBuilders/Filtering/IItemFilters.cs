using Scalar = OneOf.OneOf<string, double, System.DateTime, bool>;
using RangeTuple = OneOf.OneOf<(double Lower, double Upper), (System.DateTime Lower, System.DateTime Upper)>;
using Comparable = OneOf.OneOf<double, System.DateTime, string>;

namespace Kontent.Ai.Delivery.Abstractions;
/// <summary>
/// Interface for item filters.
/// </summary>
public interface IItemFilters
{
    /// <summary>
    /// Retrieves items where the property specified by path equals the specified value.
    /// </summary>
    IFilter Equals(IPropertyPath path, Scalar value);
    /// <summary>
    /// Retrieves items where the property specified by path does not equal the specified value.
    /// </summary>
    IFilter NotEquals(IPropertyPath path, Scalar value);
    /// <summary>
    /// Retrieves items where the property specified by path is less than the specified value.
    /// </summary>
    IFilter LessThan(IPropertyPath path, Comparable value);
    /// <summary>
    /// Retrieves items where the property specified by path is less than or equal to the specified value.
    /// </summary>
    IFilter LessThanOrEqual(IPropertyPath path, Comparable value);
    /// <summary>
    /// Retrieves items where the property specified by path is greater than the specified value.
    /// </summary>
    IFilter GreaterThan(IPropertyPath path, Comparable value);
    /// <summary>
    /// Retrieves items where the property specified by path is greater than or equal to the specified value.
    /// </summary>
    IFilter GreaterThanOrEqual(IPropertyPath path, Comparable value);
    /// <summary>
    /// Retrieves items where the property specified by path is within the specified range.
    /// </summary>
    IFilter Range(IPropertyPath path, RangeTuple range);
    /// <summary>
    /// Retrieves items where the property specified by path is in the specified collection of strings.
    /// </summary>
    IFilter In(IPropertyPath path, string[] values);
    /// <summary>
    /// Retrieves items where the property specified by path is not in the specified collection of strings.
    /// </summary>
    IFilter NotIn(IPropertyPath path, string[] values);
    /// <summary>
    /// Retrieves items where the property specified by path is in the specified collection of numbers.
    /// </summary>
    IFilter In(IPropertyPath path, double[] values);
    /// <summary>
    /// Retrieves items where the property specified by path is not in the specified collection of numbers.
    /// </summary>
    IFilter NotIn(IPropertyPath path, double[] values);
    /// <summary>
    /// Retrieves items where the property specified by path is in the specified collection of dates.
    /// </summary>
    IFilter In(IPropertyPath path, System.DateTime[] values);
    /// <summary>
    /// Retrieves items where the property specified by path is not in the specified collection of dates.
    /// </summary>
    IFilter NotIn(IPropertyPath path, System.DateTime[] values);
    /// <summary>
    /// Retrieves items where the property specified by path contains the specified value.
    /// </summary>
    IFilter Contains(IPropertyPath path, string value);
    /// <summary>
    /// Retrieves items where the property specified by path contains any of the specified values.
    /// </summary>
    IFilter Any(IPropertyPath path, params string[] values);
    /// <summary>
    /// Retrieves items where the property specified by path contains all of the specified values.
    /// </summary>
    IFilter All(IPropertyPath path, params string[] values);
    /// <summary>
    /// Retrieves items where the property specified by path is empty.
    /// </summary>
    IFilter Empty(IPropertyPath path);
    /// <summary>
    /// Retrieves items where the property specified by path is not empty.
    /// </summary>
    IFilter NotEmpty(IPropertyPath path);
}
