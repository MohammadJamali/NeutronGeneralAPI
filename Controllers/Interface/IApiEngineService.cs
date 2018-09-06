using System;
using System.Collections.Generic;
using API.Models;
using API.Models.Temporary;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Interface {
    public interface IApiEngineService<TRelation, TUser> where TUser : IdentityUser {
        int GetMaxRengeReadPage (string ResourceName);
        int GetMaxRengeReadObjectPerPage (string ResourceName);
        string GetCursorEncryptionKey ();
        Type MapRelationToType (string relation);

        void OnResourceReaded (IRequest request, object model);
        void OnResourceDeleted (IRequest request, object model);
        void OnResourceCreated (IRequest request, object model, ModelIntraction<TRelation> intraction);
        void OnResourcePatched (IRequest request, object _new);
        void OnRelationCreated (IRequest request, IRequest relatedRequest, object rel);
        void OnRelationDeleted (IRequest request, IRequest relatedRequest, object rel);
        dynamic OnRequestError (Exception exception, ControllerBase controller);
    }
}