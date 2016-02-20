using System.Data;

namespace Infrastructure.Domain
{
    public interface IUnitOfWorkFactory
    {
        IUnitOfWork Create(IsolationLevel isolation);

        IUnitOfWork Create();
    }
}