namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents a credential used to authenticate a database user.
    /// </summary>
    public abstract class DatabaseUserCredential
        : IDeepCloneable<DatabaseUserCredential>
    {
        /// <summary>
        ///     A <see cref="DatabaseUserCredentialKind"/> value that indicates the credential type.
        /// </summary>
        public abstract DatabaseUserCredentialKind Kind { get; }

        /// <summary>
        ///     Create a deep clone of the <see cref="DatabaseUserCredential"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseUserCredential"/>.
        /// </returns>
        public abstract DatabaseUserCredential Clone();
    }
}
