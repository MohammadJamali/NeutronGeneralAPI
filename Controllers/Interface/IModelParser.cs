using System;
using System.Linq.Expressions;
using API.Attributes;
using API.Enums;

namespace API.Interface {
    public interface IModelParser {
        Type IsRangeReaderAllowed(string resource);
        Type GetResourceType(string resource);
        Expression GetPropertySearchAttribute(string resource, string property);
    }
}