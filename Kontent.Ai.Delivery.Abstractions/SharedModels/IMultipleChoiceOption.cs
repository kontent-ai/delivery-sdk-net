namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents an option of a Multiple choice content element.
    /// </summary>
    public interface IMultipleChoiceOption
    {
        /// <summary>
        /// Gets the codename of the option.
        /// </summary>
        string Codename { get; }

        /// <summary>
        /// Gets the name of the option.
        /// </summary>
        string Name { get; }
    }
}