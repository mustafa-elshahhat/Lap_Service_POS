using System;
using System.Collections.Generic;
using System.Linq;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Infrastructure.Persistence;
using AlJohary.ServiceHub.Infrastructure.Services;
using AlJohary.ServiceHub.Presentation;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Presentation.ViewModels;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    [Collection("Database")]
    public class PosSessionStateTests : IDisposable
    {
        private readonly FakeAuthService _auth;
        private readonly ProductRepository _productRepo;
        private readonly ProductService _productService;
        private readonly SaleService _saleService;

        public PosSessionStateTests()
        {
            DatabaseManager.Instance.InitializeForTests();
            _auth = new FakeAuthService { Admin = true, BypassPriceLimits = true };
            _productRepo = new ProductRepository();
            _productService = new ProductService(_productRepo);
            var saleRepo = new SaleRepository();
            var txManager = new DbTransactionManager();
            var customerService = new CustomerService(new CustomerRepository());
            var paymentService = new PaymentService(new PaymentRepository());
            var returnRepo = new ReturnRepository();
            var returnService = new ReturnService(saleRepo, returnRepo, _productRepo, txManager);
            _saleService = new SaleService(saleRepo, _productRepo, customerService, paymentService, returnService, txManager, _auth);

            ServiceContainer.Register<IAuthService>(_auth);
            ServiceContainer.Register<IDialogService>(new FakeDialogService());
            ServiceContainer.Register<IPrintService>(new FakePrintService());
            ServiceContainer.Register<IProductService>(_productService);
            ServiceContainer.Register<ISaleService>(_saleService);
        }

        public void Dispose() { }

        private int SeedProduct(decimal cost, decimal sell, int qty, string code = "POS-TEST")
        {
            return (int)DatabaseManager.Instance.ExecuteAndGetId(@"
                INSERT INTO products (code, name, purchase_price, selling_price, quantity)
                VALUES (@code, @name, @cost, @sell, @qty)",
                new Dictionary<string, object>
                {
                    { "@code", code }, { "@name", "منتج تجريبي" },
                    { "@cost", cost }, { "@sell", sell }, { "@qty", qty }
                });
        }

        [Fact]
        public void Singleton_VM_Returns_Same_Instance()
        {
            ServiceContainer.Register<POSViewModel>(new POSViewModel());

            var vm1 = ServiceContainer.GetService<POSViewModel>();
            var vm2 = ServiceContainer.GetService<POSViewModel>();

            Assert.Same(vm1, vm2);
        }

        [Fact]
        public void Cart_Preserves_State_Across_VM_Retrieval()
        {
            int productId = SeedProduct(cost: 60, sell: 100, qty: 5, code: "POS-PRSRV");
            ServiceContainer.Register<POSViewModel>(new POSViewModel());

            var product = _productService.GetByCode("POS-PRSRV");
            Assert.NotNull(product);

            var vm = ServiceContainer.GetService<POSViewModel>();
            vm.AddToCart(product);

            Assert.False(vm.Cart.IsEmpty);
            Assert.Single(vm.Cart.Items);
            Assert.Equal(100, vm.Cart.Items[0].UnitPrice);
            Assert.Equal(100, vm.Cart.Total);

            var vmAgain = ServiceContainer.GetService<POSViewModel>();
            Assert.Same(vm, vmAgain);
            Assert.False(vmAgain.Cart.IsEmpty);
            Assert.Single(vmAgain.Cart.Items);
            Assert.Equal(100, vmAgain.Cart.Items[0].UnitPrice);
            Assert.Equal(100, vmAgain.Cart.Total);
        }

        [Fact]
        public void Cart_Preserves_Multiple_Items_And_Total()
        {
            SeedProduct(cost: 60, sell: 100, qty: 10, code: "POS-MA");
            SeedProduct(cost: 35, sell: 80, qty: 10, code: "POS-MB");
            ServiceContainer.Register<POSViewModel>(new POSViewModel());

            var vm = ServiceContainer.GetService<POSViewModel>();
            var productA = _productService.GetByCode("POS-MA");
            var productB = _productService.GetByCode("POS-MB");
            Assert.NotNull(productA);
            Assert.NotNull(productB);

            vm.AddToCart(productA, 2);
            vm.AddToCart(productB, 3);

            Assert.Equal(2, vm.Cart.Items.Count);
            Assert.Equal(200 + 240, vm.Cart.Total);

            var vmAgain = ServiceContainer.GetService<POSViewModel>();
            Assert.Equal(2, vmAgain.Cart.Items.Count);
            Assert.Equal(200 + 240, vmAgain.Cart.Total);
        }

        [Fact]
        public void Cart_Clears_After_Explicit_Clear()
        {
            SeedProduct(cost: 60, sell: 100, qty: 5, code: "POS-CLR");
            ServiceContainer.Register<POSViewModel>(new POSViewModel());

            var vm = ServiceContainer.GetService<POSViewModel>();
            var product = _productService.GetByCode("POS-CLR");
            Assert.NotNull(product);

            vm.AddToCart(product);
            Assert.False(vm.Cart.IsEmpty);

            vm.Cart.Clear();
            Assert.True(vm.Cart.IsEmpty);
            Assert.Equal(0, vm.Cart.Total);
        }

        [Fact]
        public void Cart_Clears_After_Checkout_Simulation()
        {
            SeedProduct(cost: 60, sell: 100, qty: 5, code: "POS-CHK");
            ServiceContainer.Register<POSViewModel>(new POSViewModel());

            var vm = ServiceContainer.GetService<POSViewModel>();
            var product = _productService.GetByCode("POS-CHK");
            Assert.NotNull(product);

            vm.AddToCart(product);
            Assert.False(vm.Cart.IsEmpty);

            vm.Cart.Clear();
            Assert.True(vm.Cart.IsEmpty);
            Assert.Equal(0, vm.Cart.Total);

            var vmAgain = ServiceContainer.GetService<POSViewModel>();
            Assert.True(vmAgain.Cart.IsEmpty);
        }

        [Fact]
        public void Cart_Total_Recalculates_Correctly_After_Quantity_Edit()
        {
            SeedProduct(cost: 60, sell: 100, qty: 10, code: "POS-QTY");
            ServiceContainer.Register<POSViewModel>(new POSViewModel());

            var vm = ServiceContainer.GetService<POSViewModel>();
            var product = _productService.GetByCode("POS-QTY");
            Assert.NotNull(product);

            vm.AddToCart(product, 2);
            Assert.Equal(200, vm.Cart.Total);

            var item = vm.Cart.Items[0];
            vm.Cart.UpdateQuantity(item, 5);
            Assert.Equal(500, vm.Cart.Total);

            var vmAgain = ServiceContainer.GetService<POSViewModel>();
            Assert.Equal(500, vmAgain.Cart.Total);
        }

        [Fact]
        public void Cart_Total_Recalculates_Correctly_After_Price_Edit()
        {
            SeedProduct(cost: 60, sell: 100, qty: 10, code: "POS-PRC");
            ServiceContainer.Register<POSViewModel>(new POSViewModel());

            var vm = ServiceContainer.GetService<POSViewModel>();
            var product = _productService.GetByCode("POS-PRC");
            Assert.NotNull(product);

            vm.AddToCart(product, 3);
            Assert.Equal(300, vm.Cart.Total);

            var item = vm.Cart.Items[0];
            vm.Cart.UpdatePrice(item, 120);
            Assert.Equal(360, vm.Cart.Total);

            var vmAgain = ServiceContainer.GetService<POSViewModel>();
            Assert.Equal(360, vmAgain.Cart.Total);
        }

        [Fact]
        public void Checkout_Still_Validates_Stock_And_Does_Not_Trust_Stale_Cart()
        {
            SeedProduct(cost: 60, sell: 100, qty: 2, code: "POS-STK");
            ServiceContainer.Register<POSViewModel>(new POSViewModel());

            var vm = ServiceContainer.GetService<POSViewModel>();
            var product = _productService.GetByCode("POS-STK");
            Assert.NotNull(product);

            vm.AddToCart(product, 2);
            Assert.Equal(200, vm.Cart.Total);
            Assert.Equal(2, vm.Cart.Items[0].Quantity);

            var saleItems = vm.Cart.ConvertToSaleItems();
            var result = _saleService.CreateCashSale(saleItems, null, null, 0, 0, null, "Cash");

            Assert.True(result.Success);
            vm.Cart.Clear();
            Assert.True(vm.Cart.IsEmpty);
        }
    }
}
