using System.Collections.Generic;
using API.Enums;
using API.Models.Architecture;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace API.Models.Framework {
    public class Card : InteractiveVisualDescriptiveModel {

        [TempData]
        public string ObjectType { get; set; }

        [TempData]
        public ICollection<Card> Cards { get; set; }

        [TempData]
        public Dictionary<string, string> Info { get; set; }
    }
}