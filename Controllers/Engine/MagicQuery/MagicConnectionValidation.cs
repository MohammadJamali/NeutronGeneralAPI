using System;
using System.Linq;
using System.Linq.Expressions;

namespace API.Engine.MagicQuery {
    public class MagicConnectionValidation<TEntity, TTarget> : MagicQuery {
        public bool Verify (
            System.Linq.IQueryable queryable,
            string hostIdProp,
            string hostIdValue,
            string clientName,
            string clientIdProp,
            string clientIdValue) {
            var expHost = Expression.Parameter (typeof (TEntity), "host");

            var expHostId = Expression.Call (
                // get host id property value as whatever it is
                Expression.Property (
                    expHost,
                    hostIdProp),
                "ToString",
                null);
            var expHostIdValue = Expression.Constant (hostIdValue);
            var expHostIdentity = Expression.Equal (expHostId, expHostIdValue);

            var expClient = Expression.Property (expHost, clientName);
            var expClientIdValue = Expression.Constant (clientIdValue);

            if (expClient.Type.Name.StartsWith ("ICollection")) {
                var clientType = expClient.Type.GenericTypeArguments.First ();

                var expClientParameter = Expression.Parameter (clientType, "client");
                var expClientId = Expression.Call (
                    // get client id property value as whatever it is
                    Expression.Property (
                        expClientParameter,
                        clientIdProp),
                    "ToString",
                    null);

                Expression expression = Expression.Equal (expClientId, expClientIdValue);

                expression = Where<TTarget> (
                    typeof (Enumerable),
                    clientType,
                    expClient,
                    expression,
                    expClientParameter);

                expression = Expression.Call (
                    typeof (Enumerable),
                    "Any",
                    new Type[] { clientType },
                    expression);

                expression = Where<TEntity> (
                    typeof (Queryable),
                    queryable.ElementType,
                    queryable.Expression,
                    Expression.AndAlso (expHostIdentity, expression),
                    expHost);

                var query = queryable.Provider
                    .CreateQuery<TEntity> (expression) as IQueryable<TEntity>;

                return query.Take (1).Any ();
            } else {
                var expClientId = Expression.Call (
                    Expression.Property (expClient, clientIdProp),
                    "ToString",
                    null);
                var expClientIdentity = Expression.Equal (expClientId, expClientIdValue);

                var predicateBody = Expression.AndAlso (expHostIdentity, expClientIdentity);

                var query = queryable.Provider.CreateQuery<TEntity> (
                    Where<TEntity> (
                        typeof (System.Linq.Queryable),
                        queryable.ElementType,
                        queryable.Expression,
                        predicateBody,
                        expHost)
                ) as IQueryable<TEntity>;

                return query.Take (1).Any ();
            }
        }

    }
}