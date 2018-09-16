using System;
using API.Engine;
using API.Enums;
using API.Interface;
using API.Models.Temporary;
using Microsoft.EntityFrameworkCore;

namespace API.RelationDependencyResolver {
    public abstract class RelationCounterResolver : IRelationDependent<Object> {

        public abstract string GetRelationName ();
        public abstract string GetPropertyName ();

        public dynamic OnRelationEvent (
            DbContext dbContext,
            object model,
            string requesterID,
            IRequest request,
            IRequest dependentRequest,
            Object intractionType,
            HttpRequestMethod httpRequestMethod) {
            var intractionName = intractionType.ToString ();

            if (intractionName != GetRelationName ())
                return null;

            if (httpRequestMethod != HttpRequestMethod.Post &&
                httpRequestMethod != HttpRequestMethod.Delete)
                return null;

            if (model == null)
                model = APIUtils.GetResource (dbContext, request) as object;
            if (model == null)
                return null;

            var countProp = model.GetType ().GetProperty (GetPropertyName ());
            var countValue = (long) countProp.GetValue (model);

            if (httpRequestMethod == HttpRequestMethod.Post) {
                countProp.SetValue (model, countValue + 1);
            } else if (httpRequestMethod == HttpRequestMethod.Delete) {
                countProp.SetValue (model, Math.Max (0, countValue - 1));
            }

            return model;
        }
    }
}