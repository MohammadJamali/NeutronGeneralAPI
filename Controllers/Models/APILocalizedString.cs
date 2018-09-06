using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using API.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace API.Models {
    public class APILocalizedString {
        [Key]
        [BindNever]
        public int Id { get; set; }

        [BindNever]
        [NotMapped]
        public Message? Code { get; set; } = null;

        [Required]
        public SupportedCulture SupportedCulture { get; set; }

        [Required]
        public string Text { get; set; }

        public static APILocalizedString Get (Message code,
            IStringLocalizer stringLocalizer, SupportedCulture culture) {
            return new APILocalizedString () {
                Code = code,
                    Text = stringLocalizer[code.ToString ()],
                    SupportedCulture = culture
            };
        }
    }

    public enum Message {
        ResourceNotValid,
        PermissionIdentificationFailed,
        ValueNotValid,
        InvalidRequestMethod,
        InvalidModelAction,
        PrivateResourceAccess,
        IncorrectPermissionConfig,
        ApplicantNotValid,
        RequestedObjectNotValid,
        ObjectOwnerPermissionFailed,
        AccessChainRequirementFailed,
        UserLicenseCheckFailed,
        Ok,
        UserNotFound,
        ContentTypeIsNotValid,
        WrongFileExtension,
        NotReadableStream,
        FileIsVerySmall,
        FileContainIllegalData,
        ThisIsNotAnImageFile,
        ImageFileIsCorrupted
    }
}