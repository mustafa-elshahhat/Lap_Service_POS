using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Infrastructure.Data;

namespace AlJohary.ServiceHub.Infrastructure.Services
{
    public class DbTransactionManager : IDbTransactionManager
    {
        public void BeginTransaction()
        {
            DatabaseManager.Instance.BeginTransaction();
        }

        public void CommitTransaction()
        {
            DatabaseManager.Instance.CommitTransaction();
        }

        public void RollbackTransaction()
        {
            DatabaseManager.Instance.RollbackTransaction();
        }
    }
}
