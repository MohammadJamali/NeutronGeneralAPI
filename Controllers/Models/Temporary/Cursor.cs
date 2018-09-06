using System;

namespace API.Models.Temporary {
    internal class Cursor {
        public string RequesterID { get; set; }
        public string IssuedFor { get; set; }
        public int PageNumber { get; set; }
        public int ObjPerPage { get; set; }
        public DateTime IssuedAt { get; set; }
        public int ExpireAfter { get; set; }

        public Cursor (string RequesterID,
            string IssuedFor,
            int ExpireAfter = 30,
            int PageNumber = 1,
            int ObjPerPage = 2) {
            this.RequesterID = RequesterID;
            this.IssuedFor = IssuedFor;
            this.PageNumber = PageNumber;
            this.ObjPerPage = ObjPerPage;
            this.IssuedAt = DateTime.Now;
            this.ExpireAfter = ExpireAfter;
        }

        public bool isExpired () {
            return IssuedAt == null ||
                DateTime.Now.Subtract (IssuedAt).TotalMinutes > ExpireAfter;
        }
    }
}