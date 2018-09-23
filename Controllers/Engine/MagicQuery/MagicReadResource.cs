using System;
using System.Collections;
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

        public static Expression CreateInnerSelect<TTarget> () {
            var fields = typeof (TTarget).GetProperties ()
                .Where (property =>
                    !property.PropertyType.FullName.Contains (AppDomain.CurrentDomain.FriendlyName) &&
                    !property.PropertyType.GetGenericArguments ().Where (generic => generic.FullName.Contains (AppDomain.CurrentDomain.FriendlyName)).Any () &&
                    !property.IsDefined (typeof (ExcludeOnSelectAttribute))
                ).Select (property => property.Name)
                .ToList ();

            var parameter = Expression.Parameter (typeof (TTarget));
            var bindings = fields
                .Select (property => Expression.Bind (
                    typeof (TTarget).GetProperty (property),
                    Expression.Property (parameter, property)
                ))
                .ToList ();

            var newType = Expression.MemberInit (Expression.New (typeof (TTarget)), bindings);
            return Expression.Lambda<Func<TTarget, TTarget>> (newType, parameter);
        }

        private List<MemberAssignment> BindProperties<TTarget> (
            ParameterExpression parameter,
            List<PropertyInfo> fields) {
            var bindings = new List<MemberAssignment> ();

            foreach (var property in fields) {
                Expression propertyExp = Expression.Property (parameter, property);

                var isICollection = property.PropertyType.FullName.Contains ("ICollection");
                Type type;
                if (isICollection) {
                    type = property.PropertyType.GetGenericArguments ().FirstOrDefault ();
                } else {
                    type = property.PropertyType;
                }

                if (type.IsEnum == false &&
                    type.FullName.Contains (AppDomain.CurrentDomain.FriendlyName)) {
                    var innerSelect = typeof (MagicReadResource<TEntity>)
                        .GetMethod ("CreateInnerSelect")
                        .MakeGenericMethod (type)
                        .Invoke (null, null) as Expression;

                    if (isICollection) {
                        propertyExp = Expression.Call (
                            typeof (System.Linq.Enumerable),
                            nameof (System.Linq.Enumerable.Select),
                            new Type[] {
                                type,
                                type
                            },
                            propertyExp,
                            innerSelect);

                        var pruneList = property.GetCustomAttribute<PruneListAttribute> ();
                        if (pruneList != null) {
                            propertyExp =
                                Expression.Call (
                                    typeof (System.Linq.Enumerable),
                                    nameof (System.Linq.Enumerable.Take),
                                    new Type[] {
                                        type
                                    },
                                    propertyExp,
                                    Expression.Constant (pruneList.Amount));
                        }

                        propertyExp =
                            Expression.Call (
                                typeof (System.Linq.Enumerable),
                                nameof (System.Linq.Enumerable.ToList),
                                new Type[] {
                                    type
                                },
                                propertyExp);
                    } else {
                        propertyExp = innerSelect;
                    }
                }

                bindings.Add (Expression.Bind (
                    typeof (TTarget).GetProperty (property.Name),
                    propertyExp
                ));
            }

            return bindings;
        }

        private Expression CreateSelect<TTarget> (
            ParameterExpression parameter,
            List<PropertyInfo> fields) {
            var mustProjectFields = fields
                .Where (property => property.IsDefined (typeof (PruneListAttribute), true))
                .ToList ();

            var normalFields = fields.Except (mustProjectFields);

            var bindings = BindProperties<TTarget> (parameter, fields);
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
                        .Where (property =>
                            property.CanRead &&
                            property.CanWrite &&
                            property.PropertyType.IsPublic)
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
                .Where (property =>
                    property.CanRead &&
                    property.CanWrite &&
                    property.PropertyType.IsPublic &&
                    !property.IsDefined (typeof (ExcludeOnSelectAttribute), true))
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
                        nameof (System.Linq.Queryable.Select),
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
                result = Expression.Call (
                    typeof (System.Linq.Queryable),
                    nameof (System.Linq.Queryable.Select),
                    new Type[] {
                        queryable.ElementType,
                            typeof (TEntity)
                    },
                    queryable.Expression,
                    CreateOptimizeSelect (parameterExpression)
                );

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