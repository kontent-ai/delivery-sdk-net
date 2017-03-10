using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Defines the contract for mapping Kentico Cloud content types to CLR types.
    /// </summary>
    public interface ICodeFirstTypeProvider
    {
        /// <summary>
        /// Returns a CLR type corresponding to the given content type.
        /// </summary>
        /// <param name="contentType">Content type identifier.</param>
        Type GetType(string contentType);
    }
}
