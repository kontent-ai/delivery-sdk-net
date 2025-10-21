global using Kontent.Ai.Delivery.Abstractions;
global using Kontent.Ai.Delivery.Api;
global using Kontent.Ai.Delivery.Api.QueryParams.Items;
global using Kontent.Ai.Delivery.Api.QueryParams.Languages;
global using Kontent.Ai.Delivery.Api.QueryParams.TaxonomyGroups;
global using Kontent.Ai.Delivery.Api.QueryParams.Types;
global using Kontent.Ai.Delivery.Api.QueryBuilders;
global using Kontent.Ai.Delivery.Extensions;
global using Kontent.Ai.Delivery.SharedModels;
global using Refit;
global using System.Threading.Tasks;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Net.Http;

// =============================================================================
// OneOf Type Aliases for Type-Safe Filtering
// =============================================================================
// These type aliases provide compile-time type safety for filter values while
// avoiding the verbosity of OneOf's generic syntax.
//
// ScalarValue: Any scalar value (string, double, DateTime, or bool)
//   - Used for equality/inequality filters
//   - Example: f.Equals(path, "text"), f.Equals(path, 42.5)
//   - Example: f.Equals(path, DateTime.Now), f.Equals(path, true)
//
// ComparableValue: Values supporting ordering (double, DateTime, or string)
//   - Used for comparison operators (<, >, <=, >=)
//   - Example: f.LessThan(path, 100.0), f.GreaterThan(path, DateTime.Now)
//   - Boolean values excluded as they don't support meaningful ordering
//
// RangeBounds: Range boundaries as tuples (numeric or date)
//   - Used for range queries with inclusive bounds [lower, upper]
//   - Example: f.Range(path, (10.0, 100.0)), f.Range(path, (start, end))
//   - String ranges have separate overloads if needed
// =============================================================================

global using ScalarValue = OneOf.OneOf<string, double, System.DateTime, bool>;
global using ComparableValue = OneOf.OneOf<double, System.DateTime, string>;
global using RangeBounds = OneOf.OneOf<(double Lower, double Upper), (System.DateTime Lower, System.DateTime Upper)>;