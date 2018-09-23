using API.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace API.Models.Architecture {
    public abstract class VisualDescriptiveModel : DescriptiveModel {
        [Include]
        public ImageModel Image { get; set; }
    }
}