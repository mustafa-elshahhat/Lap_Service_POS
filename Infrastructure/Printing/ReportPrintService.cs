using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Linq;
using CarPartsShopWPF.Infrastructure.Data;
using CarPartsShopWPF.Application.DTOs;

namespace CarPartsShopWPF.Infrastructure.Printing
{

    public class ReportPrintService : A4PrintBase
    {
        public void Print(string title, IEnumerable<Dictionary<string, object>> data, string[] columns, string[] headers)
        {
            FlowDocument doc = CreateReportDocument(title, data, columns, headers);
            PrintFlowDocument(doc, title);
        }

        public void PrintGrouped(string title, IEnumerable<GroupedReportItem> data, string[] itemColumns, string[] itemHeaders)
        {
            FlowDocument doc = CreateGroupedReportDocument(title, data, itemColumns, itemHeaders);
            PrintFlowDocument(doc, title);
        }


        private void AddHeader(FlowDocument doc, string title)
        {

            Table headerTable = new Table();
            headerTable.CellSpacing = 0;

            headerTable.Columns.Add(new TableColumn() { Width = new GridLength(1.2, GridUnitType.Star) }); 
            headerTable.Columns.Add(new TableColumn() { Width = new GridLength(2, GridUnitType.Star) });   
            headerTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });   
            
            headerTable.RowGroups.Add(new TableRowGroup());
            TableRow headerRow = new TableRow();
            
