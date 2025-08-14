namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

/// <summary>
/// All available filtering operators in the Kontent.ai Delivery API.
/// </summary>
public enum FilterOperator
{
    /// <summary>
    /// Exact match [eq].
    /// </summary>
    Equals,

    /// <summary>
    /// Does not match [neq].
    /// </summary>
    NotEquals,

    /// <summary>
    /// Less than specified value [lt].
    /// </summary>
    LessThan,

    /// <summary>
    /// Less than or equal to specified value [lte].
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Greater than specified value [gt].
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater than or equal to specified value [gte].
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Within specified range (inclusive) [range].
    /// </summary>
    Range,

    /// <summary>
    /// Matches any of specified values [in].
    /// </summary>
    In,

    /// <summary>
    /// Does not match any of specified values [nin].
    /// </summary>
    NotIn,

    /// <summary>
    /// String contains specified text or array contains specified value [contains].
    /// </summary>
    Contains,

    /// <summary>
    /// Array contains any of specified values [any].
    /// </summary>
    Any,

    /// <summary>
    /// Array contains all specified values [all].
    /// </summary>
    All,

    /// <summary>
    /// Field is empty/null [empty].
    /// </summary>
    Empty,

    /// <summary>
    /// Field is not empty/null [nempty].
    /// </summary>
    NotEmpty
}