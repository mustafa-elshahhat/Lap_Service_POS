using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Shared.Helpers;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Presentation.Views;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class CustomerPaymentViewModel : BaseViewModel
    {
        private readonly ISaleService _saleService;
        private readonly ICustomerService _customerService;
        private readonly IDialogService _dialogService;
        private readonly Dictionary<string, object> _customer;

        private List<Sale> _currentInvoices;
        private ObservableCollection<string> _paymentMethods;
        private string _selectedPaymentMethod;
        private string _customerNameHeader;
        private string _totalDebtText;
        private string _paymentAmountText;
        private string _validationMessage;
        private string _summaryAmount;
        private string _summaryRemaining;

        private string _selectedInvoiceText;
        private string _invTotalText;
        private string _invRemainingText;
        private bool _isInvoiceInfoVisible;
        private bool _isPayButtonEnabled;

        private decimal _maxPayable = 0;
        private List<Sale> _selectedInvoices = new List<Sale>();

        public Action CloseAction { get; set; }
        public bool DataChanged { get; private set; }

        public CustomerPaymentViewModel(Dictionary<string, object> customer, IDialogService dialogService = null)
        {
            _saleService = ServiceContainer.GetService<ISaleService>();
            _customerService = ServiceContainer.GetService<ICustomerService>();
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            _customer = customer;

            _paymentMethods = new ObservableCollection<string>(Shared.Helpers.PaymentMethods.GetAll());
            SelectedPaymentMethod = Shared.Helpers.PaymentMethods.Cash;

            LoadCustomerInfo();
            LoadUnpaidInvoices();
        }

        #region Properties

        public ObservableCollection<string> PaymentMethods => _paymentMethods;

        public string SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set => SetProperty(ref _selectedPaymentMethod, value);
        }

        public List<Sale> Invoices
        {
            get => _currentInvoices;
            set => SetProperty(ref _currentInvoices, value);
        }

        public string CustomerNameHeader
        {
            get => _customerNameHeader;
            set => SetProperty(ref _customerNameHeader, value);
        }

        public string TotalDebtText
        {
            get => _totalDebtText;
            set => SetProperty(ref _totalDebtText, value);
        }

        public string PaymentAmountText
        {
            get => _paymentAmountText;
            set
            {
                if (SetProperty(ref _paymentAmountText, value))
                {
                    ValidatePayment();
                    UpdateSummary();
                }
            }
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        public string SummaryAmount
        {
            get => _summaryAmount;
            set => SetProperty(ref _summaryAmount, value);
        }

        public string SummaryRemaining
        {
            get => _summaryRemaining;
            set => SetProperty(ref _summaryRemaining, value);
        }

        public string SelectedInvoiceText
        {
            get => _selectedInvoiceText;
            set => SetProperty(ref _selectedInvoiceText, value);
        }

        public string InvTotalText
        {
            get => _invTotalText;
            set => SetProperty(ref _invTotalText, value);
        }

        public string InvRemainingText
        {
            get => _invRemainingText;
            set => SetProperty(ref _invRemainingText, value);
        }

        public bool IsInvoiceInfoVisible
        {
            get => _isInvoiceInfoVisible;
            set => SetProperty(ref _isInvoiceInfoVisible, value);
        }

        public bool IsPayButtonEnabled
        {
            get => _isPayButtonEnabled;
            set => SetProperty(ref _isPayButtonEnabled, value);
        }

        #endregion

        #region Commands

        public ICommand PayCommand => new RelayCommand(Pay, () => IsPayButtonEnabled);
        public ICommand CloseCommand => new RelayCommand(Close);

        #endregion

        #region Methods

        public void UpdateSelection(System.Collections.IList selectedItems)
        {
             _selectedInvoices.Clear();
             if (selectedItems != null)
             {
                 foreach (var item in selectedItems)
                 {
                     if (item is Sale inv)
                         _selectedInvoices.Add(inv);
                 }
             }
             OnSelectionChanged();
        }

        private void LoadCustomerInfo()
        {
            if (_customer != null)
            {
                CustomerNameHeader = $"العميل: {SafeConvert.ToString(_customer["name"])}";
                var freshCust = _customerService.GetById(SafeConvert.ToInt(_customer["id"]));
                decimal totalDebt = freshCust?.TotalCredit ?? 0;
                TotalDebtText = Formatting.FormatCurrency(totalDebt);
            }
        }

        private void LoadUnpaidInvoices()
        {
            int customerId = SafeConvert.ToInt(_customer["id"]);
            Invoices = _saleService.GetUnpaidInvoices(customerId);

            IsInvoiceInfoVisible = false;
            PaymentAmountText = "";
            IsPayButtonEnabled = false;
            UpdateSummary();
        }

        private void OnSelectionChanged()
        {
            if (_selectedInvoices.Count > 0)
            {
                IsInvoiceInfoVisible = true;

                if (_selectedInvoices.Count == 1)
                    SelectedInvoiceText = $"الفاتورة: {_selectedInvoices[0].InvoiceNumber}";
                else
                    SelectedInvoiceText = $"تم تحديد: {_selectedInvoices.Count} فواتير";

                decimal total = 0;
                _maxPayable = 0;

                foreach (var inv in _selectedInvoices)
                {
                    total += inv.TotalAmount;
                    _maxPayable += inv.RemainingAmount;
                }

                InvTotalText = Formatting.FormatCurrency(total);
                InvRemainingText = Formatting.FormatCurrency(_maxPayable);
                PaymentAmountText = _maxPayable.ToString("N2");

                ValidatePayment();
                UpdateSummary();
            }
            else
            {
                _maxPayable = 0;
                IsInvoiceInfoVisible = false;
                PaymentAmountText = "0";
                ValidatePayment();
                UpdateSummary();
            }
        }

        private void ValidatePayment()
        {
            ValidationMessage = "";
            IsPayButtonEnabled = false;

            if (_selectedInvoices.Count == 0)
            {
                ValidationMessage = "اختر فاتورة واحدة على الأقل";
                return;
            }

            if (!decimal.TryParse(PaymentAmountText, out decimal amount))
            {
                ValidationMessage = "أدخل مبلغ صحيح";
                return;
            }

            if (amount <= 0)
            {
                ValidationMessage = "المبلغ يجب أن يكون أكبر من صفر";
                return;
            }

            if (amount > _maxPayable)
            {
                ValidationMessage = $"المبلغ يتجاوز المتبقي ({_maxPayable:N2})";
                return;
            }

            IsPayButtonEnabled = true;
        }

        private void UpdateSummary()
        {
            decimal payAmount = 0;
            if (decimal.TryParse(PaymentAmountText, out decimal parsed))
            {
                payAmount = parsed;
            }

            decimal remaining = _maxPayable - payAmount;
            if (remaining < 0) remaining = 0;

            SummaryAmount = Formatting.FormatCurrency(payAmount);
            SummaryRemaining = Formatting.FormatCurrency(remaining);
        }

        private void Pay()
        {
            if (!decimal.TryParse(PaymentAmountText, out decimal totalPayAmount) || totalPayAmount <= 0)
            {
                ValidationMessage = "أدخل مبلغ صحيح";
                return;
            }

            if (totalPayAmount > _maxPayable)
            {
                ValidationMessage = "المبلغ يتجاوز المتبقي";
                return;
            }

            string paymentMethod = SelectedPaymentMethod;
            if (string.IsNullOrEmpty(paymentMethod))
            {
                paymentMethod = Shared.Helpers.PaymentMethods.Cash;
            }

            try
            {
                decimal remainingToDistribute = totalPayAmount;

                foreach (var invoice in _selectedInvoices)
                {
                    if (remainingToDistribute <= 0) break;

                    decimal invRemaining = invoice.RemainingAmount;
                    decimal payForThis = Math.Min(invRemaining, remainingToDistribute);

                    int saleId = (int)invoice.Id;
                    _saleService.PayInvoiceAmount(saleId, payForThis, paymentMethod, "سداد جزئي/آجل");

                    remainingToDistribute -= payForThis;
                }

                _dialogService.ShowSuccess("تم السداد", $"تم سداد {Formatting.FormatCurrency(totalPayAmount)} بنجاح");

                DataChanged = true;
                LoadCustomerInfo();
                LoadUnpaidInvoices();
                TypedMessenger.Send("RefreshReports");
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void Close()
        {
            CloseAction?.Invoke();
        }

        #endregion
    }
}
