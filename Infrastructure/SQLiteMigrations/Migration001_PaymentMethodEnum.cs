using CarPartsShopWPF.Infrastructure.Data;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Infrastructure.SQLiteMigrations
{
    public static class Migration001_PaymentMethodEnum
    {
        public static void Execute()
        {
            var db = DatabaseManager.Instance;

            db.Execute(@"
                CREATE TABLE IF NOT EXISTS payment_methods_ref (
                    id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL UNIQUE,
                    display_name_ar TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )");

            db.Execute(@"
                INSERT OR IGNORE INTO payment_methods_ref (id, name, display_name_ar) VALUES
                (1, 'Cash', 'نقدي'),
                (2, 'CreditCard', 'فيزا / كريدت كارد'),
                (3, 'EWallet', 'محافظ إلكترونية'),
                (4, 'Deferred', 'آجل')");

            db.EnsureColumnExists("sales", "payment_method_id", "INTEGER");
            db.EnsureColumnExists("sale_payments", "payment_method_id", "INTEGER");
            db.EnsureColumnExists("credit_payments", "payment_method_id", "INTEGER");
        }
    }
}
