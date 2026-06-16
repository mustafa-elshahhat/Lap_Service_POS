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
        public string Group { get; set; }
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
        private readonly IReportService _reportService;
        private readonly IReturnService _returnService;
        private readonly IDialogService _dialogService;
        private readonly IPrintService _printService;

        private string _reportTitle;
        private string _reportSubtitle;
        private string _detailHeader;
        private string _currentReportType;
        private ObservableCollection<KpiCardViewModel> _kpiCards;
        private ObservableCollection<object> _reportData;
        private System.Windows.Visibility _kpiVisibility = System.Windows.Visibility.Visible;
        private System.Windows.Visibility _operationsVisibility = System.Windows.Visibility.Collapsed;

        public event EventHandler<List<ReportColumn>> ColumnsChanged;

        public ReportsViewModel(IDialogService dialogService = null)
        {
            _productService = ServiceContainer.GetService<IProductService>();
            _reportService = ServiceContainer.GetService<IReportService>();
            _returnService = ServiceContainer.GetService<IReturnService>();
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

        // Reports show KPI cards only; operations pages show the detailed log grid only.
        public System.Windows.Visibility KpiVisibility
        {
            get => _kpiVisibility;
            set { _kpiVisibility = value; OnPropertyChanged(); }
        }

        public System.Windows.Visibility OperationsVisibility
        {
            get => _operationsVisibility;
            set { _operationsVisibility = value; OnPropertyChanged(); }
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

                // Default: show both regions (Inventory/Returns/Suppliers use cards + grid).
                // Cards-only reports and grid-only operations pages override these below.
                KpiVisibility = System.Windows.Visibility.Visible;
                OperationsVisibility = System.Windows.Visibility.Visible;

                switch (type)
                {
                    case "Daily":
                        LoadDailyReport();
                        break;
                    case "Monthly":
                        LoadMonthlyReport();
                        break;
                    case "DailyOperations":
                        LoadDailyOperations();
                        break;
                    case "MonthlyOperations":
                        LoadMonthlyOperations();
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

            var summary = _reportService.GetDailySummary();
            BuildSummaryCards(summary, "صافي الربح اليومي");
        }


        private void LoadMonthlyReport()
        {
            int year = DateTime.Today.Year;
            int month = DateTime.Today.Month;
            string[] months = { "", "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };

            ReportTitle = "التقرير الشهري";
            ReportSubtitle = $"إحصائيات شهر {months[month]} {year}";

            var summary = _reportService.GetMonthlySummary(year, month);
            BuildSummaryCards(summary, "صافي الربح الشهري");
        }

        // Builds the required 17 KPI cards (grouped) shared by daily and monthly reports.
        // Reports are cards-only: the operations grid is hidden here. Card values come straight
        // from the summary; per-method nets are inflows - outflows for that method.
        private void BuildSummaryCards(Dictionary<string, object> summary, string netProfitTitle)
        {
            KpiVisibility = System.Windows.Visibility.Visible;
            OperationsVisibility = System.Windows.Visibility.Collapsed;
            ReportData.Clear();

            summary.TryGetValue("payment_inflows",  out object inflowObj);
            summary.TryGetValue("payment_outflows", out object outflowObj);
            var inflows  = inflowObj  as Dictionary<string, decimal> ?? new Dictionary<string, decimal>();
            var outflows = outflowObj as Dictionary<string, decimal> ?? new Dictionary<string, decimal>();

            decimal cashIn      = GetMethodSum(inflows,  PaymentMethods.Cash);
            decimal instapayIn  = GetMethodSum(inflows,  PaymentMethods.InstaPay);
            decimal ewalletIn   = GetMethodSum(inflows,  PaymentMethods.EWallet);
            decimal cashOut     = GetMethodSum(outflows, PaymentMethods.Cash);
            decimal instapayOut = GetMethodSum(outflows, PaymentMethods.InstaPay);
            decimal ewalletOut  = GetMethodSum(outflows, PaymentMethods.EWallet);

            decimal otherIn  = inflows.Values.Sum() - (cashIn + instapayIn + ewalletIn);
            decimal otherOut = outflows.Values.Sum() - (cashOut + instapayOut + ewalletOut);

            decimal SummaryVal(string key) => SafeConvert.ToDecimal(summary.ContainsKey(key) ? summary[key] : 0);

            const string gSales = "المبيعات والأرباح";
            const string gNet = "صافي الأرصدة حسب الطريقة";
            const string gFlow = "الوارد والصادر حسب الطريقة";
            const string gSalary = "الرواتب";
            const string gMaint = "الصيانة";
            const string gSupExp = "الموردين والمصروفات";

            // 1-2: Sales / profit
            KpiCards.Add(new KpiCardViewModel { Group = gSales, Title = "إجمالي المبيعات", Value = Formatting.FormatCurrency(SummaryVal("gross_sales")), Icon = "💰", ColorKey = "Primary", ToolTip = "إجمالي قيمة فواتير البيع خلال الفترة (لا يشمل الصيانة أو الموردين أو المصروفات)." });
            KpiCards.Add(new KpiCardViewModel { Group = gSales, Title = netProfitTitle, Value = Formatting.FormatCurrency(SummaryVal("net_profit")), Icon = "✅", ColorKey = "Success", ToolTip = "ربح المبيعات + ربح الصيانة - ربح مفقود من المرتجعات - المصروفات - صافي الرواتب. ملاحظة: يتم احتساب الربح عند البيع ويتم عكسه عند الإرجاع حسب تاريخ الإرجاع (نظام الاستحقاق). قد يختلف ربح اليوم الواحد إذا كان الإرجاع في فترة لاحقة." });

            // 3-5: Per-method net balances
            KpiCards.Add(new KpiCardViewModel { Group = gNet, Title = "صافي النقدية",   Value = Formatting.FormatCurrency(cashIn - cashOut),         Icon = "💱", ColorKey = "Success", ToolTip = "نقدي وارد - نقدي صادر." });
            KpiCards.Add(new KpiCardViewModel { Group = gNet, Title = "صافي إنستا باي", Value = Formatting.FormatCurrency(instapayIn - instapayOut), Icon = "💱", ColorKey = "Success", ToolTip = "إنستا باي وارد - إنستا باي صادر." });
            KpiCards.Add(new KpiCardViewModel { Group = gNet, Title = "صافي محفظة",     Value = Formatting.FormatCurrency(ewalletIn - ewalletOut),   Icon = "💱", ColorKey = "Success", ToolTip = "محفظة وارد - محفظة صادر." });
            KpiCards.Add(new KpiCardViewModel { Group = gNet, Title = "صافي آخرى",      Value = Formatting.FormatCurrency(otherIn - otherOut),       Icon = "💱", ColorKey = "Secondary", ToolTip = "مدفوعات بطرق دفع أخرى / غير محددة وارد - صادر." });

            // 6-11: Inflows / outflows by method
            KpiCards.Add(new KpiCardViewModel { Group = gFlow, Title = "نقدي وارد",      Value = Formatting.FormatCurrency(cashIn),      Icon = "💵", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Group = gFlow, Title = "نقدي صادر",      Value = Formatting.FormatCurrency(cashOut),     Icon = "💸", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Group = gFlow, Title = "إنستا باي وارد", Value = Formatting.FormatCurrency(instapayIn),  Icon = "🏦", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Group = gFlow, Title = "إنستا باي صادر", Value = Formatting.FormatCurrency(instapayOut), Icon = "🏧", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Group = gFlow, Title = "محفظة وارد",     Value = Formatting.FormatCurrency(ewalletIn),   Icon = "📱", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Group = gFlow, Title = "محفظة صادر",     Value = Formatting.FormatCurrency(ewalletOut),  Icon = "📲", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Group = gFlow, Title = "آخرى وارد",      Value = Formatting.FormatCurrency(otherIn),     Icon = "🗃️", ColorKey = "Info" });
            KpiCards.Add(new KpiCardViewModel { Group = gFlow, Title = "آخرى صادر",      Value = Formatting.FormatCurrency(otherOut),    Icon = "🗃️", ColorKey = "Danger" });

            // 12-13: Salaries (net salary cost, plus deductions separately)
            KpiCards.Add(new KpiCardViewModel { Group = gSalary, Title = "إجمالي الرواتب",   Value = Formatting.FormatCurrency(SummaryVal("net_salary_expense")),       Icon = "👨‍💼", ColorKey = "Danger",  ToolTip = "صافي تكلفة الرواتب = الرواتب المدفوعة - خصومات الموظفين." });
            KpiCards.Add(new KpiCardViewModel { Group = gSalary, Title = "خصومات الموظفين",  Value = Formatting.FormatCurrency(SummaryVal("total_employee_deductions")), Icon = "➖",   ColorKey = "Warning", ToolTip = "إجمالي خصومات الموظفين (ليست نقدية واردة)." });

            // 14-15: Maintenance (collection vs profit — clearly separate)
            KpiCards.Add(new KpiCardViewModel { Group = gMaint, Title = "تحصيل الصيانة", Value = Formatting.FormatCurrency(SummaryVal("maintenance_total")),  Icon = "🔧", ColorKey = "Warning", ToolTip = "إجمالي مدفوعات الصيانة المحصلة خلال الفترة (تحصيل وليس ربحاً)." });
            KpiCards.Add(new KpiCardViewModel { Group = gMaint, Title = "ربح الصيانة",   Value = Formatting.FormatCurrency(SummaryVal("maintenance_profit")), Icon = "📈", ColorKey = "Success", ToolTip = "ربح الصيانة = المصنعية + هامش القطع المعترف به مع الدفعات (ليس مبلغ التحصيل)." });

            // 16-17: Supplier / expenses
            KpiCards.Add(new KpiCardViewModel { Group = gSupExp, Title = "دفع مورد",  Value = Formatting.FormatCurrency(SummaryVal("total_supplier_payments")), Icon = "🚚", ColorKey = "Danger" });
            KpiCards.Add(new KpiCardViewModel { Group = gSupExp, Title = "مصروفات",   Value = Formatting.FormatCurrency(SummaryVal("total_expenses")),          Icon = "🧾", ColorKey = "Danger" });
        }

        private void LoadDailyOperations()
        {
            ReportTitle = "العمليات المالية اليومية";
            ReportSubtitle = $"كل الحركات المالية ليوم {DateTime.Today:yyyy/MM/dd}";
            DetailHeader = "سجل العمليات المالية اليومية";

            string today = DateTime.Today.ToString("yyyy-MM-dd");
            LoadOperations(today, today);
        }

        private void LoadMonthlyOperations()
        {
            int year = DateTime.Today.Year;
            int month = DateTime.Today.Month;
            string[] months = { "", "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };

            ReportTitle = "العمليات المالية الشهرية";
            ReportSubtitle = $"كل الحركات المالية لشهر {months[month]} {year}";
            DetailHeader = "سجل العمليات المالية الشهرية";

            string startDate = $"{year}-{month:D2}-01";
            string endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month)).ToString("yyyy-MM-dd");
            LoadOperations(startDate, endDate);
        }

        // Operations pages are grid-only: KPI cards are hidden, the audit log is shown.
        private void LoadOperations(string startDate, string endDate)
        {
            KpiVisibility = System.Windows.Visibility.Collapsed;
            OperationsVisibility = System.Windows.Visibility.Visible;
            KpiCards.Clear();

            var operations = _reportService.GetFinancialOperations(startDate, endDate);
            ReportData = new ObservableCollection<object>(operations.Cast<object>());

            _currentColumns = new List<ReportColumn>
            {
                new ReportColumn { Header = "التاريخ", BindingPath = "Date", Format = "yyyy-MM-dd HH:mm" },
                new ReportColumn { Header = "نوع العملية", BindingPath = "OperationType" },
                new ReportColumn { Header = "رقم المرجع", BindingPath = "Reference" },
                new ReportColumn { Header = "التفاصيل", BindingPath = "Details" },
                new ReportColumn { Header = "طريقة الدفع", BindingPath = "PaymentMethod" },
                new ReportColumn { Header = "وارد", BindingPath = "MoneyIn", Format = "FlexibleNumber" },
                new ReportColumn { Header = "صادر", BindingPath = "MoneyOut", Format = "FlexibleNumber" },
                new ReportColumn { Header = "خصم / تسوية", BindingPath = "Deduction", Format = "FlexibleNumber" },
                new ReportColumn { Header = "التأثير الصافي", BindingPath = "NetEffect", Format = "FlexibleNumber" },
                new ReportColumn { Header = "الموظف", BindingPath = "UserName" }
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

        private void ExportReport()
        {
            _dialogService.ShowInfo("تصدير", "ميزة التصدير قيد التطوير");
        }

        private void PrintReport()
        {
            // Cards-only reports (daily/monthly) print the KPI summary, not the (hidden) grid.
            // Operations pages and the legacy sub-reports print their table.
            if (_currentReportType == "Daily" || _currentReportType == "Monthly")
            {
                PrintKpiSummary();
                return;
            }

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

        // Prints the daily/monthly report as a two-column KPI summary (البيان / القيمة), reusing
        // the generic table printer. Reports are cards-only, so there is no operations table here.
        // TODO: a richer card-style print layout could be added later if needed.
        private void PrintKpiSummary()
        {
            if (KpiCards == null || KpiCards.Count == 0)
            {
                _dialogService.ShowInfo("تنبيه", "لا توجد بيانات للطباعة");
                return;
            }

            try
            {
                var data = new List<Dictionary<string, object>>();
                foreach (var card in KpiCards)
                {
                    data.Add(new Dictionary<string, object>
                    {
                        { "البيان", card.Title },
                        { "القيمة", card.Value }
                    });
                }

                _printService.PrintReport(
                    ReportTitle,
                    data,
                    new[] { "البيان", "القيمة" },
                    new[] { "البيان", "القيمة" });
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
