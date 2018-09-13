using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.RegularExpressions;
using API.Attributes;
using API.DependencyResolver;
using API.Engine;
using API.Enums;
using API.Interface;
using API.Models.Architecture;
using API.Models.Temporary;
using API.PermissionValidator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Formats;

namespace API.Models {
    public class ImageModel : RootModel {
        [Required]
        [TempData]
        [NotMapped]
        [ModelPermission (HttpRequestMethod.Post, ModelAction.Create, typeof (ImageDataValidator))]
        public string InputTempData { get; set; }

        [Required]
        [BindNever]
        [DependentValue (HttpRequestMethod.Post, ModelAction.Create, typeof (ThumbnailDependencyResolver), DependentOn : nameof (ImageModel.InputTempData))]
        public string Thumbnail { get; set; }

        [BindNever]
        public string DataType { get; set; }
    }
}