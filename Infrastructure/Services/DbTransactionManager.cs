using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Infrastructure.Data;

namespace CarPartsShopWPF.Infrastructure.Services
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
