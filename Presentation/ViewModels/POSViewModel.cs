using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Domain.Entities;
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
        private ObservableCollection<CartItem> _cartItems;
        private List<Product> _searchResults;
        private Product _selectedProduct;
        private CartItem _selectedCartItem;
        private decimal _cartTotal;

        private readonly IDialogService _dialogService;

        public POSViewModel(IDialogService dialogService = null, ISaleService saleService = null, IProductService productService = null)
        {
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            _saleService = saleService ?? ServiceContainer.GetService<ISaleService>();
            _productService = productService ?? ServiceContainer.GetService<IProductService>();
            _printService = ServiceContainer.GetService<IPrintService>();
            _auth = ServiceContainer.GetService<IAuthService>();
            
            CartItems = new ObservableCollection<CartItem>();
            CartItems.CollectionChanged += (s, e) => RecalculateTotal();
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

        public ObservableCollection<CartItem> CartItems
        {
            get => _cartItems;
            set => SetProperty(ref _cartItems, value);
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

        public CartItem SelectedCartItem
        {
            get => _selectedCartItem;
            set => SetProperty(ref _selectedCartItem, value);
        }

        public decimal CartTotal
        {
            get => _cartTotal;
            set => SetProperty(ref _cartTotal, value);
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

            int productId = product.Id;
            int currentStock = product.Quantity;

            var (available, _) = _productService.CheckStock(productId, quantity);
            if (!available)
            {
                _dialogService.ShowWarning("تحذير", $"الكمية المتاحة: {currentStock}");
                RaiseRequestSearchFocus();
                return;
            }

            var existing = CartItems.FirstOrDefault(c => c.ProductId == productId);
            if (existing != null)
            {
                int newQty = existing.Quantity + quantity;
                if (newQty > currentStock)
                {
                    _dialogService.ShowWarning("تحذير", $"الكمية المتاحة: {currentStock}");
                    RaiseRequestSearchFocus();
                    return;
                }
                existing.Quantity = newQty;
                existing.Total = existing.Quantity * existing.UnitPrice;

                int idx = CartItems.IndexOf(existing);
                CartItems[idx] = existing; 
            }
            else
            {
                var cartItem = new CartItem
                {
                    Index = CartItems.Count + 1,
                    ProductId = productId,
                    ProductName = product.Name,
                    Quantity = quantity,
                    UnitPrice = product.SellingPrice,
                    OriginalPrice = product.SellingPrice,
                    PurchasePrice = product.PurchasePrice,
                    ProductCode = product.Code
                };
                cartItem.Total = cartItem.Quantity * cartItem.UnitPrice;
                CartItems.Add(cartItem);
            }

            RecalculateTotal();
            RaiseRequestSearchFocus();
        }

        private void RemoveFromCart()
        {
            if (SelectedCartItem != null)
            {
                CartItems.Remove(SelectedCartItem);
                RecalculateIndices();
                RecalculateTotal();
                RaiseRequestSearchFocus();
            }
            else
            {
                _dialogService.ShowWarning("تحذير", "اختر عنصراً من السلة");
            }
        }

        private void ClearCart()
        {
            if (CartItems.Count > 0)
            {
                if (_dialogService.Confirm("تأكيد", "هل تريد تفريغ السلة؟"))
                {
                    CartItems.Clear();
                    RecalculateTotal();
                }
            }
            RaiseRequestSearchFocus();
        }

        private void EditQuantity()
        {
            try
            {
                var item = SelectedCartItem;
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
                        item.Quantity = newQty;
                        item.Total = item.Quantity * item.UnitPrice;
                        RefreshCartItem(item);
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
                var item = SelectedCartItem;
                if (item == null)
                {
                    _dialogService.ShowWarning("تحذير", "اختر عنصراً من السلة");
                    return;
                }

                double maxDiscount = _auth.GetMaxDiscount();
                double maxMarkup = _auth.GetMaxMarkup();
                
                decimal discountLimit = item.OriginalPrice * (1 - (decimal)(maxDiscount / 100));

                decimal minPrice = Math.Max(discountLimit, item.PurchasePrice);
                
                decimal maxPrice = item.OriginalPrice * (1 + (decimal)(maxMarkup / 100));

                string currentPrice = item.UnitPrice.ToString();
                
                if (_dialogService.ShowInputDialog("تعديل السعر",
                        $"السعر الجديد:\n(الحد الأدنى: {Formatting.FormatCurrency(minPrice)} | الحد الأقصى: {Formatting.FormatCurrency(maxPrice)})",
                        currentPrice, out string newPriceStr) == true)
                {
                    if (decimal.TryParse(newPriceStr, out decimal newPrice) && newPrice > 0)
                    {
                        if (newPrice < item.PurchasePrice)
                        {
                             _dialogService.ShowWarning("تحذير", $"غير مسموح بالبيع بأقل من سعر الشراء: {Formatting.FormatCurrency(item.PurchasePrice)}");
                             return;
                        }

                        if (newPrice < minPrice)
                        {
                            _dialogService.ShowWarning("تحذير", $"السعر أقل من الحد المسموح (نسبة الخصم): {Formatting.FormatCurrency(minPrice)}");
                            return;
                        }
                        if (newPrice > maxPrice)
                        {
                            _dialogService.ShowWarning("تحذير", $"السعر أعلى من الحد المسموح: {Formatting.FormatCurrency(maxPrice)}");
                            return;
                        }
                        item.UnitPrice = newPrice;
                        item.Total = item.Quantity * item.UnitPrice;
                        RefreshCartItem(item);
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
            if (CartItems.Count == 0)
            {
                _dialogService.ShowInfo("تنبيه", "السلة فارغة");
                RaiseRequestSearchFocus();
                return;
            }

            if (_dialogService.ShowCashSaleDialog(CartTotal, out string customerName, out string customerPhone, out string paymentMethod) == true)
            {
                try
                {
                    var items = ConvertCartToSaleItems();
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

                    CartItems.Clear();
                    RecalculateTotal();
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

        private void RecalculateTotal()
        {
            decimal total = 0;
            foreach (var item in CartItems)
            {
                total += item.Total;
            }
            CartTotal = total;
        }

        private void RecalculateIndices()
        {
            int idx = 1;
            foreach (var item in CartItems)
            {
                item.Index = idx++;
            }
        }

        private void RefreshCartItem(CartItem item)
        {
            int idx = CartItems.IndexOf(item);
            if (idx >= 0)
            {
                CartItems[idx] = item;
                RecalculateTotal();
            }
        }

        private List<SaleItem> ConvertCartToSaleItems()
        {
            return CartItems.Select(c => new SaleItem
            {
                ProductId = c.ProductId,
                ProductCode = c.ProductCode,
                ProductName = c.ProductName,
                Quantity = c.Quantity,
                UnitPurchasePrice = c.PurchasePrice,
                UnitSellingPrice = c.OriginalPrice,
                UnitFinalPrice = c.UnitPrice
            }).ToList();
        }

        private void RaiseRequestSearchFocus()
        {
             OnRequestSearchFocus();
        }

        #endregion
    }

    public class CartItem : BaseViewModel
    {
        private int _index;
        private int _productId;
        private string _productName;
        private string _productCode;
        private int _quantity;
        private decimal _unitPrice;
        private decimal _originalPrice;
        private decimal _purchasePrice;
        private decimal _total;

        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

        public int ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public string ProductCode
        {
            get => _productCode;
            set => SetProperty(ref _productCode, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set => SetProperty(ref _unitPrice, value);
        }

        public decimal OriginalPrice
        {
            get => _originalPrice;
            set => SetProperty(ref _originalPrice, value);
        }

        public decimal PurchasePrice
        {
            get => _purchasePrice;
            set => SetProperty(ref _purchasePrice, value);
        }

        public decimal Total
        {
            get => _total;
            set => SetProperty(ref _total, value);
        }
    }
}

