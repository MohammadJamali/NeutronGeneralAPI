using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using API.Engine.Extention;
using API.Engine.JSON;
using API.Enums;
using API.Models;
using API.Models.Temporary;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace API.Engine {
    public partial class NeutronGeneralAPI<TRelation, TUser> {
        private dynamic PatchResource (
            IRequest request,
            HttpRequestMethod requestMethod,
            PermissionHandler<TRelation, TUser> permissionHandler,
            string jsonData) {
            var jsonResolver = new APIJsonResolver<TRelation, TUser> {
                    DbContext = dbContext,
                    PermissionHandler = permissionHandler,
                    ModelAction = ModelAction.Create,
                    RequestMethod = requestMethod,
                    IRequest = request,
                    EngineService = EngineService,
                    IncludeKey = true,
                    IncludeBindNever = false
                };

            var serializerSettings = JsonConvert.DefaultSettings ();
            serializerSettings.ContractResolver = jsonResolver;

            var model = JsonConvert.DeserializeObject (
                jsonData,
                request.Temp_ResourceType,
                serializerSettings);

            var modelKey = model.GetKeyPropertyValue ();

            if (modelKey == null ||
                modelKey != request.IdentifierValue ||
                !verifyModelRelationChain (model))
                return BadRequest (new APIError {
                    Message =
                        "Error: Invalid relation in received model, it can be happend when you are not " +
                        "permited for this action or there are some invalid id(s)."
                });

            // dbContext.Update (model);
            // ExcludeAttributes (model);

            IncludeAttributes (model);

            var intraction = new ModelIntraction<TRelation> {
                CreatorId = permissionHandler.getRequesterID (),
                FirstModelId = permissionHandler.getRequesterID (),
                SecondModelId = model.GetKeyPropertyValue (),
                ModelAction = ModelAction.Update
            };

            dbContext.MagicAddIntraction (intraction, EngineService.MapRelationToType ("Global"));
            dbContext.SaveChanges ();

            EngineService.OnResourcePatched (request, model);

            return new OkResult ();
        }

        private void ExcludeAttributes (object model) {
            if (model == null) return;

            var entry = dbContext.Entry (model);
            var entryProperties = entry.Properties.Select (property => property.Metadata.Name).AsParallel ();
            var entryCollections = entry.Collections.Select (property => property.Metadata.Name).AsParallel ();

            var properties = model
                .GetType ()
                .GetProperties ()
                .Select (property => new {
                    Name = property.Name,
                        FullName = property.PropertyType.FullName,
                        IsNotMapped = property.IsDefined (typeof (NotMappedAttribute), true),
                        IsBindNever = property.IsDefined (typeof (BindNeverAttribute), true),
                        Value = property.GetValue (model),
                        Type = entryProperties.Contains (property.Name) ? PropertyType.Property :
                        entryCollections.Contains (property.Name) ? PropertyType.Collection :
                        PropertyType.Reference
                })
                .Where (property => property.IsNotMapped == false)
                .ToList ();

            foreach (var property in properties) {
                if (property.Value == null || property.IsBindNever) {
                    switch (property.Type) {
                        case PropertyType.Property:
                            entry.Property (property.Name).IsModified = false;
                            break;

                        case PropertyType.Collection:
                            entry.Collection (property.Name).IsModified = false;
                            break;

                        case PropertyType.Reference:
                            entry.Reference (property.Name).IsModified = false;
                            break;
                    }
                } else if (
                    property.Type == PropertyType.Collection &&
                    (property.Value as ICollection).Count > 0) {
                    foreach (var item in property.Value as ICollection) {
                        ExcludeAttributes (item);
                    }
                } else if (property.FullName.Contains (
                        AppDomain.CurrentDomain.FriendlyName)) {
                    ExcludeAttributes (property.Value);
                }
            }
        }

        private void IncludeAttributes (object model) {
            if (model == null) return;

            var entry = dbContext.Entry (model);
            var entryProperties = entry.Properties.Select (property => property.Metadata.Name).AsParallel ();
            var entryCollections = entry.Collections.Select (property => property.Metadata.Name).AsParallel ();

            var properties = model
                .GetType ()
                .GetProperties ()
                .Select (property => new {
                    Property = property,
                        IsReadOnly = property.IsDefined (typeof (NotMappedAttribute), true) ||
                        property.IsDefined (typeof (BindNeverAttribute), true),
                        Value = property.GetValue (model),
                        Type = entryProperties.Contains (property.Name) ? PropertyType.Property :
                        entryCollections.Contains (property.Name) ? PropertyType.Collection :
                        PropertyType.Reference
                })
                .Where (property => !property.IsReadOnly && property.Value != null)
                .ToList ();

            foreach (var property in properties) {
                if (property.Type == PropertyType.Collection &&
                    (property.Value as ICollection).Count > 0) {
                    foreach (var item in property.Value as ICollection) {
                        IncludeAttributes (item);
                    }
                } else if (property.Property.PropertyType.FullName.Contains (AppDomain.CurrentDomain.FriendlyName) &&
                    !property.Property.PropertyType.IsEnum) {
                    IncludeAttributes (property.Value);
                } else {
                    switch (property.Type) {
                        case PropertyType.Property:
                            entry.Property (property.Property.Name).IsModified = true;
                            break;

                        case PropertyType.Collection:
                            entry.Collection (property.Property.Name).IsModified = true;
                            break;

                        case PropertyType.Reference:
                            entry.Reference (property.Property.Name).IsModified = true;
                            break;

                    }
                }
            }
        }

        /// <summary>
        /// This function will try to get all app (not system) classes in input param object
        /// and verify whether these objects are exist and blong to input param object or not
        ///
        /// As an example:
        /// class A { B; C; string; int;}
        /// class C { bool; int; string; D;}
        ///
        /// if A is input param, at first this function will list all app classes in A which is
        /// {B, C} and verify if B exist and there is a valid connection bitween A and B, and will
        /// do the same for C, and recurcivly will check C and D, this will help to find out if
        /// somebody wants to change model Ids on update action and change database structure,
        /// so we can prevent it.
        ///
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private bool verifyModelRelationChain (object model) {
            var listOfAppClasses = model
                .GetType ()
                .GetProperties ()
                .Where (property =>
                    property.PropertyType.FullName.Contains (
                        AppDomain.CurrentDomain.FriendlyName
                    ))
                .ToList ();

            var modelType = model.GetType ();

            var idProperty = modelType
                .GetProperties ()
                .Where (prop => prop.IsDefined (typeof (KeyAttribute), true))
                .FirstOrDefault ();

            // If we have no property with KeyAttribute, it means we deal with some
            // not important objects like custom enums and ... So, no key, no problem
            if (idProperty == null) return true;

            var idValue = idProperty.GetValue (model);
            if (idValue == null) return false;

            foreach (var claim in listOfAppClasses) {
                var claimValue = claim.GetValue (model);
                if (claimValue == null ||
                    claim.PropertyType.IsEnum) continue;

                if (claim.PropertyType.Name.StartsWith ("ICollection")) {
                    foreach (var item in claimValue as ICollection)
                        if (!verifyConnection (item.GetType (), claim.Name, item, modelType, idProperty, idValue))
                            return false;
                } else {
                    if (!verifyConnection (claim.PropertyType, claim.Name, claimValue, modelType, idProperty, idValue))
                        return false;
                }

                var innerValidation = verifyModelRelationChain (claimValue);
                if (!innerValidation) return false;
            }

            return true;
        }

        /// <summary>
        /// Verify if claimed Type with claimed name and value exist and there is a valid connection between it
        /// and modelType with claimed id property and value
        /// </summary>
        private bool verifyConnection (
            Type claimType,
            string claimName,
            object claimValue,
            Type modelType,
            PropertyInfo idProperty,
            object idValue) {
            var claimId = claimType
                .GetProperties ()
                .Where (prop =>
                    prop.IsDefined (typeof (KeyAttribute), true))
                .FirstOrDefault ();

            var claimIdValue = claimId.GetValue (claimValue);
            if (claimIdValue == null) return false;

            return Utils.VerifyConnection (
                APIUtils.GetIQueryable (dbContext, modelType.Name),
                modelType,
                claimType,
                idProperty,
                idValue.ToString (),
                claimName,
                claimId,
                claimIdValue.ToString ());
        }
    }
}