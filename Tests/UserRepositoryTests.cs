using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Infrastructure.Persistence;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    [Collection("Database")]
    public class UserRepositoryTests : IDisposable
    {
        public UserRepositoryTests()
        {
            DatabaseManager.Instance.InitializeForTests();
        }

        public void Dispose() { }

        [Fact]
        public void Update_WithSentinelDiscounts_PreservesExistingDiscountValues()
        {
            var db = DatabaseManager.Instance;
            long userId = db.ExecuteAndGetId(@"
                INSERT INTO users (username, password_hash, full_name, role, max_discount_percent, max_markup_percent, is_active)
                VALUES ('emp', 'hash', 'Original', 'employee', 15.0, 25.0, 1)");

            var repo = new UserRepository();

            var patch = new User
            {
                Id                 = (int)userId,
                FullName           = "New Name",
                MaxDiscountPercent = -1.0,
                MaxMarkupPercent   = -1.0
            };
            repo.Update(patch);

            var updated = repo.GetById((int)userId);
            Assert.Equal("New Name", updated.FullName);
            Assert.Equal(15.0, updated.MaxDiscountPercent);
            Assert.Equal(25.0, updated.MaxMarkupPercent);
        }

        [Fact]
        public void Update_WithExplicitZeroDiscounts_SetsDiscountToZero()
        {
            var db = DatabaseManager.Instance;
            long userId = db.ExecuteAndGetId(@"
                INSERT INTO users (username, password_hash, full_name, role, max_discount_percent, max_markup_percent, is_active)
                VALUES ('emp2', 'hash', 'Test', 'employee', 10.0, 20.0, 1)");

            var repo = new UserRepository();

            var patch = new User
            {
                Id                 = (int)userId,
                MaxDiscountPercent = 0.0,
                MaxMarkupPercent   = 0.0
            };
            repo.Update(patch);

            var updated = repo.GetById((int)userId);
            Assert.Equal(0.0, updated.MaxDiscountPercent);
            Assert.Equal(0.0, updated.MaxMarkupPercent);
        }

        [Fact]
        public void CreateUser_WithEmployeeLink_PersistsEmployeeIdAndName()
        {
            var db = DatabaseManager.Instance;
            long employeeId = db.ExecuteAndGetId(@"
                INSERT INTO employees (full_name, base_salary, is_active)
                VALUES ('موظف مرتبط', 3000, 1)");

            var auth = CreateAdminAuthService();

            long userId = auth.CreateUser("linked", "pass123", "حساب مرتبط", "employee", 10, 20, (int)employeeId);

            var user = new UserRepository().GetById((int)userId);

            Assert.Equal((int)employeeId, user.EmployeeId);
            Assert.Equal("موظف مرتبط", user.EmployeeName);
        }

        [Fact]
        public void CreateUser_PreventsSameEmployeeLinkedToMoreThanOneActiveUser()
        {
            var db = DatabaseManager.Instance;
            long employeeId = db.ExecuteAndGetId(@"
                INSERT INTO employees (full_name, base_salary, is_active)
                VALUES ('موظف مكرر', 3000, 1)");

            var auth = CreateAdminAuthService();
            auth.CreateUser("linked1", "pass123", "الحساب الأول", "employee", 10, 20, (int)employeeId);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                auth.CreateUser("linked2", "pass123", "الحساب الثاني", "employee", 10, 20, (int)employeeId));

            Assert.Contains("مرتبط بالفعل", ex.Message);
        }

        private static AuthService CreateAdminAuthService()
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
    }
}
