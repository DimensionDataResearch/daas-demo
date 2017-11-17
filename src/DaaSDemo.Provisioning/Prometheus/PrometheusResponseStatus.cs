using System.Runtime.Serialization;

namespace DaaSDemo.Provisioning.Prometheus
{
    /// <summary>
    ///     Represents the status of a Promethius request.
    /// </summary>
    public enum PrometheusResponseStatus
    {
        /// <summary>
        ///     The request succeeded.
        /// </summary>
        [EnumMember(Value = "success")]
        Success = 1,
        
        /// <summary>
        ///     The request failed.
        /// </summary>
        [EnumMember(Value = "success")]
        Error = 2
    }
}
