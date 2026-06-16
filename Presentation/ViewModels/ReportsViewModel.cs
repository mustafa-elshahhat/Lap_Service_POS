using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{
    public class KpiCardViewModel
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string Icon { get; set; }
        public string ColorKey { get; set; }
        public string ToolTip { get; set; }
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
            summary.TryGetValue("payment_inflows",  out object _inflowObj);
            summary.TryGetValue("payment_outflows", out object _outflowObj);
            var inflows  = _inflowObj  as Dictionary<string, decimal> ?? new Dictionary<string, decimal>();
            var outflows = _outflowObj as Dictionary<string, decimal> ?? new Dictionary<string, decimal>();

            decimal cashIn     = GetMethodSum(inflows,  PaymentMethods.Cash);
            decimal instapayIn = GetMethodSum(inflows,  PaymentMethods.InstaPay);
            decimal ewalletIn  = GetMethodSum(inflows,  PaymentMethods.EWallet);
            decimal cashOut    = GetMethodSum(outflows, PaymentMethods.Cash);
            decimal instapayOut= GetMethodSum(outflows, PaymentMethods.InstaPay);
            decimal ewalletOut = GetMethodSum(outflows, PaymentMethods.EWallet);
            decimal otherIn    = GetOtherMethodsSum(inflows);
            decimal otherOut   = GetOtherMethodsSum(outflows);

            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي المبيعات",       Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["gross_sales"])),              Icon = "💰", ColorKey = "Primary" });
            KpiCards.Add(new KpiCardViewModel { Title = "أرباح اليوم",            Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["gross_profit"])),           Icon = "📈", ColorKey = "Success" });
            KpiCards.Add(new KpiCardViewModel { Title = "صافي الربح",             Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["net_profit"])),             Icon = "✅", ColorKey = "Success" });
            KpiCards.Add(new KpiCardViewModel { Title = "نقدي (وارد)",            Value = Formatting.FormatCurrency(cashIn),                                                    Icon = "💵", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Title = "إنستا باي (وارد)",       Value = Formatting.FormatCurrency(instapayIn),                                                Icon = "🏦", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Title = "محافظ (واردة)",          Value = Formatting.FormatCurrency(ewalletIn),                                                 Icon = "📱", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Title = "نقدي (صادر)",            Value = Formatting.FormatCurrency(cashOut),                                                   Icon = "💸", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "إنستا باي (صادر)",       Value = Formatting.FormatCurrency(instapayOut),                                               Icon = "🏧", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "محافظ (صادرة)",          Value = Formatting.FormatCurrency(ewalletOut),                                                Icon = "📲", ColorKey = "Danger" });
            if (otherIn != 0)  KpiCards.Add(new KpiCardViewModel { Title = "أخرى (وارد - طرق غير معروفة)", Value = Formatting.FormatCurrency(otherIn),  Icon = "❓", ColorKey = "Info" });
            if (otherOut != 0) KpiCards.Add(new KpiCardViewModel { Title = "أخرى (صادر - طرق غير معروفة)", Value = Formatting.FormatCurrency(otherOut), Icon = "❓", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "صافي التدفق النقدي",     Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["net_cash_flow"])),          Icon = "💱", ColorKey = "Success" });
            KpiCards.Add(new KpiCardViewModel { Title = "المرتجعات",              Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["returns_value"])),          Icon = "↩️", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "المصروفات",              Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["total_expenses"])),         Icon = "🧾", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي الرواتب",          Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary.ContainsKey("total_salary_payments") ? summary["total_salary_payments"] : 0)), Icon = "👨‍💼", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي خصومات الموظفين",  Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary.ContainsKey("total_employee_deductions") ? summary["total_employee_deductions"] : 0)), Icon = "➖", ColorKey = "Warning" });
            KpiCards.Add(new KpiCardViewModel { Title = "صافي تكلفة الرواتب",      Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary.ContainsKey("net_salary_expense") ? summary["net_salary_expense"] : 0)), Icon = "💼", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "تحصيل الصيانة", Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["maintenance_total"])), Icon = "🔧", ColorKey = "Warning", ToolTip = "تحصيل الصيانة = المدفوعات المسجلة خلال الفترة." });
            KpiCards.Add(new KpiCardViewModel { Title = "ربح الصيانة", Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary.ContainsKey("maintenance_profit") ? summary["maintenance_profit"] : 0)), Icon = "🔧", ColorKey = "Success", ToolTip = "ربح الصيانة = المصنعية + هامش القطع المعترف به مع الدفعات خلال الفترة." });

            string today = DateTime.Today.ToString("yyyy-MM-dd");
            var operations = _reportService.GetOperationsReport(today, today);
            ReportData = new ObservableCollection<object>(operations.Cast<object>());

            _currentColumns = new List<ReportColumn>
            {
                new ReportColumn { Header = "الموظف", BindingPath = "UserName" },
                new ReportColumn { Header = "طريقة الدفع", BindingPath = "PaymentMethod" },
                new ReportColumn { Header = "الباقي", BindingPath = "Remaining", Format = "FlexibleNumber" },
                new ReportColumn { Header = "المبلغ", BindingPath = "Amount", Format = "FlexibleNumber" },
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

            var summary  = _reportService.GetMonthlySummary(year, month);
            summary.TryGetValue("payment_inflows",  out object _mInflowObj);
            summary.TryGetValue("payment_outflows", out object _mOutflowObj);
            var inflows  = _mInflowObj  as Dictionary<string, decimal> ?? new Dictionary<string, decimal>();
            var outflows = _mOutflowObj as Dictionary<string, decimal> ?? new Dictionary<string, decimal>();

            decimal cashIn      = GetMethodSum(inflows,  PaymentMethods.Cash);
            decimal instapayIn  = GetMethodSum(inflows,  PaymentMethods.InstaPay);
            decimal ewalletIn   = GetMethodSum(inflows,  PaymentMethods.EWallet);
            decimal cashOut     = GetMethodSum(outflows, PaymentMethods.Cash);
            decimal instapayOut = GetMethodSum(outflows, PaymentMethods.InstaPay);
            decimal ewalletOut  = GetMethodSum(outflows, PaymentMethods.EWallet);
            decimal otherIn     = GetOtherMethodsSum(inflows);
            decimal otherOut    = GetOtherMethodsSum(outflows);

            decimal totalSupplierDebt = _supplierService.GetAllSuppliers().Sum(s => s.TotalDebt);

            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي المبيعات",          Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["gross_sales"])),              Icon = "📅", ColorKey = "Primary" });
            KpiCards.Add(new KpiCardViewModel { Title = "صافي الربح الشهري",        Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["net_profit"])),              Icon = "💎", ColorKey = "Success" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي نقدي (وارد)",       Value = Formatting.FormatCurrency(cashIn),                                                    Icon = "💵", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي إنستا باي (وارد)",  Value = Formatting.FormatCurrency(instapayIn),                                                Icon = "🏦", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي محافظ (واردة)",     Value = Formatting.FormatCurrency(ewalletIn),                                                 Icon = "📱", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي نقدي (صادر)",       Value = Formatting.FormatCurrency(cashOut),                                                   Icon = "💸", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي إنستا باي (صادر)",  Value = Formatting.FormatCurrency(instapayOut),                                               Icon = "🏧", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي محافظ (صادرة)",     Value = Formatting.FormatCurrency(ewalletOut),                                                Icon = "📲", ColorKey = "Danger" });
            if (otherIn != 0)  KpiCards.Add(new KpiCardViewModel { Title = "أخرى (وارد - طرق غير معروفة)", Value = Formatting.FormatCurrency(otherIn),  Icon = "❓", ColorKey = "Info" });
            if (otherOut != 0) KpiCards.Add(new KpiCardViewModel { Title = "أخرى (صادر - طرق غير معروفة)", Value = Formatting.FormatCurrency(otherOut), Icon = "❓", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "صافي التدفق النقدي",       Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["net_cash_flow"])),          Icon = "💱", ColorKey = "Success" });
            KpiCards.Add(new KpiCardViewModel { Title = "المرتجعات الشهرية",        Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["returns_value"])),          Icon = "↩️", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "المصروفات الشهرية",        Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["total_expenses"])),         Icon = "🧾", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي الرواتب",           Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary.ContainsKey("total_salary_payments") ? summary["total_salary_payments"] : 0)), Icon = "👨‍💼", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "إجمالي خصومات الموظفين",   Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary.ContainsKey("total_employee_deductions") ? summary["total_employee_deductions"] : 0)), Icon = "➖", ColorKey = "Warning" });
            KpiCards.Add(new KpiCardViewModel { Title = "صافي تكلفة الرواتب",       Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary.ContainsKey("net_salary_expense") ? summary["net_salary_expense"] : 0)), Icon = "💼", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "ديون الموردين",            Value = Formatting.FormatCurrency(totalSupplierDebt),                                         Icon = "⚠️", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Title = "تحصيل الصيانة", Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary["maintenance_total"])), Icon = "🔧", ColorKey = "Warning", ToolTip = "تحصيل الصيانة = المدفوعات المسجلة خلال الفترة." });
            KpiCards.Add(new KpiCardViewModel { Title = "ربح الصيانة", Value = Formatting.FormatCurrency(SafeConvert.ToDecimal(summary.ContainsKey("maintenance_profit") ? summary["maintenance_profit"] : 0)), Icon = "🔧", ColorKey = "Success", ToolTip = "ربح الصيانة = المصنعية + هامش القطع المعترف به مع الدفعات خلال الفترة." });

            string startDate = $"{year}-{month:D2}-01";
            DateTime endDateTime = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            string endDate = endDateTime.ToString("yyyy-MM-dd");

            var operations = _reportService.GetOperationsReport(startDate, endDate);
            ReportData = new ObservableCollection<object>(operations.Cast<object>());

            _currentColumns = new List<ReportColumn>
            {
                new ReportColumn { Header = "الموظف", BindingPath = "UserName" },
                new ReportColumn { Header = "طريقة الدفع", BindingPath = "PaymentMethod" },
                new ReportColumn { Header = "الباقي", BindingPath = "Remaining", Format = "FlexibleNumber" },
                new ReportColumn { Header = "المبلغ", BindingPath = "Amount", Format = "FlexibleNumber" },
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
                new ReportColumn { Header = "القيمة", BindingPath = "TotalAmount", Format = "FlexibleNumber", IsProperty = true },
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
                new ReportColumn { Header = "المديونية", BindingPath = "TotalDebt", Format = "FlexibleNumber", IsProperty = true },
                new ReportColumn { Header = "الهاتف", BindingPath = "Phone", IsProperty = true },
                new ReportColumn { Header = "المورد", BindingPath = "Name", IsProperty = true }
            };
            ColumnsChanged?.Invoke(this, _currentColumns);
        }

        private decimal GetMethodSum(Dictionary<string, decimal> methods, string key)
        {
            if (methods == null) return 0;
            return methods.TryGetValue(key, out decimal val) ? val : 0;
        }

        // Catch-all for any non-canonical / unknown / blank payment method so the per-method cards
        // always sum to the true total (nothing is silently dropped). = Σ(all) − Σ(known three).
        private decimal GetOtherMethodsSum(Dictionary<string, decimal> methods)
        {
            if (methods == null) return 0;
            decimal total = 0;
            foreach (var kv in methods) total += kv.Value;
            return total
                   - GetMethodSum(methods, PaymentMethods.Cash)
                   - GetMethodSum(methods, PaymentMethods.InstaPay)
                   - GetMethodSum(methods, PaymentMethods.EWallet);
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
                    if (item is Dictionary<string, object> dict) data.Add(new Dictionary<string, object>(dict));
                    else
                    {
                        var d = new Dictionary<string, object>();
                        foreach (var prop in item.GetType().GetProperties()) d[prop.Name] = prop.GetValue(item);
                        data.Add(d);
                    }
                }

                if (_currentColumns != null)
                {
                    foreach (var row in data)
                    {
                        foreach (var col in _currentColumns)
                        {
                            string key = col.BindingPath.TrimStart('[').TrimEnd(']');
                            if (row.ContainsKey(key))
                                row[key] = FormatReportValue(row[key], col.Format);
                        }
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

        private static object FormatReportValue(object value, string format)
        {
            if (format == "FlexibleNumber")
                return Formatting.FormatNumber(value);
            if (!string.IsNullOrEmpty(format) && value is IFormattable formattable)
                return formattable.ToString(format, null);
            return value;
        }
    }
}
