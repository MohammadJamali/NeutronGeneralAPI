using System;
using System.Linq;
using System.Reflection;
using API.Attributes;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;

namespace API.Engine.JSON.ValueProvider {
    public class DependentValueProvider : IValueProvider {
        public readonly string RequesterId;
        public readonly IValueProvider ValueProvider;
        private readonly object engineService;
        public readonly DependentValueAttribute DependentAttribute;
        public readonly DbContext DB;

        public DependentValueProvider (
            IValueProvider valueProvider,
            object engineService,
            DependentValueAttribute attribute,
            string requesterId,
            DbContext db) {
            this.ValueProvider = valueProvider;
            this.engineService = engineService;
            this.DependentAttribute = attribute;
            this.DB = db;
            this.RequesterId = requesterId;
        }

        public object GetValue (object target) {
            var resolvedValue = APIUtils.InvokeMethod (DependentAttribute.Resolver,
                "Resolve",
                new object[] {
                    DB,
                    engineService,
                    RequesterId,
                    DependentAttribute.DependentOn == null ?
                    target : target.GetType ().GetProperty (DependentAttribute.DependentOn).GetValue (target),
                    DependentAttribute.DependentOn
                });

            if (resolvedValue == null)
                return this.ValueProvider.GetValue (target);
            else
                return resolvedValue;
        }

        public void SetValue (object target, object value) {
            object currentModel = target;

            if (DependentAttribute.DependentOn != null) {
                var targetPath = DependentAttribute.DependentOn.Trim ().Split ('.');
                foreach (var path in targetPath) {
                    if (currentModel == null) {
                        break;
                    }

                    currentModel = currentModel.GetType ().GetProperty (path).GetValue (currentModel);
                }
            }

            var resolvedValue = APIUtils.InvokeMethod (DependentAttribute.Resolver,
                "Resolve",
                new object[] {
                    DB,
                    engineService,
                    RequesterId,
                    currentModel,
                    DependentAttribute.DependentOn
                });

            this.ValueProvider.SetValue (target, resolvedValue);
        }
    }
}