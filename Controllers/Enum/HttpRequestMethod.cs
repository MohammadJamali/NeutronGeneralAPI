using System.Runtime.Serialization;

namespace API.Enums {
    public enum HttpRequestMethod {
        [EnumMember (Value = "Post")]
        Post,

        [EnumMember (Value = "Get")]
        Get,

        [EnumMember (Value = "Delete")]
        Delete,

        [EnumMember (Value = "Patch")]
        Patch
    }
}