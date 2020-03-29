using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.Abstractions
{
	/// <summary>
	/// Represents a digital asset, such as a document or image.
	/// </summary>
	public sealed class Asset
	{
		/// <summary>
		/// Gets the name of the asset.
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; }

		/// <summary>
		/// Gets the description of the asset.
		/// </summary>
		[JsonProperty("description")]
		public string Description { get; }

		/// <summary>
		/// Gets the media type of the asset, for example "image/jpeg".
		/// </summary>
		[JsonProperty("type")]
		public string Type { get; }

		/// <summary>
		/// Gets the asset size in bytes.
		/// </summary>
		[JsonProperty("size")]
		public int Size { get; }

		/// <summary>
		/// Gets the URL of the asset.
		/// </summary>
		[JsonProperty("url")]
		public string Url { get; }

		/// <summary>
		/// Gets the width of the asset
		/// </summary>
		[JsonProperty("width")]
		public int Width { get; set; }

		/// <summary>
		/// Gets the height of the asset
		/// </summary>
		[JsonProperty("height")]
		public int Height { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Asset"/> class.
		/// </summary>
		[JsonConstructor]
		internal Asset(string name, string type, int size, string url, string description, int width, int height)
		{
			Name = name;
			Type = type;
			Size = size;
			Url = url;
			Description = description;
			Width = width;
			Height = height;
		}
	}
}
