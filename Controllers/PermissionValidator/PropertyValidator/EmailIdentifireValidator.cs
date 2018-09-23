using System;
using System.Net.Mail;
using API.Interface;

namespace API.PermissionValidator.PropertyValidator {
    public class EmailIdentifierValidator : IPropertyValidator {
        public dynamic Validate (string value) {
            try {
                new MailAddress (value);
                return true;
            } catch (FormatException) {
                return false;
            }
        }
    }
}