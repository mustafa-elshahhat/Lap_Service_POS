using System;
using System.Collections.Generic;
using System.IO;
using CarPartsShopWPF.Infrastructure.Data;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Infrastructure.SQLiteMigrations
{
    public static class Migration002_BusinessDateLocalRepair
    {
        public static void Execute()
        {
            var db = DatabaseManager.Instance;

            if (db.GetSetting("business_dates_local_repair_applied") == "true")
                return;

            try
            {
                string appPath = AppDomain.CurrentDomain.BaseDirectory;
                string backupDir = Path.Combine(appPath, "backups", "migrations");
                string backupFile = db.Backup(backupDir);
                Logger.LogInfo($"Migration002: Safety backup created at {backupFile}");

                TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
                if (offset == TimeSpan.Zero)
                {
                    db.SetSetting("business_dates_local_repair_applied", "true");
                    return;
                }

                var sales = db.FetchAll("SELECT id, invoice_number, sale_date FROM sales");
                int salesRepaired = 0;
                int relatedPaymentsRepaired = 0;
                int relatedReturnsRepaired = 0;
                int relatedHistoryRepaired = 0;

                foreach (var sale in sales)
                {
                    long saleId = SafeConvert.ToLong(sale["id"]);
                    string inv = SafeConvert.ToString(sale["invoice_number"]);
                    DateTime? sDate = SafeConvert.ToDateTime(sale["sale_date"]);

                    if (sDate.HasValue && !string.IsNullOrEmpty(inv))
                    {
                        string[] parts = inv.Split('-');
                        if (parts.Length >= 2 && parts[1].Length == 8 &&
                            DateTime.TryParseExact(parts[1], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime invDate))
                        {
                            if (sDate.Value.Date < invDate.Date)
                            {
                                DateTime repairedSaleDate = sDate.Value.Add(offset);

                                if (repairedSaleDate.Date != invDate.Date)
                                {
                                    repairedSaleDate = invDate.Date.Add(sDate.Value.TimeOfDay);
                                }

                                db.Execute("UPDATE sales SET sale_date = @d WHERE id = @id",
                                    new Dictionary<string, object> { { "@d", repairedSaleDate.ToString("yyyy-MM-dd HH:mm:ss") }, { "@id", saleId } });
                                salesRepaired++;

                                relatedPaymentsRepaired += RepairLinkedRecords(db, "sale_payments", "payment_date", "sale_id", saleId, offset, sDate.Value);
                                relatedReturnsRepaired += RepairLinkedRecords(db, "returns", "return_date", "sale_id", saleId, offset, sDate.Value);
                                relatedHistoryRepaired += RepairLinkedRecords(db, "payment_history", "payment_date", "sale_id", saleId, offset, sDate.Value);
                            }
                        }
                    }
                }

                int standaloneExpenses = RepairStandaloneRecords(db, "expenses", "expense_date", offset);
                int standaloneSupplier = RepairStandaloneRecords(db, "supplier_transactions", "transaction_date", offset);

                db.SetSetting("business_dates_local_repair_applied", "true");

                Logger.LogInfo("Migration002: Business date repair completed.");
                Logger.LogInfo($"- Sales Repaired: {salesRepaired}");
                Logger.LogInfo($"- Linked Payments Repaired: {relatedPaymentsRepaired}");
                Logger.LogInfo($"- Linked Returns Repaired: {relatedReturnsRepaired}");
                Logger.LogInfo($"- Linked Payment History Repaired: {relatedHistoryRepaired}");
                Logger.LogInfo($"- Standalone Expenses Repaired: {standaloneExpenses}");
                Logger.LogInfo($"- Standalone Supplier Transactions Repaired: {standaloneSupplier}");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Migration002 execution error");
                throw;
            }
        }

        private static int RepairLinkedRecords(DatabaseManager db, string table, string column, string fkColumn, long fkValue, TimeSpan offset, DateTime originalSaleDate)
        {
            int count = 0;
            var rows = db.FetchAll($"SELECT id, {column} FROM {table} WHERE {fkColumn} = @fk", new Dictionary<string, object> { { "@fk", fkValue } });
            foreach (var row in rows)
            {
                long id = SafeConvert.ToLong(row["id"]);
                DateTime? date = SafeConvert.ToDateTime(row[column]);
                if (date.HasValue && date.Value.Date == originalSaleDate.Date)
                {
                    DateTime repaired = date.Value.Add(offset);
                    db.Execute($"UPDATE {table} SET {column} = @d WHERE id = @id",
                        new Dictionary<string, object> { { "@d", repaired.ToString("yyyy-MM-dd HH:mm:ss") }, { "@id", id } });
                    count++;
                }
            }
            return count;
        }

        private static int RepairStandaloneRecords(DatabaseManager db, string table, string column, TimeSpan offset)
        {
            int repairedCount = 0;
            int skippedCount = 0;
            
            var rows = db.FetchAll($"SELECT id, {column} FROM {table}");
            foreach (var row in rows)
            {
                long id = SafeConvert.ToLong(row["id"]);
                DateTime? date = SafeConvert.ToDateTime(row[column]);
                
                if (date.HasValue)
                {
                    Logger.LogInfo($"Migration002: Skipping standalone record in {table} (ID: {id}, Date: {date.Value}). No reliable marker found to verify UTC storage.");
                    skippedCount++;
                }
            }
            
            Logger.LogInfo($"Migration002: {table} summary - Repaired: {repairedCount}, Skipped: {skippedCount}");
            return repairedCount;
        }
    }
}
