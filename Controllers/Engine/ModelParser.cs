using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using API.Attributes;
using API.Interface;
using API.Models.Temporary;

namespace API.Engine {
    public sealed class ModelParser : IModelParser {
        private ICollection<Type> DirectAccessAllowed { get; set; }
        private ICollection<Type> RangeReaderAllowed { get; set; }

        public ModelParser () {
            var appAssemblies = AppDomain.CurrentDomain.GetAssemblies ().AsParallel ();

            var appName = AppDomain.CurrentDomain.FriendlyName;
            var appAssembly = appAssemblies
                .Where (assembly => assembly.GetName ().Name.Equals (appName))
                .FirstOrDefault ();

            DirectAccessAllowed = appAssembly
                .GetExportedTypes ()
                .Where (model => model.IsDefined (typeof (DirectAccessAllowedAttribute), false))
                .ToList ();

            RangeReaderAllowed = DirectAccessAllowed
                .Where (assembly =>
                    assembly.IsDefined (typeof (RangeReaderAllowedAttribute), false))
                .ToList ();
        }

        public Type GetResourceType (string resource) =>
            DirectAccessAllowed
            .Where (type => type.Name.Equals (resource))
            .FirstOrDefault ();

        public Type IsRangeReaderAllowed (string resource) =>
            RangeReaderAllowed
            .Where (type => type.Name.Equals (resource))
            .FirstOrDefault ();
    }
}