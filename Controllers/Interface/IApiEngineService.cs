using System;
using System.Collections.Generic;
using API.Enums;
using API.Models;
using API.Models.Temporary;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Interface {
    public interface IApiEngineService<TRelation, TUser> where TUser : IdentityUser {
        int GetMaxRangeReadPage (string ResourceName);
        int GetMaxRangeReadObjectPerPage (string ResourceName);
        string GetCursorEncryptionKey ();
        Type MapRelationToType (string relation);

        void OnResourceRead (DbContext dbContext, IRequest request, object model);
        void OnResourceDeleted (DbContext dbContext, IRequest request, object model);
        void OnResourceCreated (DbContext dbContext, IRequest request, object model, ModelInteraction<TRelation> intraction);
        void OnResourcePatched (DbContext dbContext, IRequest request, object _new);
        void OnRelationCreated (DbContext dbContext, IRequest request, IRequest relatedRequest, object rel);
        void OnRelationDeleted (DbContext dbContext, IRequest request, IRequest relatedRequest, object rel);
        dynamic OnRequestError (DbContext dbContext, Exception exception, ControllerBase controller);
    }
}