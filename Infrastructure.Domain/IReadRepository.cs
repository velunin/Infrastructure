using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Infrastructure.Common;

namespace Infrastructure.Domain
{
    public interface IReadRepository<TEntity> where TEntity : IEntity
    {
        IEnumerable<TEntity> All(SortOrders<TEntity> orders = null);
 
        IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate,
            SortOrders<TEntity> orders = null);

        TEntity FindById(int id);

        PagedResult<TEntity> PagedAll(SortOrders<TEntity> orders = null,
            int pageNumber = 1, int pageSize = 100);

        PagedResult<TEntity> PagedFind(Expression<Func<TEntity, bool>> predicate, SortOrders<TEntity> orders = null, int pageNumber = 1, int pageSize = 20);
    }
}