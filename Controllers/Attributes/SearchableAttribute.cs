using System;
using System.Runtime.CompilerServices;
using API.Enums;

namespace API.Attributes {
    [AttributeUsage (AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class SearchableAttribute : Attribute {
        public SearchType SearchType { get; set; }
        public string AccessPath { get; set; }

        public SearchableAttribute (SearchType SearchType, string accessPath) {
            this.AccessPath = AccessPath;
            this.SearchType = SearchType;
        }
    }
}