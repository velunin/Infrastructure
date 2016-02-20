using System;
using System.Data;

namespace Infrastructure.Domain
{
    public interface IUnitOfWork : IDisposable
    {
        int Commit();
        IsolationLevel IsolationLevel { get; set; }
    }
}