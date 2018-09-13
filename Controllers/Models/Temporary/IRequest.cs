using System;
using System.Reflection;
using Newtonsoft.Json;

namespace API.Models.Temporary {
    public class IRequest {
        public string ResourceName;
        public string IdentifierName;
        public string IdentifierValue;
        public long ExtraCode { get; set; }

        [JsonIgnore]
        public Type Temp_ResourceType { get; set; }

        public bool filledWithData () =>
            !string.IsNullOrWhiteSpace (this.ResourceName) &&
            !string.IsNullOrWhiteSpace (this.IdentifierName) &&
            !string.IsNullOrWhiteSpace (this.IdentifierValue);
    }
}