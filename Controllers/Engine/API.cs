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
using API.Models.Framework;
using API.Models.Temporary;
using AspNet.Security.OAuth.Validation;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize (AuthenticationSchemes = "Identity.Application," + OAuthValidationDefaults.AuthenticationScheme)]
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
                // Parse type of request method to something we can understand
                if (!Enum.TryParse<HttpRequestMethod> (
                        Request.Method,
                        true,
                        out var httpRequestMethod)) {
                    // Request method is not valid, it must be something like Post, Delete and ...
                    return BadRequest (new APIError {
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
                    return BadRequest (new APIError {
                        Message = consistentRequest
                    });

                // If this is a relation request
                if (requestedAction == ModelAction.Relate) {
                    if (relationType == null)
                        return BadRequest (new APIError {
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
                            return BadRequest (new APIError {
                                Message = consistentRelatedRequest
                            });
                    }
                }

                //* Get which user is trying to send this request
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
                        return BadRequest (new APIError {
                            Message = "Requested ModelAction is not supported yet"
                        });
                }
            } catch (Exception exception) {
                return EngineService.OnRequestError (dbContext, exception, this);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route (template: "api/cards/{ResourceName}")]
        public dynamic RangeReader (
            string ResourceName,
            string cursor
        ) {
            var rangeReader = new RangeReader<TRelation, TUser> (
                    EngineService,
                    ModelParser,
                    dbContext
                );

            var requesterID = UserManager.GetUserId (User);
            return rangeReader.ReadCardList (ResourceName, requesterID, cursor);
        }

        [HttpGet]
        [AllowAnonymous]
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