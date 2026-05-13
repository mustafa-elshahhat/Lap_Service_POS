using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class RepairOrderRowViewModel : BaseViewModel
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string TechnicianName { get; set; }
        public int DeviceCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string OrderStatus { get; set; }
        public string OrderStatusAr => RepairStatus.ToArabic(OrderStatus);
        public string OrderStatusColor => RepairStatus.GetColorKey(OrderStatus);
        public string IntakeDate { get; set; }
        public string ExpectedDelivery { get; set; }
    }

    public class MaintenanceViewModel : BaseViewModel
    {
        private readonly IMaintenanceService _maintenanceService;
        private readonly IAuthService _auth;
        private readonly IDialogService _dialogService;
        private readonly IPrintService _printService;

        private ObservableCollection<RepairOrderRowViewModel> _orders;
        private RepairOrderRowViewModel _selectedOrder;
        private string _searchText;
        private string _statusFilter;
        private string _statusFilterAr;
        private bool _isLoading;
        private static readonly Dictionary<string, string> _arToKey = BuildArToKeyMap();
        private static readonly Dictionary<string, string> _keyToAr = BuildKeyToArMap();

        private static Dictionary<string, string> BuildArToKeyMap()
        {
            var d = new Dictionary<string, string> { { "الكل", "all" } };
            foreach (var s in RepairStatus.GetAll()) d[RepairStatus.ToArabic(s)] = s;
            return d;
        }
        private static Dictionary<string, string> BuildKeyToArMap()
        {
            var d = new Dictionary<string, string> { { "all", "الكل" } };
            foreach (var s in RepairStatus.GetAll()) d[s] = RepairStatus.ToArabic(s);
            return d;
        }

        public MaintenanceViewModel()
        {
            _maintenanceService = ServiceContainer.GetService<IMaintenanceService>();
            _auth               = ServiceContainer.GetService<IAuthService>();
            _dialogService      = ServiceContainer.GetService<IDialogService>();
            _printService       = ServiceContainer.GetService<IPrintService>();

            Orders          = new ObservableCollection<RepairOrderRowViewModel>();
            StatusFilters   = new ObservableCollection<string>();
            StatusFilters.Add("all");
            foreach (var s in RepairStatus.GetAll()) StatusFilters.Add(s);
            StatusFiltersAr = new ObservableCollection<string>();
            StatusFiltersAr.Add("الكل");
            foreach (var s in RepairStatus.GetAll()) StatusFiltersAr.Add(RepairStatus.ToArabic(s));
            StatusFilter    = "all";
            _statusFilterAr = "الكل";

            LoadOrders();
        }

        public ObservableCollection<RepairOrderRowViewModel> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        public RepairOrderRowViewModel SelectedOrder
        {
            get => _selectedOrder;
            set => SetProperty(ref _selectedOrder, value);
        }

        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); LoadOrders(); }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { SetProperty(ref _statusFilter, value); LoadOrders(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<string> StatusFilters   { get; }
        public ObservableCollection<string> StatusFiltersAr { get; }

        public string StatusFilterAr
        {
            get => _statusFilterAr;
            set
            {
                SetProperty(ref _statusFilterAr, value);
                if (_arToKey.TryGetValue(value ?? "الكل", out var key))
                    StatusFilter = key;
            }
        }

        public ICommand NewOrderCommand      => new RelayCommand(NewOrder);
        public ICommand OpenOrderCommand     => new RelayCommand(OpenOrder,    () => SelectedOrder != null);
        public ICommand CancelOrderCommand   => new RelayCommand(CancelOrder,  () => SelectedOrder != null && !RepairStatus.IsFinal(SelectedOrder?.OrderStatus));
        public ICommand DeliverOrderCommand  => new RelayCommand(DeliverOrder, () => SelectedOrder?.OrderStatus == RepairStatus.Completed);
        public ICommand PrintIntakeCommand   => new RelayCommand(PrintIntake,  () => SelectedOrder != null);
        public ICommand PrintInvoiceCommand  => new RelayCommand(PrintInvoice, () => SelectedOrder != null);
        public ICommand RefreshCommand       => new RelayCommand(LoadOrders);

        private void NewOrder()
        {
            var vm  = new RepairOrderFormViewModel();
            var dlg = new Views.RepairOrderDialog(vm);
            dlg.Owner = System.Windows.Application.Current.MainWindow;
            dlg.ShowDialog();
            LoadOrders();
        }

        private void OpenOrder()
        {
            if (SelectedOrder == null) return;
            var order = _maintenanceService.GetOrder(SelectedOrder.Id);
            if (order == null) return;

            var vm  = new RepairOrderFormViewModel(order);
            var dlg = new Views.RepairOrderDialog(vm);
            dlg.Owner = System.Windows.Application.Current.MainWindow;
            dlg.ShowDialog();
            LoadOrders();
        }

        private void CancelOrder()
        {
            if (SelectedOrder == null) return;
            if (!_dialogService.Confirm("إلغاء طلب", $"هل تريد إلغاء الطلب {SelectedOrder.OrderNumber}؟\nسيتم استعادة قطع المخزون تلقائياً."))
                return;

            try
            {
                _maintenanceService.CancelOrder(SelectedOrder.Id, _auth.GetUserId());
                _dialogService.ShowSuccess("تم", "تم إلغاء الطلب.");
                LoadOrders();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void DeliverOrder()
        {
            if (SelectedOrder == null) return;
            if (SelectedOrder.RemainingAmount > 0)
            {
                if (!_dialogService.Confirm("تسليم مع رصيد متبقي", $"يوجد مبلغ متبقي {SelectedOrder.RemainingAmount:N2} ج.م\nهل تريد المتابعة؟"))
                    return;
            }

            try
            {
                _maintenanceService.MarkDelivered(SelectedOrder.Id, _auth.GetUserId());
                _dialogService.ShowSuccess("تم التسليم", $"تم تسليم الطلب {SelectedOrder.OrderNumber}.");
                LoadOrders();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void PrintIntake()
        {
            if (SelectedOrder == null) return;
            try
            {
                var order   = _maintenanceService.GetOrder(SelectedOrder.Id);
                var devices = _maintenanceService.GetDevices(SelectedOrder.Id);
                _printService.PrintRepairIntake(order, devices);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ في الطباعة", ex.Message);
            }
        }

        private void PrintInvoice()
        {
            if (SelectedOrder == null) return;
            try
            {
                var order    = _maintenanceService.GetOrder(SelectedOrder.Id);
                var devices  = _maintenanceService.GetDevices(SelectedOrder.Id);
                var parts    = _maintenanceService.GetOrderParts(SelectedOrder.Id);
                var payments = _maintenanceService.GetPayments(SelectedOrder.Id);
                _printService.PrintRepairInvoice(order, devices, parts, payments);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ في الطباعة", ex.Message);
            }
        }

        private void LoadOrders()
        {
            try
            {
                IsLoading = true;
                Orders.Clear();
                var filter = StatusFilter == "all" ? null : StatusFilter;
                foreach (var o in _maintenanceService.GetOrders(filter, SearchText))
                {
                    Orders.Add(new RepairOrderRowViewModel
                    {
                        Id               = o.Id,
                        OrderNumber      = o.OrderNumber,
                        CustomerName     = o.CustomerName,
                        CustomerPhone    = o.CustomerPhone,
                        TechnicianName   = o.TechnicianName,
                        DeviceCount      = o.DeviceCount,
                        TotalAmount      = o.TotalAmount,
                        PaidAmount       = o.PaidAmount,
                        RemainingAmount  = o.RemainingAmount,
                        OrderStatus      = o.OrderStatus,
                        IntakeDate       = o.IntakeDate.ToString("yyyy-MM-dd"),
                        ExpectedDelivery = o.ExpectedDelivery?.ToString("yyyy-MM-dd") ?? "-"
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "MaintenanceViewModel.LoadOrders");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
