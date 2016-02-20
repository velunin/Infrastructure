using System;

namespace Infrastructure.Domain
{
    public interface IWriteRepository<in TEntity> where TEntity : IEntity
    {
        void Create(TEntity entity);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        void Delete(int id);
    }
}