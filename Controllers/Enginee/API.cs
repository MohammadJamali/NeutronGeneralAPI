using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using API.Attributes;
using API.Engine.Extention;
using API.Engine.JSON;
using API.Enums;
using API.Interface;
using API.Models;
using API.Models.Temporary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace API.Engine {
    public abstract partial class NeutronGeneralAPI<TRelation, TUser> : Controller where TUser : IdentityUser {
        private readonly IApiEngineService<TRelation, TUser> EngineService;
        private readonly DbContext dbContext;
        private readonly UserManager<TUser> UserManager;
        private readonly APIUtils Utils;
        private readonly IModelParser ModelParser;

        public NeutronGeneralAPI (
            DbContext dbContext,
            UserManager<TUser> userManager,
            IModelParser modelParser,
            IApiEngineService<TRelation, TUser> engineService) {
            this.UserManager = userManager;
            this.dbContext = dbContext;
            this.ModelParser = modelParser;
            this.EngineService = engineService;

            this.Utils = new APIUtils ();
        }

        private IActionResult Handle (
            IRequest request,
            ModelAction requestedAction,
            TRelation relationType,
            IRequest relatedRequest,
            string jsonObject) {
            try {
                // Parse type of request method to somwthing we can understand
                if (!Enum.TryParse<HttpRequestMethod> (
                        Request.Method,
                        true,
                        out var httpRequestMethod)) {
                    // Request method is not valid, it must be something like Post, Delete and ...
                    return BadRequest (new {
                        Message = "Request method is not valid, it must be " +
                            "{Post | Get | Patch | Delete}"
                    });
                }

                var permissionHandler = new PermissionHandler<TRelation, TUser> (
                        UserManager.GetUserId (User),
                        ModelParser,
                        EngineService,
                        dbContext);

                //* Security Checks *//
                //* Check whether this set is consistent or not {HttpRequestMethod,
                //* IRequest, ModelAction, RelationType}
                var consistentRequest =
                    permissionHandler.ValidateRequest (
                        httpRequestMethod,
                        request,
                        requestedAction,
                        relationType);
                if (!(consistentRequest is bool && (bool) consistentRequest))
                    return BadRequest (new {
                        Message = consistentRequest
                    });

                // If this is a relation request
                if (requestedAction == ModelAction.Relate) {
                    if (relationType == null)
                        return BadRequest (new {
                            Message = "Relation type must be determined"
                        });

                    if (relatedRequest == null || relatedRequest.ResourceName == null) {
                        // if this isn't a relation request, related input args are better to be empty
                        relatedRequest = null;
                    } else {
                        //* Check whether this set is consistent or not {HttpRequestMethod,
                        //* IRequest, ModelAction, RelationType}
                        var consistentRelatedRequest =
                            permissionHandler.ValidateRequest (
                                httpRequestMethod,
                                relatedRequest,
                                requestedAction,
                                relationType);
                        if (!(consistentRelatedRequest is bool && (bool) consistentRelatedRequest))
                            return BadRequest (new {
                                Message = consistentRelatedRequest
                            });
                    }
                }

                //* Get which user is tring to send this request
                switch (requestedAction) {
                    case ModelAction.Create:
                        return CreateResource (
                            request,
                            httpRequestMethod,
                            permissionHandler,
                            jsonObject);
                    case ModelAction.Read:
                        return ReadResource (
                            request,
                            httpRequestMethod,
                            permissionHandler);
                    case ModelAction.Delete:
                        return DeleteResource (
                            request,
                            relationType,
                            relatedRequest,
                            permissionHandler);
                    case ModelAction.Update:
                        return PatchResource (
                            request,
                            httpRequestMethod,
                            permissionHandler,
                            jsonObject);
                    case ModelAction.Relate:
                        return JoinResourceAsync (
                            request,
                            relatedRequest,
                            relationType,
                            permissionHandler,
                            httpRequestMethod);
                    default:
                        return BadRequest (new {
                            Message = "Requested ModelAction is not supported yet"
                        });
                }
            } catch (Exception exception) {
                return EngineService.OnRequestError (exception, this);
            }
        }

        [HttpGet]
        [Route (template: "api/cards/{ResourceName}")]
        public dynamic RangeReader (
            string ResourceName,
            string cursor
        ) {
            Cursor objCursor = null;
            if (cursor != null && !string.IsNullOrWhiteSpace (cursor)) {
                try {
                    var decryptedCursor = cursor.DecryptString (
                        EngineService.GetCursorEncryptionKey ()
                    );
                    objCursor = JsonConvert.DeserializeObject<Cursor> (decryptedCursor);
                } catch (System.Exception) { }
            }

            var maxPage = EngineService.GetMaxRengeReadPage (ResourceName);
            var maxOPP = EngineService.GetMaxRengeReadObjectPerPage (ResourceName);

            var requesterID = UserManager.GetUserId (User);
            if (objCursor == null) {
                objCursor = new Cursor (requesterID, ResourceName);
            } else {
                if (objCursor.isExpired ())
                    return BadRequest (new {
                        Message = "Cursor time limit has been expired."
                    });

                if (objCursor.RequesterID != requesterID ||
                    objCursor.IssuedFor != ResourceName)
                    return BadRequest (new {
                        Message = "Cursor has been issued for someone else."
                    });

                if (objCursor.PageNumber < 1 ||
                    objCursor.PageNumber > maxPage ||
                    objCursor.ObjPerPage < 1 ||
                    objCursor.ObjPerPage > maxOPP)
                    return BadRequest (new {
                        Message = "Cursor page number or object/page is out of bound."
                    });
            }

            var resourceType = ModelParser.IsRangeReaderAllowed (ResourceName);
            if (resourceType == null)
                return NotFound ();

            var endPoint = objCursor.PageNumber * objCursor.ObjPerPage;
            var startpoint = endPoint - objCursor.ObjPerPage;

            var rangeAttribute = resourceType.GetCustomAttribute<RangeReaderAllowedAttribute> ();
            if (endPoint > rangeAttribute.MaxObjToRead) {
                return BadRequest (new {
                    Message = "Requested range is exceeded from resource limitations (" +
                        rangeAttribute.MaxObjToRead + ")"
                });
            }

            var nextCursor = string.Empty;
            if (endPoint + objCursor.ObjPerPage <= rangeAttribute.MaxObjToRead &&
                objCursor.PageNumber <= maxPage &&
                objCursor.ObjPerPage <= maxOPP) {
                try {
                    nextCursor =
                        JsonConvert.SerializeObject (
                            new Cursor (
                                requesterID,
                                ResourceName,
                                objCursor.PageNumber + 1,
                                objCursor.ObjPerPage
                            )
                        )
                        .EncryptString (EngineService.GetCursorEncryptionKey ());
                } catch (Exception) {
                    return BadRequest (new {
                        Message = "Cursor creation has been failed"
                    });
                }
            }

            var result = Utils.GetResourceWithRange (
                APIUtils.GetIQueryable (dbContext, resourceType.Name),
                startpoint,
                endPoint) as ICollection;

            if (result == null || result.Count == 0)
                return NoContent ();

            var rangerIdProp = resourceType.GetProperties ()
                .Where (prop => prop.IsDefined (typeof (KeyAttribute), false))
                .FirstOrDefault ();

            var permissionHandler = new PermissionHandler<TRelation, TUser> (requesterID, ModelParser, EngineService, dbContext);
            var iRequest = new IRequest {
                ResourceName = resourceType.Name,
                IdentifierName = rangerIdProp.Name
            };

            foreach (var item in result) {
                iRequest.IdentifierValue = rangerIdProp.GetValue (item).ToString ();
                permissionHandler.CheckPermissionRequirements (
                    iRequest,
                    ModelAction.Read,
                    HttpRequestMethod.Get,
                    item);
            }

            return JsonConvert.SerializeObject (
                new {
                    page = objCursor.PageNumber,
                        cursor = nextCursor,
                        cards = result
                }
            );
        }

        [HttpGet]
        [Route (template: "api/search/{ResourceName}/{PropertyName}/{FirstValue}/{SecondValue?}")]
        public dynamic Search (
            string ResourceName,
            string PropertyName,
            string FirstValue,
            string SecondValue
        ) {
            return new NotFoundResult ();
        }
    }
}