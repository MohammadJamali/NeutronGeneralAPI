using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace API.Engine.MagicQuery {
    public abstract class MagicQuery {
        protected dynamic Where<TEntity> (
            Type methodReflector,
            Type queryableElementType,
            Expression queryableExpression,
            Expression predicateBody,
            ParameterExpression parameterExpression) {
            return Expression.Call (
                methodReflector, // typeof (System.Linq.Queryable)
                "Where",
                new Type[] { queryableElementType }, // queryable.ElementType
                queryableExpression, // queryable.Expression
                Expression.Lambda<Func<TEntity, bool>> (
                    predicateBody,
                    parameterExpression));
        }

        protected dynamic AsNoTracking (
            System.Linq.IQueryable queryable,
            Expression expression) {
            return Expression.Call (
                typeof (EntityFrameworkQueryableExtensions),
                nameof (EntityFrameworkQueryableExtensions.AsNoTracking),
                new Type[] { queryable.ElementType },
                expression);
        }

        protected dynamic Any (
            System.Linq.IQueryable queryable,
            Expression expression) {
            return Expression.Call (
                typeof (System.Linq.Queryable),
                "Any",
                new Type[] { queryable.ElementType },
                expression);
        }
    }
}