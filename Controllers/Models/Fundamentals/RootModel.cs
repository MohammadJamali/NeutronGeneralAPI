using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using API.Attributes;
using API.Enums;
using API.PermissionValidator;
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
        [IdentifireValidator (typeof (GuidIdentifireValidator))]
        public Guid Id { get; set; }

        [Required]
        [BindNever]
        [Editable (false)]
        [JsonIgnore]
        public DateTime CreateDateTime { get; set; }

        [JsonConverter (typeof (StringEnumConverter))]
        [Editable (false)]
        [BindNever]
        public ArtifactState ArtifactState { get; set; }

        public RootModel () {
            Id = Guid.NewGuid ();
            CreateDateTime = DateTime.Now;
            ArtifactState = ArtifactState.NotVerified;
        }
    }
}