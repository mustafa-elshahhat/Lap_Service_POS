using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AlJohary.ServiceHub.Application.DTOs;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{
    public class SupplierPurchaseLineRow : BaseViewModel
    {
        private string _productName;
        private int _quantity = 1;
        private decimal _unitPurchasePrice;

        public string ProductName { get => _productName; set => SetProperty(ref _productName, value); }
        public int Quantity { get => _quantity; set { if (SetProperty(ref _quantity, value)) OnPropertyChanged(nameof(LineTotal)); } }
        public decimal UnitPurchasePrice { get => _unitPurchasePrice; set { if (SetProperty(ref _unitPurchasePrice, value)) OnPropertyChanged(nameof(LineTotal)); } }
        public decimal LineTotal => Quantity * UnitPurchasePrice;
    }

    public class SupplierPurchaseDialogViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;
        private readonly IPurchaseImportService _importService;
        private decimal _manualAmount;
        private decimal _paidAmount;
        private string _paymentMethod = "نقدي";
        private SupplierPurchaseLineRow _selectedLine;
        private string _newProductName;
        private string _newQuantity = "1";
        private string _newUnitPurchasePrice;

        public SupplierPurchaseDialogViewModel(string supplierName, decimal currentDebt, IDialogService dialogService = null)
        {
            SupplierName = supplierName;
            CurrentDebtText = Formatting.FormatCurrency(currentDebt);
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            _importService = ServiceContainer.GetService<IPurchaseImportService>();
            Lines = new ObservableCollection<SupplierPurchaseLineRow>();
            Lines.CollectionChanged += Lines_CollectionChanged;
        }

        public Action<bool> CloseAction { get; set; }
        public SupplierPurchaseDialogResult Result { get; private set; }
        public string SupplierName { get; }
        public string CurrentDebtText { get; }
        public ObservableCollection<SupplierPurchaseLineRow> Lines { get; }

        public SupplierPurchaseLineRow SelectedLine
        {
            get => _selectedLine;
            set
            {
                if (SetProperty(ref _selectedLine, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public string NewProductName
        {
            get => _newProductName;
            set => SetProperty(ref _newProductName, value);
        }

        public string NewQuantity
        {
            get => _newQuantity;
            set => SetProperty(ref _newQuantity, value);
        }

        public string NewUnitPurchasePrice
        {
            get => _newUnitPurchasePrice;
            set => SetProperty(ref _newUnitPurchasePrice, value);
        }

        public decimal ManualAmount
        {
            get => _manualAmount;
            set
            {
                if (SetProperty(ref _manualAmount, value))
                {
                    OnPropertyChanged(nameof(TotalAmount));
                    OnPropertyChanged(nameof(RemainingText));
                }
            }
        }

        public decimal TotalAmount => Lines.Count > 0 ? Lines.Sum(l => l.LineTotal) : ManualAmount;
        public bool ManualAmountEnabled => Lines.Count == 0;

        public decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                if (SetProperty(ref _paidAmount, value))
                    OnPropertyChanged(nameof(RemainingText));
            }
        }

        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        public string RemainingText
        {
            get
            {
                decimal remaining = TotalAmount - PaidAmount;
                return remaining > 0
                    ? "المتبقي المضاف للدين: " + Formatting.FormatCurrency(remaining)
                    : "سيتم سداد المشتريات بالكامل - لن يضاف دين";
            }
        }

        public ICommand AddLineCommand => new RelayCommand(AddLine);
        public ICommand RemoveLineCommand => new RelayCommand(RemoveLine, () => SelectedLine != null);
        public ICommand ImportExcelCommand => new RelayCommand(ImportCsv);
        public ICommand CopyAiPromptCommand => new RelayCommand(CopyAiPrompt);
        public ICommand SaveCommand => new RelayCommand(Save);
        public ICommand CancelCommand => new RelayCommand(() => CloseAction?.Invoke(false));

        private void AddLine()
        {
            string productName = NewProductName?.Trim();
            if (string.IsNullOrWhiteSpace(productName))
            {
                _dialogService.ShowWarning("تنبيه", "اسم المنتج مطلوب لإضافة سطر مشتريات");
                return;
            }

            decimal quantityValue = SafeConvert.ToDecimal(NewQuantity, -1m);
            if (quantityValue <= 0 || quantityValue != decimal.Truncate(quantityValue) || quantityValue > int.MaxValue)
            {
                _dialogService.ShowWarning("تنبيه", "الكمية يجب أن تكون رقماً صحيحاً أكبر من صفر");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewUnitPurchasePrice))
            {
                _dialogService.ShowWarning("تنبيه", "سعر الشراء مطلوب لإضافة سطر مشتريات");
                return;
            }

            decimal unitPurchasePrice = SafeConvert.ToDecimal(NewUnitPurchasePrice, -1m);
            if (unitPurchasePrice < 0)
            {
                _dialogService.ShowWarning("تنبيه", "سعر الشراء يجب أن يكون رقماً غير سالب");
                return;
            }

            var row = new SupplierPurchaseLineRow
            {
                ProductName = productName,
                Quantity = (int)quantityValue,
                UnitPurchasePrice = unitPurchasePrice
            };
            AttachRow(row);
            Lines.Add(row);
            SelectedLine = row;
            NewProductName = string.Empty;
            NewQuantity = "1";
            NewUnitPurchasePrice = string.Empty;
        }

        private void RemoveLine()
        {
            if (SelectedLine == null) return;
            Lines.Remove(SelectedLine);
            SelectedLine = null;
        }

        private void ImportCsv()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV Files|*.csv|All Files|*.*",
                Title = "استيراد مشتريات المورد"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var import = _importService.Import(dialog.FileName);
                foreach (var row in import.Rows)
                {
                    var line = new SupplierPurchaseLineRow
                    {
                        ProductName = row.ProductName,
                        Quantity = row.Quantity,
                        UnitPurchasePrice = row.UnitPurchasePrice
                    };
                    AttachRow(line);
                    Lines.Add(line);
                }

                string summary = $"تم استيراد {import.Rows.Count} سطر، {import.Warnings.Count} تحذير، {import.Errors.Count} خطأ";
                if (import.Errors.Count > 0)
                    _dialogService.ShowWarning("نتيجة الاستيراد", summary + "\n" + string.Join("\n", import.Errors.Take(8)));
                else
                    _dialogService.ShowSuccess("نتيجة الاستيراد", summary);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", "تعذر قراءة الملف: " + ex.Message);
            }
        }

        private void CopyAiPrompt()
        {
            try
            {
                Clipboard.SetText(AiPromptText);
                _dialogService.ShowSuccess("تم النسخ", "تم نسخ البرومبت إلى الحافظة");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", "تعذر النسخ: " + ex.Message);
            }
        }

        private void Save()
        {
            if (TotalAmount <= 0)
            {
                _dialogService.ShowWarning("تنبيه", "قيمة المشتريات يجب أن تكون أكبر من الصفر");
                return;
            }
            if (PaidAmount < 0 || PaidAmount > TotalAmount)
            {
                _dialogService.ShowWarning("تنبيه", "المبلغ المدفوع يجب أن يكون بين صفر وقيمة المشتريات");
                return;
            }

            var lines = Lines.Select(l => new SupplierPurchaseLineInput
            {
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                UnitPurchasePrice = l.UnitPurchasePrice
            }).ToList();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.ProductName) || line.Quantity <= 0 || line.UnitPurchasePrice < 0)
                {
                    _dialogService.ShowWarning("تنبيه", "تأكد من أن كل سطر يحتوي اسم منتج وكمية صحيحة وسعر شراء غير سالب");
                    return;
                }
            }

            Result = new SupplierPurchaseDialogResult
            {
                TotalAmount = TotalAmount,
                PaidAmount = PaidAmount,
                PaymentMethod = string.IsNullOrWhiteSpace(PaymentMethod) ? "نقدي" : PaymentMethod,
                Lines = lines
            };
            CloseAction?.Invoke(true);
        }

        private void Lines_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (SupplierPurchaseLineRow row in e.OldItems)
                    row.PropertyChanged -= Row_PropertyChanged;
            if (e.NewItems != null)
                foreach (SupplierPurchaseLineRow row in e.NewItems)
                    AttachRow(row);
            NotifyTotalsChanged();
        }

        private void AttachRow(SupplierPurchaseLineRow row)
        {
            row.PropertyChanged -= Row_PropertyChanged;
            row.PropertyChanged += Row_PropertyChanged;
        }

        private void Row_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SupplierPurchaseLineRow.Quantity) ||
                e.PropertyName == nameof(SupplierPurchaseLineRow.UnitPurchasePrice) ||
                e.PropertyName == nameof(SupplierPurchaseLineRow.LineTotal))
            {
                NotifyTotalsChanged();
            }
        }

        private void NotifyTotalsChanged()
        {
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(ManualAmountEnabled));
            OnPropertyChanged(nameof(RemainingText));
        }

        private static string AiPromptText => @"حول فاتورة المورد المرفقة إلى جدول Excel مناسب للاستيراد داخل نظام الموردين.

المطلوب استخراج أصناف الفاتورة فقط، وليس إضافتها للمخزون.

اخرج ملف Excel أو جدول يمكن نسخه إلى Excel بالأعمدة التالية فقط وبنفس الأسماء الإنجليزية:

ProductName
Quantity
UnitPurchasePrice

قواعد مهمة:

* اجعل كل صنف في الفاتورة في صف مستقل.
* لا تضف أعمدة أخرى.
* ProductName مطلوب.
* Quantity يجب أن يكون رقماً فقط وأكبر من صفر.
* UnitPurchasePrice يجب أن يكون رقماً فقط بدون رمز عملة.
* لا تكتب سعر بيع.
* لا تكتب Barcode.
* لا تكتب ProductCode أو كود المنتج.
* لا تخترع بيانات غير موجودة.
* لا تضف صف إجمالي.
* لا تكتب رموز عملة داخل الخلايا الرقمية.
* لا تحدث المخزون ولا تضف منتجات للمخزون، هذه تفاصيل أسطر فاتورة مورد فقط.
* راجع أن مجموع Quantity * UnitPurchasePrice قريب من إجمالي الفاتورة إذا كان الإجمالي ظاهراً.";
    }
}
