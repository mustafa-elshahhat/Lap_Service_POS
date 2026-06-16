using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Presentation.Models;
using AlJohary.ServiceHub.Presentation.Views;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{
    public class POSViewModel : BaseViewModel
    {
        private readonly ISaleService _saleService;
        private readonly IProductService _productService;
        private readonly IPrintService _printService;
        private readonly IAuthService _auth;

        private string _searchText;
        private CartModel _cart;
        private List<Product> _searchResults;
        private Product _selectedProduct;

        private readonly IDialogService _dialogService;
        private readonly PriceEditPolicy _priceEditPolicy;

        public POSViewModel(IDialogService dialogService = null, ISaleService saleService = null, IProductService productService = null)
        {
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            _saleService = saleService ?? ServiceContainer.GetService<ISaleService>();
            _productService = productService ?? ServiceContainer.GetService<IProductService>();
            _printService = ServiceContainer.GetService<IPrintService>();
            _auth = ServiceContainer.GetService<IAuthService>();

            Cart = new CartModel();
            _priceEditPolicy = new PriceEditPolicy(_auth);
        }

        #region Properties

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    PerformSearch();
                }
            }
        }

        public CartModel Cart
        {
            get => _cart;
            set => SetProperty(ref _cart, value);
        }

        public List<Product> SearchResults
        {
            get => _searchResults;
            set => SetProperty(ref _searchResults, value);
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        #endregion

        #region Commands

        public ICommand SearchCommand => new RelayCommand(PerformSearch);
        public ICommand SearchEnterCommand => new RelayCommand(HandleSearchEnterKey);
        public ICommand AddToCartCommand => new RelayCommand(AddSelectedToCart);
        public ICommand RemoveFromCartCommand => new RelayCommand(RemoveFromCart);
        public ICommand ClearCartCommand => new RelayCommand(ClearCart);
        public ICommand CheckoutCashCommand => new RelayCommand(CheckoutCash);
        public ICommand ReturnInvoiceCommand => new RelayCommand(ReturnInvoice);
        public ICommand EditQuantityCommand => new RelayCommand(EditQuantity);
        public ICommand EditPriceCommand => new RelayCommand(EditPrice);
        public ICommand QuickAddCommand => new RelayCommand<Product>(p => AddToCart(p));

        #endregion

        #region Methods

        public void HandleSearchEnterKey()
        {
            string query = SearchText?.Trim();
            if (string.IsNullOrEmpty(query)) return;

            var product = _productService.GetByCode(query);
            if (product != null)
            {
                AddToCart(product);
                ClearSearch();
                return;
            }

            var products = _productService.Search(query);
            if (products.Count == 1)
            {
                AddToCart(products[0]);
                ClearSearch();
                return;
            }
            
            SearchResults = products;
        }

        public void PerformSearch()
        {
            string query = SearchText?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                SearchResults = new List<Product>();
                return;
            }
            SearchResults = _productService.Search(query);
        }

        private void ClearSearch()
        {
            SearchText = "";
            SearchResults = null;
            RaiseRequestSearchFocus();
        }

        private void AddSelectedToCart()
        {
            if (SelectedProduct != null)
            {
                AddToCart(SelectedProduct);
                ClearSearch();
            }
        }

        public void AddToCart(Product product, int quantity = 1)
        {
            if (product == null) return;

            int currentStock = product.Quantity;

            var (available, _) = _productService.CheckStock(product.Id, quantity);
            if (!available)
            {
                _dialogService.ShowWarning("تحذير", $"الكمية المتاحة: {currentStock}");
                RaiseRequestSearchFocus();
                return;
            }

            var existing = Cart.Items.FirstOrDefault(c => c.ProductId == product.Id);
            if (existing != null)
            {
                int newQty = existing.Quantity + quantity;
                if (newQty > currentStock)
                {
                    _dialogService.ShowWarning("تحذير", $"الكمية المتاحة: {currentStock}");
                    RaiseRequestSearchFocus();
                    return;
                }
                Cart.UpdateQuantity(existing, newQty);
            }
            else
            {
                Cart.AddItem(product.Id, product.Name, product.Code, quantity,
                    product.SellingPrice, product.SellingPrice, product.PurchasePrice);
            }

            RaiseRequestSearchFocus();
        }

        private void RemoveFromCart()
        {
            if (Cart.SelectedItem != null)
            {
                Cart.RemoveItem(Cart.SelectedItem);
                RaiseRequestSearchFocus();
            }
            else
            {
                _dialogService.ShowWarning("تحذير", "اختر عنصراً من السلة");
            }
        }

        private void ClearCart()
        {
            if (!Cart.IsEmpty)
            {
                if (_dialogService.Confirm("تأكيد", "هل تريد تفريغ السلة؟"))
                {
                    Cart.Clear();
                }
            }
            RaiseRequestSearchFocus();
        }

        private void EditQuantity()
        {
            try
            {
                var item = Cart.SelectedItem;
                if (item == null)
                {
                    _dialogService.ShowWarning("تحذير", "اختر عنصراً من السلة");
                    return;
                }

                if (_dialogService.ShowInputDialog("تعديل الكمية", "الكمية الجديدة:", item.Quantity.ToString(), out string newQtyStr) == true)
                {
                    if (int.TryParse(newQtyStr, out int newQty) && newQty > 0)
                    {
                        var (available, current) = _productService.CheckStock(item.ProductId, newQty);
                        if (!available)
                        {
                            _dialogService.ShowWarning("تحذير", $"الكمية المتاحة: {current}");
                            return;
                        }
                        Cart.UpdateQuantity(item, newQty);
                    }
                }
            }
            finally
            {
                RaiseRequestSearchFocus();
            }
        }

        private void EditPrice()
        {
            try
            {
                var item = Cart.SelectedItem;
                if (item == null)
                {
                    _dialogService.ShowWarning("تحذير", "اختر عنصراً من السلة");
                    return;
                }

                string currentPrice = item.UnitPrice.ToString();
                string dialogMessage;

                if (_priceEditPolicy.CanBypassLimits)
                {
                    dialogMessage = "السعر الجديد:";
                }
                else
                {
                    var (minPrice, maxPrice) = _priceEditPolicy.GetPriceBounds(item);
                    dialogMessage = $"السعر الجديد:\n(الحد الأدنى: {Formatting.FormatCurrency(minPrice)} | الحد الأقصى: {Formatting.FormatCurrency(maxPrice)})";
                }

                if (_dialogService.ShowInputDialog("تعديل السعر", dialogMessage, currentPrice, out string newPriceStr) == true)
                {
                    if (decimal.TryParse(newPriceStr, out decimal newPrice) && newPrice > 0)
                    {
                        string error = _priceEditPolicy.Validate(item, newPrice);
                        if (error != null)
                        {
                            _dialogService.ShowWarning("تحذير", error);
                            return;
                        }
                        Cart.UpdatePrice(item, newPrice);
                    }
                }
            }
            finally
            {
                RaiseRequestSearchFocus();
            }
        }

        private void CheckoutCash()
        {
            if (Cart.IsEmpty)
            {
                _dialogService.ShowInfo("تنبيه", "السلة فارغة");
                RaiseRequestSearchFocus();
                return;
            }

            if (_dialogService.ShowCashSaleDialog(Cart.Total, out string customerName, out string customerPhone, out string paymentMethod) == true)
            {
                try
                {
                    var items = Cart.ConvertToSaleItems();
                    var result = _saleService.CreateCashSale(
                        items,
                        string.IsNullOrWhiteSpace(customerName) ? null : customerName,
                        string.IsNullOrWhiteSpace(customerName) ? null : customerPhone,
                        0, 0, null, 
                        paymentMethod
                    );

                    _dialogService.ShowSuccess("نجاح", $"تم إنشاء الفاتورة بنجاح\nرقم الفاتورة: {result.InvoiceNumber}");

                    var sale = _saleService.GetByInvoiceNumber(result.InvoiceNumber);
                    if (sale != null)
                    {
                         var sItems = _saleService.GetSaleItems((int)sale.Id);
                         _printService.PrintSaleReceipt(sale, sItems);
                    }

                    Cart.Clear();
                    TypedMessenger.Send("RefreshReports");
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError("خطأ", ex.Message);
                }
            }
            RaiseRequestSearchFocus();
        }

        private void ReturnInvoice()
        {
            try
            {
                if (_dialogService.ShowInputDialog("استرجاع فاتورة", "أدخل رقم الفاتورة المراد استرجاعها:", "", out string invoice) == true)
                {
                    invoice = invoice?.Trim();
                    if (string.IsNullOrEmpty(invoice))
                    {
                        _dialogService.ShowWarning("تنبيه", "الرجاء إدخال رقم فاتورة صالح");
                        return;
                    }

                    _dialogService.ShowInvoiceViewDialog(invoice);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        #endregion

        #region Helpers

        private void RaiseRequestSearchFocus()
        {
             OnRequestSearchFocus();
        }

        #endregion
    }
}
