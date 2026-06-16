using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Converters;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Presentation.Services;
using AlJohary.ServiceHub.Presentation.ViewModels;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class ReportsPage : Page
    {
        private ReportsViewModel _viewModel;

        public ReportsPage()
        {
            InitializeComponent();
            var dialogService = ServiceContainer.GetService<IDialogService>();

            _viewModel = new ReportsViewModel(dialogService);
            DataContext = _viewModel;

            _viewModel.ColumnsChanged += OnColumnsChanged;

            if (_viewModel.LoadReportCommand.CanExecute("Daily"))
            {
                _viewModel.LoadReportCommand.Execute("Daily");
            }
        }

        private void OnColumnsChanged(object sender, List<ReportColumn> columns)
        {
            ReportDataGrid.Columns.Clear();
            if (columns == null) return;

            for (int i = columns.Count - 1; i >= 0; i--)
            {
                var col = columns[i];
                var bindingPath = col.IsProperty ? col.BindingPath : $"[{col.BindingPath}]";
                var binding = new Binding(bindingPath);
                if (col.Format == "FlexibleNumber")
                    binding.Converter = new FlexibleNumberConverter();
                else if (!string.IsNullOrEmpty(col.Format))
                    binding.StringFormat = col.Format;

                bool isCopyable = col.Header == "رقم العملية" || col.Header == "رقم الفاتورة" || col.Header == "رقم المرتجع" || col.Header == "الهاتف";

                if (isCopyable)
                {
                    var templateCol = new DataGridTemplateColumn
                    {
                        Header = col.Header,
                        Width = new DataGridLength(140),
                        MinWidth = 100,
                        CanUserResize = true
                    };

                    string xaml = $@"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Grid Background=""Transparent"">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width=""Auto""/>
            <ColumnDefinition Width=""Auto""/>
        </Grid.ColumnDefinitions>
        <TextBlock Text=""{{Binding {bindingPath}}}"" VerticalAlignment=""Center"" HorizontalAlignment=""Center"" FontWeight=""SemiBold"" FlowDirection=""LeftToRight""/>
        <Button Grid.Column=""1"" Margin=""8,0,0,0"" Padding=""4""
                Background=""Transparent"" BorderThickness=""0"" Cursor=""Hand""
                Visibility=""Collapsed"" x:Name=""CopyBtn""
                Command=""{{Binding DataContext.CopyTextCommand, RelativeSource={{RelativeSource AncestorType=Page}}}}""
                CommandParameter=""{{Binding {bindingPath}}}""
                ToolTip=""نسخ"">
                <Viewbox Width=""14"" Height=""14"">
                    <Path Data=""M19,21H8V7H19M19,5H8A2,2 0 0,0 6,7V21A2,2 0 0,0 8,23H19A2,2 0 0,0 21,21V7A2,2 0 0,0 19,5M16,1H4A2,2 0 0,0 2,3V17H4V3H16V1Z""
                          Fill=""#7F8C8D""/>
                </Viewbox>
        </Button>
    </Grid>
    <DataTemplate.Triggers>
        <Trigger Property=""IsMouseOver"" Value=""True"">
            <Setter TargetName=""CopyBtn"" Property=""Visibility"" Value=""Visible""/>
        </Trigger>
    </DataTemplate.Triggers>
</DataTemplate>";

                    try
                    {
                        templateCol.CellTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Parse(xaml);
                        ReportDataGrid.Columns.Add(templateCol);
                        continue;
                    }
                    catch { }
                }

                var width = DataGridLength.Auto;
                bool isLongText = col.Header.Contains("العميل") || col.Header.Contains("المورد") ||
                                col.Header.Contains("البيانات") || col.Header.Contains("البيان") ||
                                col.Header.Contains("العنوان") || col.Header.Contains("السبب") ||
                                col.Header.Contains("تفاصيل") || col.Header.Contains("اسم") ||
                                col.Header.Contains("ملاحظات") || col.Header.Contains("الوصف") ||
                                col.Header.Contains("المنتج");

                if (isLongText)
                {
                    width = new DataGridLength(1, DataGridLengthUnitType.Star);
                }

                var column = new DataGridTextColumn
                {
                    Header = col.Header,
                    Binding = binding,
                    Width = width,
                    MinWidth = 60,
                    CanUserResize = true
                };

                var elementStyle = new Style(typeof(TextBlock));

                var horizontalAlign = HorizontalAlignment.Right;
                bool isNumeric = col.Header.Contains("القيمة") || col.Header.Contains("المبلغ") ||
                                 col.Header.Contains("الكمية") || col.Header.Contains("إجمالي") ||
                                 col.Header.Contains("صافي") || col.Header.Contains("عدد");

                if (isNumeric) horizontalAlign = HorizontalAlignment.Center;

                elementStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, horizontalAlign));
                elementStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                elementStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, horizontalAlign == HorizontalAlignment.Center ? TextAlignment.Center : TextAlignment.Right));
                elementStyle.Setters.Add(new Setter(TextBlock.FlowDirectionProperty, FlowDirection.RightToLeft));
                elementStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(12, 4, 12, 4)));

                if (col.Header == "العملية")
                {
                    horizontalAlign = HorizontalAlignment.Center;
                    elementStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));

                    var triggerSale = new DataTrigger { Binding = binding, Value = "بيع" };
                    triggerSale.Setters.Add(new Setter(TextBlock.ForegroundProperty, System.Windows.Application.Current.Resources["SecondaryDarkBrush"]));

                    var triggerReturn = new DataTrigger { Binding = binding, Value = "استرجاع" };
                    triggerReturn.Setters.Add(new Setter(TextBlock.ForegroundProperty, System.Windows.Application.Current.Resources["DangerBrush"]));

                    var triggerExpense = new DataTrigger { Binding = binding, Value = "مصروفات" };
                    triggerExpense.Setters.Add(new Setter(TextBlock.ForegroundProperty, System.Windows.Application.Current.Resources["WarningDarkBrush"]));

                    var triggerSupplier = new DataTrigger { Binding = binding, Value = "سداد مورد" };
                    triggerSupplier.Setters.Add(new Setter(TextBlock.ForegroundProperty, System.Windows.Application.Current.Resources["InfoDarkBrush"]));

                    var triggerMaintenance = new DataTrigger { Binding = binding, Value = "صيانة" };
                    triggerMaintenance.Setters.Add(new Setter(TextBlock.ForegroundProperty, System.Windows.Application.Current.Resources["WarningDarkBrush"]));

                    var triggerSalary = new DataTrigger { Binding = binding, Value = "مرتب موظف" };
                    triggerSalary.Setters.Add(new Setter(TextBlock.ForegroundProperty, System.Windows.Application.Current.Resources["DangerBrush"]));

                    var triggerDeduction = new DataTrigger { Binding = binding, Value = "خصم موظف" };
                    triggerDeduction.Setters.Add(new Setter(TextBlock.ForegroundProperty, System.Windows.Application.Current.Resources["InfoDarkBrush"]));

                    elementStyle.Triggers.Add(triggerSale);
                    elementStyle.Triggers.Add(triggerReturn);
                    elementStyle.Triggers.Add(triggerExpense);
                    elementStyle.Triggers.Add(triggerSupplier);
                    elementStyle.Triggers.Add(triggerMaintenance);
                    elementStyle.Triggers.Add(triggerSalary);
                    elementStyle.Triggers.Add(triggerDeduction);
                }

                column.ElementStyle = elementStyle;
                ReportDataGrid.Columns.Add(column);
            }
        }
    }
}
