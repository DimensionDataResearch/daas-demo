using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     HTTPIngressRuleValue is a list of http selectors pointing to backends. In the example: http://<host>/<path>?<searchpart> -> backend where where parts of the url correspond to RFC 3986, this resource will be used to match against everything after the last '/' and before the first '?' or '#'.
    /// </summary>
    public class HTTPIngressRuleValueV1Beta1
    {
        /// <summary>
        ///     A collection of paths that map requests to backends.
        /// </summary>
        [JsonProperty("paths", NullValueHandling = NullValueHandling.Ignore)]
        public List<HTTPIngressPathV1Beta1> Paths { get; set; } = new List<HTTPIngressPathV1Beta1>();
    }
}
