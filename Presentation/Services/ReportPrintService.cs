using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Presentation.ViewModels;

namespace AlJohary.ServiceHub.Presentation.Services
{
    public class ReportPrintService
    {
        private readonly IDialogService _dialogService;

        public ReportPrintService(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        public void PrintReport(ReportsViewModel viewModel)
        {
             try
            {
                FlowDocument doc = new FlowDocument();
                doc.FlowDirection = FlowDirection.RightToLeft;
                doc.Blocks.Add(new Paragraph(new Run(viewModel.ReportTitle)) { FontSize = 24, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
                doc.Blocks.Add(new Paragraph(new Run(viewModel.ReportSubtitle)) { FontSize = 14, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20) });

                string kpiSummary = "";
                foreach (var kpi in viewModel.KpiCards)
                {
                    kpiSummary += $"{kpi.Title}: {kpi.Value}   |   ";
                }

                if (!string.IsNullOrEmpty(kpiSummary))
                {
                    doc.Blocks.Add(new Paragraph(new Run(kpiSummary)) { FontSize = 12, TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold, Background = Brushes.GhostWhite, Padding = new Thickness(10) });
                }

                doc.Blocks.Add(new Paragraph(new Run("(تفاصيل التقرير مرفقة في الجدول أدناه)")) { Margin = new Thickness(0, 20, 0, 0) });

                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    doc.PageHeight = printDialog.PrintableAreaHeight;
                    doc.PageWidth = printDialog.PrintableAreaWidth;
                    doc.PagePadding = new Thickness(40);
                    doc.ColumnGap = 0;
                    doc.ColumnWidth = printDialog.PrintableAreaWidth;

                    IDocumentPaginatorSource idpSource = doc;
                    printDialog.PrintDocument(idpSource.DocumentPaginator, "تقرير POS");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ الطباعة", ex.Message);
            }
        }
    }
}

