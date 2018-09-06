using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using API.Attributes;
using API.Engine.Extention;
using API.Engine.JSON;
using API.Enums;
using API.Models;
using API.Models.Temporary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Engine {
    public partial class NeutronGeneralAPI<TRelation, TUser> {
        private dynamic CreateResource (
            IRequest request,
            HttpRequestMethod requestMethod,
            PermissionHandler permissionHandler,
            string jsonData) {

            var jsonResolver = new APIJsonResolver {
                DbContext = dbContext,
                PermissionHandler = permissionHandler,
                ModelAction = ModelAction.Create,
                RequestMethod = requestMethod,
                IRequest = request,
                IncludeKey = false,
                IncludeBindNever = false,
                EngineService = EngineService
            };

            var serializerSettings = JsonConvert.DefaultSettings ();
            serializerSettings.ContractResolver = jsonResolver;

            var model =
                JsonConvert.DeserializeObject (
                    value: jsonData,
                    type: request.Temp_ResourceType,
                    settings: serializerSettings);

            dbContext.Add (model);
            dbContext.SaveChanges ();

            var intraction = new ModelIntraction<TRelation> {
                CreatorId = permissionHandler.getRequesterID (),
                FirstModelId = permissionHandler.getRequesterID (),
                SecondModelId = model.GetKeyPropertyValue (),
                ModelAction = ModelAction.Create
            };

            dbContext.MagicAddIntraction (intraction, EngineService.MapRelationToType ("Global"));
            dbContext.SaveChangesAsync ();

            EngineService.OnResourceCreated (request, model, intraction);

            return new OkObjectResult (model);
        }
    }
}