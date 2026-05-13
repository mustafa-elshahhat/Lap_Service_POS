using System.Collections.ObjectModel;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Shared.Helpers;
using CarPartsShopWPF.Presentation.Views;
using CarPartsShopWPF.Domain.Entities;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class SuppliersViewModel : BaseViewModel
    {
        private readonly ISupplierService _supplierService;
        private readonly IDialogService _dialogService;

        private ObservableCollection<Supplier> _suppliers;
        private Supplier _selectedSupplier;
        private string _searchText;

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
                out decimal amount,
                out string method,
                out decimal paid) == true)
            {
                try
                {
                    _supplierService.AddSupplierPurchase(SelectedSupplier.Id, amount, method);
                    if (paid > 0)
                    {
                        _supplierService.AddSupplierPayment(SelectedSupplier.Id, paid, method);
                    }

                    _dialogService.ShowSuccess("نجاح", "تم تسجيل المشتريات بنجاح");
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
            _dialogService.ShowSupplierTransactionsDialog(SelectedSupplier.Id, SelectedSupplier.Name);
            OnRequestSearchFocus();
        }
    }
}
