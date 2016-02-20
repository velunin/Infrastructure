using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Dapper;
using Infrastructure.Common;
using Infrastructure.Common.Database;
using Infrastructure.Domain;

namespace Infrastructure.Dapper
{
    public abstract class DapperReadRepository<TEntity> : IReadRepository<TEntity> where TEntity : IEntity
    {
        private readonly IDbConnection _connection;
        // ReSharper disable once MemberCanBePrivate.Global
        protected IDbConnection Connection
        {
            get
            {
                if (_connection.State == ConnectionState.Closed)
                    _connection.Open();

                return _connection;
            }
        }

        protected DapperReadRepository(IDbConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            _connection = connection;
        }

        public virtual IEnumerable<TEntity> All(SortOrders<TEntity> orders = null)
        {
            var query = new SqlDynamicQuery<TEntity>()
                .SetSorting(orders)
                .ToSql();

            return Connection.Query<TEntity>(query.Query);
        }

        public virtual IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate, SortOrders<TEntity> orders = null)
        {
            var query = new SqlDynamicQuery<TEntity>()
                .Where(predicate)
                .SetSorting(orders)
                .ToSql();

            return Connection.Query<TEntity>(query.Query, (object)query.Parameters);
        }

        public virtual TEntity FindById(int id)
        {
            var query = new SqlDynamicQuery<TEntity>()
                .Where(x => x.Id == id)
                .Take(1)
                .ToSql();

            return Connection.Query<TEntity>(query.Query, (object)query.Parameters).FirstOrDefault();
        }

        public virtual PagedResult<TEntity> PagedAll(SortOrders<TEntity> orders = null, int pageNumber = 1, int pageSize = 100)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 1 : pageSize;

            var queryDynamic = new SqlDynamicQuery<TEntity>()
                .SetSorting(orders);

            var countQuery = queryDynamic.CountQuery().Query;
            var count = (int)Connection.ExecuteScalar(countQuery);

            var query = queryDynamic
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize).ToSql().Query;

            return new PagedResult<TEntity>()
            {
                PageIndex = pageNumber - 1,
                PageSize = pageSize,
                PageNum = pageNumber,
                TotalRows = count,
                Results = Connection.Query<TEntity>(query)
            };
        }

        public virtual PagedResult<TEntity> PagedFind(Expression<Func<TEntity, bool>> predicate, SortOrders<TEntity> orders = null, int pageNumber = 1, int pageSize = 20)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 1 : pageSize;

            var queryDynamic = new SqlDynamicQuery<TEntity>()
                .Where(predicate)
                .SetSorting(orders);

            var countQuery = queryDynamic.CountQuery();
            var count = (int)Connection.ExecuteScalar(countQuery.Query, (object)countQuery.Parameters);

            var query = queryDynamic
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize).ToSql();

            return new PagedResult<TEntity>()
            {
                PageIndex = pageNumber - 1,
                PageSize = pageSize,
                PageNum = pageNumber,
                TotalRows = count,
                Results = Connection.Query<TEntity>(query.Query, (object)query.Parameters)
            };
        }
    }
}