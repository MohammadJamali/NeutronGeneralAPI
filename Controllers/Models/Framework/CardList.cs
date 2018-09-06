using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace API.Models.Framework {
    public class CardList {
        public CardListType ListType { get; set; }
        public string Title { get; set; }
        public bool HasMore { get; set; }
        public bool UUIDAsSubtext { get; set; }
        public ICollection<Card> Cards { get; set; }
    }

    public enum CardListType {
        [Display (Name = "Wide")]
        Wide, [Display (Name = "Square")]
        Square
    }
}