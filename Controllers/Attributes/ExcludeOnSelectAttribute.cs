using System;

namespace API.Attributes {
    [AttributeUsage (AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ExcludeOnSelectAttribute : Attribute { }
}