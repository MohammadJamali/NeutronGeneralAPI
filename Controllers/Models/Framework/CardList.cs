using System.Collections.Generic;
using API.Enums;

namespace API.Models.Framework {
    public class CardList {
        public int PageNumber { get; set; }
        public string Cursor { get; set; }
        public CardListType ListType { get; set; }
        public string Title { get; set; }
        public bool HasMore { get; set; }
        public bool UUIDAsSubtext { get; set; }
        public List<Card> Cards { get; set; }
    }
}