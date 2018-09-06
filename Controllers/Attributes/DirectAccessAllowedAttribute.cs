using System;

namespace API.Attributes {
    /// <summary>
    /// Only resources with direct access allowed attribute can be CRUD/R, any other resources
    /// only must be access indirectly within other DirectAccessAllowed models
    /// </summary>
    public sealed class DirectAccessAllowedAttribute : Attribute { }
}