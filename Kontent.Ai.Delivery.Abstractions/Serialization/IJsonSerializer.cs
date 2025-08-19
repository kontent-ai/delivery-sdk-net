using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions.Serialization
{
    /// <summary>
    /// Defines the contract for JSON serialization operations in the Delivery SDK.
    /// </summary>
    public interface IJsonSerializer
    {
        /// <summary>
        /// Serializes the specified object to a JSON string.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        string Serialize(object value);

        /// <summary>
        /// Serializes the specified object to a JSON string.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="value">The object to serialize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        string Serialize<T>(T value);

        /// <summary>
        /// Deserializes the JSON string to an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        T Deserialize<T>(string json);

        /// <summary>
        /// Deserializes the JSON string to an object of the specified type.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="type">The type of the object to deserialize to.</param>
        /// <returns>The deserialized object.</returns>
        object Deserialize(string json, System.Type type);

        /// <summary>
        /// Asynchronously deserializes the JSON stream to an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <param name="stream">The stream containing JSON data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous deserialization operation.</returns>
        Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously serializes the specified object to a JSON stream.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="stream">The stream to write the JSON data to.</param>
        /// <param name="value">The object to serialize.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous serialization operation.</returns>
        Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default);
    }
}