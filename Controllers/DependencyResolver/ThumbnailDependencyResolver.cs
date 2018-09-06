using System;
using System.IO;
using API.Interface;
using API.Models;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Transforms;
using SixLabors.Primitives;

namespace API.DependencyResolver {
    /// <summary>
    /// With this dependency resolver you can create thumbnail from DependentOn property value, it can be
    /// either a byte[] or Base64String
    /// </summary>
    public class ThumbnailDependencyResolver : IDependencyResolver {

        public dynamic Resolve (
            DbContext dbContext,
            object engineService,
            string requesterId,
            object currentModel,
            string DependentOn) {
            if (currentModel == null) return null;

            byte[] imageBytes =
                currentModel is string ?
                Convert.FromBase64String (currentModel as string) :
                currentModel is byte[] ?
                currentModel as byte[] :
                null;

            if (imageBytes == null) return null;

            using (var image = Image.Load (data: imageBytes)) {
                image.Mutate (_image =>
                    _image.Resize (
                        options: new ResizeOptions {
                            Mode = ResizeMode.Max,
                                Size = new Size (value: 512)
                        }));

                byte[] thumbnail = null;
                using (var stream = new MemoryStream ()) {
                    image.SaveAsJpeg (stream: stream);
                    thumbnail = new byte[stream.Capacity];
                    stream.GetBuffer ().CopyTo (array: thumbnail, index: 0);
                }

                return Convert.ToBase64String (inArray: thumbnail, options: Base64FormattingOptions.None);
            }
        }
    }
}