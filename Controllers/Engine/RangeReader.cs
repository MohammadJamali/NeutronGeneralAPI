using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using API.Attributes;
using API.Engine.Extention;
using API.Enums;
using API.Interface;
using API.Models.Framework;
using API.Models.Temporary;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Engine {
    public class RangeReader<TRelation, TUser> where TUser : IdentityUser {
        private readonly IApiEngineService<TRelation, TUser> engineService;
        private readonly IModelParser modelParser;
        private readonly DbContext dbContext;

        public RangeReader (
            IApiEngineService<TRelation, TUser> engineService,
            IModelParser modelParser,
            DbContext dbContext) {
            this.engineService = engineService;
            this.modelParser = modelParser;
            this.dbContext = dbContext;
        }

        public dynamic ReadCardList (
            string resourceName,
            string requesterID,
            string cursor
        ) {
            Cursor objCursor = null;
            if (cursor != null && !string.IsNullOrWhiteSpace (cursor)) {
                try {
                    var decryptedCursor = cursor.DecryptString (
                        engineService.GetCursorEncryptionKey ()
                    );
                    objCursor = JsonConvert.DeserializeObject<Cursor> (decryptedCursor);
                } catch (Exception) { }
            }

            var maxPage = engineService.GetMaxRangeReadPage (resourceName);
            var maxOPP = engineService.GetMaxRangeReadObjectPerPage (resourceName);

            if (objCursor == null) {
                objCursor = new Cursor (requesterID, resourceName);
            } else {
                if (objCursor.isExpired ())
                    return new BadRequestObjectResult (new APIError {
                        Message = "Cursor time limit has been expired."
                    });

                if (objCursor.RequesterID != requesterID ||
                    objCursor.IssuedFor != resourceName)
                    return new BadRequestObjectResult (new APIError {
                        Message = "Cursor has been issued for someone else."
                    });

                if (objCursor.PageNumber < 1 ||
                    objCursor.PageNumber > maxPage ||
                    objCursor.ObjPerPage < 1 ||
                    objCursor.ObjPerPage > maxOPP)
                    return new BadRequestObjectResult (new APIError {
                        Message = "Cursor page number or object/page is out of bound."
                    });
            }

            var resourceType = modelParser.IsRangeReaderAllowed (resourceName);
            if (resourceType == null)
                return new NotFoundResult ();

            var endPoint = objCursor.PageNumber * objCursor.ObjPerPage;
            var startpoint = endPoint - objCursor.ObjPerPage;

            var rangeAttribute = resourceType.GetCustomAttribute<RangeReaderAllowedAttribute> ();
            if (endPoint > rangeAttribute.MaxObjToRead) {
                return new BadRequestObjectResult (new APIError {
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
                                resourceName,
                                objCursor.PageNumber + 1,
                                objCursor.ObjPerPage
                            )
                        )
                        .EncryptString (engineService.GetCursorEncryptionKey ());
                } catch (Exception) {
                    return new BadRequestObjectResult (new APIError {
                        Message = "Cursor creation has been failed"
                    });
                }
            }

            APIUtils utils = new APIUtils ();

            var result = utils.GetResourceWithRange (
                APIUtils.GetIQueryable (dbContext, resourceType.Name),
                startpoint,
                endPoint) as ICollection<Card>;

            if (result == null || result.Count == 0)
                return new CardList ();

            var rangerIdProp = resourceType.GetProperties ()
                .Where (prop => prop.IsDefined (typeof (KeyAttribute), false))
                .FirstOrDefault ();

            var permissionHandler = new PermissionHandler<TRelation, TUser> (
                    requesterID,
                    modelParser,
                    engineService,
                    dbContext);
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

            return new CardList {
                PageNumber = objCursor.PageNumber,
                    Cursor = nextCursor,
                    Cards = result.ToList (),
            };
        }

    }
}