using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using Infrastructure.Common;
using Infrastructure.Domain;


namespace Infrastructure.EntityFramework
{
    public abstract class EntityFrameworkRepository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
    {
        private readonly DbContext _context;
        private readonly DbSet<TEntity> _dbSet;

        public EntityFrameworkRepository(IDbContextFactory contextFactory)
        {
            _context = contextFactory.GetContext();
            _dbSet = _context.Set<TEntity>();
        }

        public virtual void Create(TEntity entity)
        {
            _dbSet.Add(entity);
        }

        public virtual void Update(TEntity entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public virtual void Delete(TEntity entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
        }

        public virtual void Delete(int id)
        {
            var entity = FindById(id);
            Delete(entity);
        }

        public virtual TEntity FindById(int id)
        {
            var query = _dbSet
                .Where(x => x.Id == id);

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

        public virtual IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> whereExpression, SortOrders<TEntity> orders = null)
        {
            IQueryable<TEntity> tempResult = _dbSet;

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

            IQueryable<TEntity> tempResult = _dbSet;

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