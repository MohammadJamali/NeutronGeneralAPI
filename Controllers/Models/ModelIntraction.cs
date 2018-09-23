using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using API.Enums;
using Microsoft.EntityFrameworkCore;

namespace API.Models {
    public class ModelInteraction<TRelation> {
        [Key]
        [Required]
        public string CreatorId { get; set; }

        [Key]
        [Required]
        public string FirstModelId { get; set; }

        [Key]
        [Required]
        public string SecondModelId { get; set; }

        [Key]
        [Required]
        public TRelation IntractionType { get; set; }

        public ModelAction ModelAction { get; set; }

        public string InvoiceId { get; set; }

        [Key]
        [Required]
        public DateTime CreateDateTime { get; set; }

        public DateTime? ValidUntil { get; set; }

        public bool Valid { get; set; }

        public string Information { get; set; }

        public ModelInteraction (ModelInteraction<TRelation> intraction) {
            this.CreatorId = intraction.CreatorId;
            this.FirstModelId = intraction.FirstModelId;
            this.SecondModelId = intraction.SecondModelId;
            this.IntractionType = intraction.IntractionType;
            this.ModelAction = intraction.ModelAction;
            this.InvoiceId = intraction.InvoiceId;
            this.CreateDateTime = intraction.CreateDateTime;
            this.ValidUntil = intraction.ValidUntil;
            this.Valid = intraction.Valid;
            this.Information = intraction.Information;
        }

        public ModelInteraction () {
            CreateDateTime = DateTime.Now;
            Valid = true;
        }
    }
}