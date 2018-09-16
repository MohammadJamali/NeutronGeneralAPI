using System;
using System.Reflection;
using API.Enums;
using API.Models.Temporary;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace API.Engine.JSON.ValueProvider {
    public class SerializeValueProvider<TRelation, TUser> : IValueProvider where TUser : IdentityUser {
        protected readonly IValueProvider ValueProvider;
        protected readonly PermissionHandler<TRelation, TUser> PermissionHandler;
        protected readonly IRequest IRequest;
        protected readonly PropertyInfo PropertyType;
        protected readonly ModelAction ModelAction;
        protected readonly TRelation Relation;
        protected readonly HttpRequestMethod RequestMethod;

        public SerializeValueProvider (
            IValueProvider ValueProvider,
            PermissionHandler<TRelation, TUser> PermissionHandler,
            IRequest IRequest,
            TRelation Relation,
            PropertyInfo PropertyType,
            ModelAction modelAction,
            HttpRequestMethod requestMethod) {
            this.ValueProvider = ValueProvider;
            this.PermissionHandler = PermissionHandler;
            this.IRequest = IRequest;
            this.PropertyType = PropertyType;
            this.ModelAction = modelAction;
            this.RequestMethod = requestMethod;
            this.Relation = Relation;
        }

        public object GetValue (object target) {
            var permision = PermissionHandler.ModelPropertyValidation (
                Request: this.IRequest,
                PropertyInfo: this.PropertyType,
                Model: target,
                ModelAction: this.ModelAction,
                RequestMethod: this.RequestMethod);

            if (!(permision is bool && (bool) permision)) {
                return null;
            } else {
                return ValueProvider.GetValue (target);
            }
        }

        public void SetValue (object target, object value) {
            var permision = PermissionHandler.GeneralAccessChainValidation (
                Request: this.IRequest,
                Type: this.PropertyType,
                ModelAction: this.ModelAction,
                RequestMethod: this.RequestMethod,
                RelationType: this.Relation,
                ModelItself: target,
                TypeValue: value);

            if (!(permision is bool && (bool) permision)) {
                throw new Exception (permision as string);
            } else {
                ValueProvider.SetValue (target, value);
            }
        }
    }
}