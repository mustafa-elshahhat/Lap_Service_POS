using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Presentation.Views;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class InventoryViewModel : BaseViewModel
    {
        private readonly IProductService _productService;
        private readonly IDialogService _dialogService;
        private readonly IPrintService _printService;
        private ObservableCollection<Product> _products;
        private Product _selectedProduct;
        private string _searchText;

        public InventoryViewModel(IDialogService dialogService = null, IPrintService printService = null)
        {
            _productService = ServiceContainer.GetService<IProductService>();
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            _printService = printService ?? ServiceContainer.GetService<IPrintService>();
            Products = new ObservableCollection<Product>();
            LoadProducts();
        }

        #region Properties

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

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

        #endregion

        #region Commands

        public ICommand RefreshCommand => new RelayCommand(LoadProducts);
        public ICommand SearchCommand => new RelayCommand(PerformSearch);
        public ICommand AddProductCommand => new RelayCommand(AddProduct);
        public ICommand LowStockCommand => new RelayCommand(ShowLowStock);
        public ICommand EditProductCommand => new RelayCommand(EditProduct, () => SelectedProduct != null);
        public ICommand AdjustQuantityCommand => new RelayCommand(AdjustQuantity, () => SelectedProduct != null);
        public ICommand PrintBarcodeCommand => new RelayCommand(PrintBarcode, () => SelectedProduct != null);
        public ICommand DeleteProductCommand => new RelayCommand(DeleteProduct, () => SelectedProduct != null);

        #endregion

        #region Methods

        private void LoadProducts()
        {
            try
            {
                var list = _productService.GetAll();
                UpdateProductsList(list);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void PerformSearch()
        {
            string query = SearchText?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                LoadProducts();
            }
            else
            {
                var list = _productService.Search(query);
                UpdateProductsList(list);
            }
        }

        private void UpdateProductsList(List<Product> list)
        {
            Products.Clear();
            foreach (var p in list)
            {
                Products.Add(p);
            }
        }

        private void AddProduct()
        {
            var vm = new ProductFormViewModel(_productService);

            if (_dialogService.ShowProductFormDialog(vm) == true)
            {
                LoadProducts();
                OnRequestSearchFocus();
            }
            else
            {
                OnRequestSearchFocus();
            }
        }

        private void ShowLowStock()
        {
            var list = _productService.GetLowStock();
            UpdateProductsList(list);
            OnRequestSearchFocus();
        }

        private void EditProduct()
        {
            if (SelectedProduct == null) return;

            var vm = new ProductFormViewModel(_productService, SelectedProduct);

            if (_dialogService.ShowProductFormDialog(vm) == true)
            {
                LoadProducts();
                OnRequestSearchFocus();
            }
            else
            {
                OnRequestSearchFocus();
            }
        }

        private void AdjustQuantity()
        {
            if (SelectedProduct == null) return;

            if (_dialogService.ShowInputDialog("تعديل الكمية",
                $"المنتج: {SelectedProduct.Name}\nالكمية الحالية: {SelectedProduct.Quantity}\n\nالكمية الجديدة:",
                SelectedProduct.Quantity.ToString(), out string newQtyStr) == true)
            {
                if (int.TryParse(newQtyStr, out int newQty) && newQty >= 0)
                {
                    try
                    {
                        _productService.SetQuantity(SelectedProduct.Id, newQty);
                        LoadProducts();
                    }
                    catch (Exception ex)
                    {
                        _dialogService.ShowError("خطأ", ex.Message);
                    }
                }
                OnRequestSearchFocus();
            }
        }

        private void PrintBarcode()
        {
            if (SelectedProduct == null) return;

            if (string.IsNullOrEmpty(SelectedProduct.Barcode))
            {
                _dialogService.ShowInfo("تنبيه", "هذا المنتج ليس له باركود");
                return;
            }

            try
            {
                _printService.PrintBarcode(SelectedProduct);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", "فشل طباعة الباركود: " + ex.Message);
            }
        }

        private void DeleteProduct()
        {
            if (SelectedProduct == null) return;

            if (_dialogService.Confirm("تأكيد الحذف", $"هل تريد حذف المنتج:\n{SelectedProduct.Name}؟"))
            {
                try
                {
                    _productService.Delete(SelectedProduct.Id);
                    LoadProducts();
                    OnRequestSearchFocus();
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError("خطأ", ex.Message);
                    OnRequestSearchFocus();
                }
            }
        }

        #endregion
    }
}
