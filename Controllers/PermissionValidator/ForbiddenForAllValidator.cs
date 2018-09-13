using System;
using API.Engine;
using API.Enums;
using API.Interface;
using API.Models.Temporary;
using Microsoft.EntityFrameworkCore;

namespace API.PermissionValidator {
    public class ForbiddenForAllValidator : IAccessChainValidator<Object> {
        public dynamic Validate (
            DbContext dbContext,
            string requesterID,
            IRequest request,
            string typeName,
            object typeValue,
            ModelAction modelAction,
            HttpRequestMethod requestMethod,
            Object relationType) => false;
    }
}