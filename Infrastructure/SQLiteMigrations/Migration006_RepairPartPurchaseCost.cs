using System;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.SQLiteMigrations
{
    public static class Migration006_RepairPartPurchaseCost
    {
        public static void Execute()
        {
            var db = DatabaseManager.Instance;

            if (db.GetSetting("migration006_applied") == "true")
                return;

            try
            {
                db.EnsureColumnExists("repair_parts", "purchase_cost", "REAL NOT NULL DEFAULT 0");

                db.Execute("CREATE INDEX IF NOT EXISTS idx_repair_orders_delivery ON repair_orders(delivery_date)");

                db.SetSetting("migration006_applied", "true");
                Logger.LogInfo("Migration006: Added purchase_cost to repair_parts and delivery_date index.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Migration006 execution error");
                throw;
            }
        }
    }
}
