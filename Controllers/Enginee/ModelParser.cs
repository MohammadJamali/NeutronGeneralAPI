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
        private ICollection<Searchable> Searchables;

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

            // var searchables = DirectAccessAllowed
            //     .Where (assembly => assembly.IsDefined (typeof (SearchableAttribute), true))
            //     .ToList ();

            // foreach (var assembly in searchables) {
            //     foreach (var searchableAttribute in assembly.GetCustomAttributes<SearchableAttribute> ()) {
            //         var accessPath = searchableAttribute.AccessPath.Trim ().Split (" > ");

            //         var searchable = new Searchable ();
            //         searchable.ResourceType = assembly.GetType ();
            //         searchable.PropertyName = accessPath[accessPath.Length - 1];
            //         searchable.AccessExpression = CreateAccessPath (assembly, accessPath);
            //     }
            // }
        }

        public Expression GetPropertySearchAttribute (string resource, string property) =>
            Searchables
            .Where (searchable =>
                searchable.ResourceType.Name.Equals (resource) &&
                searchable.PropertyName.Equals (property))
            .Select (searchable => searchable.AccessExpression)
            .FirstOrDefault ();

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