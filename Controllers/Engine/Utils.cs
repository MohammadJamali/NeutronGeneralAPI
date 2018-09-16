using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using API.Attributes;
using API.Engine;
using API.Engine.MagicQuery;
using API.Models;
using API.Models.Temporary;
using Microsoft.EntityFrameworkCore;

namespace API.Engine {
    public class APIUtils {
        internal static dynamic InvokeMethod (Object obj, MethodInfo method, object[] parameters) {
            try {
                if (method == null) return false;
                if (obj == null) return false;

                return method.Invoke (obj, parameters);
            } catch (Exception) {
                return false;
            }
        }

        internal static dynamic InvokeMethod (Type type, string method, object[] parameters) {
            try {
                var validatorMethod = type.GetMethod (method);
                if (validatorMethod == null) return false;

                var validator = Activator.CreateInstance (type);
                if (validator == null) return false;

                return validatorMethod.Invoke (validator, parameters);
            } catch (Exception) {
                return false;
            }
        }

        public static IQueryable<dynamic> GetIQueryable (
                DbContext db,
                string resourceName,
                bool needPostFix = true) =>
            db.GetType ()
            .GetProperty (resourceName + (needPostFix ? "Table" : ""))
            .GetValue (db) as IQueryable<dynamic>;

        public static dynamic GetResource (
                DbContext db,
                IRequest request) =>
            GetResource (GetIQueryable (db, request.ResourceName), request);

        private static dynamic MagicRead (
            IQueryable<dynamic> queryableData,
            string func,
            object[] parameters
        ) {
            var magic = Activator.CreateInstance (typeof (MagicReadResource<>)
                .MakeGenericType (
                    new Type[] {
                        queryableData.ElementType
                    }
                ));

            var result = magic.GetType ().GetMethod (func).Invoke (magic, parameters);
            return result;
        }

        public static dynamic GetResource (
            IQueryable<dynamic> queryableData,
            IRequest request) => MagicRead (queryableData,
            "GetResource",
            new object[] {
                queryableData,
                request.IdentifierName,
                request.IdentifierValue
            });

        public dynamic GetResourceWithRange (
            IQueryable<dynamic> queryableData,
            int startpoint,
            int endPoint) => MagicRead (queryableData,
            "GetResourceWithRange",
            new object[] {
                queryableData,
                startpoint,
                endPoint,
                true
            });

        public bool VerifyConnection (
            IQueryable<dynamic> queryable,
            Type hostModelType,
            Type guestModelType,
            PropertyInfo idProperty,
            string idValue,
            string secondObjectName,
            PropertyInfo claimId,
            string claimIdValue) {
            var magic = Activator.CreateInstance (typeof (MagicConnectionValidation<,>)
                .MakeGenericType (
                    new Type[] {
                        hostModelType,
                        guestModelType
                    }));
            var result = magic.GetType ().GetMethod ("Verify").Invoke (magic,
                new object[] {
                    queryable,
                    idProperty.Name,
                    idValue,
                    secondObjectName,
                    claimId.Name,
                    claimIdValue
                });

            return (bool) result;
        }
    }
}