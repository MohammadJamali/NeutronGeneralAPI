using System;

namespace API.Attributes {
    [AttributeUsage (AttributeTargets.Property, AllowMultiple = true)]
    public sealed class PruneListAttribute : Attribute {
        public int Amount { get; set; }
        public PruneListAttribute (int Amount = 5) {
            this.Amount = Amount;
        }
    }
}