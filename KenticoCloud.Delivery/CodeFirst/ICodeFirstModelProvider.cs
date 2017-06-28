using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Defines the contract for mapping content items to code-first models.
    /// </summary>
    public interface ICodeFirstModelProvider
    {
        /// <summary>
        /// Builds a code-first model based on given JSON input.
        /// </summary>
        /// <typeparam name="T">Strongly typed content item model.</typeparam>
        /// <param name="item">Content item data.</param>
        /// <param name="modularContent">Modular content items.</param>
        /// <returns>Strongly typed POCO model of the generic type.</returns>
        T GetContentItemModel<T>(JToken item, JToken modularContent);

        /// <summary>
        /// Ensures mapping between Kentico Cloud content types and CLR types.
        /// </summary>
        ICodeFirstTypeProvider TypeProvider { get; set; }

        /// <summary>
        /// Ensures mapping between Kentico Cloud content item fields and model properties.
        /// </summary>
        ICodeFirstPropertyMapper PropertyMapper { get; set; }
    }
}