using System;

namespace API.Attributes {
    /// <summary>
    /// For consistency any permission attribute must be child of PermissionAttribute
    /// </summary>
    [AttributeUsage (AttributeTargets.All, AllowMultiple = true)]
    public abstract class PermissionAttribute : Attribute { }
}