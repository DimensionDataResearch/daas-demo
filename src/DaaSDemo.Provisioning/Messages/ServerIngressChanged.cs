namespace DaaSDemo.Provisioning.Messages
{
    using Models.Data;

    /// <summary>
    ///     Message indicating the the ingress (external IP / port) details for a database server have changed.
    /// </summary>
    public class ServerIngressChanged
    {
        /// <summary>
        ///     Create a new <see cref="ServerIngressChanged"/> message.
        /// </summary>
        /// <param name="serverId">
        ///     The Id of the server whose ingress details have changed.
        /// </param>
        /// <param name="publicFQDN">
        ///     The server's public fully-qualified domain name.
        /// </param>
        /// <param name="publicPort">
        ///     The server's public TCP port.
        /// </param>
        public ServerIngressChanged(string serverId, string publicFQDN, int? publicPort)
        {
            ServerId = serverId;
            PublicFQDN = publicFQDN;
            PublicPort = publicPort;
        }

        /// <summary>
        ///     The Id of the server whose ingress details have changed.
        /// </summary>
        public string ServerId { get; }

        /// <summary>
        ///     The server's ingress (external) IP address.
        /// </summary>
        public string PublicFQDN { get; }

        /// <summary>
        ///     The server's ingress (external) TCP port.
        /// </summary>
        public int? PublicPort { get; }
    }
}
