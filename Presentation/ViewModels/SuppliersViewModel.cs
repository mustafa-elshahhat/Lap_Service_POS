using System.Collections.ObjectModel;
using System.Windows.Input;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Shared.Helpers;
using AlJohary.ServiceHub.Presentation.Views;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Application.DTOs;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{
    public class SuppliersViewModel : BaseViewModel
    {
        private readonly ISupplierService _supplierService;
        private readonly IDialogService _dialogService;

        private ObservableCollection<Supplier> _suppliers;
        private Supplier _selectedSupplier;
        private string _searchText;

        public System.Action<Supplier> NavigateToTransactionsAction { get; set; }

        public SuppliersViewModel(IDialogService dialogService = null)
        {
            _supplierService = ServiceContainer.GetService<ISupplierService>();
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            Suppliers = new ObservableCollection<Supplier>();
            LoadSuppliers();
        }

        public ObservableCollection<Supplier> Suppliers
        {
            get => _suppliers;
            set => SetProperty(ref _suppliers, value);
        }

        public Supplier SelectedSupplier
        {
            get => _selectedSupplier;
            set => SetProperty(ref _selectedSupplier, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    LoadSuppliers();
                }
            }
        }

        public ICommand LoadSuppliersCommand => new RelayCommand(LoadSuppliers);
        public ICommand AddSupplierCommand => new RelayCommand(AddSupplier);
        public ICommand EditSupplierCommand => new RelayCommand(EditSupplier, () => SelectedSupplier != null);
        public ICommand DeleteSupplierCommand => new RelayCommand(DeleteSupplier, () => SelectedSupplier != null);
        public ICommand AddPaymentCommand => new RelayCommand(AddPayment, () => SelectedSupplier != null);
        public ICommand AddPurchaseCommand => new RelayCommand(AddPurchase, () => SelectedSupplier != null);
        public ICommand ViewTransactionsCommand => new RelayCommand(ViewTransactions, () => SelectedSupplier != null);
        public ICommand LoadAllCommand => new RelayCommand(LoadAll);

        private void LoadAll()
        {
            SearchText = "";
            LoadSuppliers();
            OnRequestSearchFocus();
        }

        private void LoadSuppliers()
        {
            try
            {
                var suppliers = _supplierService.SearchSuppliers(SearchText);
                Suppliers.Clear();
                foreach (var supplier in suppliers)
                {
                    Suppliers.Add(supplier);
                }
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void AddSupplier()
        {
            try
            {
                var vm = new SupplierFormViewModel(false);
                if (_dialogService.ShowSupplierFormDialog(vm) == true)
                {
                    _supplierService.CreateSupplier(vm.Name, vm.Phone, vm.Address);
                    _dialogService.ShowSuccess("نجاح", "تم إضافة المورد بنجاح");
                    LoadSuppliers();
                }
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowError("خطأ", $"حدث خطأ أثناء الحفظ:\n{ex.Message}");
            }
            OnRequestSearchFocus();
        }

        private void EditSupplier()
        {
            if (SelectedSupplier == null) return;

            try
            {
                var vm = new SupplierFormViewModel(true)
                {
                    Name = SelectedSupplier.Name,
                    Phone = SelectedSupplier.Phone,
                    Address = SelectedSupplier.Address
                };

                if (_dialogService.ShowSupplierFormDialog(vm) == true)
                {
                    _supplierService.UpdateSupplier(SelectedSupplier.Id, vm.Name, vm.Phone, vm.Address);
                    _dialogService.ShowSuccess("نجاح", "تم تعديل بيانات المورد بنجاح");
                    LoadSuppliers();
                }
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowError("خطأ", $"حدث خطأ أثناء التحديث:\n{ex.Message}");
            }
            OnRequestSearchFocus();
        }

        private void DeleteSupplier()
        {
            if (SelectedSupplier == null) return;

            if (_dialogService.Confirm("تأكيد الحذف", $"هل تريد حذف المورد:\n{SelectedSupplier.Name}؟"))
            {
                try
                {
                    _supplierService.DeleteSupplier(SelectedSupplier.Id);
                    LoadSuppliers();
                }
                catch (System.Exception ex)
                {
                    _dialogService.ShowError("خطأ", ex.Message);
                }
            }
            OnRequestSearchFocus();
        }

        private void AddPayment()
        {
            if (SelectedSupplier == null) return;

            if (SelectedSupplier.TotalDebt <= 0)
            {
                _dialogService.ShowInfo("تنبيه", "لا توجد مديونية على هذا المورد");
                OnRequestSearchFocus();
                return;
            }

            if (_dialogService.ShowSupplierPaymentDialog(
                SelectedSupplier.Name,
                SelectedSupplier.TotalDebt,
                out decimal amount,
                out string method) == true)
            {
                try
                {
                    _supplierService.AddSupplierPayment(SelectedSupplier.Id, amount, method);
                    _dialogService.ShowSuccess("نجاح", "تم تسجيل الدفعة بنجاح");
                    LoadSuppliers();
                    TypedMessenger.Send("RefreshReports");
                }
                catch (System.Exception ex)
                {
                    _dialogService.ShowError("خطأ", ex.Message);
                }
            }
            OnRequestSearchFocus();
        }

        private void AddPurchase()
        {
            if (SelectedSupplier == null) return;

            if (_dialogService.ShowSupplierPurchaseDialog(
                SelectedSupplier.Name,
                SelectedSupplier.TotalDebt,
                out SupplierPurchaseDialogResult purchase) == true)
            {
                try
                {
                    var result = _supplierService.AddSupplierPurchaseWithItems(
                        SelectedSupplier.Id,
                        purchase.TotalAmount,
                        purchase.PaidAmount,
                        purchase.PaymentMethod,
                        purchase.Lines);
                    if (!result.Success)
                    {
                        throw new System.Exception(result.Message);
                    }

                    _dialogService.ShowSuccess("نجاح", result.Message);
                    LoadSuppliers();
                    TypedMessenger.Send("RefreshReports");
                }
                catch (System.Exception ex)
                {
                    _dialogService.ShowError("خطأ", ex.Message);
                }
            }
            OnRequestSearchFocus();
        }

        private void ViewTransactions()
        {
            if (SelectedSupplier == null) return;
            NavigateToTransactionsAction?.Invoke(SelectedSupplier);
            OnRequestSearchFocus();
        }
    }
}
