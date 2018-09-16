using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using API.Attributes;
using API.Engine.JSON.ValueProvider;
using API.Enums;
using API.Models.Temporary;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace API.Engine.JSON {
    public class APIJsonResolver<TRelation, TUser> : DefaultContractResolver where TUser : IdentityUser {
        public PermissionHandler<TRelation, TUser> PermissionHandler { get; set; }
        public object EngineService { get; set; }
        public ModelAction ModelAction { get; set; }
        public HttpRequestMethod RequestMethod { get; set; }
        public IRequest IRequest { get; set; }
        public TRelation Relation { get; set; }
        public DbContext DbContext { get; set; }
        public bool IncludeKey { get; set; }
        public bool IncludeBindNever { get; set; }

        protected override IList<JsonProperty> CreateProperties (
            Type type,
            MemberSerialization memberSerialization) {
            var propertyList = base.CreateProperties (type, memberSerialization);

            foreach (var jProperty in propertyList) {
                var PropertyType = jProperty.DeclaringType.GetProperty (name: jProperty.PropertyName);

                var isBindNever = PropertyType.IsDefined (typeof (BindNeverAttribute), true);
                var isKey = PropertyType.IsDefined (typeof (KeyAttribute));
                var dependent = PropertyType.GetCustomAttribute<DependentValueAttribute> ();
                var isDependent =
                    dependent != null &&
                    dependent.ModelAction == this.ModelAction &&
                    dependent.RequestMethod == this.RequestMethod;

                if (isKey && IncludeKey == false) {
                    jProperty.Ignored = true;
                    jProperty.ShouldDeserialize = i => false;
                } else if (!isKey && isBindNever && IncludeBindNever == false && !isDependent) {
                    jProperty.Ignored = true;
                    jProperty.ShouldDeserialize = i => false;
                } else if (isDependent) {
                    jProperty.DefaultValue = null;
                    jProperty.DefaultValueHandling = DefaultValueHandling.Populate;
                    jProperty.NullValueHandling = NullValueHandling.Include;
                    jProperty.Ignored = false;
                    jProperty.ShouldDeserialize = i => true;
                    jProperty.ValueProvider = new DependentValueProvider (
                        valueProvider: jProperty.ValueProvider,
                        engineService: EngineService,
                        attribute: PropertyType.GetCustomAttribute<DependentValueAttribute> (),
                        requesterId: PermissionHandler.getRequesterID (),
                        db: DbContext
                    );
                } else if (PropertyType.IsDefined (typeof (ModelPermissionAttribute))) {
                    jProperty.ValueProvider = new SerializeValueProvider<TRelation, TUser> (
                        ValueProvider: jProperty.ValueProvider,
                        PermissionHandler: this.PermissionHandler,
                        IRequest: this.IRequest,
                        Relation: this.Relation,
                        PropertyType: PropertyType,
                        modelAction: this.ModelAction,
                        requestMethod: this.RequestMethod);
                }
            }

            return propertyList;
        }
    }
}