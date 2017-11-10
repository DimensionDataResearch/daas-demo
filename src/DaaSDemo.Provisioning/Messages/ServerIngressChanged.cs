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
        /// <param name="ingressIP">
        ///     The server's ingress (external) IP address.
        /// </param>
        /// <param name="ingressPort">
        ///     The server's ingress (external) TCP port.
        /// </param>
        public ServerIngressChanged(int serverId, string ingressIP, int? ingressPort)
        {
            ServerId = serverId;
            IngressIP = ingressIP;
            IngressPort = ingressPort;
        }

        /// <summary>
        ///     The Id of the server whose ingress details have changed.
        /// </summary>
        public int ServerId { get; }

        /// <summary>
        ///     The server's ingress (external) IP address.
        /// </summary>
        public string IngressIP { get; }

        /// <summary>
        ///     The server's ingress (external) TCP port.
        /// </summary>
        public int? IngressPort { get; }
    }
}