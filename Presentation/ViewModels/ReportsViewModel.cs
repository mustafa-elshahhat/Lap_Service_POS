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

        private DateTime _filterStartDate = DateTime.Today;
        private DateTime _filterEndDate = DateTime.Today;
        private System.Windows.Visibility _dateFilterVisibility = System.Windows.Visibility.Collapsed;
        private bool _isDateRangeValid = true;
        private string _dateValidationMessage;
        private bool _isSettingDefaults;

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

        public DateTime FilterStartDate
        {
            get => _filterStartDate;
            set
            {
                if (_filterStartDate != value)
                {
                    _filterStartDate = value;
                    OnPropertyChanged();
                    ValidateDateRange();
                    // Reload using the user-selected dates. Must NOT call LoadReport here:
                    // LoadReport re-applies the report-type default dates and would discard
                    // the value the user just picked.
                    if (!_isSettingDefaults && IsDateRangeValid && !string.IsNullOrEmpty(_currentReportType))
                        ReloadCurrentReport();
                }
            }
        }

        public DateTime FilterEndDate
        {
            get => _filterEndDate;
            set
            {
                if (_filterEndDate != value)
                {
                    _filterEndDate = value;
                    OnPropertyChanged();
                    ValidateDateRange();
                    // Reload using the user-selected dates (see FilterStartDate note above).
                    if (!_isSettingDefaults && IsDateRangeValid && !string.IsNullOrEmpty(_currentReportType))
                        ReloadCurrentReport();
                }
            }
        }

        public System.Windows.Visibility DateFilterVisibility
        {
            get => _dateFilterVisibility;
            set { _dateFilterVisibility = value; OnPropertyChanged(); }
        }

        public bool IsDateRangeValid
        {
            get => _isDateRangeValid;
            set { _isDateRangeValid = value; OnPropertyChanged(); }
        }

        public string DateValidationMessage
        {
            get => _dateValidationMessage;
            set { _dateValidationMessage = value; OnPropertyChanged(); }
        }

        private void ValidateDateRange()
        {
            if (_filterStartDate > _filterEndDate)
            {
                IsDateRangeValid = false;
                DateValidationMessage = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية";
            }
            else
            {
                IsDateRangeValid = true;
                DateValidationMessage = null;
            }
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

        // Selecting/changing a report TYPE: apply that type's default date range, then load.
        // The date defaults are applied under _isSettingDefaults so the date setters do not
        // trigger an extra reload here; we load exactly once via ReloadCurrentReport().
        private void LoadReport(string type)
        {
            try
            {
                _currentReportType = type;

                _isSettingDefaults = true;
                ApplyDefaultDateRange(type);
                _isSettingDefaults = false;

                ReloadCurrentReport();
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        // Sets the default filter dates + date-filter visibility for a report type.
        // Called only when the report type is (re)selected — never on a user date change,
        // so a user-picked date is preserved.
        private void ApplyDefaultDateRange(string type)
        {
            switch (type)
            {
                case "Daily":
                case "DailyOperations":
                    FilterStartDate = DateTime.Today;
                    FilterEndDate = DateTime.Today;
                    DateFilterVisibility = System.Windows.Visibility.Visible;
                    break;
                case "Monthly":
                case "MonthlyOperations":
                    FilterStartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    FilterEndDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
                    DateFilterVisibility = System.Windows.Visibility.Visible;
                    break;
                case "Returns":
                    FilterStartDate = DateTime.Today.AddDays(-30);
                    FilterEndDate = DateTime.Today;
                    DateFilterVisibility = System.Windows.Visibility.Visible;
                    break;
                case "Inventory":
                case "Suppliers":
                    DateFilterVisibility = System.Windows.Visibility.Collapsed;
                    break;
            }
        }

        // Loads the current report using the CURRENT filter values without resetting the dates.
        // Used both on report-type selection (after defaults are applied) and whenever the user
        // changes a date filter.
        private void ReloadCurrentReport()
        {
            if (string.IsNullOrEmpty(_currentReportType)) return;

            try
            {
                KpiCards.Clear();
                ReportData.Clear();

                // Default: show both regions (Inventory/Returns/Suppliers use cards + grid).
                // Cards-only reports and grid-only operations pages override these below.
                KpiVisibility = System.Windows.Visibility.Visible;
                OperationsVisibility = System.Windows.Visibility.Visible;

                switch (_currentReportType)
                {
                    case "Daily":             LoadDailyReport();       break;
                    case "Monthly":           LoadMonthlyReport();     break;
                    case "DailyOperations":   LoadDailyOperations();   break;
                    case "MonthlyOperations": LoadMonthlyOperations(); break;
                    case "Inventory":         LoadInventoryReport();   break;
                    case "Returns":           LoadReturnsReport();     break;
                    case "Suppliers":         LoadSuppliersReport();   break;
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
            ReportSubtitle = $"إحصائيات {FilterStartDate:yyyy/MM/dd}";

            var summary = _reportService.GetDailySummary(FilterStartDate.ToString("yyyy-MM-dd"));
            BuildSummaryCards(summary, "صافي الربح اليومي");
        }


        private void LoadMonthlyReport()
        {
            ReportTitle = "التقرير الشهري";
            ReportSubtitle = $"إحصائيات {MonthlyPeriodLabel()}";

            // Use the selected start/end range so the end-date picker is not decorative.
            // Defaults remain the current month (first day → last day), so the default
            // figure is identical to the previous month-based summary.
            var summary = _reportService.GetPeriodSummary(
                FilterStartDate.ToString("yyyy-MM-dd"),
                FilterEndDate.ToString("yyyy-MM-dd"));
            BuildSummaryCards(summary, "صافي الربح الشهري");
        }

        // Human-readable label for the selected monthly period: a single-month label when the
        // range covers exactly one calendar month, otherwise an explicit from→to range.
        private string MonthlyPeriodLabel()
        {
            string[] months = { "", "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };

            bool isWholeSingleMonth =
                FilterStartDate.Year == FilterEndDate.Year &&
                FilterStartDate.Month == FilterEndDate.Month &&
                FilterStartDate.Day == 1 &&
                FilterEndDate.Day == DateTime.DaysInMonth(FilterEndDate.Year, FilterEndDate.Month);

            return isWholeSingleMonth
                ? $"شهر {months[FilterStartDate.Month]} {FilterStartDate.Year}"
                : $"من {FilterStartDate:yyyy/MM/dd} إلى {FilterEndDate:yyyy/MM/dd}";
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
            ReportSubtitle = $"كل الحركات المالية ليوم {FilterStartDate:yyyy/MM/dd}";
            DetailHeader = "سجل العمليات المالية اليومية";

            string start = FilterStartDate.ToString("yyyy-MM-dd");
            LoadOperations(start, start);
        }

        private void LoadMonthlyOperations()
        {
            ReportTitle = "العمليات المالية الشهرية";
            ReportSubtitle = $"كل الحركات المالية {MonthlyPeriodLabel()}";
            DetailHeader = "سجل العمليات المالية الشهرية";

            // Use the selected start/end range (defaults to the current month).
            LoadOperations(
                FilterStartDate.ToString("yyyy-MM-dd"),
                FilterEndDate.ToString("yyyy-MM-dd"));
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
            string dateLabel = FilterStartDate == FilterEndDate
                ? $"لتاريخ {FilterStartDate:yyyy/MM/dd}"
                : $"من {FilterStartDate:yyyy/MM/dd} إلى {FilterEndDate:yyyy/MM/dd}";
            ReportSubtitle = $"الفواتير المسترجعة {dateLabel}";
            DetailHeader = "سجل المرتجعات";

            var start = FilterStartDate.ToString("yyyy-MM-dd");
            var end = FilterEndDate.ToString("yyyy-MM-dd");
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
            if (ReportData == null || ReportData.Count == 0)
            {
                _dialogService.ShowWarning("تصدير", "لا توجد بيانات للتصدير");
                return;
            }

            if (_currentColumns == null || _currentColumns.Count == 0)
            {
                _dialogService.ShowWarning("تصدير", "لا توجد أعمدة للتصدير");
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "تصدير التقرير",
                Filter = "ملفات CSV (*.csv)|*.csv",
                FileName = $"{_currentReportType}_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (AlJohary.ServiceHub.Presentation.Helpers.WindowHelper.ShowDialogOwned(dialog) != true)
                return;

            try
            {
                using (var writer = new System.IO.StreamWriter(dialog.FileName, false, new System.Text.UTF8Encoding(true)))
                {
                    writer.WriteLine(string.Join(",", _currentColumns.Select(c => $"\"{c.Header}\"")));

                    foreach (var item in ReportData)
                    {
                        var values = new List<string>();
                        foreach (var col in _currentColumns)
                        {
                            string key = col.BindingPath.TrimStart('[').TrimEnd(']');
                            object val = null;

                            if (item is Dictionary<string, object> dict)
                            {
                                dict.TryGetValue(key, out val);
                            }
                            else
                            {
                                var prop = item.GetType().GetProperty(key);
                                if (prop != null)
                                    val = prop.GetValue(item);
                            }

                            string formatted = FormatReportValue(val, col.Format)?.ToString() ?? "";
                            values.Add($"\"{formatted.Replace("\"", "\"\"")}\"");
                        }
                        writer.WriteLine(string.Join(",", values));
                    }
                }

                _dialogService.ShowSuccess("تصدير", $"تم تصدير التقرير بنجاح إلى:\n{dialog.FileName}");
            }
            catch (System.IO.IOException ex)
            {
                _dialogService.ShowError("تصدير", $"الملف قيد الاستخدام: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                _dialogService.ShowError("تصدير", $"لا يمكن الكتابة إلى الملف: {ex.Message}");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("تصدير", $"فشل التصدير: {ex.Message}");
            }
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
        // Card-style print layout deferred: would require ReportPrintService changes and is
        // optional polish outside Phase 5 scope. The current two-column table is functional and
        // prints identical numbers.
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
