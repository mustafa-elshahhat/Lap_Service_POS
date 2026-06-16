using System;
using System.Collections.Generic;
using System.IO;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Infrastructure.Persistence;
using AlJohary.ServiceHub.Shared.Helpers;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    [Collection("Database")]
    public class ForcePasswordChangeTests : IDisposable
    {
        public ForcePasswordChangeTests()
        {
            DatabaseManager.Instance.InitializeForTests();
        }

        public void Dispose() { }

        [Fact]
        public void SeededAdmin_HasForcePasswordChangeTrue()
        {
            var db = DatabaseManager.Instance;
            string val = db.GetSetting("force_password_change");
            Assert.Equal("true", val);
        }

        [Fact]
        public void IsForcePasswordChangeRequired_ReturnsTrue_ForAdmin()
        {
            var auth = new AuthService(new UserRepository());
            auth.SetSession(new Dictionary<string, object>
            {
                { "id", 1 },
                { "username", "admin" },
                { "full_name", "مدير النظام" },
                { "role", "admin" },
                { "max_discount_percent", 100.0 },
                { "max_markup_percent", 100.0 },
                { "is_active", true }
            });

            Assert.True(auth.IsForcePasswordChangeRequired());
        }

        [Fact]
        public void AfterChangePasswordAndClearFlag_IsForcePasswordChangeRequired_ReturnsFalse()
        {
            var auth = new AuthService(new UserRepository());
            auth.SetSession(new Dictionary<string, object>
            {
                { "id", 1 },
                { "username", "admin" },
                { "full_name", "مدير النظام" },
                { "role", "admin" },
                { "max_discount_percent", 100.0 },
                { "max_markup_percent", 100.0 },
                { "is_active", true }
            });

            auth.ChangeUserPassword(1, "newPassword123");
            auth.ClearForcePasswordChangeFlag();

            Assert.False(auth.IsForcePasswordChangeRequired());
        }

        [Fact]
        public void IsForcePasswordChangeRequired_ReturnsFalse_ForNonAdmin()
        {
            var auth = new AuthService(new UserRepository());
            auth.SetSession(new Dictionary<string, object>
            {
                { "id", 2 },
                { "username", "employee" },
                { "full_name", "موظف" },
                { "role", "employee" },
                { "max_discount_percent", 10.0 },
                { "max_markup_percent", 20.0 },
                { "is_active", true }
            });

            Assert.False(auth.IsForcePasswordChangeRequired());
        }
    }

    // N-2: contextual seeding of force_password_change. Uses a file-backed DB so startup seeding
    // can be re-run on a database that already has data (simulating an existing install). The flag
    // must only force a change when the default credential actually still exists.
    [Collection("Database")]
    public class ForcePasswordChangeSeedingTests : IDisposable
    {
        private readonly string _dbPath;

        // Unique file per test + Pooling=False so each test starts from a genuinely fresh file
        // and the file handle is released between re-initializations (no stale pooled connection).
        private string ConnStr => $"Data Source={_dbPath};Pooling=False";

        public ForcePasswordChangeSeedingTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"fpc_seed_{Guid.NewGuid():N}.db");
            DatabaseManager.Instance.InitializeForTests(ConnStr);
        }

        public void Dispose()
        {
            try { DatabaseManager.Instance.Close(); } catch { }
            try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
            DatabaseManager.Instance.InitializeForTests("Data Source=:memory:;");
        }

        private static AuthService NewAdminAuth()
        {
            var auth = new AuthService(new UserRepository());
            auth.SetSession(new Dictionary<string, object>
            {
                { "id", 1 },
                { "username", "admin" },
                { "full_name", "مدير النظام" },
                { "role", "admin" },
                { "max_discount_percent", 100.0 },
                { "max_markup_percent", 100.0 },
                { "is_active", true }
            });
            return auth;
        }

        [Fact]
        public void FreshInstall_DefaultAdmin_ForcePasswordChangeTrue()
        {
            var db = DatabaseManager.Instance;
            Assert.Equal("true", db.GetSetting("force_password_change"));
            Assert.True(NewAdminAuth().IsForcePasswordChangeRequired());
        }

        [Fact]
        public void ExistingInstall_DefaultPassword_MissingSetting_ReseededTrue()
        {
            var db = DatabaseManager.Instance;
            // Simulate a pre-flag install that still uses admin123: the setting does not exist yet.
            db.Execute("DELETE FROM settings WHERE key = 'force_password_change'");
            Assert.Null(db.GetSetting("force_password_change"));

            // Re-run startup seeding on the same (existing) database.
            db.InitializeForTests(ConnStr);

            Assert.Equal("true", db.GetSetting("force_password_change"));
        }

        [Fact]
        public void ExistingInstall_CustomPassword_MissingSetting_NotReprompted()
        {
            var db = DatabaseManager.Instance;
            // Admin already moved off the default credential before the flag existed.
            db.Execute("UPDATE users SET password_hash = @h WHERE username = 'admin'",
                new Dictionary<string, object> { { "@h", Security.HashPassword("myCustomPass1") } });
            db.Execute("DELETE FROM settings WHERE key = 'force_password_change'");

            // Re-run startup seeding on the same (existing) database.
            db.InitializeForTests(ConnStr);

            Assert.Equal("false", db.GetSetting("force_password_change"));
            Assert.False(NewAdminAuth().IsForcePasswordChangeRequired());
        }

        [Fact]
        public void ExistingInstall_SettingAlreadyPresent_NotOverwritten()
        {
            var db = DatabaseManager.Instance;
            // An explicit prior decision (false) must survive a restart even on the default password.
            db.SetSetting("force_password_change", "false");

            db.InitializeForTests(ConnStr);

            Assert.Equal("false", db.GetSetting("force_password_change"));
        }
    }
}
