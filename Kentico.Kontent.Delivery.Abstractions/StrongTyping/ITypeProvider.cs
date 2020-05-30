using System;

namespace Kentico.Kontent.Delivery.Abstractions.StrongTyping
{
    /// <summary>
    /// Defines the contract for mapping Kentico Kontent content types to CLR types.
    /// </summary>
    public interface ITypeProvider
    {
        /// <summary>
        /// Returns a CLR type corresponding to the given content type.
        /// </summary>
        /// <param name="contentType">Content type identifier.</param>
        Type GetType(string contentType);

        /// <summary>
        /// Returns a codename corresponding to the given content type model.
        /// </summary>
        /// <param name="contentType">Content type model.</param>
        string GetCodename(Type contentType);
    }
}