using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Kentico.Kontent.Delivery.Caching.Extensions
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
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore // Linked items can be recursive, this will prevent the StackOverflowException
            //PreserveReferencesHandling = PreserveReferencesHandling.All, // Not implemented for collections. Will result into "Cannot preserve reference to array or readonly list, or list created from a non-default constructor" exception.
            //ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor // Not required
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
}
