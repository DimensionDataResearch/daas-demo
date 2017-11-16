using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     A label selector is a label query over a set of resources. The result of matchLabels and matchExpressions are ANDed. An empty label selector matches all objects. A null label selector matches no objects.
    /// </summary>
    public class LabelSelectorV1
    {
        /// <summary>
        ///     matchExpressions is a list of label selector requirements. The requirements are ANDed.
        /// </summary>
        [JsonProperty("matchExpressions")]
        public List<LabelSelectorRequirementV1> MatchExpressions { get; set; }
    }
}
