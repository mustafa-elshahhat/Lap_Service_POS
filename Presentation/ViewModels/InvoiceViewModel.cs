using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class InvoiceViewModel : BaseViewModel
    {
        private readonly ISaleService _saleService;
        private readonly IDialogService _dialogService;
        private readonly IPrintService _printService;
        private readonly string _invoiceNumber;

        private Sale _sale;
        private List<SaleItem> _items;

        private string _invoiceMeta;
        private string _totalText;
        private string _returnsText;
        private string _paidText;
        private string _remainingText;
        private string _statusText;
        private Brush _statusForeground;
        private Brush _statusBackground;
        private ObservableCollection<ReturnLineItem> _returnLines;
        private ObservableCollection<PaymentDisplayItem> _paymentMethods;

        private string _refundTotalText;
        private bool _isRefundPreviewVisible;
        private bool _isRefundButtonEnabled;
        private string _refundButtonContent;

        public Action CloseAction { get; set; }
        public Action PrintAction { get; set; }

        public InvoiceViewModel(string invoiceNumber, IDialogService dialogService = null)
        {
            _invoiceNumber = invoiceNumber;
            _saleService = ServiceContainer.GetService<ISaleService>();
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            _printService = ServiceContainer.GetService<IPrintService>();
            LoadInvoice();
        }

        #region Properties

        public string InvoiceMeta { get => _invoiceMeta; set => SetProperty(ref _invoiceMeta, value); }
        public string TotalText { get => _totalText; set => SetProperty(ref _totalText, value); }
        public string ReturnsText { get => _returnsText; set => SetProperty(ref _returnsText, value); }
        public string PaidText { get => _paidText; set => SetProperty(ref _paidText, value); }
        public string RemainingText { get => _remainingText; set => SetProperty(ref _remainingText, value); }
        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
        public Brush StatusForeground { get => _statusForeground; set => SetProperty(ref _statusForeground, value); }
        public Brush StatusBackground { get => _statusBackground; set => SetProperty(ref _statusBackground, value); }

        public ObservableCollection<ReturnLineItem> ReturnLines { get => _returnLines; set => SetProperty(ref _returnLines, value); }
        public ObservableCollection<PaymentDisplayItem> PaymentMethods { get => _paymentMethods; set => SetProperty(ref _paymentMethods, value); }

        public string RefundTotalText { get => _refundTotalText; set => SetProperty(ref _refundTotalText, value); }
        public bool IsRefundPreviewVisible { get => _isRefundPreviewVisible; set => SetProperty(ref _isRefundPreviewVisible, value); }
        public bool IsRefundButtonEnabled { get => _isRefundButtonEnabled; set => SetProperty(ref _isRefundButtonEnabled, value); }
        public string RefundButtonContent { get => _refundButtonContent; set => SetProperty(ref _refundButtonContent, value); }

        #endregion

        #region Commands

        public ICommand CloseCommand => new RelayCommand(() => CloseAction?.Invoke());
        public ICommand PrintCommand => new RelayCommand(PrintInvoice);
        public ICommand ProcessRefundCommand => new RelayCommand(ProcessRefund);
        public ICommand IncReturnCommand => new RelayCommand<ReturnLineItem>(IncReturn);
        public ICommand DecReturnCommand => new RelayCommand<ReturnLineItem>(DecReturn);

        #endregion

        #region Methods

        public void LoadInvoice()
        {
            try
            {
                _sale = _saleService.GetByInvoiceNumber(_invoiceNumber);

                if (_sale == null)
                {
                    _dialogService.ShowError("خطأ", "لم يتم العثور على الفاتورة");
                    CloseAction?.Invoke();
                    return;
                }

                int saleId = (int)_sale.Id;
                _items = _saleService.GetSaleItems(saleId);

                var returnedMap = _saleService.GetReturnedQuantities(saleId);
                decimal totalReturnedValue = 0;

                ReturnLines = new ObservableCollection<ReturnLineItem>();
                int idx = 1;
                foreach (var it in _items)
                {
                    var q = it.Quantity;
                    var unit = it.UnitFinalPrice;
                    int saleItemId = (int)it.Id;
                    int alreadyRet = returnedMap.ContainsKey(saleItemId) ? returnedMap[saleItemId] : 0;

                    if (alreadyRet > 0)
                    {
                        totalReturnedValue += alreadyRet * unit;
                    }

                    var line = new ReturnLineItem
                    {
                        Index = idx,
                        SaleItemId = saleItemId,
                        ProductName = it.ProductName,
                        Quantity = q,
                        UnitPrice = unit,
                        TotalPrice = it.TotalPrice,
                        AlreadyReturned = alreadyRet,
                        PaidAmount = it.PaidAmount,
                        RemainingAmount = it.RemainingAmount,
                        ReturnQuantity = 0
                    };

                    line.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ReturnLineItem.ReturnQuantity))
                            UpdateRefundTotals();
                    };

                    ReturnLines.Add(line);
                    idx++;
                }

                var dateStr = _sale.SaleDate.ToString("yyyy/MM/dd HH:mm");
                var userName = _sale.UserName;
                InvoiceMeta = $"التاريخ: {dateStr}  |  المستخدم: {userName}";

                decimal originalTotal = _sale.TotalAmount;
                decimal originalPaid = _sale.PaidAmount;
                decimal originalRemaining = _sale.RemainingAmount;
                decimal currentTotal = originalTotal - totalReturnedValue;
                decimal netPaid = originalPaid > totalReturnedValue ? (originalPaid - totalReturnedValue) : 0;

                TotalText = Formatting.FormatCurrency(currentTotal);
                ReturnsText = Formatting.FormatCurrency(totalReturnedValue);
                PaidText = Formatting.FormatCurrency(netPaid);
                RemainingText = Formatting.FormatCurrency(originalRemaining);

                UpdateStatusBadge(originalRemaining, currentTotal);
                UpdateRefundTotals();

                var payments = _saleService.GetSalePaymentsBreakdown(saleId);
                PaymentMethods = new ObservableCollection<PaymentDisplayItem>();
                bool showAmounts = payments.Count > 1;

                foreach (var kvp in payments)
                {
                    PaymentMethods.Add(new PaymentDisplayItem
                    {
                        Key = kvp.Key,
                        Value = kvp.Value,
                        ShowAmount = showAmounts
                    });
                }
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
                CloseAction?.Invoke();
            }
        }

        private void UpdateStatusBadge(decimal remaining, decimal total)
        {
            if (remaining <= 0)
            {
                StatusText = "مدفوعة بالكامل";
                StatusForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
                StatusBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4"));
            }
            else if (remaining < total)
            {
                StatusText = "مدفوعة جزئياً";
                StatusForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EA580C"));
                StatusBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7ED"));
            }
            else
            {
                StatusText = "غير مدفوعة";
                StatusForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                StatusBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2"));
            }
        }

        private void UpdateRefundTotals()
        {
            decimal refundTotalValue = 0;
            decimal totalCashRefund = 0;
            decimal totalDebtDeduction = 0;
            int refundCount = 0;

            foreach (var line in ReturnLines)
            {
                if (line.ReturnQuantity > 0)
                {
                    int remainingQtyBeforeReturn = line.Quantity - line.AlreadyReturned;
                    
                    decimal returnedValue = line.Quantity > 0 
                        ? line.TotalPrice * line.ReturnQuantity / line.Quantity 
                        : 0;

                    decimal returnedPaidPart = remainingQtyBeforeReturn > 0
                        ? line.PaidAmount * line.ReturnQuantity / remainingQtyBeforeReturn
                        : 0;

                    decimal returnedRemainingPart = remainingQtyBeforeReturn > 0
                        ? line.RemainingAmount * line.ReturnQuantity / remainingQtyBeforeReturn
                        : 0;

                    refundTotalValue += returnedValue;
                    totalCashRefund += returnedPaidPart;
                    totalDebtDeduction += returnedRemainingPart;
                    refundCount++;
                }
            }

            if (refundCount > 0)
            {
                decimal cashRefund = Math.Min(_sale.PaidAmount, totalCashRefund);
                decimal debtDeduction = Math.Min(_sale.RemainingAmount, totalDebtDeduction);

                RefundTotalText = Formatting.FormatCurrency(refundTotalValue);
                IsRefundPreviewVisible = true;
                IsRefundButtonEnabled = true;
                RefundButtonContent = $"استرجاع (نقدي: {Formatting.FormatCurrency(cashRefund)})";
            }
            else
            {
                IsRefundPreviewVisible = false;
                IsRefundButtonEnabled = false;
                RefundButtonContent = "استرجاع المحدد";
            }
        }

        private void IncReturn(ReturnLineItem item)
        {
            if (item == null) return;
            if (item.ReturnQuantity + item.AlreadyReturned < item.Quantity)
            {
                item.ReturnQuantity++;
            }
        }

        private void DecReturn(ReturnLineItem item)
        {
            if (item == null) return;
            if (item.ReturnQuantity > 0)
            {
                item.ReturnQuantity--;
            }
        }

        private void ProcessRefund()
        {
            try
            {
                var returnItems = new List<ReturnItem>();
                decimal totalValue = 0;
                decimal totalCashRefund = 0;
                decimal totalDebtDeduction = 0;

                foreach (var ln in ReturnLines)
                {
                    if (ln.ReturnQuantity > 0)
                    {
                        if (ln.ReturnQuantity + ln.AlreadyReturned > ln.Quantity)
                        {
                            _dialogService.ShowWarning("تنبيه", $"الكمية المرتجعة للـ {ln.ProductName} تتجاوز الكمية المتاحة ({ln.Quantity - ln.AlreadyReturned})");
                            return;
                        }

                        returnItems.Add(new ReturnItem
                        {
                            SaleItemId = ln.SaleItemId,
                            Quantity = ln.ReturnQuantity
                        });

                        int remainingQtyBeforeReturn = ln.Quantity - ln.AlreadyReturned;
                        
                        decimal returnedValue = ln.Quantity > 0 
                            ? ln.TotalPrice * ln.ReturnQuantity / ln.Quantity 
                            : 0;

                        decimal returnedPaidPart = remainingQtyBeforeReturn > 0
                            ? ln.PaidAmount * ln.ReturnQuantity / remainingQtyBeforeReturn
                            : 0;

                        decimal returnedRemainingPart = remainingQtyBeforeReturn > 0
                            ? ln.RemainingAmount * ln.ReturnQuantity / remainingQtyBeforeReturn
                            : 0;

                        totalValue += returnedValue;
                        totalCashRefund += returnedPaidPart;
                        totalDebtDeduction += returnedRemainingPart;
                    }
                }

                if (returnItems.Count == 0) return;

                decimal cashRefund = Math.Min(_sale.PaidAmount, totalCashRefund);
                decimal debtDeduction = Math.Min(_sale.RemainingAmount, totalDebtDeduction);

                string reason = "مرتجع من صفحة التفاصيل";
                string msg = $"إجمالي قيمة العناصر: {Formatting.FormatCurrency(totalValue)}\n" +
                             $"المبلغ الذي سيتم رده نقداً: {Formatting.FormatCurrency(cashRefund)}\n" +
                             (debtDeduction > 0 ? $"سيتم خصم {Formatting.FormatCurrency(debtDeduction)} من المديونية.\n" : "") +
                             $"أدخل اسم الشخص الذي قام بالاسترجاع (أو ملاحظات):";

                if (_dialogService.ShowInputDialog("تأكيد الاسترجاع", msg, "", out string inputReason) == true)
                {
                    if (!string.IsNullOrWhiteSpace(inputReason))
                        reason = inputReason;
                }
                else
                {
                    return;
                }

                int saleId = (int)_sale.Id;
                string refundMethod = PaymentMethods != null && PaymentMethods.Count > 0
                    ? PaymentMethods.OrderByDescending(p => p.Value).First().Key
                    : Shared.Helpers.PaymentMethods.Cash;
                var result = _saleService.CreateReturn(saleId, returnItems, AuthService.Instance.GetUserId(), reason, refundMethod);

                _dialogService.ShowSuccess("نجاح", $"تم تسجيل المرتجع رقم {result["return_number"]} بنجاح");

                LoadInvoice();
                TypedMessenger.Send("RefreshReports");
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void PrintInvoice()
        {
            if (_sale != null && _items != null)
            {
                _printService.PrintSaleReceipt(_sale, _items);
            }
        }

        #endregion

        public class ReturnLineItem : BaseViewModel
        {
            public int Index { get; set; }
            public int SaleItemId { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
            public int AlreadyReturned { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal RemainingAmount { get; set; }

            private int _returnQuantity;
            public int ReturnQuantity
            {
                get => _returnQuantity;
                set => SetProperty(ref _returnQuantity, value);
            }

            public bool IsFullyReturned => AlreadyReturned >= Quantity;
            public bool HasReturns => AlreadyReturned > 0;
        }

        public class PaymentDisplayItem
        {
            public string Key { get; set; }
            public decimal Value { get; set; }
            public bool ShowAmount { get; set; }
        }
    }
}
