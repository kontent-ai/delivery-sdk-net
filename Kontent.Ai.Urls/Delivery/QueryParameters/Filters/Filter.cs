using System;
using System.Globalization;
using System.Linq;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Urls.Delivery.QueryParameters.Filters
{
    /// <summary>
    /// Provides the base class for filter implementations.
    /// </summary>
    public abstract class Filter<T> : IQueryParameter
    {
        private static readonly string SEPARATOR = Uri.EscapeDataString(",");
        private static readonly string DECIMAL_FORMAT = "##########.##########";
        private static readonly string DATETIME_FORMAT = "yyyy-MM-ddTHH:mm:ssZ";

        /// <summary>
        /// Gets the codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.
        /// </summary>
        public string ElementOrAttributePath { get; protected set; }

        /// <summary>
        /// Gets the filter values.
        /// </summary>
        public T[] Values { get; protected set; }

        /// <summary>
        /// Gets the filter operator.
        /// </summary>
        public string Operator { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter{T}"/> class.
        /// </summary>
        /// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
        /// <param name="values">The filter values.</param>
        protected Filter(string elementOrAttributePath, params T[] values)
        {
            ElementOrAttributePath = elementOrAttributePath;
            Values = values;
        }

        /// <summary>
        /// Returns the query string representation of the filter.
        /// </summary>
        public string GetQueryStringParameter()
        {
            var values = Values switch
            {
                string[] strings => strings,
                short[] shorts => shorts.Select(shortValue => shortValue.ToString(DECIMAL_FORMAT)),
                ushort[] unsignedShorts => unsignedShorts.Select( shortValue => shortValue.ToString(DECIMAL_FORMAT )),
                int[] integers => integers.Select(integer => integer.ToString(DECIMAL_FORMAT)),
                uint[] unsignedIntegers => unsignedIntegers.Select( integer => integer.ToString(DECIMAL_FORMAT)),
                long[] longs => longs.Select( longValue => longValue.ToString(DECIMAL_FORMAT)),
                ulong[] unsignedLongs => unsignedLongs.Select( longValue => longValue.ToString(DECIMAL_FORMAT)),
                float[] floats => floats.Select(floatValue => floatValue.ToString(DECIMAL_FORMAT)),
                double[] doubles => doubles.Select(doubleValue => doubleValue.ToString(DECIMAL_FORMAT)),
                decimal[] decimals => decimals.Select(decimalValue => decimalValue.ToString(DECIMAL_FORMAT)),
                DateTime[] dateTimes => dateTimes.Select(dateTime => dateTime.ToString(DATETIME_FORMAT, CultureInfo.InvariantCulture)),
                _ => Values.Select(value => value.ToString())
            };

            var escapedValues = values.Select(Uri.EscapeDataString);
            return $"{Uri.EscapeDataString(ElementOrAttributePath)}{Uri.EscapeDataString(Operator ?? string.Empty)}={string.Join(SEPARATOR, escapedValues)}";
        }
    }
}
