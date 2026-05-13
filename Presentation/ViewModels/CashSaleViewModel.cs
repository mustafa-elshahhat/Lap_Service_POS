using System;
using System.Collections.Generic;
using System.Windows.Input;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{
    public class CashSaleViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;
        private decimal _totalAmount;
        private string _paymentMethod;
        private string _customerName;
        private string _customerPhone;

        public Action<bool> CloseAction { get; set; }

        public CashSaleViewModel(decimal totalAmount, IDialogService dialogService)
        {
            _totalAmount = totalAmount;
            _dialogService = dialogService;
            _paymentMethod = PaymentMethods.Cash;
        }

        public ICommand ConfirmCommand => new RelayCommand(Confirm);
        public ICommand CancelCommand => new RelayCommand(Cancel);

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

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

        public List<string> MethodList => PaymentMethods.GetAll();

        private void Confirm()
        {
            CloseAction?.Invoke(true);
        }

        private void Cancel()
        {
            CloseAction?.Invoke(false);
        }
    }
}
