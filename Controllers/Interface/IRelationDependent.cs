
using API.Engine;
using API.Enums;
using API.Models.Temporary;
using Microsoft.EntityFrameworkCore;

namespace API.Interface {
    public interface IRelationDependent<TRelation> {
        dynamic OnRelationEvent (
            DbContext dbContext,
            object model,
            string requesterID,
            IRequest request,
            IRequest dependentRequest,
            TRelation intractionType,
            HttpRequestMethod httpRequestMethod);
    }
}