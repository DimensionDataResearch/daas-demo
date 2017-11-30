namespace DaaSDemo.Models.Data
{
    /// <summary>
    ///     Represents a password used to authenticate a database user.
    /// </summary>
    public sealed class DatabaseUserPassword
        : DatabaseUserCredential
    {
        /// <summary>
        ///     A <see cref="DatabaseUserCredentialKind"/> value that indicates the credential type.
        /// </summary>
        public override DatabaseUserCredentialKind Kind => DatabaseUserCredentialKind.Password;

        /// <summary>
        ///     The password used to authenticate the database user.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///     Create a deep clone of the <see cref="DatabaseUserPassword"/>.
        /// </summary>
        /// <returns>
        ///     The cloned <see cref="DatabaseUserPassword"/>.
        /// </returns>
        public override DatabaseUserCredential Clone()
        {
            return new DatabaseUserPassword
            {
                Password = Password
            };
        }
    }
}
