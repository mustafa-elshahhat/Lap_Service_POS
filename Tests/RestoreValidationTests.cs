using System;
using System.IO;
using System.Linq;
using AlJohary.ServiceHub.Infrastructure.Data;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    [Collection("Database")]
    public class RestoreValidationTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _validBackupPath;
        private readonly string _invalidNoTablesPath;
        private readonly string _invalidCorruptPath;
        private readonly string _databasePath;

        public RestoreValidationTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"restore_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);

            _validBackupPath = Path.Combine(_tempDir, "valid_backup.db");
            _invalidNoTablesPath = Path.Combine(_tempDir, "no_tables.db");
            _invalidCorruptPath = Path.Combine(_tempDir, "corrupt.db");
            _databasePath = DatabaseManager.Instance.DatabasePath;

            CreateValidBackup();
            CreateInvalidBackups();
        }

        private void CreateValidBackup()
        {
            DatabaseManager.Instance.InitializeForTests($"Data Source={_validBackupPath};");

            var db = DatabaseManager.Instance;
            var cnt = db.FetchAll("SELECT COUNT(*) AS cnt FROM settings");
            if (cnt.Count == 0 || (long)cnt[0]["cnt"] == 0)
            {
                db.Execute("INSERT INTO settings(key, value) VALUES('version', 'test')");
            }

            DatabaseManager.Instance.Close();
        }

        private void CreateInvalidBackups()
        {
            File.WriteAllBytes(_invalidCorruptPath, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });

            DatabaseManager.Instance.InitializeForTests($"Data Source={_invalidNoTablesPath};");
            DatabaseManager.Instance.Execute("DROP TABLE IF EXISTS settings");
            DatabaseManager.Instance.Close();
        }

        public void Dispose()
        {
            DatabaseManager.Instance.InitializeForTests("Data Source=:memory:;");

            try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); }
            catch { }
        }

        [Fact]
        public void Restore_ValidBackup_Succeeds()
        {
            DatabaseManager.Instance.InitializeForTests($"Data Source={_databasePath};");
            DatabaseManager.Instance.Restore(_validBackupPath);
        }

        [Fact]
        public void Restore_NonExistentPath_Throws()
        {
            DatabaseManager.Instance.InitializeForTests($"Data Source={_databasePath};");
            var bad = Path.Combine(_tempDir, "nonexistent.db");
            Assert.Throws<System.IO.FileNotFoundException>(() =>
                DatabaseManager.Instance.Restore(bad));
        }

        [Fact]
        public void Restore_CorruptFile_Throws()
        {
            DatabaseManager.Instance.InitializeForTests($"Data Source={_databasePath};");
            Assert.Throws<InvalidDataException>(() =>
                DatabaseManager.Instance.Restore(_invalidCorruptPath));
        }

        [Fact]
        public void Restore_MissingRequiredTables_Throws()
        {
            DatabaseManager.Instance.InitializeForTests($"Data Source={_databasePath};");
            Assert.Throws<InvalidDataException>(() =>
                DatabaseManager.Instance.Restore(_invalidNoTablesPath));
        }

        [Fact]
        public void Restore_CreatesSafetyCopyBeforeRestore()
        {
            DatabaseManager.Instance.InitializeForTests($"Data Source={_databasePath};");
            DatabaseManager.Instance.Restore(_validBackupPath);

            var dbDir = Path.GetDirectoryName(_databasePath);
            var safetyFiles = Directory.GetFiles(dbDir, "backup_*.db");
            Assert.NotEmpty(safetyFiles);
        }
    }
}
