using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace API.Enums {
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ModelAction {
        [Display (Name = "Create")]
        [EnumMember (Value = "Create")]
        Create = 0,

        [Display (Name = "Read")]
        [EnumMember (Value = "Read")]
        Read = 1,

        [Display (Name = "Update")]
        [EnumMember (Value = "Update")]
        Update = 2,

        [Display (Name = "Delete")]
        [EnumMember (Value = "Delete")]
        Delete = 3,

        [Display (Name = "Relate")]
        [EnumMember (Value = "Relate")]
        Relate = 4
    }
}