            StackPanel shopPanel = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };

            string shopName = DatabaseManager.Instance.GetSetting("shop_name", "الجوهري");

            UIElement logoElement = null;
            try 
            {
                 var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                 bitmap.BeginInit(); 
                 bitmap.UriSource = new Uri("pack://application:,,,/Presentation/Resources/Images/logo.png"); 
                 bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; 
                 bitmap.EndInit();
                 
                 logoElement = new Image() { Source = bitmap, Width = 110, Height = 90, Stretch = Stretch.Uniform, FlowDirection = FlowDirection.LeftToRight };
            } 
            catch { }

            if (logoElement == null)
            {
                 string logoPath = DatabaseManager.Instance.GetSetting("logo_path", "");
                 if (!string.IsNullOrEmpty(logoPath) && System.IO.File.Exists(logoPath))
                 {
                     try {
                         var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                         bitmap.BeginInit(); bitmap.UriSource = new Uri(logoPath); bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; bitmap.EndInit();
                         logoElement = new Image() { Source = bitmap, Width = 110, Height = 90, Stretch = Stretch.Uniform, FlowDirection = FlowDirection.LeftToRight };
                     } catch { }
                 }
            }

            if (logoElement == null)
            {
                 Border defaultLogo = new Border() { Width = 50, Height = 50, CornerRadius = new CornerRadius(8), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")), Child = new TextBlock(new Run(shopName.Substring(0, 1))) { FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
                 logoElement = defaultLogo;
            }
            shopPanel.Children.Add(logoElement);
            headerRow.Cells.Add(new TableCell(new BlockUIContainer(shopPanel)) { TextAlignment = TextAlignment.Left });

            headerRow.Cells.Add(new TableCell(new Paragraph(new Run(title)) { FontSize = 22, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")), TextAlignment = TextAlignment.Center }));

            StackPanel datePanel = new StackPanel();
            datePanel.Children.Add(new TextBlock(new Run("تاريخ التقرير")) { FontWeight = FontWeights.Bold, FontSize = 11, Foreground = Brushes.Gray, TextAlignment = TextAlignment.Right });
            datePanel.Children.Add(new TextBlock(new Run($"{DateTime.Now:yyyy-MM-dd HH:mm}")) { FontSize = 12, TextAlignment = TextAlignment.Right });
            headerRow.Cells.Add(new TableCell(new BlockUIContainer(datePanel)));
            
            headerTable.RowGroups[0].Rows.Add(headerRow);
            doc.Blocks.Add(headerTable);

            doc.Blocks.Add(new Paragraph() { 
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")), 
                BorderThickness = new Thickness(0, 3, 0, 1),
                Margin = new Thickness(0, 5, 0, 15),
                Padding = new Thickness(0, 2, 0, 0)
            });
        }


        private FlowDocument CreateReportDocument(string title, IEnumerable<Dictionary<string, object>> data, string[] columns, string[] headers)
        {
            Array.Reverse(columns);
            Array.Reverse(headers);

            FlowDocument doc = InitializeDocument();
            AddHeader(doc, title);

            Table table = new Table() { CellSpacing = 0, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")), BorderThickness = new Thickness(1) };
            
            foreach (var col in columns)
                table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });

            TableRowGroup group = new TableRowGroup();

            TableRow tableHeaderRow = new TableRow() { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")) };
            foreach (var header in headers)
            {
                var cell = new TableCell(new Paragraph(new Run(header)) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Foreground = Brushes.White, Margin = new Thickness(0, 6, 0, 6) }) { Padding = new Thickness(4), BorderThickness = new Thickness(0, 0, 1, 0), BorderBrush = Brushes.White };
                tableHeaderRow.Cells.Add(cell);
            }
            group.Rows.Add(tableHeaderRow);

            bool isEven = false;
            foreach (var rowData in data)
            {
                TableRow row = new TableRow();
                row.Background = isEven ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF")) : Brushes.White;
                foreach (var col in columns)
                {
                    string val = rowData.ContainsKey(col) ? rowData[col]?.ToString() : "";
                    var cell = new TableCell(new Paragraph(new Run(val)) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 4, 0, 4) }) { Padding = new Thickness(4), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")), BorderThickness = new Thickness(0, 0, 0, 1) };
                    row.Cells.Add(cell);
                }
                group.Rows.Add(row);
                isEven = !isEven;
            }

            table.RowGroups.Add(group);
            doc.Blocks.Add(table);

            AddDocumentFooter(doc, "--- نهاية التقرير ---");
            return doc;
        }

        private FlowDocument CreateGroupedReportDocument(string title, IEnumerable<GroupedReportItem> data, string[] itemColumns, string[] itemHeaders)
        {

            Array.Reverse(itemColumns);
            Array.Reverse(itemHeaders);

            FlowDocument doc = InitializeDocument();
            AddHeader(doc, title);

            foreach (var group in data)
            {

                Border groupCard = new Border() {
                     Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6")),
                     BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                     BorderThickness = new Thickness(1),
                     CornerRadius = new CornerRadius(6),
                     Padding = new Thickness(10),
                     Margin = new Thickness(0, 10, 0, 5)
                };
                
                WrapPanel headerPanel = new WrapPanel();
                foreach(var key in group.GroupHeader.Keys)
                {
                    if(key.StartsWith("_")) continue;
                    var val = group.GroupHeader[key];
                    StackPanel field = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 20, 5) };
                    field.Children.Add(new TextBlock(new Run(key + ": ")) { FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151")) });
                    field.Children.Add(new TextBlock(new Run(val?.ToString())) { Foreground = Brushes.Black });
                    headerPanel.Children.Add(field);
                }
                groupCard.Child = headerPanel;
                doc.Blocks.Add(new BlockUIContainer(groupCard));

                if(group.Items != null && group.Items.Count > 0)
                {
                    Table table = new Table() { CellSpacing = 0, Margin = new Thickness(20, 0, 0, 15) };
                    foreach (var col in itemColumns) table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });

                    TableRowGroup trg = new TableRowGroup();

                    TableRow hRow = new TableRow() { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE")) };
                    foreach (var h in itemHeaders)
                    {
                         hRow.Cells.Add(new TableCell(new Paragraph(new Run(h)) { FontWeight = FontWeights.Bold, FontSize = 11 }) { Padding = new Thickness(5) });
                    }
                    trg.Rows.Add(hRow);

                    foreach (var item in group.Items)
                    {
                        TableRow row = new TableRow();
                        foreach (var col in itemColumns)
                        {
                            string val = item.ContainsKey(col) ? item[col]?.ToString() : "";
                            row.Cells.Add(new TableCell(new Paragraph(new Run(val)) { FontSize = 11 }) { Padding = new Thickness(5), BorderThickness = new Thickness(0,0,0,1), BorderBrush = Brushes.LightGray });
                        }
                        trg.Rows.Add(row);
                    }
                    table.RowGroups.Add(trg);
                    doc.Blocks.Add(table);
                }
            }

            AddDocumentFooter(doc, "--- نهاية التقرير ---");
            return doc;
        }
    }
}
