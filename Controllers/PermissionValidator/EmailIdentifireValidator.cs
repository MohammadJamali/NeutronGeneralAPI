using System;
using System.Net.Mail;
using API.Interface;

namespace API.PermissionValidator {
    public class EmailIdentifireValidator : IPropertyValidator {
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