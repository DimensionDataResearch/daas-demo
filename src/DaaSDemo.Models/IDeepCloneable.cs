namespace DaaSDemo.Models
{
    /// <summary>
    ///     Represents a deeply-cloneable entity.
    /// </summary>
    public interface IDeepCloneable<T>
    {
        /// <summary>
        ///     Create a deep clone of the entity.
        /// </summary>
        /// <returns>
        ///     The cloned entity.
        /// </returns>
        T Clone();
    }
}
