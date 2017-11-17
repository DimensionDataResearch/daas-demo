using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DaaSDemo.Provisioning.Filters
{
    using KubeClient.Models;

    /// <summary>
    ///     A filter for events relating to Kubernetes resources.
    /// </summary>
    public sealed class ResourceEventFilter
        : EventFilter, IEquatable<ResourceEventFilter>
    {
        /// <summary>
        ///     No filter (i.e. match all events).
        /// </summary>
        public static readonly ResourceEventFilter Empty = new ResourceEventFilter();

        /// <summary>
        ///     Create a new <see cref="ResourceEventFilter"/>.
        /// </summary>
        ResourceEventFilter()
        {
            LabelSelectors = ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Create a new <see cref="ResourceEventFilter"/> by copying properties from the specified <see cref="ResourceEventFilter"/>.
        /// </summary>
        /// <param name="copyFrom">
        ///     The <see cref="ResourceEventFilter"/> to copy from.
        /// </param>
        ResourceEventFilter(ResourceEventFilter copyFrom)
        {
            if (copyFrom == null)
                throw new ArgumentNullException(nameof(copyFrom));
            
            Name = copyFrom.Name;
            LabelSelectors = copyFrom.LabelSelectors;
        }

        /// <summary>
        ///     Filter out events relating to resources that don't have the specified name.
        /// </summary>
        /// <remarks>
        ///     If <c>null</c>, then no events will be filtered by resource name.
        /// </remarks>
        public string Name { get; private set; }

        /// <summary>
        ///     Filter out events relating to resources that don't match the specified label selectors.
        /// </summary>
        /// <remarks>
        ///     If <c>null</c>, then no events will be filtered by label selector.
        /// </remarks>
        public ImmutableDictionary<string, string> LabelSelectors { get; private set; }

        /// <summary>
        ///     Determine whether the filter matches the specified resource metadata.
        /// </summary>
        /// <param name="resourceMetadata">
        ///     The resource metadata to match.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the filter matches the metadata; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsMatch(ObjectMetaV1 resourceMetadata)
        {
            if (resourceMetadata == null)
                throw new ArgumentNullException(nameof(resourceMetadata));
            
            if (!String.IsNullOrWhiteSpace(Name) && !String.Equals(Name, resourceMetadata.Name))
                return false;

            return MatchLabelSelectors(LabelSelectors, resourceMetadata.Labels);
        }

        /// <summary>
        ///     Determine whether the <see cref="ResourceEventFilter"/> is equal to another <see cref="ResourceEventFilter"/>.
        /// </summary>
        /// <param name="other">
        ///     The other <see cref="ResourceEventFilter"/>.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the <see cref="ResourceEventFilter"/> is equal to the other <see cref="ResourceEventFilter"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ResourceEventFilter other)
        {
            if (ReferenceEquals(other, null))
                return false;

            if (other.Name != Name)
                return false;

            if (!MatchLabelSelectors(LabelSelectors, other.LabelSelectors))
                return false;

            // AF: Lazy!
            if (!MatchLabelSelectors(other.LabelSelectors, LabelSelectors))
                return false;

            return true;
        }

        /// <summary>
        ///     Determine whether the <see cref="ResourceEventFilter"/> is equal to the specified object.
        /// </summary>
        /// <param name="obj">
        ///     The object to test for equality.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the <see cref="ResourceEventFilter"/> is equal to the object; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) => Equals(obj as ResourceEventFilter);

        /// <summary>
        ///     Get a hash code for the <see cref="ResourceEventFilter"/>.
        /// </summary>
        /// <returns>
        ///     The hash code.
        /// </returns>
        public override int GetHashCode()
        {
            int hashCode = 17;
            unchecked
            {
                hashCode += Name?.GetHashCode() ?? 0;
                hashCode *= 37;

                foreach (string label in LabelSelectors.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
                {
                    string value = LabelSelectors[label];

                    hashCode += $"{label}={value}".GetHashCode();
                    hashCode *= 37;
                }
            }

            return hashCode;
        }

        /// <summary>
        ///     Create a copy of the <see cref="ResourceEventFilter"/>, but with the specified name.
        /// </summary>
        /// <param name="name">
        ///     The name to filter on.
        /// </param>
        /// <returns>
        ///     The new <see cref="ResourceEventFilter"/>.
        /// </returns>
        public ResourceEventFilter WithName(string name)
        {
            if (name == Name)
                return this;

            var copy = new ResourceEventFilter(this)
            {
                Name = name
            };
            if (!String.IsNullOrWhiteSpace(name))
                copy.LabelSelectors = Empty.LabelSelectors;

            return copy;
        }

        /// <summary>
        ///     Create a copy of the <see cref="ResourceEventFilter"/>, but with the specified label selector.
        /// </summary>
        /// <param name="name">
        ///     The label selectors to filter on.
        /// </param>
        /// <returns>
        ///     The new <see cref="ResourceEventFilter"/>.
        /// </returns>
        public ResourceEventFilter WithLabelSelectors(ImmutableDictionary<string, string> labelSelectors)
        {
            labelSelectors = labelSelectors ?? Empty.LabelSelectors;

            if (MatchLabelSelectors(labelSelectors, labelSelectors))
                return this;

            var copy = new ResourceEventFilter(this)
            {
                LabelSelectors = labelSelectors
            };
            if (labelSelectors.Count > 0)
                copy.Name = null;

            return copy;
        }

        /// <summary>
        ///     Create a <see cref="ResourceEventFilter"/> that matches the specified resource metadata.
        /// </summary>
        /// <param name="metadata">
        ///     The resource metadata.
        /// </param>
        /// <returns>
        ///     The <see cref="ResourceEventFilter"/>.
        /// </returns>
        public static ResourceEventFilter FromMetatadata(ObjectMetaV1 metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            return new ResourceEventFilter
            {
                Name = metadata.Name,
                LabelSelectors = metadata.Labels != null ? metadata.Labels.ToImmutableDictionary() : Empty.LabelSelectors
            };
        }

        /// <summary>
        ///     Determine whether the specified label selectors match the specified resource labels.
        /// </summary>
        /// <param name="labelSelectors">
        ///     The label selectors to match.
        /// </param>
        /// <param name="resourceLabels">
        ///     The resource labels.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if all the selectors match their corresponding labels; otherwise, <c>false</c>.
        /// </returns>
        internal static bool MatchLabelSelectors(ImmutableDictionary<string, string> labelSelectors, IDictionary<string, string> resourceLabels)
        {
            if (labelSelectors == null)
                throw new ArgumentNullException(nameof(labelSelectors));
            
            if (resourceLabels == null)
                return labelSelectors.Count == 0;
            
            foreach (string label in labelSelectors.Keys)
            {
                string selector;
                labelSelectors.TryGetValue(label, out selector);

                string resourceLabel;
                resourceLabels.TryGetValue(label, out resourceLabel);

                if (!String.Equals(selector, resourceLabel))
                    return false;
            }

            return true;
        }
    }
}
