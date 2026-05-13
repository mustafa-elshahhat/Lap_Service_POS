using System;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Infrastructure.Data;
using CarPartsShopWPF.Infrastructure.Persistence;
using Xunit;

namespace CarPartsShopWPF.Tests
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
    }
}
