using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using Infrastructure.Common;
using Infrastructure.Domain;


namespace Infrastructure.EntityFramework
{
    public abstract class EntityFrameworkRepository<TEntity, TKey> : IRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey> 
        where TKey : struct
    {
        protected DbContext Context { get; set; }
        protected DbSet<TEntity> DbSet { get; }

        public EntityFrameworkRepository(IDbContextFactory contextFactory)
        {
            Context = contextFactory.GetContext();
            DbSet = Context.Set<TEntity>();
        }

        public virtual void Create(TEntity entity)
        {
            DbSet.Add(entity);
        }

        public virtual void Update(TEntity entity)
        {
            DbSet.Attach(entity);
            Context.Entry(entity).State = EntityState.Modified;
        }

        public virtual void Delete(TEntity entity)
        {
            if (Context.Entry(entity).State == EntityState.Detached)
            {
                DbSet.Attach(entity);
            }
            DbSet.Remove(entity);
        }

        public virtual void Delete(TKey id)
        {
            var entity = FindById(id);
            Delete(entity);
        }

        public virtual TEntity FindById(TKey id)
        {
            var query = DbSet
                .Where(x => (object)x.Id == (object)id);

            return query.FirstOrDefault();
        }

        public virtual IEnumerable<TEntity> All(SortOrders<TEntity> orders = null)
        {
            return Find(null, orders);
        }

        public virtual PagedResult<TEntity> PagedAll(SortOrders<TEntity> orders,
            int pageNumber = 1, int pageSize = 100)
        {

            return PagedFind(null, orders, pageNumber, pageSize);
        }

        public virtual IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> whereExpression,
            SortOrders<TEntity> orders = null)
        {
            IQueryable<TEntity> tempResult = DbSet;

            if (whereExpression != null) tempResult = tempResult.Where(whereExpression);
            if (orders != null) tempResult = tempResult.OrderBy(orders);

            return tempResult.AsEnumerable();
        }

        public virtual PagedResult<TEntity> PagedFind(
            Expression<Func<TEntity, bool>> predicate,
            SortOrders<TEntity> orders = null,
            int pageNumber = 1,
            int pageSize = 20)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 1 : pageSize;

            IQueryable<TEntity> tempResult = DbSet;

            if (predicate != null) tempResult = tempResult.Where(predicate);
            if (orders != null) tempResult = tempResult.OrderBy(orders);

            var result = new PagedResult<TEntity>
            {
                PageIndex = pageNumber - 1,
                PageSize = pageSize,
                PageNum = pageNumber,
                TotalRows = tempResult.Count(),
                Results = tempResult.Skip((pageNumber - 1)*pageSize).Take(pageSize).AsEnumerable()
            };

            return result;
        }
    }
}