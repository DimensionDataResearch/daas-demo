using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     Probe describes a health check to be performed against a container to determine whether it is alive or ready to receive traffic.
    /// </summary>
    public class ProbeV1
    {
        /// <summary>
        ///     One and only one of the following should be specified. Exec specifies the action to take.
        /// </summary>
        [JsonProperty("exec")]
        public ExecActionV1 Exec { get; set; }

        /// <summary>
        ///     Minimum consecutive failures for the probe to be considered failed after having succeeded. Defaults to 3. Minimum value is 1.
        /// </summary>
        [JsonProperty("failureThreshold")]
        public int FailureThreshold { get; set; }

        /// <summary>
        ///     HTTPGet specifies the http request to perform.
        /// </summary>
        [JsonProperty("httpGet")]
        public HTTPGetActionV1 HttpGet { get; set; }

        /// <summary>
        ///     Number of seconds after the container has started before liveness probes are initiated. More info: https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle#container-probes
        /// </summary>
        [JsonProperty("initialDelaySeconds")]
        public int InitialDelaySeconds { get; set; }

        /// <summary>
        ///     How often (in seconds) to perform the probe. Default to 10 seconds. Minimum value is 1.
        /// </summary>
        [JsonProperty("periodSeconds")]
        public int PeriodSeconds { get; set; }

        /// <summary>
        ///     Minimum consecutive successes for the probe to be considered successful after having failed. Defaults to 1. Must be 1 for liveness. Minimum value is 1.
        /// </summary>
        [JsonProperty("successThreshold")]
        public int SuccessThreshold { get; set; }

        /// <summary>
        ///     TCPSocket specifies an action involving a TCP port. TCP hooks not yet supported
        /// </summary>
        [JsonProperty("tcpSocket")]
        public TCPSocketActionV1 TcpSocket { get; set; }
    }
}
