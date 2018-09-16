using System;
using API.Engine;
using API.Enums;
using API.Interface;
using API.Models.Temporary;
using Microsoft.EntityFrameworkCore;

namespace API.PermissionValidator {
    public class FreeForAllValidator : IAccessChainValidator<Object> {
        public dynamic Validate (
            DbContext dbContext,
            string requesterID,
            IRequest request,
            object modelItself,
            string typeName,
            object typeValue,
            ModelAction modelAction,
            HttpRequestMethod requestMethod,
            Object relationType) => true;
    }
}