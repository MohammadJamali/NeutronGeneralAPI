using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace API.Enums {
    [Flags]
    [JsonConverter (typeof (StringEnumConverter))]
    public enum SupportedCulture {
        [EnumMember (Value = "English")]
        [Display (Name = "English")]
        EN = 1, [EnumMember (Value = "German")]
        [Display (Name = "German")]
        DE = 2, [EnumMember (Value = "Persian")]
        [Display (Name = "Persian")]
        FA = 3, [EnumMember (Value = "Arabic")]
        [Display (Name = "Arabic")]
        AR = 4
    }
}