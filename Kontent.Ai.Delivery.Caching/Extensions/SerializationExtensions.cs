﻿using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Converters;

namespace Kontent.Ai.Delivery.Caching.Extensions
{
    /// <summary>
    /// JSON/BSON serialization extensions.
    /// </summary>
    public static class SerializationExtensions
    {
        /// <summary>
        /// Default serialization settings (should be shared for both serialization and deserialization to ensure consistent results).
        /// </summary>
        public static JsonSerializerSettings Settings => new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All, // Allow preserving type information (necessary for deserializing interfaces into implemented types) 
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize, // The content item models contain recursive references which are supposed to be preserved.
            PreserveReferencesHandling = PreserveReferencesHandling.All, // The code must not use arrays and readonly collections, otherwise it'll result in "Cannot preserve reference to array or readonly list, or list created from a non-default constructor" exception. (more details at https://stackoverflow.com/a/41307438/1332034)
            Converters = new List<JsonConverter> { new UtcDateTimeConverter() }
        };

        /// <summary>
        /// Serializes given object to the BSON data format (http://bsonspec.org/).
        /// </summary>
        /// <param name="obj">Object to serialize.</param>
        /// <returns>Byte array of BSON representation of the given object.</returns>
        public static byte[] ToBson(this object obj)
        {
            if (obj == null)
            {
                return null;
            }
            using var ms = new MemoryStream();
            using var writer = new BsonDataWriter(ms);
            var serializer = JsonSerializer.Create(Settings);
            serializer.Serialize(writer, obj);
            return ms.ToArray();
        }

        /// <summary>
        /// Deserializes given object from the BSON data format (http://bsonspec.org/).
        /// </summary>
        /// <typeparam name="T">Target type to deserialize the object to.</typeparam>
        /// <param name="byteArray">Byte array of BSON representation of the given object.</param>
        /// <returns>Strongly-typed deserialized object.</returns>
        public static T FromBson<T>(this byte[] byteArray) where T : class
        {
            using var ms = new MemoryStream(byteArray);
            using var reader = new BsonDataReader(ms);
            var serializer = JsonSerializer.Create(Settings);
            return serializer.Deserialize<T>(reader);
        }
    }

    internal class UtcDateTimeConverter : IsoDateTimeConverter
    {
        public UtcDateTimeConverter()
        {
            DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
            DateTimeStyles = DateTimeStyles.AdjustToUniversal;
        }
    }
}
