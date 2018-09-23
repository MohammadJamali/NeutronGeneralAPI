using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using API.Attributes;
using API.Enums;
using API.PermissionValidator;
using API.PermissionValidator.PropertyValidator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace API.Models.Architecture {
    public abstract class RootModel {
        [Key]
        [Required]
        [BindNever]
        [Editable (false)]
        [IdentifierValidator (typeof (GuidIdentifierValidator))]
        public Guid Id { get; set; }

        [Required]
        [BindNever]
        [JsonIgnore]
        [Editable (false)]
        public DateTime CreateDateTime { get; set; }

        [BindNever]
        [Editable (false)]
        [JsonConverter (typeof (StringEnumConverter))]
        public ArtifactState ArtifactState { get; set; }

        [Required]
        [BindNever]
        [JsonIgnore]
        [Editable (false)]
        public bool Deactivated { get; set; }

        public RootModel () {
            Id = Guid.NewGuid ();
            CreateDateTime = DateTime.Now;
            ArtifactState = ArtifactState.NotVerified;
            Deactivated = false;
        }
    }
}