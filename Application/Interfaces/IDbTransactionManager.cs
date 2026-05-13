using System;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface IDbTransactionManager
    {
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
    }
}
