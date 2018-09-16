using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Attributes;
using API.Engine.Extention;
using API.Enums;
using API.Interface;
using API.Models.Temporary;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Engine {
    public class PermissionHandler<TRelation, TUser> where TUser : IdentityUser {
        private readonly string RequesterID;
        private readonly DbContext DbContext;
        private readonly IModelParser modelParser;
        private readonly IApiEngineService<TRelation, TUser> EngineService;

        public PermissionHandler (
            string RequesterID,
            IModelParser modelParser,
            IApiEngineService<TRelation, TUser> EngineService,
            DbContext DbContext) {
            this.RequesterID = RequesterID;
            this.DbContext = DbContext;
            this.modelParser = modelParser;
            this.EngineService = EngineService;
        }

        public string getRequesterID () => RequesterID;

        public void CheckPermissionRequirements (
            IRequest Request,
            ModelAction ModelAction,
            HttpRequestMethod RequestMethod,
            object model) {
            if (model == null) return;

            var protectedAttributes = model.GetPropertiesWithAttribute (typeof (ModelPermissionAttribute));

            foreach (var attribute in protectedAttributes) {
                var attributeValue = attribute.GetValue (model);
                if (attributeValue == null) continue;

                var permision = ModelPropertyValidation (
                    Request: Request,
                    PropertyInfo: attribute,
                    Model: model,
                    ModelAction: ModelAction,
                    RequestMethod: RequestMethod);

                if (!(permision is bool && (bool) permision)) {
                    attribute.SetValue (model, null);
                }
            }

            var innerProperties = model
                .GetType ()
                .GetProperties ()
                .Where (property =>
                    property.PropertyType.FullName.Contains (
                        AppDomain.CurrentDomain.FriendlyName
                    ) ||
                    property.PropertyType.FullName.StartsWith ("API") ||
                    (property.PropertyType.Name.StartsWith ("ICollection") &&
                        property.PropertyType
                        .GetGenericArguments ()
                        .FirstOrDefault ()
                        .FullName
                        .Contains (
                            AppDomain.CurrentDomain.FriendlyName
                        )
                    ) &&
                    !property.PropertyType.IsEnum &&
                    property.PropertyType.IsClass
                ).ToList ();

            foreach (var property in innerProperties) {
                if (property == null) continue;

                var attributeValue = property.GetValue (model);
                if (attributeValue == null) continue;

                if (property.PropertyType.Name.StartsWith ("ICollection")) {
                    var collection = attributeValue as IEnumerable;
                    foreach (var item in collection)
                        CheckPermissionRequirements (Request, ModelAction, RequestMethod, item);
                } else {
                    CheckPermissionRequirements (Request, ModelAction, RequestMethod, attributeValue);
                }
            }
        }

        public dynamic ModelPropertyValidation (
                IRequest Request,
                PropertyInfo PropertyInfo,
                object Model,
                ModelAction ModelAction,
                HttpRequestMethod RequestMethod) =>
            GeneralAccessChainValidation (
                Request: Request,
                Type: PropertyInfo,
                ModelAction: ModelAction,
                RequestMethod: RequestMethod,
                RelationType: default (TRelation),
                TypeValue: PropertyInfo.GetValue (Model),
                ModelItself: Model,
                DefaultPolicy: true);

        public dynamic ModelValidation (
                IRequest Request,
                Type ModelType,
                ModelAction ModelAction,
                HttpRequestMethod RequestMethod,
                TRelation RelationType) =>
            GeneralAccessChainValidation (
                Request: Request,
                Type: ModelType,
                ModelAction: ModelAction,
                RequestMethod: RequestMethod,
                RelationType: RelationType,
                ModelItself: null,
                TypeValue: Request.IdentifierValue,
                DefaultPolicy: false);

        public dynamic GeneralAccessChainValidation (
            IRequest Request,
            MemberInfo Type,
            ModelAction ModelAction,
            HttpRequestMethod RequestMethod,
            TRelation RelationType,
            object ModelItself,
            object TypeValue = null,
            bool DefaultPolicy = false) {
            var typeName = Type.GetType ().GetProperty ("Name").GetValue (Type) as string;
            var modelPermissions = Type.GetCustomAttributes<ModelPermissionAttribute> ();

            var requirements = modelPermissions
                .AsParallel ()
                .Where (requirement =>
                    requirement.ModelAction == ModelAction &&
                    requirement.RequestMethod == RequestMethod)
                .ToList ();

            if (requirements != null) {
                foreach (var requirement in requirements) {
                    var validation = APIUtils.InvokeMethod (
                        requirement.AccessChainResolver,
                        "Validate",
                        new object[] {
                            DbContext,
                            RequesterID,
                            Request,
                            ModelItself,
                            typeName,
                            TypeValue,
                            ModelAction,
                            RequestMethod,
                            RelationType
                        });

                    if (!(validation is bool && (bool) validation)) {
                        return "Requirement validation with name { " + requirement.AccessChainResolver.Name +
                            " } has been faild with result { " + validation + " }";
                    }
                }
            } else if (DefaultPolicy == false)
                return "Requested action { " + ModelAction + " } is not valid for request method { " +
                    RequestMethod + " }, or this action is not valid for this type { " +
                    typeName + " } at all";

            return true;
        }

        public dynamic ValidateRequest (
            HttpRequestMethod requestMethod,
            IRequest request,
            ModelAction requestedAction,
            TRelation relationType) {

            if (request == null ||
                request.ResourceName == null || string.IsNullOrWhiteSpace (request.ResourceName))
                return "Request Error: Route parameters should not be empty";

            // Check ResourceName
            //* Check whether resource is exist or not? is direct access allowed or not?
            var resourceType = modelParser.GetResourceType (request.ResourceName);

            if (resourceType == null)
                return "Requested resource {" + request.ResourceName +
                    "} is not exist or direct access is not permitted";

            request.Temp_ResourceType = resourceType;

            if (requestedAction != ModelAction.Create) {
                if (request.IdentifierName == null || string.IsNullOrWhiteSpace (request.IdentifierName) ||
                    request.IdentifierValue == null || string.IsNullOrWhiteSpace (request.IdentifierValue))
                    return "Request Error: Route parameters should not be empty";

                //* Check whether this identifire is exist or not for model
                var identifireValidator =
                    (resourceType.GetCustomAttributes (typeof (IdentifireValidatorAttribute), true) as IdentifireValidatorAttribute[])
                    .Union (
                        resourceType.GetProperties ()
                        .Where (property => property.IsDefined (typeof (IdentifireValidatorAttribute)))
                        .Select (validator => validator.GetCustomAttribute (typeof (IdentifireValidatorAttribute), true) as IdentifireValidatorAttribute)
                    )
                    .Where (validator => validator.PropertyName == request.IdentifierName)
                    .FirstOrDefault ();

                if (identifireValidator == null || identifireValidator.Validator == null)
                    return "Requested model identifire does not exist or it's not permitted to use it as an identifire";

                var identifierValidation =
                    APIUtils.InvokeMethod (
                        identifireValidator.Validator,
                        "Validate",
                        new object[] {
                            request.IdentifierValue
                        });

                if (!(identifierValidation is bool && (bool) identifierValidation))
                    return "Request Error: The value {" + request.IdentifierValue +
                        "} is not a valid value for identifier {" + request.IdentifierName + "}";
            }

            return ModelValidation (
                Request: request,
                ModelType: resourceType,
                ModelAction: requestedAction,
                RequestMethod: requestMethod,
                RelationType: relationType);
        }
    }
}