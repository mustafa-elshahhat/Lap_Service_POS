using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CarPartsShopWPF.Application.DTOs;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class RepairPartLineViewModel : BaseViewModel
    {
        public long Id { get; set; }
        public long DeviceId { get; set; }
        public long OrderId { get; set; }
        public int? ProductId { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public bool IsFromInventory { get; set; }
        public string SourceLabel => IsFromInventory ? "مخزون" : "خارجي";
    }

    public class RepairDeviceFormViewModel : BaseViewModel
    {
        private readonly IMaintenanceService _maintenanceService;
        private readonly IProductService _productService;
        private readonly IDialogService _dialogService;

        private long _deviceId;
        private long _orderId;
        private bool _isEditMode;

        private string _deviceType;
        private string _brand;
        private string _model;
        private string _serialNumber;
        private string _condition;
        private string _reportedIssue;
        private string _accessories;
        private decimal _estimatedCost;
        private decimal _laborCost;
        private string _deviceStatus;
        private string _diagnosisNotes;
        private string _repairNotes;

        private ObservableCollection<RepairPartLineViewModel> _parts;
        private RepairPartLineViewModel _selectedPart;

        public event Action RequestClose;
        public bool Saved { get; private set; }

        public RepairDeviceFormViewModel(long orderId, RepairDevice existingDevice = null)
        {
            _maintenanceService = ServiceContainer.GetService<IMaintenanceService>();
            _productService     = ServiceContainer.GetService<IProductService>();
            _dialogService      = ServiceContainer.GetService<IDialogService>();

            _orderId    = orderId;
            _isEditMode = existingDevice != null;
            Parts       = new ObservableCollection<RepairPartLineViewModel>();

            DeviceTypes      = new ObservableCollection<string> { "laptop", "printer", "other" };
            StatusOptions    = new ObservableCollection<string>(RepairStatus.GetAll());
            StatusOptionsAr  = new ObservableCollection<string>(RepairStatus.GetAll().Select(RepairStatus.ToArabic));

            if (_isEditMode)
            {
                _deviceId     = existingDevice.Id;
                DeviceType    = existingDevice.DeviceType;
                Brand         = existingDevice.Brand;
                Model         = existingDevice.Model;
                SerialNumber  = existingDevice.SerialNumber;
                Condition     = existingDevice.Condition;
                ReportedIssue = existingDevice.ReportedIssue;
                Accessories   = existingDevice.Accessories;
                EstimatedCost = existingDevice.EstimatedCost;
                LaborCost     = existingDevice.LaborCost;
                DeviceStatus    = existingDevice.DeviceStatus;
                SelectedStatusAr = RepairStatus.ToArabic(existingDevice.DeviceStatus);
                DiagnosisNotes   = existingDevice.DiagnosisNotes;
                RepairNotes   = existingDevice.RepairNotes;
                LoadParts();
            }
            else
            {
                DeviceType       = "laptop";
                DeviceStatus     = RepairStatus.Received;
                SelectedStatusAr = RepairStatus.ToArabic(RepairStatus.Received);
            }
        }

        public ObservableCollection<string> DeviceTypes    { get; }
        public ObservableCollection<string> StatusOptions  { get; }
        public ObservableCollection<string> StatusOptionsAr { get; }

        private string _selectedStatusAr;
        public string SelectedStatusAr
        {
            get => _selectedStatusAr;
            set
            {
                SetProperty(ref _selectedStatusAr, value);
                var match = RepairStatus.GetAll().FirstOrDefault(s => RepairStatus.ToArabic(s) == value);
                if (match != null) DeviceStatus = match;
            }
        }

        public string DeviceType    { get => _deviceType;    set => SetProperty(ref _deviceType, value); }
        public string Brand         { get => _brand;         set => SetProperty(ref _brand, value); }
        public string Model         { get => _model;         set => SetProperty(ref _model, value); }
        public string SerialNumber  { get => _serialNumber;  set => SetProperty(ref _serialNumber, value); }
        public string Condition     { get => _condition;     set => SetProperty(ref _condition, value); }
        public string ReportedIssue { get => _reportedIssue; set => SetProperty(ref _reportedIssue, value); }
        public string Accessories   { get => _accessories;   set => SetProperty(ref _accessories, value); }
        public decimal EstimatedCost { get => _estimatedCost; set => SetProperty(ref _estimatedCost, value); }
        public decimal LaborCost    { get => _laborCost;     set { SetProperty(ref _laborCost, value); } }
        public string DeviceStatus  { get => _deviceStatus;  set => SetProperty(ref _deviceStatus, value); }
        public string DiagnosisNotes { get => _diagnosisNotes; set => SetProperty(ref _diagnosisNotes, value); }
        public string RepairNotes   { get => _repairNotes;   set => SetProperty(ref _repairNotes, value); }

        public ObservableCollection<RepairPartLineViewModel> Parts
        {
            get => _parts;
            set => SetProperty(ref _parts, value);
        }

        public RepairPartLineViewModel SelectedPart
        {
            get => _selectedPart;
            set => SetProperty(ref _selectedPart, value);
        }

        private string _productSearch;
        private Product _selectedProduct;
        private ObservableCollection<Product> _searchedProducts;
        private int _newPartQty = 1;
        private decimal _newPartCost;
        private string _customPartName;
        private int _customPartQty = 1;
        private decimal _customPartCost;

        public string ProductSearch
        {
            get => _productSearch;
            set { SetProperty(ref _productSearch, value); SearchProducts(); }
        }
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set { SetProperty(ref _selectedProduct, value); if (value != null) NewPartCost = value.PurchasePrice; }
        }
        public ObservableCollection<Product> SearchedProducts
        {
            get => _searchedProducts;
            set => SetProperty(ref _searchedProducts, value);
        }
        public int NewPartQty       { get => _newPartQty;    set => SetProperty(ref _newPartQty, value); }
        public decimal NewPartCost  { get => _newPartCost;   set => SetProperty(ref _newPartCost, value); }
        public string CustomPartName  { get => _customPartName;  set => SetProperty(ref _customPartName, value); }
        public int CustomPartQty    { get => _customPartQty; set => SetProperty(ref _customPartQty, value); }
        public decimal CustomPartCost { get => _customPartCost; set => SetProperty(ref _customPartCost, value); }

        public ICommand SaveCommand               => new RelayCommand(Save);
        public ICommand CancelCommand             => new RelayCommand(() => RequestClose?.Invoke());
        public ICommand AddInventoryPartCommand   => new RelayCommand(AddInventoryPart, () => _isEditMode && SelectedProduct != null && NewPartQty > 0);
        public ICommand AddCustomPartCommand      => new RelayCommand(AddCustomPart,    () => _isEditMode && !string.IsNullOrWhiteSpace(CustomPartName));
        public ICommand RemovePartCommand         => new RelayCommand(RemovePart, () => SelectedPart != null);

        private void SearchProducts()
        {
            if (SearchedProducts == null) SearchedProducts = new ObservableCollection<Product>();
            SearchedProducts.Clear();
            if (string.IsNullOrWhiteSpace(ProductSearch)) return;
            foreach (var p in _productService.Search(ProductSearch, 20))
                SearchedProducts.Add(p);
        }

        private void Save()
        {
            if (!Validate(out string err)) { _dialogService.ShowError("خطأ", err); return; }

            try
            {
                var input = BuildInput();
                if (_isEditMode)
                    _maintenanceService.UpdateDevice(_deviceId, input);
                else
                {
                    _deviceId   = _maintenanceService.AddDevice(_orderId, input);
                    _isEditMode = true;
                    LoadParts();
                }

                Saved = true;
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void AddInventoryPart()
        {
            if (SelectedProduct == null || NewPartQty <= 0) return;
            try
            {
                _maintenanceService.AddInventoryPart(_deviceId, _orderId, SelectedProduct.Id, NewPartQty, NewPartCost);
                SelectedProduct = null;
                ProductSearch   = string.Empty;
                NewPartQty      = 1;
                NewPartCost     = 0;
                LoadParts();
            }
            catch (Exception ex) { _dialogService.ShowError("خطأ", ex.Message); }
        }

        private void AddCustomPart()
        {
            if (string.IsNullOrWhiteSpace(CustomPartName)) return;
            try
            {
                _maintenanceService.AddCustomPart(_deviceId, _orderId, CustomPartName, CustomPartQty, CustomPartCost);
                CustomPartName = string.Empty;
                CustomPartQty  = 1;
                CustomPartCost = 0;
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
            if (_deviceId == 0) return;
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
        }

        private RepairDeviceInput BuildInput() => new RepairDeviceInput
        {
            DeviceType     = DeviceType,
            Brand          = Brand,
            Model          = Model,
            SerialNumber   = SerialNumber,
            Condition      = Condition,
            ReportedIssue  = ReportedIssue,
            Accessories    = Accessories,
            EstimatedCost  = EstimatedCost,
            LaborCost      = LaborCost,
            DeviceStatus   = DeviceStatus,
            DiagnosisNotes = DiagnosisNotes,
            RepairNotes    = RepairNotes
        };

        private bool Validate(out string err)
        {
            err = string.Empty;
            if (string.IsNullOrWhiteSpace(ReportedIssue))
            { err = "وصف المشكلة مطلوب."; return false; }
            if (string.IsNullOrWhiteSpace(DeviceType))
            { err = "نوع الجهاز مطلوب."; return false; }
            return true;
        }
    }
}
