using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using API.Attributes;
using API.Engine.Extention;
using API.Engine.MagicQuery;
using API.Enums;
using API.Interface;
using API.Models;
using API.Models.Temporary;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Engine {
    public partial class NeutronGeneralAPI<TRelation, TUser> {

        private dynamic JoinResourceAsync (
            IRequest request,
            IRequest relatedRequest,
            TRelation relationType,
            PermissionHandler<TRelation, TUser> permissionHandler,
            HttpRequestMethod httpRequestMethod) {

            var oneWayRelation = relatedRequest == null || !relatedRequest.filledWithData ();

            if (oneWayRelation) relatedRequest = request;

            var resourceType =
                oneWayRelation ?
                typeof (TUser) :
                ModelParser.GetResourceType (request.ResourceName);

            var relatedResourceType = ModelParser.GetResourceType (relatedRequest.ResourceName);

            bool firstIdentifierNameIsKey = true;
            if (!oneWayRelation)
                firstIdentifierNameIsKey = resourceType.GetProperties ()
                .Where (property =>
                    property.Name.Equals (request.IdentifierName) &&
                    property.IsDefined (typeof (KeyAttribute), true))
                .Any ();

            bool secondIdentifierNameIsKey = relatedResourceType.GetProperties ()
                .Where (prop =>
                    prop.Name.Equals (relatedRequest.IdentifierName) &&
                    prop.IsDefined (typeof (KeyAttribute), true))
                .Any ();

            if (!firstIdentifierNameIsKey || !secondIdentifierNameIsKey)
                return BadRequest (new {
                    Message = "To create relation only key identifier is acceptable"
                });

            // Check whether request is exist or not
            var requestModel = oneWayRelation ?
                UserManager.FindByIdAsync (permissionHandler.getRequesterID ()).Result :
                APIUtils.GetResource (dbContext, request);
            if (requestModel == null)
                return NotFound (request);

            // Check if relationType is valid for request
            var resourceCheck =
                permissionHandler.ModelValidation (
                    Request: request,
                    ModelType: resourceType,
                    ModelAction: ModelAction.Relate,
                    RequestMethod: httpRequestMethod,
                    RelationType: relationType);
            if (!(resourceCheck is bool && (bool) resourceCheck))
                return BadRequest (new {
                    Message = "Request Error: " + resourceCheck
                });

            // Check whether related request is exist or not
            var relatedRequestModel = APIUtils.GetResource (dbContext, relatedRequest);

            if (relatedRequestModel == null)
                return NotFound (relatedRequest);

            // Check if relationType is valid for related request
            var relatedSourceCheck =
                permissionHandler.ModelValidation (
                    Request: relatedRequest,
                    ModelType: relatedResourceType,
                    ModelAction: ModelAction.Relate,
                    RequestMethod: httpRequestMethod,
                    RelationType: relationType);

            if (!(relatedSourceCheck is bool && (bool) relatedSourceCheck))
                return BadRequest (new {
                    Message = "Request Error: " + relatedSourceCheck
                });

            if (!oneWayRelation) {
                // Check if relationType is valid for requesterID
                var userCheck =
                    permissionHandler.ModelValidation (
                        Request: request,
                        ModelType: typeof (TUser),
                        ModelAction: ModelAction.Relate,
                        RequestMethod: httpRequestMethod,
                        RelationType: relationType);
                if (!(userCheck is bool && (bool) userCheck))
                    return BadRequest (new {
                        Message = "Request Error: " + userCheck
                    });
            }

            var intractionType = EngineService.MapRelationToType (relationType.ToString ());
            var queryable = dbContext.MagicDbSet (intractionType);

            var FirstModelId = oneWayRelation ? permissionHandler.getRequesterID () : request.IdentifierValue;
            var SecondModelId = relatedRequest.IdentifierValue;

            var relation =
                (queryable as IEnumerable<dynamic>)
                .Where (intraction =>
                    intraction.Valid &&
                    ((intraction.ValidUntil == null || intraction.ValidUntil.HasValue == false) ||
                        (intraction.ValidUntil.HasValue && DateTime.Now.CompareTo (intraction.ValidUntil.Value) < 0)) &&
                    (intraction.CreatorId.Equals (permissionHandler.getRequesterID ()) &&
                        intraction.FirstModelId.Equals (FirstModelId) &&
                        intraction.SecondModelId.Equals (SecondModelId)))
                .Take (1)
                .FirstOrDefault ();

            // Create relation
            if (httpRequestMethod == HttpRequestMethod.Post) {
                if (relation != null)
                    return new OkObjectResult (relation);

                relation = new ModelIntraction<TRelation> {
                    CreatorId = permissionHandler.getRequesterID (),
                    FirstModelId = FirstModelId,
                    SecondModelId = SecondModelId,
                    IntractionType = relationType
                };

                MagicExtentions.MagicAddIntraction (queryable, relation, intractionType);

                EngineService.OnRelationCreated (request, relatedRequest, relation);
            } else if (httpRequestMethod == HttpRequestMethod.Delete) {
                if (relation == null)
                    return new OkResult ();

                queryable.Remove (relation);

                relation.Valid = false;
                relation.ValidUntil = DateTime.Now;
                relation.Information = "Extinct by " + permissionHandler.getRequesterID ();

                dbContext.MagicAddIntraction (relation as object, EngineService.MapRelationToType ("Invalid"));
                EngineService.OnRelationDeleted (request, relatedRequest, relation);
            } else {
                return BadRequest ();
            }

            ResolveRelationDependency (
                requestModel,
                permissionHandler.getRequesterID (),
                request,
                relatedRequest,
                relationType,
                httpRequestMethod);

            ResolveRelationDependency (
                relatedRequestModel,
                permissionHandler.getRequesterID (),
                request,
                relatedRequest,
                relationType,
                httpRequestMethod);

            dbContext.SaveChanges ();

            return new OkObjectResult (relation);
        }

        private void ResolveRelationDependency (
            object model,
            string requesterID,
            IRequest request,
            IRequest relatedRequest,
            TRelation intractionType,
            HttpRequestMethod httpRequestMethod) {
            var propertyList = model.GetType ()
                .GetProperties ()
                .Where (property => property.IsDefined (typeof (RelationDependentValueAttribute), true))
                .ToList ();

            var updateModel = false;
            foreach (var item in propertyList) {
                var attribute = item.GetCustomAttribute<RelationDependentValueAttribute> ();

                var relationDependentResolver =
                    httpRequestMethod == HttpRequestMethod.Post ?
                    attribute.OnRelationCreated :
                    attribute.OnReleationDeleted;

                var result = APIUtils.InvokeMethod (
                    relationDependentResolver,
                    "OnRelationEvent",
                    new object[] {
                        dbContext,
                        model,
                        requesterID,
                        request,
                        relatedRequest,
                        intractionType,
                        httpRequestMethod
                    });

                if (result != null) {
                    model = result;
                    updateModel = true;
                }
            }

            if (updateModel) {
                dbContext.Update (model);
            }
        }
    }
}