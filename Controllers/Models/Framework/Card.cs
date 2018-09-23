using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using API.Enums;
using API.Models.Architecture;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace API.Models.Framework {
    public class Card : InteractiveVisualDescriptiveModel {

        [TempData]
        [BindNever]
        [NotMapped]
        public string ObjectType { get; set; }

        [TempData]
        [BindNever]
        [NotMapped]
        public ICollection<Card> Cards { get; set; }

        [TempData]
        [BindNever]
        [NotMapped]
        public Dictionary<string, string> Info { get; set; }
    }
}