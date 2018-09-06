using System;
using System.Collections.Generic;
using System.Linq;
using API.Engine;
using API.Engine.Extention;
using API.Interface;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.DependencyResolver {
    /// <summary>
    /// This dependency resolver will try to resolve if any relation of type <value>GetRelationName()</value>
    /// which created by <value>requesterId</value> and it's at least one way connected to <value>requesterId</value>
    /// and <value>currentModel</value> exist or not
    /// </summary>
    public abstract class UserHasRelationDependencyResolver : IDependencyResolver {
        public abstract string GetRelationName ();

        public dynamic Resolve (
            DbContext dbContext,
            object engineService,
            string requesterId,
            object currentModel,
            string DependentOn) {
            var relationType = engineService
                .GetType ()
                .GetMethod (name: "MapRelationToType")
                .Invoke (obj: engineService, parameters: new object[] { GetRelationName () });

            var relationEnumType = engineService
                .GetType ()
                .GetInterfaces ()
                .Where (_interface => _interface.Name.Contains (value: "IApiEngineService"))
                .FirstOrDefault () // retrieve interface
                .GetGenericArguments ()
                .FirstOrDefault (); // retrieve enum type

            var relation = Enum.Parse (enumType: relationEnumType, value: GetRelationName ());

            var dbSet = dbContext.MagicDbSet (type: relationType as Type);

            return (dbSet as IEnumerable<dynamic>)
                .Where (predicate: intraction =>
                    intraction.IntractionType.Equals(relation) &&
                    intraction.Valid &&
                    ((intraction.ValidUntil == null || intraction.ValidUntil.HasValue == false) ||
                        (intraction.ValidUntil.HasValue && System.DateTime.Now.CompareTo (intraction.ValidUntil.Value) < 0)) &&
                    (intraction.CreatorId.Equals(requesterId) &&
                        intraction.FirstModelId.Equals(requesterId) &&
                        intraction.SecondModelId.Equals(currentModel.GetKeyPropertyValue ())))
                .Any ();
        }
    }
}