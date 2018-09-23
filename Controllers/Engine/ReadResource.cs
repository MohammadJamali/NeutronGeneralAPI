using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using API.Engine.JSON;
using API.Enums;
using API.Models.Temporary;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Engine {
    public partial class NeutronGeneralAPI<TRelation, TUser> {
        private dynamic ReadResource (
            IRequest request,
            HttpRequestMethod requestMethod,
            PermissionHandler<TRelation, TUser> permissionHandler) {
            var result = APIUtils.GetResource (dbContext, request);

            if (result == null)
                return new NotFoundResult ();

            var serializerSettings = JsonConvert.DefaultSettings ();
            serializerSettings.ContractResolver = new APIJsonResolver<TRelation, TUser> {
                PermissionHandler = permissionHandler,
                ModelAction = ModelAction.Read,
                RequestMethod = requestMethod,
                IRequest = request,
                DbContext = dbContext,
                EngineService = EngineService,
                IncludeBindNever = true,
                IncludeKey = true
            };

            EngineService.OnResourceRead (dbContext, request, result);

            Response.Headers.Add ("Server", "NG-API");

            return new OkObjectResult (JsonConvert.SerializeObject (result, serializerSettings));
        }
    }
}