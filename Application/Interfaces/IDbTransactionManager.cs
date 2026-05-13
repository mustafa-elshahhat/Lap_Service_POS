using System;

namespace CarPartsShopWPF.Application.Interfaces
{
    public interface IDbTransactionManager
    {
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
    }
}
