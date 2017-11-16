using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DaaSDemo.KubeClient.Models
{
    /// <summary>
    ///     APIResource specifies the name of a resource and whether it is namespaced.
    /// </summary>
    public class APIResourceV1
    {
        /// <summary>
        ///     categories is a list of the grouped resources this resource belongs to (e.g. 'all')
        /// </summary>
        [JsonProperty("categories")]
        public List<string> Categories { get; set; }

        /// <summary>
        ///     kind is the kind for the resource (e.g. 'Foo' is the kind for a resource 'foo')
        /// </summary>
        [JsonProperty("kind")]
        public string Kind { get; set; }

        /// <summary>
        ///     name is the plural name of the resource.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        ///     namespaced indicates if a resource is namespaced or not.
        /// </summary>
        [JsonProperty("namespaced")]
        public bool Namespaced { get; set; }

        /// <summary>
        ///     shortNames is a list of suggested short names of the resource.
        /// </summary>
        [JsonProperty("shortNames")]
        public List<string> ShortNames { get; set; }

        /// <summary>
        ///     singularName is the singular name of the resource.  This allows clients to handle plural and singular opaquely. The singularName is more correct for reporting status on a single item and both singular and plural are allowed from the kubectl CLI interface.
        /// </summary>
        [JsonProperty("singularName")]
        public string SingularName { get; set; }
    }
}
