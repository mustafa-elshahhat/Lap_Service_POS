using System;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Infrastructure.Persistence;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    [Collection("Database")]
    public class CustomerServiceTests : IDisposable
    {
        private readonly CustomerService _service;
        private readonly CustomerRepository _repo;

        public CustomerServiceTests()
        {
            DatabaseManager.Instance.InitializeForTests();
            _repo = new CustomerRepository();
            _service = new CustomerService(_repo);
        }

        public void Dispose() { }

        [Fact]
        public void GetOrCreateCustomer_NullName_ReturnsNull()
        {
            Assert.Null(_service.GetOrCreateCustomer(null, "01001234567"));
        }

        [Fact]
        public void GetOrCreateCustomer_BlankName_ReturnsNull()
        {
            Assert.Null(_service.GetOrCreateCustomer("", "01001234567"));
            Assert.Null(_service.GetOrCreateCustomer("   ", "01001234567"));
        }

        [Fact]
        public void GetOrCreateCustomer_NewPhone_CreatesAndReturnsId()
        {
            var id = _service.GetOrCreateCustomer("عميل جديد", "01001111111");
            Assert.NotNull(id);
            Assert.True(id > 0);

            var customer = _repo.GetById(id.Value);
            Assert.NotNull(customer);
            Assert.Equal("عميل جديد", customer.Name);
            Assert.Equal("01001111111", customer.Phone);
        }

        [Fact]
        public void GetOrCreateCustomer_ExistingPhone_ReturnsExistingId()
        {
            var firstId = _service.GetOrCreateCustomer("عميل أول", "01002222222");
            Assert.NotNull(firstId);

            var secondId = _service.GetOrCreateCustomer("عميل مختلف", "01002222222");
            Assert.NotNull(secondId);
            Assert.Equal(firstId, secondId);
        }

        [Fact]
        public void GetOrCreateCustomer_ExistingPhone_UpdatesNameWhenChanged()
        {
            var id = _service.GetOrCreateCustomer("الاسم القديم", "01003333333");
            Assert.NotNull(id);

            _service.GetOrCreateCustomer("الاسم الجديد", "01003333333");

            var customer = _repo.GetById(id.Value);
            Assert.Equal("الاسم الجديد", customer.Name);
        }

        [Fact]
        public void GetOrCreateCustomer_PhoneWithSpacesDashes_MatchesNormalizedExisting()
        {
            var normalizedPhone = "01004444444";
            _repo.Create(new Customer { Name = "عميل موجود", Phone = normalizedPhone });

            var result = _service.GetOrCreateCustomer("عميل موجود", "0100 444-4444");
            Assert.NotNull(result);

            var customer = _repo.GetById(result.Value);
            Assert.Equal("عميل موجود", customer.Name);
            Assert.Equal(normalizedPhone, customer.Phone);
        }
    }
}
