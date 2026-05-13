using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Collections.Generic;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Infrastructure.Data
{
    public class DatabaseManager
    {
        private static DatabaseManager _instance;
        private static readonly object _lock = new object();
        private SQLiteConnection _connection;
        private readonly string _databasePath;
        public SQLiteTransaction CurrentTransaction { get; set; }

        public static DatabaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new DatabaseManager();
                    }
                }
                return _instance;
            }
        }

        private DatabaseManager()
        {
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            _databasePath = Path.Combine(appPath, "car_parts_shop.db");
        }

        public string DatabasePath => _databasePath;

        public void Restore(string backupFilePath)
        {
            lock (_lock)
            {
                Close();
                File.Copy(backupFilePath, _databasePath, true);
                Initialize();
            }
        }

        public void Initialize()
        {
            if (_connection != null && _connection.State == ConnectionState.Open) return;

            string connectionString = $"Data Source={_databasePath};Version=3;";
            _connection = new SQLiteConnection(connectionString);
            _connection.Open();

            Execute("PRAGMA foreign_keys = ON");

            CreateTables();
        }

        public void InitializeForTests(string connectionString = "Data Source=:memory:;Version=3;")
        {
            lock (_lock)
            {
                if (_connection != null)
                {
                    _connection.Close();
                    _connection.Dispose();
                }
                _connection = new SQLiteConnection(connectionString);
                _connection.Open();
                Execute("PRAGMA foreign_keys = ON");
                CreateTables();
            }
        }

        public SQLiteConnection GetConnection()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                Initialize();
            }
            return _connection;
        }

        public SQLiteTransaction BeginTransaction()
        {
            if (CurrentTransaction != null)
            {
                throw new InvalidOperationException("A transaction is already active. Nested transactions are not supported in this manager.");
            }
            CurrentTransaction = GetConnection().BeginTransaction();
            return CurrentTransaction;
        }

        public void CommitTransaction()
        {
            if (CurrentTransaction == null) return;
            try
            {
                CurrentTransaction.Commit();
            }
            finally
            {
                CurrentTransaction.Dispose();
                CurrentTransaction = null;
            }
        }

        public void RollbackTransaction()
        {
            if (CurrentTransaction == null) return;
            try
            {
                CurrentTransaction.Rollback();
            }
            finally
            {
                CurrentTransaction.Dispose();
                CurrentTransaction = null;
            }
        }

        public int Execute(string query, Dictionary<string, object> parameters = null, SQLiteTransaction transaction = null)
        {
            lock (_lock)
            {
                using (var cmd = new SQLiteCommand(query, GetConnection()))
                {
                    cmd.Transaction = transaction ?? CurrentTransaction;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public long ExecuteAndGetId(string query, Dictionary<string, object> parameters = null, SQLiteTransaction transaction = null)
        {
            lock (_lock)
            {
                using (var cmd = new SQLiteCommand(query, GetConnection()))
                {
                    cmd.Transaction = transaction ?? CurrentTransaction;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    cmd.ExecuteNonQuery();
                    return GetConnection().LastInsertRowId;
                }
            }
        }

        public Dictionary<string, object> FetchOne(string query, Dictionary<string, object> parameters = null)
        {
            lock (_lock)
            {
                using (var cmd = new SQLiteCommand(query, GetConnection()))
                {
                    if (CurrentTransaction != null) cmd.Transaction = CurrentTransaction;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            return row;
                        }
                    }
                }
                return null;
            }
        }

        public List<Dictionary<string, object>> FetchAll(string query, Dictionary<string, object> parameters = null)
        {
            lock (_lock)
            {
                var results = new List<Dictionary<string, object>>();
                using (var cmd = new SQLiteCommand(query, GetConnection()))
                {
                    if (CurrentTransaction != null) cmd.Transaction = CurrentTransaction;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            results.Add(row);
                        }
                    }
                }
                return results;
            }
        }

        public object FetchScalar(string query, Dictionary<string, object> parameters = null)
        {
            lock (_lock)
            {
                using (var cmd = new SQLiteCommand(query, GetConnection()))
                {
                    if (CurrentTransaction != null) cmd.Transaction = CurrentTransaction;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    return cmd.ExecuteScalar();
                }
            }
        }

        public string GetSetting(string key, string defaultValue = null)
        {
            var result = FetchOne("SELECT value FROM settings WHERE key = @key",
                new Dictionary<string, object> { { "@key", key } });
            return result != null ? result["value"]?.ToString() : defaultValue;
        }

        public void SetSetting(string key, string value)
        {
            Execute(@"INSERT OR REPLACE INTO settings (key, value, updated_at) 
                      VALUES (@key, @value, datetime('now'))",
                new Dictionary<string, object> 
                { 
                    { "@key", key }, 
                    { "@value", value } 
                });
        }

        public string GenerateInvoiceNumber()
        {
            string prefix = GetSetting("invoice_prefix", "INV");
            string datePart = DateTime.Now.ToString("yyyyMMdd");

            var result = FetchOne(
                @"SELECT invoice_number FROM sales 
                  WHERE invoice_number LIKE @pattern
                  ORDER BY id DESC LIMIT 1",
                new Dictionary<string, object> { { "@pattern", $"{prefix}-{datePart}-%" } });

            int seq = 1;
            if (result != null)
            {
                string lastNumber = result["invoice_number"]?.ToString();
                if (!string.IsNullOrEmpty(lastNumber))
                {
                    string[] parts = lastNumber.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[parts.Length - 1], out int lastSeq))
                    {
                        seq = lastSeq + 1;
                    }
                }
            }

            return $"{prefix}-{datePart}-{seq:D4}";
        }

        public string GenerateReturnNumber()
        {
            string prefix = GetSetting("return_prefix", "RET");
            string datePart = DateTime.Now.ToString("yyyyMMdd");

            var result = FetchOne(
                @"SELECT return_number FROM returns 
                  WHERE return_number LIKE @pattern
                  ORDER BY id DESC LIMIT 1",
                new Dictionary<string, object> { { "@pattern", $"{prefix}-{datePart}-%" } });

            int seq = 1;
            if (result != null)
            {
                string lastNumber = result["return_number"]?.ToString();
                if (!string.IsNullOrEmpty(lastNumber))
                {
                    string[] parts = lastNumber.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[parts.Length - 1], out int lastSeq))
                    {
                        seq = lastSeq + 1;
                    }
                }
            }

            return $"{prefix}-{datePart}-{seq:D4}";
        }

        public string GenerateRepairOrderNumber()
        {
            string prefix = GetSetting("repair_order_prefix", "MNT");
            string datePart = DateTime.Now.ToString("yyyyMMdd");

            var result = FetchOne(
                @"SELECT order_number FROM repair_orders 
                  WHERE order_number LIKE @pattern
                  ORDER BY id DESC LIMIT 1",
                new Dictionary<string, object> { { "@pattern", $"{prefix}-{datePart}-%" } });

            int seq = 1;
            if (result != null)
            {
                string lastNumber = result["order_number"]?.ToString();
                if (!string.IsNullOrEmpty(lastNumber))
                {
                    string[] parts = lastNumber.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[parts.Length - 1], out int lastSeq))
                        seq = lastSeq + 1;
                }
            }

            return $"{prefix}-{datePart}-{seq:D4}";
        }

        public void EnsureSchemaExtended()
        {
             EnsureColumnExists("returns", "payment_method", "TEXT DEFAULT 'نقدي'");
             EnsureColumnExists("sale_items", "paid_amount", "REAL DEFAULT 0");
             EnsureColumnExists("sale_items", "remaining_amount", "REAL DEFAULT 0");
        }

        public void EnsureColumnExists(string tableName, string columnName, string columnDef)
        {
            try
            {
                var schema = FetchAll($"PRAGMA table_info({tableName})");
                bool exists = false;
                foreach (var col in schema)
                {
                    if (SafeConvert.ToString(col["name"]) == columnName)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    Execute($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDef}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Error ensuring column {columnName} in {tableName}");
            }
        }

        public void LogActivity(int userId, string action, string tableName = null, 
            int? recordId = null, string details = null)
        {
            Execute(@"INSERT INTO activity_log (user_id, action, table_name, record_id, details) 
                      VALUES (@userId, @action, @tableName, @recordId, @details)",
                new Dictionary<string, object>
                {
                    { "@userId", userId },
                    { "@action", action },
                    { "@tableName", tableName },
                    { "@recordId", recordId },
                    { "@details", details }
                });
        }

        public string Backup(string backupPath)
        {
            if (!Directory.Exists(backupPath))
                Directory.CreateDirectory(backupPath);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFile = Path.Combine(backupPath, $"backup_{timestamp}.db");

            File.Copy(_databasePath, backupFile, true);
            return backupFile;
        }

        public void Close()
        {
            lock (_lock)
            {
                if (_connection != null)
                {
                    _connection.Close();
                    _connection.Dispose();
                    _connection = null;
                }
            }
        }

        private void CreateTables()
        {
            foreach (string sql in DatabaseSchema.GetCreateTableStatements())
            {
                try
                {
                    Execute(sql);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Error creating table: {sql.Substring(0, Math.Min(sql.Length, 50))}...");
                }
            }

            CreateDefaultAdmin();
            CreateDefaultSettings();
            EnsureSchemaExtended();
        }

        private void CreateDefaultAdmin()
        {
            var count = FetchScalar("SELECT COUNT(*) FROM users");
            if (Convert.ToInt64(count) == 0)
            {
                string passwordHash = Security.HashPassword("admin123");
                Execute(@"INSERT INTO users (username, password_hash, full_name, role, max_discount_percent, max_markup_percent) 
                          VALUES ('admin', @password, 'مدير النظام', 'admin', 100.0, 100.0)",
                    new Dictionary<string, object> { { "@password", passwordHash } });
            }
        }

        private void CreateDefaultSettings()
        {
            var defaultSettings = new Dictionary<string, (string value, string description)>
            {
                { "shop_name", ("الجوهري", "اسم المحل") },
                { "shop_address", ("", "عنوان المحل") },
                { "shop_phone", ("", "رقم هاتف المحل") },
                { "max_discount_percent", ("10.0", "الحد الأقصى للخصم (%)") },
                { "max_markup_percent", ("20.0", "الحد الأقصى للزيادة (%)") },
                { "low_stock_threshold", ("5", "حد التنبيه لانخفاض المخزون") },
                { "invoice_prefix", ("INV", "بادئة رقم الفاتورة") },
            };

            foreach (var setting in defaultSettings)
            {
                Execute(@"INSERT OR IGNORE INTO settings (key, value, description) VALUES (@key, @value, @desc)",
                    new Dictionary<string, object>
                    {
                        { "@key", setting.Key },
                        { "@value", setting.Value.value },
                        { "@desc", setting.Value.description }
                    });
            }

            MigrateBranding();
        }

        private void MigrateBranding()
        {
            var oldDefaults = new[] { "محل قطع غيار السيارات", "الهندسية", "خدمة الصيانة", "نظام إدارة قطع الغيار" };
            foreach (var old in oldDefaults)
            {
                Execute(@"UPDATE settings SET value = 'الجوهري' WHERE key = 'shop_name' AND value = @old",
                    new Dictionary<string, object> { { "@old", old } });
            }
        }
    }
}
