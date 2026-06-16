using System;
using System.Data;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Collections.Generic;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.Data
{
    public class DatabaseManager
    {
        private static DatabaseManager _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Exposed for use by extracted helper classes (SqlExecutor, etc.) that share
        /// the singleton's lock to serialize all database access.
        /// </summary>
        public static object InstanceLock => _lock;
        private SqliteConnection _connection;
        private readonly string _databasePath;

        private readonly SqlExecutor _sql;
        private readonly SettingsManager _settings;
        private readonly NumberGenerator _numberGen;

        /// <summary>
        /// Gets or sets the current active transaction for the entire process.
        /// This is a SINGLE-THREADED, SINGLE-TRANSACTION model: only one logical
        /// operation may hold a transaction at a time. All repositories share this
        /// instance via <see cref="Instance"/> and auto-enlist in queries.
        /// Nested/cross-operation transactions are NOT supported — they will throw.
        /// This design is safe for single-operator desktop use. Before any
        /// multi-user / networked deployment, replace with connection-per-unit-of-work.
        /// </summary>
        public SqliteTransaction CurrentTransaction { get; set; }

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
            _sql = new SqlExecutor(this);
            _settings = new SettingsManager(_sql);
            _numberGen = new NumberGenerator(_sql, _settings);
            try
            {
                string appPath = AppContext.BaseDirectory;
                if (string.IsNullOrEmpty(appPath))
                {
                    appPath = AppDomain.CurrentDomain.BaseDirectory;
                }
                
                if (string.IsNullOrEmpty(appPath))
                {
                    appPath = Directory.GetCurrentDirectory();
                }

                // Ensure appPath is an absolute path
                appPath = Path.GetFullPath(appPath);

                // Set DataDirectory as some libraries use it
                AppDomain.CurrentDomain.SetData("DataDirectory", appPath);

                _databasePath = Path.Combine(appPath, "aljohary_service_hub.db");

                // Ensure the directory exists
                string dir = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "DatabaseManager Constructor");
                throw;
            }
        }

        public string DatabasePath => _databasePath;

        public void Restore(string backupFilePath)
        {
            lock (_lock)
            {
                if (!File.Exists(backupFilePath))
                    throw new FileNotFoundException("ملف النسخة الاحتياطية غير موجود", backupFilePath);

                ValidateDatabaseFile(backupFilePath);

                string safetyPath = Backup(Path.GetDirectoryName(_databasePath));
                Logger.LogInfo($"Pre-restore safety copy saved to {safetyPath}");

                Close();
                File.Copy(backupFilePath, _databasePath, true);
                Initialize();
            }
        }

        public void Initialize()
        {
            if (_connection != null && _connection.State == ConnectionState.Open) return;

            if (string.IsNullOrEmpty(_databasePath))
            {
                throw new InvalidOperationException("Database path was not initialized properly.");
            }

            try
            {
                string connectionString = $"Data Source={_databasePath};";
                _connection = new SqliteConnection(connectionString);
                _connection.Open();

                Execute("PRAGMA foreign_keys = ON");
                CreateTables();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "DatabaseManager Initialize");
                throw;
            }
        }

        public void InitializeForTests(string connectionString = "Data Source=:memory:;")
        {
            lock (_lock)
            {
                if (_connection != null)
                {
                    _connection.Close();
                    _connection.Dispose();
                }
                _connection = new SqliteConnection(connectionString);
                _connection.Open();
                Execute("PRAGMA foreign_keys = ON");
                CreateTables();
            }
        }

        public SqliteConnection GetConnection()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                Initialize();
            }
            return _connection;
        }

        public SqliteTransaction BeginTransaction()
        {
            if (CurrentTransaction != null)
            {
                throw new InvalidOperationException(
                    "A transaction is already active. Nested/concurrent transactions are not supported. " +
                    "This manager uses a single process-wide transaction model; overlapping operations " +
                    "must not begin their own transaction while another is in flight.");
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

        public int Execute(string query, Dictionary<string, object> parameters = null, SqliteTransaction transaction = null)
            => _sql.Execute(query, parameters, transaction);

        public long ExecuteAndGetId(string query, Dictionary<string, object> parameters = null, SqliteTransaction transaction = null)
            => _sql.ExecuteAndGetId(query, parameters, transaction);

        public Dictionary<string, object> FetchOne(string query, Dictionary<string, object> parameters = null)
            => _sql.FetchOne(query, parameters);

        public List<Dictionary<string, object>> FetchAll(string query, Dictionary<string, object> parameters = null)
            => _sql.FetchAll(query, parameters);

        public object FetchScalar(string query, Dictionary<string, object> parameters = null)
            => _sql.FetchScalar(query, parameters);

        public string GetSetting(string key, string defaultValue = null)
            => _settings.GetSetting(key, defaultValue);

        public void SetSetting(string key, string value)
            => _settings.SetSetting(key, value);

        public string GenerateInvoiceNumber()
            => _numberGen.GenerateInvoiceNumber();

        public string GenerateReturnNumber()
            => _numberGen.GenerateReturnNumber();

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
                throw;
            }
        }

        private void ValidateDatabaseFile(string filePath)
        {
            try
            {
                using (var testConn = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly;"))
                {
                    testConn.Open();
                    using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='settings'", testConn))
                    {
                        long count = (long)cmd.ExecuteScalar();
                        if (count == 0)
                            throw new InvalidDataException("ملف الاستعادة لا يحتوي على بنية قاعدة البيانات المطلوبة (settings)");
                    }
                    using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM settings", testConn))
                    {
                        cmd.ExecuteScalar();
                    }
                }
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"ملف الاستعادة غير صالح أو تالف: {ex.Message}", ex);
            }
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
                { "shop_phones", ("", "أرقام هاتف المحل") },
                { "max_discount_percent", ("10.0", "الحد الأقصى للخصم (%)") },
                { "max_markup_percent", ("20.0", "الحد الأقصى للزيادة (%)") },
                { "low_stock_threshold", ("5", "حد التنبيه لانخفاض المخزون") },
                { "invoice_prefix", ("INV", "بادئة رقم الفاتورة") },
                { "force_password_change", ("true", "فرض تغيير كلمة المرور للمسؤول الافتراضي") },
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
