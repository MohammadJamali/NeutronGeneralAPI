using System;
using System.Runtime.CompilerServices;

namespace API.Attributes {
    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public class RangeReaderExtraAttribute : Attribute {
        public string propertyName { get; set; }

        public RangeReaderExtraAttribute ([CallerMemberName] string propertyName = null) {
            this.propertyName = propertyName;
        }
    }
}