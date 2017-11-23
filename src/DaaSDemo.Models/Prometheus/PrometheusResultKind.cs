namespace DaaSDemo.Models.Prometheus
{
    /// <summary>
    ///     Well-known kinds of results from Prometheus queries.
    /// </summary>
    public enum PrometheusResultKind
    {
        /// <summary>
        ///     A matrix result (array of arrays).
        /// </summary>
        Matrix = 1,

        /// <summary>
        ///     A vector result (array).
        /// </summary>
        Vector = 2,

        /// <summary>
        ///     A scalar result (raw value).
        /// </summary>
        Scalar = 3,

        /// <summary>
        ///     A string result.
        /// </summary>
        String = 4
    }
}
