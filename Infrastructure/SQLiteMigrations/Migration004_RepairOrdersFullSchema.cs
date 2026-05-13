using System;
using System.IO;
using CarPartsShopWPF.Infrastructure.Data;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Infrastructure.SQLiteMigrations
{
    public static class Migration004_RepairOrdersFullSchema
    {
        public static void Execute()
        {
            var db = DatabaseManager.Instance;

            if (db.GetSetting("repair_orders_full_schema_applied") == "true")
                return;

            try
            {
                string appPath = AppDomain.CurrentDomain.BaseDirectory;
                string backupDir = Path.Combine(appPath, "backups", "migrations");
                string backupFile = db.Backup(backupDir);
                Logger.LogInfo($"Migration004: Safety backup created at {backupFile}");

                db.EnsureColumnExists("repair_orders", "customer_name",      "TEXT");
                db.EnsureColumnExists("repair_orders", "customer_phone",     "TEXT");
                db.EnsureColumnExists("repair_orders", "technician_name",    "TEXT");
                db.EnsureColumnExists("repair_orders", "total_amount",       "REAL NOT NULL DEFAULT 0");
                db.EnsureColumnExists("repair_orders", "paid_amount",        "REAL NOT NULL DEFAULT 0");
                db.EnsureColumnExists("repair_orders", "remaining_amount",   "REAL NOT NULL DEFAULT 0");
                db.EnsureColumnExists("repair_orders", "order_status",       "TEXT NOT NULL DEFAULT 'received'");
                db.EnsureColumnExists("repair_orders", "expected_delivery",  "DATE");
                db.EnsureColumnExists("repair_orders", "intake_date",        "TIMESTAMP DEFAULT CURRENT_TIMESTAMP");
                db.EnsureColumnExists("repair_orders", "delivery_date",      "TIMESTAMP");
                db.EnsureColumnExists("repair_orders", "updated_at",         "TIMESTAMP DEFAULT CURRENT_TIMESTAMP");

                db.Execute(@"CREATE TABLE IF NOT EXISTS repair_devices (
                    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    order_id            INTEGER NOT NULL,
                    device_type         TEXT NOT NULL DEFAULT 'laptop',
                    brand               TEXT,
                    model               TEXT,
                    serial_number       TEXT,
                    condition           TEXT,
                    reported_issue      TEXT NOT NULL,
                    accessories         TEXT,
                    estimated_cost      REAL DEFAULT 0,
                    service_cost        REAL DEFAULT 0,
                    labor_cost          REAL DEFAULT 0,
                    device_status       TEXT NOT NULL DEFAULT 'received',
                    diagnosis_notes     TEXT,
                    repair_notes        TEXT,
                    created_at          TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (order_id) REFERENCES repair_orders(id) ON DELETE CASCADE
                )");

                db.Execute(@"CREATE TABLE IF NOT EXISTS repair_parts (
                    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    device_id           INTEGER NOT NULL,
                    order_id            INTEGER NOT NULL,
                    product_id          INTEGER,
                    part_name           TEXT NOT NULL,
                    quantity            INTEGER NOT NULL DEFAULT 1,
                    unit_cost           REAL NOT NULL DEFAULT 0,
                    total_cost          REAL NOT NULL DEFAULT 0,
                    is_from_inventory   INTEGER NOT NULL DEFAULT 0,
                    created_at          TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (device_id)  REFERENCES repair_devices(id) ON DELETE CASCADE,
                    FOREIGN KEY (order_id)   REFERENCES repair_orders(id)  ON DELETE CASCADE,
                    FOREIGN KEY (product_id) REFERENCES products(id)
                )");

                db.Execute(@"CREATE TABLE IF NOT EXISTS repair_payments (
                    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    order_id            INTEGER NOT NULL,
                    amount              REAL NOT NULL,
                    payment_method      TEXT DEFAULT 'نقدي',
                    payment_date        TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    notes               TEXT,
                    user_id             INTEGER,
                    created_at          TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (order_id) REFERENCES repair_orders(id) ON DELETE CASCADE,
                    FOREIGN KEY (user_id)  REFERENCES users(id)
                )");

                db.Execute("CREATE INDEX IF NOT EXISTS idx_repair_orders_intake   ON repair_orders(intake_date)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_repair_orders_status   ON repair_orders(order_status)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_repair_orders_customer ON repair_orders(customer_id)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_repair_devices_order   ON repair_devices(order_id)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_repair_parts_device    ON repair_parts(device_id)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_repair_parts_order     ON repair_parts(order_id)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_repair_payments_order  ON repair_payments(order_id)");

                db.Execute(@"INSERT OR IGNORE INTO settings (key, value, description)
                             VALUES ('repair_order_prefix', 'MNT', 'بادئة رقم طلب الصيانة')");

                db.SetSetting("repair_orders_full_schema_applied", "true");
                Logger.LogInfo("Migration004: Repair orders full schema applied successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Migration004 execution error");
                throw;
            }
        }
    }
}
