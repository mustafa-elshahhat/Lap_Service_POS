using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Infrastructure.Persistence;
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
}
