using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using API.Engine;
using API.Engine.Extention;
using API.Enums;
using API.Models;
using API.Models.Architecture;
using API.Models.Temporary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Engine {
    public partial class NeutronGeneralAPI<TRelation, TUser> {
        private dynamic DeleteResource (
            IRequest request,
            TRelation relationType,
            IRequest relatedRequest,
            PermissionHandler<TRelation, TUser> permissionHandler) {

            var resource = APIUtils.GetResource (dbContext, request) as object;

            if (resource == null)
                return new NotFoundResult ();

            if (!(resource is RootModel)) {
                return new BadRequestObjectResult (
                    new APIError {
                        Code = StatusCodes.Status424FailedDependency,
                            Message = "Only api architecture based models can be deleted"
                    }
                );
            }

            var globalIntraction = dbContext.MagicDbSet (EngineService.MapRelationToType ("Global")) as IEnumerable<dynamic>;
            var globalRelation = Enum.Parse (enumType: typeof (TRelation), value: "Global");

            var isCreator = globalIntraction
                .Where (predicate: intraction =>
                    intraction.IntractionType.Equals (globalRelation) &&
                    intraction.Valid &&
                    ((intraction.ValidUntil == null || intraction.ValidUntil.HasValue == false) ||
                        (intraction.ValidUntil.HasValue && System.DateTime.Now.CompareTo (intraction.ValidUntil.Value) < 0)) &&
                    (intraction.CreatorId.Equals (permissionHandler.getRequesterID ()) &&
                        intraction.FirstModelId.Equals (permissionHandler.getRequesterID ()) &&
                        intraction.SecondModelId.Equals (resource.GetKeyPropertyValue ())))
                .Any ();

            if (!isCreator) {
                return new BadRequestObjectResult (
                new APIError {
                Code = StatusCodes.Status403Forbidden,
                Message = "Only object creator (The owner) can delete it"
                });
            }

            resource.GetType ().GetProperty (nameof (RootModel.Deactivated)).SetValue (resource, true);

            dbContext.Entry (resource).Property (nameof (RootModel.Deactivated)).IsModified = true;
            dbContext.SaveChanges ();

            EngineService.OnResourceDeleted (dbContext, request, resource);

            return new OkResult ();
        }
    }
}