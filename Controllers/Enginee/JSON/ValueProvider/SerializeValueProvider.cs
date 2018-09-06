using System;
using System.Reflection;
using API.Enums;
using API.Models.Temporary;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace API.Engine.JSON.ValueProvider {
    public class SerializeValueProvider : IValueProvider {
        protected readonly IValueProvider ValueProvider;
        protected readonly PermissionHandler PermissionHandler;
        protected readonly IRequest IRequest;
        protected readonly PropertyInfo PropertyType;
        protected readonly ModelAction ModelAction;
        protected readonly int IntractionType;
        protected readonly HttpRequestMethod RequestMethod;

        public SerializeValueProvider (
            IValueProvider ValueProvider,
            PermissionHandler PermissionHandler,
            IRequest IRequest,
            int IntractionType,
            PropertyInfo PropertyType,
            ModelAction modelAction,
            HttpRequestMethod requestMethod) {
            this.ValueProvider = ValueProvider;
            this.PermissionHandler = PermissionHandler;
            this.IRequest = IRequest;
            this.PropertyType = PropertyType;
            this.ModelAction = modelAction;
            this.RequestMethod = requestMethod;
            this.IntractionType = IntractionType;
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
                RelationType: this.IntractionType,
                TypeValue: value);

            if (!(permision is bool && (bool) permision)) {
                throw new Exception (permision as string);
            } else {
                ValueProvider.SetValue (target, value);
            }
        }
    }
}