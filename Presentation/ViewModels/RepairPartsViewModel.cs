using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Presentation.Interfaces;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class RepairPartsViewModel : BaseViewModel
    {
        private readonly IMaintenanceService _maintenanceService;
        private readonly IProductService     _productService;
        private readonly IDialogService      _dialogService;

        private readonly long _deviceId;
        private readonly long _orderId;

        public string DeviceTitle { get; }

        public event Action RequestClose;

        public RepairPartsViewModel(long deviceId, long orderId, string deviceTitle)
        {
            _maintenanceService = ServiceContainer.GetService<IMaintenanceService>();
            _productService     = ServiceContainer.GetService<IProductService>();
            _dialogService      = ServiceContainer.GetService<IDialogService>();

            _deviceId   = deviceId;
            _orderId    = orderId;
            DeviceTitle = deviceTitle;

            Parts            = new ObservableCollection<RepairPartLineViewModel>();
            SearchedProducts = new ObservableCollection<Product>();

            LoadParts();
        }

        // ── Parts list ──────────────────────────────────────────────────────
        private ObservableCollection<RepairPartLineViewModel> _parts;
        public ObservableCollection<RepairPartLineViewModel> Parts
        {
            get => _parts;
            set => SetProperty(ref _parts, value);
        }

        private RepairPartLineViewModel _selectedPart;
        public RepairPartLineViewModel SelectedPart
        {
            get => _selectedPart;
            set => SetProperty(ref _selectedPart, value);
        }

        private decimal _totalPartsCost;
        public decimal TotalPartsCost
        {
            get => _totalPartsCost;
            set => SetProperty(ref _totalPartsCost, value);
        }

        // ── Inventory search ────────────────────────────────────────────────
        // ── Source selection ─────────────────────────────────────────────────
        public ObservableCollection<string> PartSources { get; } = new ObservableCollection<string> { "من المخزون", "خارجي" };

        private string _selectedPartSource = "من المخزون";
        public string SelectedPartSource
        {
            get => _selectedPartSource;
            set
            {
                SetProperty(ref _selectedPartSource, value);
                OnPropertyChanged(nameof(IsInventorySource));
                OnPropertyChanged(nameof(IsCustomSource));
                ResetForm();
            }
        }

        public bool IsInventorySource => _selectedPartSource == "من المخزون";
        public bool IsCustomSource    => _selectedPartSource == "خارجي";

        // ── Shared fields ───────────────────────────────────────────────────
        private int     _partQty  = 1;
        private decimal _partCost = 0;
        public int     PartQty  { get => _partQty;  set => SetProperty(ref _partQty, value); }
        public decimal PartCost { get => _partCost; set => SetProperty(ref _partCost, value); }

        // ── Inventory source ─────────────────────────────────────────────────
        private string _productSearch;
        public string ProductSearch
        {
            get => _productSearch;
            set { SetProperty(ref _productSearch, value); SearchProducts(); }
        }

        private Product _selectedProduct;
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set { SetProperty(ref _selectedProduct, value); if (value != null) PartCost = value.PurchasePrice; }
        }

        public ObservableCollection<Product> SearchedProducts { get; }

        // ── Custom source ────────────────────────────────────────────────────
        private string _customPartName;
        public string CustomPartName { get => _customPartName; set => SetProperty(ref _customPartName, value); }

        // Keep old properties for backward compat (unused but avoids dead-code warnings)
        private int     _newPartQty  = 1;
        private decimal _newPartCost = 0;
        public int     NewPartQty  { get => _newPartQty;  set => SetProperty(ref _newPartQty, value); }
        public decimal NewPartCost { get => _newPartCost; set => SetProperty(ref _newPartCost, value); }
        public string  CustomPartQty  { get; set; }
        public string  CustomPartCost { get; set; }

        // ── Commands ────────────────────────────────────────────────────────
        public ICommand AddPartCommand => new RelayCommand(AddPart, CanAddPart);

        public ICommand AddInventoryPartCommand => new RelayCommand(AddInventoryPart,
            () => SelectedProduct != null && PartQty > 0);

        public ICommand AddCustomPartCommand => new RelayCommand(AddCustomPart,
            () => !string.IsNullOrWhiteSpace(CustomPartName) && PartQty > 0);

        public ICommand RemovePartCommand => new RelayCommand(RemovePart,
            () => SelectedPart != null);

        public ICommand CloseCommand => new RelayCommand(() => RequestClose?.Invoke());

        // ── Logic ───────────────────────────────────────────────────────────
        private bool CanAddPart() =>
            IsInventorySource ? SelectedProduct != null && PartQty > 0
                              : !string.IsNullOrWhiteSpace(CustomPartName) && PartQty > 0;

        private void AddPart()
        {
            if (IsInventorySource) AddInventoryPart();
            else                   AddCustomPart();
        }

        private void ResetForm()
        {
            PartQty         = 1;
            PartCost        = 0;
            CustomPartName  = string.Empty;
            SelectedProduct = null;
            ProductSearch   = string.Empty;
        }

        private void SearchProducts()
        {
            SearchedProducts.Clear();
            if (string.IsNullOrWhiteSpace(ProductSearch)) return;
            foreach (var p in _productService.Search(ProductSearch, 20))
                SearchedProducts.Add(p);
        }

        private void AddInventoryPart()
        {
            if (SelectedProduct == null || PartQty <= 0) return;
            try
            {
                _maintenanceService.AddInventoryPart(_deviceId, _orderId, SelectedProduct.Id, PartQty, PartCost);
                ResetForm();
                LoadParts();
            }
            catch (Exception ex) { _dialogService.ShowError("خطأ", ex.Message); }
        }

        private void AddCustomPart()
        {
            if (string.IsNullOrWhiteSpace(CustomPartName)) return;
            try
            {
                _maintenanceService.AddCustomPart(_deviceId, _orderId, CustomPartName, PartQty, PartCost);
                ResetForm();
                LoadParts();
            }
            catch (Exception ex) { _dialogService.ShowError("خطأ", ex.Message); }
        }

        private void RemovePart()
        {
            if (SelectedPart == null) return;
            if (!_dialogService.Confirm("حذف قطعة", $"هل تريد حذف '{SelectedPart.PartName}'؟")) return;
            try
            {
                _maintenanceService.RemovePart(SelectedPart.Id);
                LoadParts();
            }
            catch (Exception ex) { _dialogService.ShowError("خطأ", ex.Message); }
        }

        private void LoadParts()
        {
            Parts.Clear();
            foreach (var p in _maintenanceService.GetDeviceParts(_deviceId))
            {
                Parts.Add(new RepairPartLineViewModel
                {
                    Id              = p.Id,
                    DeviceId        = p.DeviceId,
                    OrderId         = p.OrderId,
                    ProductId       = p.ProductId,
                    PartName        = p.PartName,
                    Quantity        = p.Quantity,
                    UnitCost        = p.UnitCost,
                    TotalCost       = p.TotalCost,
                    IsFromInventory = p.IsFromInventory
                });
            }
            TotalPartsCost = Parts.Sum(p => p.TotalCost);
        }
    }
}
