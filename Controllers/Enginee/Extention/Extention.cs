using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using API.Engine.MagicQuery;
using API.Models;
using API.Models.Architecture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace API.Engine.Extention {
    public static class Extentions {
        public static IEnumerable<dynamic> GetPropertiesWithAttribute (this Type type, Type attributeType) =>
            type.GetProperties ()
            .Where (prop => prop.IsDefined (attributeType, true))
            .ToList ();

        public static IEnumerable<dynamic> GetPropertiesWithAttribute (this Object type, Type attributeType) =>
            type.GetType ().GetPropertiesWithAttribute (attributeType);

        public static dynamic GetPropertyValueWithAttribute (this object model, Type attributeType) {
            if (model == null || attributeType == null) return null;
            var properties = model.GetType ().GetPropertiesWithAttribute (attributeType);

            if (properties == null || properties.Count () == 0)
                return null;

            var property = properties.FirstOrDefault ();
            if (property == null)
                return null;

            var value = property.GetValue (model);
            return value;
        }

        public static string GetKeyPropertyValue (this object model) =>
            (model.GetPropertyValueWithAttribute (typeof (KeyAttribute)) as object).ToString ();
    }
}