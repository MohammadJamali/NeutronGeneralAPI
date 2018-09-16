using API.Engine;
using API.Enums;
using API.Models.Temporary;
using Microsoft.EntityFrameworkCore;

namespace API.Interface {
    public interface IAccessChainValidator<TRelation> {
        dynamic Validate (
            DbContext dbContext,
            string requesterID,
            IRequest request,
            object modelItself,
            string typeName,
            object typeValue,
            ModelAction modelAction,
            HttpRequestMethod requestMethod,
            TRelation relationType);
    }
}