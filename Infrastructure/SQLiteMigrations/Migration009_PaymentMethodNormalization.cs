using System;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.SQLiteMigrations
{
    /// <summary>
    /// Migration009: Folds the legacy non-canonical cash payment-method literal "كاش" into the
    /// canonical PaymentMethods.Cash ("نقدي") on every money table where it is a *payment method*,
    /// and adds the reporting indexes that were missing for the financial-flow queries.
    ///
    /// IMPORTANT:
    /// - This NEVER touches employee_salary_transactions: deduction rows intentionally carry a NULL
    ///   payment_method (a deduction is a cost reducer, not a cash movement) and must stay NULL.
    /// - The literal "كاش" used as a *sale-type label* (Utilities.GetSaleTypeArabic /
    ///   CustomerInvoicesViewModel) is unaffected — only the payment_method columns are updated.
    /// - Idempotent: the UPDATEs are no-ops when no legacy rows exist; indexes use IF NOT EXISTS.
    /// </summary>
    public static class Migration009_PaymentMethodNormalization
    {
        public static void Execute()
        {
            var db = DatabaseManager.Instance;
            if (db.GetSetting("migration009_applied", "false") == "true") return;

            try
            {
                // Defensive: returns.payment_method is written by SaleRepository.CreateReturn but was
                // historically absent from the base schema on some installs. Ensure it exists before
                // the refund-outflow breakdown query (ReportRepository) relies on it.
                db.EnsureColumnExists("returns", "payment_method", "TEXT");

                // Fold legacy cash variant "كاش" -> canonical "نقدي" wherever it is a payment method.
                // Literals are hard-coded (not user input) so inline string SQL is safe here.
                db.Execute("UPDATE sales SET payment_method = 'نقدي' WHERE payment_method = 'كاش'");
                db.Execute("UPDATE sale_payments SET payment_method = 'نقدي' WHERE payment_method = 'كاش'");
                db.Execute("UPDATE expenses SET payment_method = 'نقدي' WHERE payment_method = 'كاش'");
                db.Execute("UPDATE supplier_transactions SET payment_method = 'نقدي' WHERE payment_method = 'كاش' AND transaction_type = 'payment'");
                db.Execute("UPDATE repair_payments SET payment_method = 'نقدي' WHERE payment_method = 'كاش'");
                db.Execute("UPDATE returns SET payment_method = 'نقدي' WHERE payment_method = 'كاش'");

                // Reporting indexes that were previously missing (financial-flow queries).
                db.Execute("CREATE INDEX IF NOT EXISTS idx_sale_payments_date ON sale_payments(payment_date)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_sale_payments_sale ON sale_payments(sale_id)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_returns_date ON returns(return_date)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_return_items_return ON return_items(return_id)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_return_items_sale_item ON return_items(sale_item_id)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_repair_payments_date ON repair_payments(payment_date)");

                db.SetSetting("migration009_applied", "true");
                Logger.LogInfo("Migration009: Payment-method normalization and reporting indexes applied.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Migration009 execution error");
                throw;
            }
        }
    }
}
