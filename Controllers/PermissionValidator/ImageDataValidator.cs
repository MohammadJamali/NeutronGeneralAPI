using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using API.Engine;
using API.Enums;
using API.Interface;
using API.Models.Temporary;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;

namespace API.PermissionValidator {
    public class ImageDataValidator : IAccessChainValidator<Object> {
        public dynamic Validate (
            DbContext dbContext,
            string requesterID,
            IRequest request,
            string typeName,
            object typeValue,
            ModelAction modelAction,
            HttpRequestMethod requestMethod,
            Object relationType) {

            var buffer = Convert.FromBase64String ((string) typeValue);
            //-------------------------------------------
            //  Try to instantiate new Bitmap, if .NET will throw exception
            //  we can assume that it's not a valid image
            //-------------------------------------------
            try {
                var bitmap = Image.Identify (new MemoryStream (buffer));
            } catch (Exception) {
                return "Request Error: Image file is corrupted";
            }

            const int ImageMinimumBytes = 512;
            const int ImageMaximumBytes = 8388608; // 8 * 1024 * 1024;

            using (var image = Image.Load (buffer, out SixLabors.ImageSharp.Formats.IImageFormat format)) {
                //-------------------------------------------
                //  Check the image mime types
                //-------------------------------------------
                var validMimeTypes = new List<string> { "image/jpg", "image/jpeg", "image/x-png", "image/png" };
                if (!validMimeTypes.Contains (format.DefaultMimeType.ToLower ()))
                    return "Request Error: Content Type is not valid, it must be {image/jpg | image/jpeg | image/png}";

                //-------------------------------------------
                //  Check the image extension
                //-------------------------------------------
                var validExtensions = new List<string> { "jpg", "jpeg", "png" };
                if (!validExtensions.Contains (format.Name.ToLower ()))
                    return "Request Error: Wrong file extension, it must be {jpg | jpeg | png}";

                //-------------------------------------------
                //  Attempt to read the file and check the first bytes
                //-------------------------------------------
                // if (image.MetaData.HorizontalResolution < 50 || image.MetaData.VerticalResolution < 50)
                //     return "Request Error: image resolution is less then required";

                if (buffer.Length < ImageMinimumBytes)
                    return "Request Error: image size is less than required limit {" + ImageMinimumBytes + " B}";

                if (buffer.Length > ImageMaximumBytes)
                    return "Request Error: image size is more than required limit {" + (ImageMaximumBytes / (1048576)) + " MB}";

                string content = Encoding.UTF8.GetString (buffer);
                if (Regex.IsMatch (content, @"<script|<html|<head|<title|<body|<pre|<table|<a\s+href|<img|<plaintext|<cross\-domain\-policy",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline))
                    return "Request Error: File contain illegal content";
            }

            return true;
        }
    }
}