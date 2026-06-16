using System;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.SQLiteMigrations
{
    public static class Migration008_SupplierPurchaseItems
    {
        public static void Execute()
        {
            var db = DatabaseManager.Instance;
            if (db.GetSetting("migration008_applied", "false") == "true") return;

            try
            {
                db.EnsureColumnExists("supplier_transactions", "paid_amount", "REAL NOT NULL DEFAULT 0");
                db.EnsureColumnExists("supplier_transactions", "item_count", "INTEGER NOT NULL DEFAULT 0");

                db.Execute(@"CREATE TABLE IF NOT EXISTS supplier_purchase_items (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    supplier_transaction_id INTEGER NOT NULL,
                    supplier_id INTEGER NOT NULL,
                    product_name TEXT NOT NULL,
                    quantity INTEGER NOT NULL,
                    unit_purchase_price REAL NOT NULL,
                    line_total REAL NOT NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (supplier_transaction_id) REFERENCES supplier_transactions(id) ON DELETE CASCADE,
                    FOREIGN KEY (supplier_id) REFERENCES suppliers(id)
                )");

                db.Execute("CREATE INDEX IF NOT EXISTS idx_supplier_purchase_items_transaction ON supplier_purchase_items(supplier_transaction_id)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_supplier_purchase_items_supplier ON supplier_purchase_items(supplier_id)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_supplier_purchase_items_name ON supplier_purchase_items(product_name)");

                db.SetSetting("migration008_applied", "true");
                Logger.LogInfo("Migration008: Supplier purchase items schema applied.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Migration008 execution error");
                throw;
            }
        }
    }
}
