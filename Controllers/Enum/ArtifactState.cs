using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace API.Enums {
    [Flags]
    [JsonConverter (typeof (StringEnumConverter))]
    public enum ArtifactState {
        [EnumMember (Value = "NotVerified")]
        [Display (Name = "NotVerified")]
        NotVerified = 0,

        [EnumMember (Value = "Verified")]
        [Display (Name = "Verified")]
        Verified = 1,

        [EnumMember (Value = "Blocked")]
        [Display (Name = "Blocked")]
        Blocked = 2,

        [EnumMember (Value = "Deleted")]
        [Display (Name = "Deleted")]
        Deleted = 2
    }
}