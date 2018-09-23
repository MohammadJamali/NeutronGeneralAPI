using System;

namespace API.Attributes {
    /// <summary>
    /// Because of ef core lazy loading, we need to include some properties like ICollections
    /// and so on, when you declare any property with this attribute, API query engine will include
    /// it when try to build select dynamic expression
    ///
    /// <remarks>ONLY direct properties will include in this version, so you can not include inner
    /// properties. This will fix on next versions.</remarks>
    /// </summary>
    public sealed class IncludeAttribute : Attribute { }
}