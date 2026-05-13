using System;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class ProductFormViewModel : BaseViewModel
    {
        private readonly IProductService _productService;
        private readonly IDialogService _dialogService;
        private int? _id;
        private string _code;
        private string _name;
        private decimal _purchasePrice;
        private decimal _sellingPrice;
        private int _quantity;
        private int _minQuantity = 5;
        private string _category;
        private string _title;
        private string _titleIcon;

        public Action<bool> CloseAction { get; set; }

        public ProductFormViewModel(IProductService productService, Product product = null, IDialogService dialogService = null)
        {
            _productService = productService;
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();

            if (product != null)
            {

                _id = product.Id;
                Code = product.Code;
                Name = product.Name;
                PurchasePrice = product.PurchasePrice;
                SellingPrice = product.SellingPrice;
                Quantity = product.Quantity;
                MinQuantity = product.MinQuantity;
                Category = product.Category;

                Title = "تعديل المنتج";
                TitleIcon = "✏️";
            }
            else
            {

                Title = "إضافة منتج جديد";
                TitleIcon = "➕";
                Code = _productService.GenerateProductCode();
            }
        }

        #region Properties

        public int? Id => _id;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string TitleIcon
        {
            get => _titleIcon;
            set => SetProperty(ref _titleIcon, value);
        }

        public string Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public decimal PurchasePrice
        {
            get => _purchasePrice;
            set => SetProperty(ref _purchasePrice, value);
        }

        public decimal SellingPrice
        {
            get => _sellingPrice;
            set => SetProperty(ref _sellingPrice, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public int MinQuantity
        {
            get => _minQuantity;
            set => SetProperty(ref _minQuantity, value);
        }

        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        #endregion

        #region Commands

        public ICommand SaveCommand => new RelayCommand(Save);
        public ICommand CancelCommand => new RelayCommand(Cancel);

        #endregion

        #region Methods

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                _dialogService.ShowWarning("تنبيه", "الرجاء إدخال كود المنتج");
                return;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                _dialogService.ShowWarning("تنبيه", "الرجاء إدخال اسم المنتج");
                return;
            }

            try
            {
                if (_id.HasValue)
                {
                    _productService.Update(
                        _id.Value,
                        code: Code.Trim(),
                        barcode: null,
                        name: Name.Trim(),
                        purchasePrice: PurchasePrice,
                        sellingPrice: SellingPrice,
                        quantity: Quantity,
                        minQuantity: MinQuantity,
                        supplierName: null,
                        category: Category?.Trim(),
                        description: null
                    );
                    _dialogService.ShowSuccess("نجاح", "تم تحديث المنتج بنجاح");
                }
                else
                {
                    _productService.Create(
                        code: Code.Trim(),
                        name: Name.Trim(),
                        purchasePrice: PurchasePrice,
                        sellingPrice: SellingPrice,
                        quantity: Quantity,
                        barcode: null,
                        minQuantity: MinQuantity,
                        supplierName: null,
                        category: Category?.Trim(),
                        description: null
                    );
                    _dialogService.ShowSuccess("نجاح", "تم إضافة المنتج بنجاح");
                }

                CloseAction?.Invoke(true);
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void Cancel()
        {
            CloseAction?.Invoke(false);
        }

        #endregion
    }
}

