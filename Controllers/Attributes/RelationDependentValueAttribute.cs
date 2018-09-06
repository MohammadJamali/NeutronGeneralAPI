using System;

namespace API.Attributes {
    /// <summary>
    /// Sometimes there are some properties which must be update when new relation created, for example
    /// every time a user wants to like a post, api will create a relation with name "like" for both
    /// objects (user and post) and invoke OnRelationCreated for properties of both types which have this
    /// attribute, for example it can increment the value of number of likes and OnReleationDeleted will
    /// invoke when user wants to unlike a post, so it can decrement the number of likes
    ///
    /// <remarks>Inner properties will not be covered</remarks>
    /// </summary>
    [AttributeUsage (AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class RelationDependentValueAttribute : Attribute {
        public Type OnRelationCreated { get; set; }
        public Type OnReleationDeleted { get; set; }

        public RelationDependentValueAttribute (
            Type OnRelationCreated,
            Type OnReleationDeleted) {
            this.OnRelationCreated = OnRelationCreated;
            this.OnReleationDeleted = OnReleationDeleted;
        }
    }
}