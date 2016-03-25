using System.Data;
using Infrastructure.Domain;

namespace Infrastructure.EntityFramework
{
    public class EntityFrameworkUnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IDbContextFactory _contextFactory;

        public EntityFrameworkUnitOfWorkFactory(IDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public IUnitOfWork Create(IsolationLevel isolation)
        {
            return new EntityFrameworkUnitOfWork(_contextFactory) { IsolationLevel = isolation };
        }

        public IUnitOfWork Create()
        {
            return new EntityFrameworkUnitOfWork(_contextFactory);
        }
    }
}