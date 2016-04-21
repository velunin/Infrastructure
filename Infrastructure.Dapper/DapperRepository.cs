using System.Data;
using Infrastructure.Domain;

namespace Infrastructure.Dapper
{
    public abstract class DapperRepository<TEntity, TKey> : 
        DapperReadRepository<TEntity, TKey>,
        IRepository<TEntity, TKey> 
        where TEntity : IEntity<TKey> 
        where TKey : struct
    {
        protected DapperRepository(IDbConnection connection) : base(connection)
        {
        }

        public virtual void Create(TEntity entity)
        {
            throw new System.NotImplementedException();
        }

        public virtual void Update(TEntity entity)
        {
            throw new System.NotImplementedException();
        }

        public virtual void Delete(TEntity entity)
        {
            throw new System.NotImplementedException();
        }

        public virtual void Delete(TKey id)
        {
            throw new System.NotImplementedException();
        }
    }
}