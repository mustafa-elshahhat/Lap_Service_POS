using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Infrastructure.SQLiteMigrations;
using AlJohary.ServiceHub.Shared.Helpers;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    [Collection("Database")]
    public class MigrationTests : IDisposable
    {
        private readonly string _dbPath;

        public MigrationTests()
        {
            _dbPath = DatabaseManager.Instance.DatabasePath;
            var dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            DatabaseManager.Instance.InitializeForTests($"Data Source={_dbPath};");
        }

        public void Dispose()
        {
            DatabaseManager.Instance.InitializeForTests("Data Source=:memory:;");
        }

        private static void RunAllMigrations()
        {
            Migration002_BusinessDateLocalRepair.Execute();
            Migration003_AddCashRefundToReturns.Execute();
            Migration004_RepairOrdersFullSchema.Execute();
            Migration005_DropDeadPaymentMethodSchema.Execute();
            Migration006_RepairPartPurchaseCost.Execute();
            Migration007_Employees.Execute();
            Migration008_SupplierPurchaseItems.Execute();
            Migration009_PaymentMethodNormalization.Execute();
            Migration010_ExpenseSoftDelete.Execute();
        }

        [Fact]
        public void AllMigrations_RunTwice_SecondRunDoesNotThrow()
        {
            RunAllMigrations();
            RunAllMigrations();
        }

        [Fact]
        public void AllMigrations_RunTwice_SchemaRemainsStable()
        {
            RunAllMigrations();
            RunAllMigrations();

            var db = DatabaseManager.Instance;

            var cols = db.FetchAll("PRAGMA table_info(sales)");
            var colNames = cols.Select(c => SafeConvert.ToString(c["name"])).ToHashSet();
            Assert.Contains("subtotal", colNames);
            Assert.Contains("discount_amount", colNames);
            Assert.Contains("total_amount", colNames);
            Assert.Contains("profit", colNames);

            cols = db.FetchAll("PRAGMA table_info(sale_items)");
            colNames = cols.Select(c => SafeConvert.ToString(c["name"])).ToHashSet();
            Assert.Contains("paid_amount", colNames);
            Assert.Contains("remaining_amount", colNames);

            cols = db.FetchAll("PRAGMA table_info(expenses)");
            colNames = cols.Select(c => SafeConvert.ToString(c["name"])).ToHashSet();
            Assert.Contains("is_deleted", colNames);
            Assert.Contains("deleted_at", colNames);
            Assert.Contains("deleted_by", colNames);
        }

        [Fact]
        public void EnsureColumnExists_AlreadyExistingColumn_IsNoOp()
        {
            RunAllMigrations();
            DatabaseManager.Instance.EnsureColumnExists("expenses", "is_deleted", "INTEGER NOT NULL DEFAULT 0");
        }

        [Fact]
        public void EnsureColumnExists_ValidNewColumn_Succeeds()
        {
            RunAllMigrations();
            var db = DatabaseManager.Instance;
            db.EnsureColumnExists("sales", "_test_temp_col", "TEXT");
            var cols = db.FetchAll("PRAGMA table_info(sales)");
            Assert.Contains(cols, c => SafeConvert.ToString(c["name"]) == "_test_temp_col");
        }

        [Fact]
        public void EnsureColumnExists_NonexistentTable_Throws()
        {
            RunAllMigrations();
            Assert.ThrowsAny<Exception>(() =>
                DatabaseManager.Instance.EnsureColumnExists("nonexistent_table", "col", "TEXT"));
        }

        [Fact]
        public void EnsureSchemaExtended_AfterMigrations_RunsWithoutError()
        {
            RunAllMigrations();
            DatabaseManager.Instance.EnsureSchemaExtended();
        }
    }
}
