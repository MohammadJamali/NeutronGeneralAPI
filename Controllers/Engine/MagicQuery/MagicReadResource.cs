using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using API.Attributes;
using API.Engine;
using API.Models.Architecture;
using API.Models.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace API.Engine.MagicQuery {

    public class MagicReadResource<TEntity> : MagicQuery {

        private Expression CreateSelect<TTarget> (ParameterExpression parameter, List<string> fields) {
            var bindings = fields
                .Select (name => Expression.Bind (
                    typeof (TTarget).GetProperty (name),
                    Expression.Property (parameter, name)
                ));
            var newType = Expression.MemberInit (Expression.New (typeof (TTarget)), bindings);
            var lambda = Expression.Lambda<Func<TEntity, TTarget>> (newType, parameter);
            return lambda;
        }

        private Expression CreateCardSelect (ParameterExpression parameter) {
            var entityType = typeof (TEntity);
            var cardLevels = new Type[] {
                typeof (InteractiveVisualDescriptiveModel),
                typeof (VisualDescriptiveModel),
                typeof (DescriptiveModel),
                typeof (RootModel)
            };

            for (int i = 0; i < cardLevels.Count (); i++) {
                if (entityType.IsSubclassOf (cardLevels[i])) {
                    var targetProperties = cardLevels[i]
                        .GetProperties ()
                        .Select (property => new {
                            property.Name,
                                property.PropertyType.IsPublic,
                                property.CanRead,
                                property.CanWrite
                        })
                        .Where (property =>
                            property.CanRead &&
                            property.CanWrite &&
                            property.IsPublic)
                        .Select (property => property.Name)
                        .ToList ();

                    return CreateSelect<Card> (
                        parameter,
                        targetProperties
                    );
                }
            }

            throw new Exception ("This model can not be selected as a card");
        }

        private Expression CreateOptimizeSelect (ParameterExpression parameter) {
            var targetProperties = typeof (TEntity).GetProperties ();
            var chosenProperties = targetProperties
                .Select (property => new {
                    property.Name,
                        property.PropertyType.IsPublic,
                        property.CanRead,
                        property.CanWrite,
                        IsLargData = property.IsDefined (typeof (ExcludeOnSelectAttribute), true)
                })
                .Where (property =>
                    property.CanRead &&
                    property.CanWrite &&
                    property.IsPublic &&
                    !property.IsLargData)
                .Select (property => property.Name)
                .ToList ();

            return CreateSelect<TEntity> (
                parameter,
                chosenProperties
            );
        }

        private dynamic Include (
            IQueryable queryable,
            Expression expression
        ) {
            var includeNeeded = typeof (TEntity)
                .GetProperties ()
                .Where (property => property.IsDefined (typeof (IncludeAttribute)))
                .AsEnumerable ();

            foreach (var property in includeNeeded)
                expression = Expression.Call (
                    typeof (EntityFrameworkQueryableExtensions),
                    "Include",
                    new Type[] { queryable.ElementType },
                    expression,
                    Expression.Constant (property.Name));

            return expression;
        }

        public dynamic GetResourceWithRange (
            IQueryable<TEntity> queryable,
            int startPoint,
            int endPoint,
            bool asCard = true
        ) {
            var parameterExpression = Expression.Parameter (typeof (TEntity), "model");

            var result = Include (queryable, queryable.Expression);

            if (asCard) {
                result =
                    Expression.Call (
                        typeof (System.Linq.Queryable),
                        "Select",
                        new Type[] {
                            queryable.ElementType,
                                typeof (Card)
                        },
                        result,
                        CreateCardSelect (parameterExpression));

                return (queryable.Provider
                        .CreateQuery<Card> (result) as IQueryable<Card>)
                    .Skip (startPoint)
                    .Take (endPoint)
                    .ToList ();
            } else {
                return (queryable.Provider
                        .CreateQuery<TEntity> (result) as IQueryable<TEntity>)
                    .Skip (startPoint)
                    .Take (endPoint)
                    .ToList ();
            }
        }

        public dynamic GetResource (
            IQueryable<TEntity> queryable,
            string idProp,
            string idValue) {
            var parameterExpression = Expression.Parameter (typeof (TEntity), "model");

            var expIdProp = Expression.Call (
                Expression.Property (
                    parameterExpression,
                    idProp),
                "ToString",
                null);
            var expIdValue = Expression.Constant (idValue);
            var predicateBody = Expression.Equal (expIdProp, expIdValue);

            var expression = Expression.Call (
                typeof (System.Linq.Queryable),
                "Select",
                new Type[] {
                    queryable.ElementType,
                        typeof (TEntity)
                },
                queryable.Expression,
                CreateOptimizeSelect (parameterExpression));

            expression = Where<TEntity> (
                typeof (System.Linq.Queryable),
                queryable.ElementType,
                expression,
                predicateBody,
                parameterExpression);

            // expression = Include (queryable, expression);
            expression = AsNoTracking (queryable, expression);

            var quary = queryable.Provider.CreateQuery<TEntity> (expression) as IQueryable<TEntity>;
            return quary.Take (1).ToList ().FirstOrDefault ();
        }
    }
}