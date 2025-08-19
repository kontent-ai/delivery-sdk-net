using System;
using MessagePack;

namespace Kontent.Ai.Delivery.Caching.Extensions
{
    /// <summary>
    /// MessagePack serialization extensions for caching.
    /// </summary>
    public static class SerializationExtensions
    {
        /// <summary>
        /// Default MessagePack serializer options optimized for delivery SDK caching.
        /// </summary>
        private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard
            .WithCompression(MessagePackCompression.Lz4BlockArray)
            .WithOldSpec(false)
            .WithOmitAssemblyVersion(true);

        /// <summary>
        /// Serializes given object to MessagePack binary format.
        /// </summary>
        /// <param name="obj">Object to serialize.</param>
        /// <returns>Byte array of MessagePack representation of the given object.</returns>
        public static byte[]? ToMessagePack(this object? obj)
        {
            if (obj == null)
            {
                return null;
            }

            try
            {
                return MessagePackSerializer.Serialize(obj, Options);
            }
            catch (Exception)
            {
                // If MessagePack serialization fails, return null
                // This maintains backward compatibility for cached items
                return null;
            }
        }

        /// <summary>
        /// Deserializes given object from MessagePack binary format.
        /// </summary>
        /// <typeparam name="T">Target type to deserialize the object to.</typeparam>
        /// <param name="byteArray">Byte array of MessagePack representation of the given object.</param>
        /// <returns>Strongly-typed deserialized object.</returns>
        public static T? FromMessagePack<T>(this byte[]? byteArray) where T : class
        {
            if (byteArray == null || byteArray.Length == 0)
            {
                return null;
            }

            try
            {
                return MessagePackSerializer.Deserialize<T>(byteArray, Options);
            }
            catch (Exception)
            {
                // If deserialization fails, return null
                // This handles cache invalidation for incompatible cached data
                return null;
            }
        }

        /// <summary>
        /// Legacy method for backward compatibility - converts BSON calls to MessagePack.
        /// </summary>
        /// <param name="obj">Object to serialize.</param>
        /// <returns>Byte array of MessagePack representation of the given object.</returns>
        [Obsolete("Use ToMessagePack instead. This method is kept for backward compatibility during migration.")]
        public static byte[]? ToBson(this object? obj) => obj.ToMessagePack();

        /// <summary>
        /// Legacy method for backward compatibility - converts BSON calls to MessagePack.
        /// </summary>
        /// <typeparam name="T">Target type to deserialize the object to.</typeparam>
        /// <param name="byteArray">Byte array representation of the object.</param>
        /// <returns>Strongly-typed deserialized object.</returns>
        [Obsolete("Use FromMessagePack instead. This method is kept for backward compatibility during migration.")]
        public static T? FromBson<T>(this byte[]? byteArray) where T : class => byteArray.FromMessagePack<T>();
    }
}