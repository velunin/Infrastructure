using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Infrastructure.Common
{
    public class SortOrders<TEntity>
    {
        private readonly List<IOrderByItem<TEntity>> _orders = new List<IOrderByItem<TEntity>>();

        public IList<IOrderByItem<TEntity>> Orders
        {
            get { return _orders.AsReadOnly(); }
        }

        private void AddSorting<TKey>(Expression<Func<TEntity, TKey>> orderBy, SortDirection direction)
        {
            _orders.Add(new OrderByItem<TEntity, TKey>
            {
                OrderByExpr = orderBy,
                Direction = direction
            });
        }

        public SortOrders<TEntity> AddSortBy<TKey>(Expression<Func<TEntity, TKey>> orderBy, SortDirection direction)
        {
            AddSorting(orderBy, direction);
            return this;
        }

        public SortOrders<TEntity> AddSortBy<TKey>(OrderByItem<TEntity, TKey> order)
        {
            _orders.Add(order);
            return this;
        }

        public static SortOrders<TEntity> Create()
        {
            return new SortOrders<TEntity>();
        }

        public IQueryable<TEntity> DoSort(IQueryable<TEntity> collection)
        {
            var initial = true;
            foreach (var order in Orders)
            {
                collection = order.ApplyOrder(collection, initial);
                initial = false;
            }

            return collection;
        }
    }

    /// <summary>
    /// Contains info for sorting
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class OrderByItem<TEntity,TKey> : IOrderByItem<TEntity>
    {
        private Expression<Func<TEntity, TKey>> _orderByExpr;

        public Type ReturnType
        {
            get { return _orderByExpr.ReturnType; }
        }

        public ParameterExpression ParameterExpression
        {
            get { return _orderByExpr.Parameters[0]; }
        }

        public Expression BodyExpression
        {
            get { return _orderByExpr; }
        }

        public IQueryable<TEntity> ApplyOrder(IQueryable<TEntity> collection, bool initial)
        {
            if (!initial && collection is IOrderedQueryable<TEntity>)
            {
                return Direction ==
                       SortDirection.Ascending
                    ? ((IOrderedQueryable<TEntity>) collection).ThenBy(_orderByExpr)
                    : ((IOrderedQueryable<TEntity>) collection).ThenByDescending(_orderByExpr);
            }

            return Direction ==
                   SortDirection.Ascending
                ? ((IOrderedQueryable<TEntity>) collection).OrderBy(_orderByExpr)
                : ((IOrderedQueryable<TEntity>) collection).OrderByDescending(_orderByExpr);
        }

        /// <summary>
        /// Expression for .OrderBy extension method of IQueriable
        /// </summary>
        public Expression<Func<TEntity, TKey>> OrderByExpr
        {
            get { return _orderByExpr; }
            set
            {
                _orderByExpr = value;
            }
        }

        /// <summary>
        /// Sort direction
        /// </summary>
        public SortDirection Direction { get; set; }
    }

    public enum SortDirection
    {
        Ascending = 0,
        Descending = 1
    }
}