using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.SQLiteMigrations
{
    public static class Migration003_AddCashRefundToReturns
    {
        public static void Execute()
        {
            var db = DatabaseManager.Instance;
            if (db.GetSetting("migration003_applied") == "true")
                return;

            db.EnsureColumnExists("returns", "cash_refund", "REAL DEFAULT 0");
            db.EnsureColumnExists("returns", "debt_deduction", "REAL DEFAULT 0");

            db.Execute("UPDATE returns SET cash_refund = total_amount WHERE cash_refund = 0 AND total_amount > 0");

            db.SetSetting("migration003_applied", "true");
            Logger.LogInfo("Migration003: Return financial columns and backfill applied.");
        }
    }
}
