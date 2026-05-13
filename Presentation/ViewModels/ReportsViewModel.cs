using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.ViewModels
{
    public class KpiCardViewModel
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string Icon { get; set; }
        public string ColorKey { get; set; }
    }

    public class ReportColumn
    {
        public string Header { get; set; }
        public string BindingPath { get; set; }
        public string Format { get; set; }
        public bool IsProperty { get; set; }
    }

    public class ReportsViewModel : BaseViewModel
    {
        private readonly IProductService _productService;
        private readonly ISaleService _saleService;
        private readonly IReportService _reportService;
        private readonly IReturnService _returnService;
        private readonly ISupplierService _supplierService;
        private readonly IDialogService _dialogService;
        private readonly IPrintService _printService;

        private string _reportTitle;
        private string _reportSubtitle;
        private string _detailHeader;
        private string _currentReportType;
        private ObservableCollection<KpiCardViewModel> _kpiCards;
        private ObservableCollection<object> _reportData;

        public event EventHandler<List<ReportColumn>> ColumnsChanged;

        public ReportsViewModel(IDialogService dialogService = null)
        {
            _productService = ServiceContainer.GetService<IProductService>();
            _saleService = ServiceContainer.GetService<ISaleService>();
            _reportService = ServiceContainer.GetService<IReportService>();
            _returnService = ServiceContainer.GetService<IReturnService>();
            _supplierService = ServiceContainer.GetService<ISupplierService>();
            _dialogService = dialogService ?? ServiceContainer.GetService<IDialogService>();
            _printService = ServiceContainer.GetService<IPrintService>();

            KpiCards = new ObservableCollection<KpiCardViewModel>();
            ReportData = new ObservableCollection<object>();

            TypedMessenger.Subscribe<string>(msg => {
                if (msg == "RefreshReports" && !string.IsNullOrEmpty(_currentReportType))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => LoadReport(_currentReportType));
                }
            });
        }

        public string ReportTitle
        {
            get => _reportTitle;
            set { _reportTitle = value; OnPropertyChanged(); }
        }

        public string ReportSubtitle
        {
            get => _reportSubtitle;
            set { _reportSubtitle = value; OnPropertyChanged(); }
        }

        public string DetailHeader
        {
            get => _detailHeader;
            set { _detailHeader = value; OnPropertyChanged(); }
        }

        public ObservableCollection<KpiCardViewModel> KpiCards
        {
            get => _kpiCards;
            set { _kpiCards = value; OnPropertyChanged(); }
        }

        public ObservableCollection<object> ReportData
        {
            get => _reportData;
            set { _reportData = value; OnPropertyChanged(); }
        }

        public ICommand LoadReportCommand => new RelayCommand<string>(LoadReport);
        public ICommand PrintReportCommand => new RelayCommand(PrintReport);
        public ICommand PrintCommand => new RelayCommand(PrintReport);
        public ICommand ExportCommand => new RelayCommand(ExportReport);

        private List<ReportColumn> _currentColumns;

        private void LoadReport(string type)
        {
            try
            {
                _currentReportType = type;
                KpiCards.Clear();
                ReportData.Clear();

                switch (type)
                {
                    case "Daily":
                        LoadDailyReport();
                        break;
                    case "Monthly":
                        LoadMonthlyReport();
                        break;
                    case "Inventory":
                        LoadInventoryReport();
                        break;
                    case "Returns":
                        LoadReturnsReport();
                        break;
                    case "Suppliers":
                        LoadSuppliersReport();
                        break;
                }
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void LoadDailyReport()
        {
            ReportTitle = "التقرير اليومي";
            ReportSubtitle = $"إحصائيات {DateTime.Today:yyyy/MM/dd}";
            DetailHeader = "العمليات اليومية";

            var summary = _reportService.GetDailySummary();
            var methods = summary["payment_details"] as Dictionary<string, decimal>;

            decimal cash     = GetMethodSum(methods, PaymentMethods.Cash, "Cash", "نقد", "كاش", "نقدي");
            decimal instapay = GetMethodSum(methods, PaymentMethods.InstaPay, "Insta", "إنستا");
            decimal ewallet  = GetMethodSum(methods, PaymentMethods.EWallet, "Vodafone", "فودافون", "Wallet", "محفظة");

            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي المبيعات",   Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["gross_sales"])),           Icon = "💰", ColorKey = "Primary" });
            KpiCards.Add(new KpiCardViewModel { Title = "أرباح اليوم",        Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["gross_profit"])),          Icon = "�", ColorKey = "Success" });
            KpiCards.Add(new KpiCardViewModel { Title = "صافي الربح",          Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["net_profit"])),            Icon = "�", ColorKey = "Success" });
            KpiCards.Add(new KpiCardViewModel { Title = "التحصيل النقدي",     Value = Formatting.FormatCurrency(cash),                                                    Icon = "💵", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Title = "إنستا باي",           Value = Formatting.FormatCurrency(instapay),                                                Icon = "🏦", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Title = "محافظ إلكترونية",    Value = Formatting.FormatCurrency(ewallet),                                                 Icon = "�", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Title = "المرتجعات",           Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["returns_value"])),         Icon = "↩️", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "المصروفات",           Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["total_expenses"])),        Icon = "💸", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "سداد الموردين",       Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["total_supplier_payments"])), Icon = "�", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "الصيانة",             Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["maintenance_total"])),     Icon = "�", ColorKey = "Warning" });

            string today = DateTime.Today.ToString("yyyy-MM-dd");
            var operations = _reportService.GetOperationsReport(today, today);
            ReportData = new ObservableCollection<object>(operations.Cast<object>());

            _currentColumns = new List<ReportColumn>
            {
                new ReportColumn { Header = "الموظف", BindingPath = "UserName" },
                new ReportColumn { Header = "طريقة الدفع", BindingPath = "PaymentMethod" },
                new ReportColumn { Header = "الباقي", BindingPath = "Remaining", Format = "N2" },
                new ReportColumn { Header = "المبلغ", BindingPath = "Amount", Format = "N2" },
                new ReportColumn { Header = "التفاصيل", BindingPath = "Details" },
                new ReportColumn { Header = "الوقت", BindingPath = "Date", Format = "yyyy-MM-dd HH:mm" },
                new ReportColumn { Header = "رقم العملية", BindingPath = "Reference" },
                new ReportColumn { Header = "العملية", BindingPath = "OperationName" }
            };
            ColumnsChanged?.Invoke(this, _currentColumns);
        }


        private void LoadMonthlyReport()
        {
            int year = DateTime.Today.Year;
            int month = DateTime.Today.Month;
            string[] months = { "", "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };

            ReportTitle = "التقرير الشهري";
            ReportSubtitle = $"إحصائيات شهر {months[month]} {year}";
            DetailHeader = "عمليات الشهر";

            var summary = _reportService.GetMonthlySummary(year, month);
            var methods = summary["payment_details"] as Dictionary<string, decimal>;

            decimal totalCash     = GetMethodSum(methods, PaymentMethods.Cash, "Cash", "نقد", "كاش", "نقدي");
            decimal totalInstapay = GetMethodSum(methods, PaymentMethods.InstaPay, "Insta", "إنستا");
            decimal totalWallets  = GetMethodSum(methods, PaymentMethods.EWallet, "Vodafone", "فودافون", "Wallet", "محفظة");

            decimal totalSupplierDebt = _supplierService.GetAllSuppliers().Sum(s => s.TotalDebt);

            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي المبيعات",          Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["gross_sales"])),           Icon = "📅", ColorKey = "Primary" });
            KpiCards.Add(new KpiCardViewModel { Title = "صافي الربح الشهري",        Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["net_profit"])),            Icon = "💎", ColorKey = "Success" });
            KpiCards.Add(new KpiCardViewModel { Title = "المرتجعات الشهرية",        Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["returns_value"])),         Icon = "↩️", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "المصروفات الشهرية",        Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["total_expenses"])),        Icon = "�", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي التحصيل النقدي",   Value = Formatting.FormatCurrency(totalCash),                                               Icon = "💵", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي إنستا باي",         Value = Formatting.FormatCurrency(totalInstapay),                                           Icon = "🏦", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي المحافظ",           Value = Formatting.FormatCurrency(totalWallets),                                            Icon = "📱", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي سداد الموردين",    Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["total_supplier_payments"])), Icon = "🚚", ColorKey = "Warning" });
            KpiCards.Add(new KpiCardViewModel { Title = "ديون الموردين",            Value = Formatting.FormatCurrency(totalSupplierDebt),                                       Icon = "⚠️", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي الصيانة",          Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["maintenance_total"])),     Icon = "🔧", ColorKey = "Warning" });

            string startDate = $"{year}-{month:D2}-01";
            DateTime endDateTime = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            string endDate = endDateTime.ToString("yyyy-MM-dd");

            var operations = _reportService.GetOperationsReport(startDate, endDate);
            ReportData = new ObservableCollection<object>(operations.Cast<object>());

            _currentColumns = new List<ReportColumn>
            {
                new ReportColumn { Header = "الموظف", BindingPath = "UserName" },
                new ReportColumn { Header = "طريقة الدفع", BindingPath = "PaymentMethod" },
                new ReportColumn { Header = "الباقي", BindingPath = "Remaining", Format = "N2" },
                new ReportColumn { Header = "المبلغ", BindingPath = "Amount", Format = "N2" },
                new ReportColumn { Header = "العميل", BindingPath = "Details" },
                new ReportColumn { Header = "التاريخ", BindingPath = "Date", Format = "yyyy-MM-dd HH:mm" },
                new ReportColumn { Header = "رقم العملية", BindingPath = "Reference" },
                new ReportColumn { Header = "العملية", BindingPath = "OperationName" }
            };
            ColumnsChanged?.Invoke(this, _currentColumns);
        }

        private void LoadReturnsReport()
        {
            ReportTitle = "تقرير المرتجعات";
            ReportSubtitle = "الفواتير المسترجعة وحالتها";
            DetailHeader = "سجل المرتجعات";

            var start = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd");
            var end = DateTime.Today.ToString("yyyy-MM-dd");
            var returns = _returnService.GetReturnsReport(start, end);

            decimal totalReturns = returns.Sum(r => r.TotalAmount);

            KpiCards.Add(new KpiCardViewModel { Title = "عدد المرتجعات", Value = returns.Count.ToString(), Icon = "🔢", ColorKey = "Primary" });
            KpiCards.Add(new KpiCardViewModel { Title = "قيمة المرتجعات", Value = Formatting.FormatCurrency(totalReturns), Icon = "💸", ColorKey = "Danger" });

            ReportData = new ObservableCollection<object>(returns);

            _currentColumns = new List<ReportColumn>
            {
                new ReportColumn { Header = "الموظف", BindingPath = "UserName", IsProperty = true },
                new ReportColumn { Header = "السبب / المرتجع", BindingPath = "Reason", IsProperty = true },
                new ReportColumn { Header = "القيمة", BindingPath = "TotalAmount", Format = "N2", IsProperty = true },
                new ReportColumn { Header = "العميل", BindingPath = "CustomerName", IsProperty = true },
                new ReportColumn { Header = "رقم الفاتورة", BindingPath = "InvoiceNumber", IsProperty = true },
                new ReportColumn { Header = "تاريخ", BindingPath = "ReturnDate", Format = "yyyy-MM-dd HH:mm", IsProperty = true },
                new ReportColumn { Header = "رقم المرتجع", BindingPath = "ReturnNumber", IsProperty = true }
            };
            ColumnsChanged?.Invoke(this, _currentColumns);
        }

        private void LoadInventoryReport()
        {
            ReportTitle = "تقرير المخزون";
            ReportSubtitle = "حالة المنتجات والمخزون الحالي";
            DetailHeader = "منتجات منخفضة المخزون";

            var inventory = _productService.GetTotalInventoryValue();
            var lowStock = _productService.GetLowStock();

            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي المنتجات", Value = SafeConvert.ToInt(inventory["total_products"]).ToString(), Icon = "📦", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي القطع", Value = SafeConvert.ToInt(inventory["total_quantity"]).ToString(), Icon = "🔢", ColorKey = "Primary" });
            KpiCards.Add(new KpiCardViewModel { Title = "قيمة الشراء", Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(inventory["purchase_value"])), Icon = "💲", ColorKey = "Warning" });
            KpiCards.Add(new KpiCardViewModel { Title = "قيمة البيع", Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(inventory["selling_value"])), Icon = "💰", ColorKey = "Success" });

            if (lowStock.Count > 0)
            {
                ReportData = new ObservableCollection<object>(lowStock);
                _currentColumns = new List<ReportColumn>
                {
                    new ReportColumn { Header = "المورد", BindingPath = "SupplierName", IsProperty = true },
                    new ReportColumn { Header = "الحد الأدنى", BindingPath = "MinQuantity", IsProperty = true },
                    new ReportColumn { Header = "الكمية", BindingPath = "Quantity", IsProperty = true },
                    new ReportColumn { Header = "المنتج", BindingPath = "Name", IsProperty = true },
                    new ReportColumn { Header = "كود", BindingPath = "Code", IsProperty = true }
                };
                ColumnsChanged?.Invoke(this, _currentColumns);
            }
            else
            {
                ReportData.Clear();
                _currentColumns = new List<ReportColumn>();
                ColumnsChanged?.Invoke(this, _currentColumns);
            }
        }

        private void LoadSuppliersReport()
        {
            ReportTitle = "تقرير مديونية الموردين";
            ReportSubtitle = "المبالغ المستحقة للموردين";
            DetailHeader = "قائمة الموردين الدائنين";

            var supplierService = ServiceContainer.GetService<ISupplierService>();
            var suppliers = supplierService.GetAllSuppliers().Where(s => s.TotalDebt > 0).OrderByDescending(s => s.TotalDebt).ToList();

            decimal totalDebt = suppliers.Sum(s => s.TotalDebt);

            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي المستحق", Value = Formatting.FormatCurrency(totalDebt), Icon = "💸", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "عدد الموردين", Value = suppliers.Count.ToString(), Icon = "🚚", ColorKey = "Primary" });

            ReportData = new ObservableCollection<object>(suppliers.Cast<object>());

            _currentColumns = new List<ReportColumn>
            {
                new ReportColumn { Header = "العنوان", BindingPath = "Address", IsProperty = true },
                new ReportColumn { Header = "المديونية", BindingPath = "TotalDebt", Format = "N2", IsProperty = true },
                new ReportColumn { Header = "الهاتف", BindingPath = "Phone", IsProperty = true },
                new ReportColumn { Header = "المورد", BindingPath = "Name", IsProperty = true }
            };
            ColumnsChanged?.Invoke(this, _currentColumns);
        }

        private decimal GetMethodSum(Dictionary<string, decimal> methods, params string[] keys)
        {
            decimal sum = 0;
            if (methods == null) return 0;

            var processedKeys = new HashSet<string>();

            foreach (var key in keys)
            {
                foreach (var method in methods)
                {
                    if (processedKeys.Contains(method.Key)) continue;

                    bool isMatch = false;

                    if (string.Equals(method.Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        isMatch = true;
                    }
                    else if (!PaymentMethods.GetAll().Contains(method.Key) &&
                             method.Key.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        bool isCashKey = (key == "Cash" || key == "نقدي" || key == "كاش" || key == "نقد");
                        bool isWalletMethod = (method.Key.IndexOf("Vodafone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                               method.Key.IndexOf("Wallet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                               method.Key.IndexOf("Insta", StringComparison.OrdinalIgnoreCase) >= 0);

                        isMatch = !(isCashKey && isWalletMethod);
                    }

                    if (isMatch)
                    {
                        sum += method.Value;
                        processedKeys.Add(method.Key);
                    }
                }
            }
            return sum;
        }

        private void ExportReport()
        {
            _dialogService.ShowInfo("تصدير", "ميزة التصدير قيد التطوير");
        }

        private void PrintReport()
        {
            if (ReportData == null || ReportData.Count == 0)
            {
                _dialogService.ShowInfo("تنبيه", "لا توجد بيانات للطباعة");
                return;
            }

            try
            {
                var data = new List<Dictionary<string, object>>();
                foreach (var item in ReportData)
                {
                    if (item is Dictionary<string, object> dict) data.Add(dict);
                    else
                    {
                        var d = new Dictionary<string, object>();
                        foreach (var prop in item.GetType().GetProperties()) d[prop.Name] = prop.GetValue(item);
                        data.Add(d);
                    }
                }

                string[] columns = _currentColumns?.Select(c => c.BindingPath.TrimStart('[').TrimEnd(']')).ToArray() ?? new string[0];
                string[] headers = _currentColumns?.Select(c => c.Header).ToArray() ?? new string[0];

                _printService.PrintReport(ReportTitle, data, columns, headers);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", "فشل الطباعة: " + ex.Message);
            }
        }
    }
}
