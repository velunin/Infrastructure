using System;

namespace Infrastructure.Domain
{
    public interface IWriteRepository<in TEntity, TKey> where TEntity : IEntity<TKey> where TKey : struct
    {
        void Create(TEntity entity);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        void Delete(TKey id);
    }
}