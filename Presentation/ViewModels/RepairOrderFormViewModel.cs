using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AlJohary.ServiceHub.Application.DTOs;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Presentation.Services;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{
    public class RepairDeviceRowViewModel : BaseViewModel
    {
        public long Id { get; set; }
        public string DeviceType { get; set; }
        public string DisplayName { get; set; }
        public string ReportedIssue { get; set; }
        public string DeviceStatus { get; set; }
        public string DeviceStatusAr => RepairStatus.ToArabic(DeviceStatus);
        public decimal LaborCost { get; set; }
    }

    public class RepairPaymentRowViewModel : BaseViewModel
    {
        public long Id { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentDate { get; set; }
        public string Notes { get; set; }
    }

    public class RepairOrderFormViewModel : BaseViewModel
    {
        private readonly IMaintenanceService _maintenanceService;
        private readonly ICustomerService _customerService;
        private readonly IAuthService _auth;
        private readonly IDialogService _dialogService;

        private long _orderId;
        private bool _isEditMode;

        private string _customerName;
        private string _customerPhone;
        private string _technicianName;
        private DateTime? _expectedDelivery;
        private string _notes;
        private decimal _initialPayment;
        private string _initialPaymentMethod;

        private string _customerSearch;
        private ObservableCollection<Customer> _searchedCustomers;
        private Customer _selectedCustomer;

        private ObservableCollection<RepairDeviceRowViewModel> _devices;
        private RepairDeviceRowViewModel _selectedDevice;

        private ObservableCollection<RepairPaymentRowViewModel> _payments;
        private decimal _addPaymentAmount;
        private string _addPaymentMethod;
        private string _addPaymentNotes;

        private int _selectedTabIndex;

        public event Action RequestClose;
        public bool Saved { get; private set; }
        public long CreatedOrderId { get; private set; }

        public RepairOrderFormViewModel(RepairOrder existingOrder = null)
        {
            _maintenanceService = ServiceContainer.GetService<IMaintenanceService>();
            _customerService    = ServiceContainer.GetService<ICustomerService>();
            _auth               = ServiceContainer.GetService<IAuthService>();
            _dialogService      = ServiceContainer.GetService<IDialogService>();

            Devices          = new ObservableCollection<RepairDeviceRowViewModel>();
            Payments         = new ObservableCollection<RepairPaymentRowViewModel>();
            SearchedCustomers = new ObservableCollection<Customer>();
            PaymentMethods   = new ObservableCollection<string>(Shared.Helpers.PaymentMethods.GetAll());
            InitialPaymentMethod = Shared.Helpers.PaymentMethods.Cash;
            AddPaymentMethod     = Shared.Helpers.PaymentMethods.Cash;

            _isEditMode = existingOrder != null;

            if (_isEditMode)
            {
                _orderId            = existingOrder.Id;
                CustomerName        = existingOrder.CustomerName;
                CustomerPhone       = existingOrder.CustomerPhone;
                TechnicianName      = existingOrder.TechnicianName;
                ExpectedDelivery    = existingOrder.ExpectedDelivery;
                Notes               = existingOrder.Notes;
                LoadDevices();
                LoadPayments();
            }
            else
            {
                ExpectedDelivery = DateTime.Today.AddDays(3);
            }
        }

        public ObservableCollection<string> PaymentMethods { get; }

        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }
        public string CustomerPhone
        {
            get => _customerPhone;
            set => SetProperty(ref _customerPhone, value);
        }
        public string TechnicianName
        {
            get => _technicianName;
            set => SetProperty(ref _technicianName, value);
        }
        public DateTime? ExpectedDelivery
        {
            get => _expectedDelivery;
            set => SetProperty(ref _expectedDelivery, value);
        }
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }
        public decimal InitialPayment
        {
            get => _initialPayment;
            set => SetProperty(ref _initialPayment, value);
        }
        public string InitialPaymentMethod
        {
            get => _initialPaymentMethod;
            set => SetProperty(ref _initialPaymentMethod, value);
        }

        public string CustomerSearch
        {
            get => _customerSearch;
            set { SetProperty(ref _customerSearch, value); SearchCustomers(); }
        }
        public ObservableCollection<Customer> SearchedCustomers
        {
            get => _searchedCustomers;
            set => SetProperty(ref _searchedCustomers, value);
        }
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                SetProperty(ref _selectedCustomer, value);
                if (value != null)
                {
                    CustomerName  = value.Name;
                    CustomerPhone = value.Phone;
                    CustomerSearch = string.Empty;
                    SearchedCustomers.Clear();
                }
            }
        }

        public ObservableCollection<RepairDeviceRowViewModel> Devices
        {
            get => _devices;
            set => SetProperty(ref _devices, value);
        }
        public RepairDeviceRowViewModel SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }

        public ObservableCollection<RepairPaymentRowViewModel> Payments
        {
            get => _payments;
            set => SetProperty(ref _payments, value);
        }
        public decimal AddPaymentAmount
        {
            get => _addPaymentAmount;
            set => SetProperty(ref _addPaymentAmount, value);
        }
        public string AddPaymentMethod
        {
            get => _addPaymentMethod;
            set => SetProperty(ref _addPaymentMethod, value);
        }
        public string AddPaymentNotes
        {
            get => _addPaymentNotes;
            set => SetProperty(ref _addPaymentNotes, value);
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public bool IsEditMode => _isEditMode;
        public string Title => _isEditMode ? "تعديل طلب الصيانة" : "طلب صيانة جديد";

        private decimal _orderTotal;
        private decimal _orderPaid;
        private decimal _orderRemaining;

        public decimal OrderTotal     { get => _orderTotal;     set => SetProperty(ref _orderTotal, value); }
        public decimal OrderPaid      { get => _orderPaid;      set => SetProperty(ref _orderPaid, value); }
        public decimal OrderRemaining { get => _orderRemaining; set => SetProperty(ref _orderRemaining, value); }

        public ICommand SaveOrderInfoCommand  => new RelayCommand(SaveOrderInfo);
        public ICommand AddDeviceCommand      => new RelayCommand(AddDevice,      () => _isEditMode);
        public ICommand EditDeviceCommand     => new RelayCommand(EditDevice,     () => _isEditMode && SelectedDevice != null);
        public ICommand RemoveDeviceCommand   => new RelayCommand(RemoveDevice,   () => _isEditMode && SelectedDevice != null);
        public ICommand ManagePartsCommand    => new RelayCommand(ManageParts,    () => _isEditMode && SelectedDevice != null);
        public ICommand RegisterPaymentCommand => new RelayCommand(RegisterPayment, () => _isEditMode && AddPaymentAmount > 0);
        public ICommand CloseCommand          => new RelayCommand(() => RequestClose?.Invoke());

        private void SaveOrderInfo()
        {
            if (!Validate(out string err)) { _dialogService.ShowError("خطأ", err); return; }

            try
            {
                var input = BuildInput();

                if (_isEditMode)
                {
                    _maintenanceService.UpdateOrderInfo(_orderId, input);
                    _dialogService.ShowSuccess("تم", "تم تحديث بيانات الطلب.");
                }
                else
                {
                    var order = _maintenanceService.CreateOrder(input, _auth.GetUserId());
                    _orderId    = order.Id;
                    _isEditMode = true;
                    CreatedOrderId = _orderId;
                    Saved = true;
                    OnPropertyChanged(nameof(IsEditMode));
                    OnPropertyChanged(nameof(Title));
                    LoadPayments();
                    _dialogService.ShowSuccess("تم", $"تم إنشاء الطلب رقم {order.OrderNumber}");
                    SelectedTabIndex = 1;
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void ManageParts()
        {
            if (SelectedDevice == null) return;
            var title = SelectedDevice.DisplayName;
            var vm    = new RepairPartsViewModel(SelectedDevice.Id, _orderId, title);
            var dlg   = new Views.RepairPartsDialog(vm);
            DialogService.ConfigureOwnedWindow(dlg);
            dlg.ShowDialog();
            LoadDevices();
            RefreshTotals();
        }

        private void AddDevice()
        {
            var vm = new RepairDeviceFormViewModel(_orderId);
            var dlg = new Views.RepairDeviceDialog(vm);
            DialogService.ConfigureOwnedWindow(dlg);
            dlg.ShowDialog();
            if (vm.Saved) { LoadDevices(); RefreshTotals(); }
        }

        private void EditDevice()
        {
            if (SelectedDevice == null) return;
            var devices = _maintenanceService.GetDevices(_orderId);
            var device  = devices.Find(d => d.Id == SelectedDevice.Id);
            if (device == null) return;

            var vm = new RepairDeviceFormViewModel(_orderId, device);
            var dlg = new Views.RepairDeviceDialog(vm);
            DialogService.ConfigureOwnedWindow(dlg);
            dlg.ShowDialog();
            if (vm.Saved) { LoadDevices(); RefreshTotals(); }
        }

        private void RemoveDevice()
        {
            if (SelectedDevice == null) return;
            if (!_dialogService.Confirm("حذف جهاز", $"هل تريد حذف '{SelectedDevice.DisplayName}'؟ سيتم استعادة قطع المخزون تلقائياً.")) return;

            try
            {
                _maintenanceService.RemoveDevice(SelectedDevice.Id, _auth.GetUserId());
                LoadDevices();
                RefreshTotals();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void RegisterPayment()
        {
            if (AddPaymentAmount <= 0) return;
            try
            {
                bool hasNoProfitBasis = HasNoProfitBasis();
                _maintenanceService.RegisterPayment(_orderId, AddPaymentAmount, AddPaymentMethod, _auth.GetUserId(), AddPaymentNotes);
                AddPaymentAmount = 0;
                AddPaymentNotes  = string.Empty;
                LoadPayments();
                RefreshTotals();
                if (hasNoProfitBasis)
                {
                    _dialogService.ShowWarning("تنبيه", "تم تسجيل الدفعة كتحصيل، لكن ربح الصيانة سيظل 0 لأن أجر العمل وهامش القطع غير مسجلين.");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private bool HasNoProfitBasis()
        {
            decimal labor = 0m;
            foreach (var d in _maintenanceService.GetDevices(_orderId))
                labor += d.LaborCost;

            decimal partsMargin = 0m;
            foreach (var p in _maintenanceService.GetOrderParts(_orderId))
                partsMargin += p.TotalCost - (p.PurchaseCost * p.Quantity);

            return labor == 0m && partsMargin <= 0m;
        }

        private void LoadDevices()
        {
            Devices.Clear();
            foreach (var d in _maintenanceService.GetDevices(_orderId))
            {
                Devices.Add(new RepairDeviceRowViewModel
                {
                    Id            = d.Id,
                    DeviceType    = d.DeviceType,
                    DisplayName   = d.DisplayName,
                    ReportedIssue = d.ReportedIssue,
                    DeviceStatus  = d.DeviceStatus,
                    LaborCost     = d.LaborCost
                });
            }
        }

        private void LoadPayments()
        {
            Payments.Clear();
            foreach (var p in _maintenanceService.GetPayments(_orderId))
            {
                Payments.Add(new RepairPaymentRowViewModel
                {
                    Id            = p.Id,
                    Amount        = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    PaymentDate   = p.PaymentDate.ToString("yyyy-MM-dd HH:mm"),
                    Notes         = p.Notes
                });
            }
            RefreshTotals();
        }

        private void RefreshTotals()
        {
            var order = _maintenanceService.GetOrder(_orderId);
            if (order == null) return;
            OrderTotal     = order.TotalAmount;
            OrderPaid      = order.PaidAmount;
            OrderRemaining = order.RemainingAmount;
        }

        private void SearchCustomers()
        {
            SearchedCustomers.Clear();
            if (string.IsNullOrWhiteSpace(CustomerSearch)) return;
            foreach (var c in _customerService.SearchCustomers(CustomerSearch))
                SearchedCustomers.Add(c);
        }

        private RepairOrderInput BuildInput() => new RepairOrderInput
        {
            CustomerId = SelectedCustomer?.Id,
            CustomerName = CustomerName,
            CustomerPhone = CustomerPhone,
            TechnicianName = TechnicianName,
            ExpectedDelivery = ExpectedDelivery,
            Notes = Notes,
            InitialPayment = _isEditMode ? 0 : InitialPayment,
            InitialPaymentMethod = InitialPaymentMethod
        };

        private bool Validate(out string err)
        {
            err = string.Empty;
            if (string.IsNullOrWhiteSpace(CustomerName))
            { err = "اسم العميل مطلوب."; return false; }
            return true;
        }
    }
}
