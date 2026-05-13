using System;
using System.Windows;
using System.Linq;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Shared.Helpers;
using CarPartsShopWPF.Presentation.Views;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class CreditSaleViewModel : BaseViewModel
    {
        public Action<bool> CloseAction { get; set; }
        private readonly IDialogService _dialogService;
        private readonly decimal _totalAmount;

        private string _customerName;
        private string _customerPhone;
        private string _paidAmountText = "0";
        private string _paymentMethod = PaymentMethods.Cash;

        public CreditSaleViewModel(decimal totalAmount, IDialogService dialogService, Action<bool> closeAction = null)
        {
            _totalAmount = totalAmount;
            _dialogService = dialogService;
            CloseAction = closeAction;
        }

        public string TotalAmountText => Formatting.FormatCurrency(_totalAmount);

        public string CustomerName
        {
            get => _customerName;
            set
            {
                _customerName = value;
                OnPropertyChanged(nameof(CustomerName));
            }
        }

        public string CustomerPhone
        {
            get => _customerPhone;
            set
            {
                _customerPhone = value;
                OnPropertyChanged(nameof(CustomerPhone));
            }
        }

        public string PaidAmountText
        {
            get => _paidAmountText;
            set
            {
                _paidAmountText = value;
                OnPropertyChanged(nameof(PaidAmountText));
            }
        }

        public string PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                _paymentMethod = value;
                OnPropertyChanged(nameof(PaymentMethod));
            }
        }

        public decimal PaidAmount => SafeConvert.ToDecimal(PaidAmountText);
        public System.Collections.Generic.List<string> PaymentMethodsList => PaymentMethods.GetAll();

        public ICommand ConfirmCommand => new RelayCommand(ExecuteConfirm);
        public ICommand CancelCommand => new RelayCommand(ExecuteCancel);
        public ICommand SelectCustomerCommand => new RelayCommand(ExecuteSelectCustomer);

        private void ExecuteSelectCustomer()
        {
            var vm = new CustomerSelectionViewModel(_dialogService);
            var view = new CustomerSelectionDialog 
            { 
                DataContext = vm,
                Owner = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) 
            };
            
            vm.CloseAction = () => view.Close();
            vm.OnCustomerSelected = (customer) =>
            {
                CustomerName = customer.Name;
                CustomerPhone = customer.Phone;
            };

            view.ShowDialog();
        }

        private void ExecuteConfirm()
        {
            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                _dialogService.ShowWarning("تنبيه", "الرجاء إدخال اسم العميل");
                return;
            }

            if (string.IsNullOrWhiteSpace(CustomerPhone) || CustomerPhone.Length < 11)
            {
                _dialogService.ShowWarning("تنبيه", "رقم الهاتف مطلوب وبشكل صحيح لضمان حقوق المحل (11 رقم)");
                return;
            }

            CloseAction?.Invoke(true);
        }

        private void ExecuteCancel()
        {
            CloseAction?.Invoke(false);
        }
    }
}

