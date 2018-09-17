using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace API.Enums {
    [JsonConverter (typeof (StringEnumConverter))]
    public enum CardListType {
        [Display (Name = "Wide")]
        [EnumMember (Value = "Wide")]
        Wide,

        [Display (Name = "Square")]
        [EnumMember (Value = "Square")]
        Square,

        [Display (Name = "Header")]
        [EnumMember (Value = "Header")]
        Header
    }
}