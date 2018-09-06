using System;
using System.Linq.Expressions;
using System.Reflection;

namespace API.Models.Temporary {
    public class Searchable {
        public Type ResourceType { get; set; }
        public string PropertyName { get; set; }
        public Expression AccessExpression { get; set; }
    }
}