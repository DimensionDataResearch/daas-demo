using System;

namespace DaaSDemo.Models.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EntitySet
        : Attribute
    {
        public EntitySet(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'entitySetName'.", nameof(name));
            
            Name = name;
        }

        public string Name { get; }
    }
}
