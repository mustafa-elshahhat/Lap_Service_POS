using CarPartsShopWPF.Infrastructure.Data;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Infrastructure.SQLiteMigrations
{
    public static class Migration005_DropDeadPaymentMethodSchema
    {
        public static void Execute()
        {
            var db = DatabaseManager.Instance;

            if (db.GetSetting("migration005_applied") == "true")
                return;

            try
            {
                db.Execute("DROP TABLE IF EXISTS payment_methods_ref");

                db.Execute("DROP TABLE IF EXISTS credit_payments");

                try { db.Execute("ALTER TABLE sales DROP COLUMN payment_method_id"); } catch { }
                try { db.Execute("ALTER TABLE sale_payments DROP COLUMN payment_method_id"); } catch { }

                db.SetSetting("migration005_applied", "true");
                Logger.LogInfo("Migration005: Dropped dead payment_methods_ref table and payment_method_id columns.");
            }
            catch (System.Exception ex)
            {
                Logger.LogException(ex, "Migration005 execution error");
                throw;
            }
        }
    }
}
