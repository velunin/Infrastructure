using System;
using System.Linq;
using Infrastructure.Common;

// ReSharper disable once CheckNamespace
namespace Infrastructure.Domain
{
    public static class SortingExtensions
    {
        public static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> @this, SortOrders<TEntity> orderObject)
        {
            if (orderObject == null) throw new ArgumentNullException("orderObject");

            return orderObject.DoSort(@this);
        }
    }
}
