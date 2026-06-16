using System;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.SQLiteMigrations
{
    /// <summary>
    /// Migration010: Adds soft-delete + audit columns to expenses so deletions become auditable and
    /// reversible-by-record instead of a destructive hard DELETE. Reports filter on
    /// COALESCE(is_deleted,0)=0 so pre-migration rows (no column / NULL) are treated as active.
    /// Idempotent: EnsureColumnExists is a no-op when the column already exists.
    /// </summary>
    public static class Migration010_ExpenseSoftDelete
    {
        public static void Execute()
        {
            var db = DatabaseManager.Instance;
            if (db.GetSetting("migration010_applied", "false") == "true") return;

            try
            {
                db.EnsureColumnExists("expenses", "is_deleted", "INTEGER NOT NULL DEFAULT 0");
                db.EnsureColumnExists("expenses", "deleted_at", "TIMESTAMP");
                db.EnsureColumnExists("expenses", "deleted_by", "INTEGER");

                db.Execute("CREATE INDEX IF NOT EXISTS idx_expenses_is_deleted ON expenses(is_deleted)");

                db.SetSetting("migration010_applied", "true");
                Logger.LogInfo("Migration010: Expense soft-delete columns applied.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Migration010 execution error");
                throw;
            }
        }
    }
}
