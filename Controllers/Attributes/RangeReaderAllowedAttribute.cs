using System;

namespace API.Attributes {
    /// <summary>
    /// Any object with RangeReaderAllowed attribute and <seealso cref="DirectAccessAllowedAttribute"/> can be
    /// read as an array, for example client can request for a list of it or you can use this type in
    /// explore maker.
    ///
    /// <param name="MaxObjToRead">We don't want to pass entire database to client so with this property
    /// you can define maximum number of allowed object which user can request for</param>
    /// </summary>
    public sealed class RangeReaderAllowedAttribute : Attribute {
        public int MaxObjToRead { get; set; }

        public RangeReaderAllowedAttribute (int MaxObjToRead = 50) {
            this.MaxObjToRead = MaxObjToRead;
        }
    }
}