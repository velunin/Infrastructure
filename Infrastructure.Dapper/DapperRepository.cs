using System.Data;
using Infrastructure.Domain;

namespace Infrastructure.Dapper
{
    public abstract class DapperRepository<TEntity> : DapperReadRepository<TEntity>, IRepository<TEntity> where TEntity : IEntity
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

        public virtual void Delete(int id)
        {
            throw new System.NotImplementedException();
        }
    }
}