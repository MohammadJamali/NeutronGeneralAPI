using System.Text.RegularExpressions;
using API.Interface;

namespace API.PermissionValidator.PropertyValidator {
    public class GuidIdentifierValidator : IPropertyValidator {
        public dynamic Validate (string value) =>
            value == null ||
            string.IsNullOrWhiteSpace (value) ||
            Regex.IsMatch (
                Regex.Escape (value),
                "^([0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12})$");
    }
}