using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using API.Attributes;
using API.Enums;

namespace API.Models.Architecture {

    [Searchable (SearchType.Like, "* > Title > Text")]
    [Searchable (SearchType.FullText, "* > Description > Text")]
    public abstract class DescriptiveModel : RootModel {
        [Include]
        public ICollection<APILocalizedString> Title { get; set; }

        [Include]
        public ICollection<APILocalizedString> Description { get; set; }
    }
}