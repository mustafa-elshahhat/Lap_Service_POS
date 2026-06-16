using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Presentation;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.Printing
{
    public abstract class A4PrintBase
    {
        protected static readonly Color PrimaryColor      = (Color)ColorConverter.ConvertFromString("#2563EB");
        protected static readonly Color LightBlueColor    = (Color)ColorConverter.ConvertFromString("#EFF6FF");
        protected static readonly Color SubHeaderColor    = (Color)ColorConverter.ConvertFromString("#DBEAFE");
        protected static readonly Color BorderColor       = (Color)ColorConverter.ConvertFromString("#E5E7EB");
        protected static readonly Color CardBgColor       = (Color)ColorConverter.ConvertFromString("#F3F4F6");
        protected static readonly Color TextMutedColor    = Color.FromRgb(107, 114, 128);
        protected static readonly Color TextDarkColor     = Color.FromRgb(55, 65, 81);

        protected void PrintFlowDocument(FlowDocument doc, string jobName)
        {
            try
            {
                PrintDialog dlg = new PrintDialog();
                if (dlg.ShowDialog() == true)
                {
                    double a4W = 794, a4H = 1123;
                    double pageWidth  = dlg.PrintableAreaWidth  > 500 ? dlg.PrintableAreaWidth  : a4W;
                    double pageHeight = dlg.PrintableAreaHeight > 800 ? dlg.PrintableAreaHeight : a4H;

                    doc.PageWidth   = pageWidth;
                    doc.PageHeight  = pageHeight;
                    doc.PagePadding = new Thickness(40);
                    doc.ColumnGap   = 0;
                    doc.ColumnWidth = pageWidth - 80;
                    doc.FlowDirection = FlowDirection.RightToLeft;

                    dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, jobName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الطباعة: " + ex.Message, "خطأ طباعة",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected FlowDocument InitializeDocument()
        {
            return new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize   = 12,
                FlowDirection = FlowDirection.RightToLeft,
                Background    = Brushes.White,
                PagePadding   = new Thickness(40)
            };
        }

        protected void AddDocumentHeader(FlowDocument doc, string title, string subtitle = null)
        {
            string shopName = DatabaseManager.Instance.GetSetting("shop_name", "الجوهري");
            string phonesText = GetPhonesText();

            Table t = new Table { CellSpacing = 0 };
            t.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) });
            t.Columns.Add(new TableColumn { Width = new GridLength(2,   GridUnitType.Star) });
            t.Columns.Add(new TableColumn { Width = new GridLength(1,   GridUnitType.Star) });
            t.RowGroups.Add(new TableRowGroup());
            TableRow row = new TableRow();

            UIElement logo = LoadLogo(shopName);
            row.Cells.Add(new TableCell(new BlockUIContainer(logo)) { TextAlignment = TextAlignment.Left });

            StackPanel titlePanel = new StackPanel();
            titlePanel.Children.Add(new TextBlock(new Run(title))
            {
                FontSize = 22, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(PrimaryColor),
                TextAlignment = TextAlignment.Center
            });
            if (!string.IsNullOrEmpty(subtitle))
                titlePanel.Children.Add(new TextBlock(new Run(subtitle))
                {
                    FontSize = 13, Foreground = new SolidColorBrush(TextMutedColor),
                    TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 4, 0, 0)
                });
            row.Cells.Add(new TableCell(new BlockUIContainer(titlePanel)));

            StackPanel meta = new StackPanel();
            meta.Children.Add(new TextBlock(new Run(shopName))
            {
                FontWeight = FontWeights.Bold, FontSize = 12, TextAlignment = TextAlignment.Right
            });
            if (!string.IsNullOrWhiteSpace(phonesText))
            {
                meta.Children.Add(new TextBlock(new Run(phonesText))
                {
                    FontSize = 11, Foreground = new SolidColorBrush(TextMutedColor),
                    TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 3, 0, 0),
                    FlowDirection = FlowDirection.LeftToRight
                });
            }
            meta.Children.Add(new TextBlock(new Run(DateTime.Now.ToString("yyyy-MM-dd  HH:mm")))
            {
                FontSize = 11, Foreground = new SolidColorBrush(TextMutedColor),
                TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 3, 0, 0)
            });
            row.Cells.Add(new TableCell(new BlockUIContainer(meta)));

            t.RowGroups[0].Rows.Add(row);
            doc.Blocks.Add(t);

            doc.Blocks.Add(new Paragraph
            {
                BorderBrush = new SolidColorBrush(PrimaryColor),
                BorderThickness = new Thickness(0, 3, 0, 1),
                Margin = new Thickness(0, 5, 0, 15),
                Padding = new Thickness(0, 2, 0, 0)
            });
        }

        protected void AddDocumentFooter(FlowDocument doc, string thankYouNote = null)
        {
            StackPanel panel = new StackPanel { Margin = new Thickness(0, 30, 0, 0) };

            if (!string.IsNullOrEmpty(thankYouNote))
                panel.Children.Add(new TextBlock(new Run(thankYouNote))
                {
                    FontSize = 13, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(PrimaryColor),
                    TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 12)
                });

            panel.Children.Add(new TextBlock(new Run("نهاية المستند"))
            {
                FontSize = 10, Foreground = Brushes.LightGray,
                TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8)
            });

            Border bar = new Border
            {
                BorderThickness = new Thickness(0, 1, 0, 0),
                BorderBrush     = new SolidColorBrush(PrimaryColor),
                Padding         = new Thickness(0, 6, 0, 0)
            };
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            string appUserName = ServiceContainer.GetService<IAuthService>()?.GetUserName() ?? Environment.UserName;
            var leftTxt = new TextBlock(new Run("طبع بواسطة: " + appUserName))
            { FontSize = 10, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Left };
            Grid.SetColumn(leftTxt, 0);
            grid.Children.Add(leftTxt);

            string sysName = DatabaseManager.Instance.GetSetting("shop_name", "الجوهري");
            var rightTxt = new TextBlock(new Run(sysName))
            { FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(rightTxt, 1);
            grid.Children.Add(rightTxt);

            bar.Child = grid;
            panel.Children.Add(bar);
            doc.Blocks.Add(new BlockUIContainer(panel));
        }

        protected void AddSectionTitle(FlowDocument doc, string title)
        {
            doc.Blocks.Add(new Paragraph(new Run(title))
            {
                FontSize   = 14, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(PrimaryColor),
                BorderBrush = new SolidColorBrush(SubHeaderColor),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(0, 0, 0, 4),
                Margin  = new Thickness(0, 16, 0, 8)
            });
        }

        protected Table CreateInfoGrid(List<(string label, string value)> items)
        {
            Table table = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 0, 0, 16),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1)
            };
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            TableRowGroup grp = new TableRowGroup();
            bool even = false;
            for (int i = 0; i < items.Count; i += 2)
            {
                TableRow row = new TableRow
                {
                    Background = even ? new SolidColorBrush(LightBlueColor) : Brushes.White
                };
                row.Cells.Add(BuildInfoCell(items[i].label, items[i].value));
                row.Cells.Add(i + 1 < items.Count
                    ? BuildInfoCell(items[i + 1].label, items[i + 1].value)
                    : new TableCell());
                grp.Rows.Add(row);
                even = !even;
            }
            table.RowGroups.Add(grp);
            return table;
        }

        protected Table CreateItemsTable(string[] headers, List<string[]> rows, double[] starWidths = null)
        {
            Table table = new Table
            {
                CellSpacing = 0,
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 16)
            };

            if (starWidths != null)
                foreach (var w in starWidths)
                    table.Columns.Add(new TableColumn { Width = new GridLength(w, GridUnitType.Star) });
            else
                foreach (var _ in headers)
                    table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            TableRowGroup grp = new TableRowGroup();

            TableRow hRow = new TableRow { Background = new SolidColorBrush(PrimaryColor) };
            foreach (var h in headers)
                hRow.Cells.Add(new TableCell(
                    new Paragraph(new Run(h))
                    {
                        FontWeight = FontWeights.Bold, Foreground = Brushes.White,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 6, 0, 6)
                    })
                {
                    Padding = new Thickness(6, 4, 6, 4),
                    BorderThickness = new Thickness(0, 0, 1, 0),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(180, 200, 240))
                });
            grp.Rows.Add(hRow);

            bool even = false;
            foreach (var cells in rows)
            {
                TableRow row = new TableRow
                {
                    Background = even ? new SolidColorBrush(LightBlueColor) : Brushes.White
                };
                foreach (var val in cells)
                    row.Cells.Add(new TableCell(
                        new Paragraph(new Run(val ?? ""))
                        {
                            TextAlignment = TextAlignment.Center,
                            Margin = new Thickness(0, 4, 0, 4)
                        })
                    {
                        Padding = new Thickness(6, 4, 6, 4),
                        BorderBrush = new SolidColorBrush(BorderColor),
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    });
                grp.Rows.Add(row);
                even = !even;
            }

            table.RowGroups.Add(grp);
            return table;
        }

        protected void AddTotalsBox(FlowDocument doc, List<(string label, string value, bool highlight)> items)
        {
            Table table = new Table { CellSpacing = 0, Margin = new Thickness(250, 0, 0, 16) };
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            TableRowGroup grp = new TableRowGroup();
            bool even = false;
            foreach (var item in items)
            {
                TableRow row = new TableRow { Background = even ? new SolidColorBrush(LightBlueColor) : Brushes.White };

                FontWeight fw = item.highlight ? FontWeights.Bold : FontWeights.Normal;
                Brush valBrush = item.highlight ? new SolidColorBrush(PrimaryColor) : Brushes.Black;

                row.Cells.Add(new TableCell(
                    new Paragraph(new Run(item.label))
                    { TextAlignment = TextAlignment.Right, FontWeight = fw, Margin = new Thickness(0, 5, 0, 5) })
                { Padding = new Thickness(10, 4, 10, 4) });

                row.Cells.Add(new TableCell(
                    new Paragraph(new Run(item.value))
                    { TextAlignment = TextAlignment.Left, FontWeight = fw, Foreground = valBrush, Margin = new Thickness(0, 5, 0, 5) })
                { Padding = new Thickness(10, 4, 10, 4) });

                grp.Rows.Add(row);
                even = !even;
            }
            table.RowGroups.Add(grp);
            doc.Blocks.Add(table);
        }

        protected void AddSignatureLine(FlowDocument doc)
        {
            Table t = new Table { CellSpacing = 0, Margin = new Thickness(0, 24, 0, 0) };
            t.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            t.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            TableRowGroup g = new TableRowGroup();
            TableRow r = new TableRow();
            r.Cells.Add(new TableCell(
                new Paragraph(new Run("توقيع المستلم:  _______________________"))
                { FontSize = 12, TextAlignment = TextAlignment.Right })
            { Padding = new Thickness(10) });
            r.Cells.Add(new TableCell(
                new Paragraph(new Run("توقيع الموظف:   _______________________"))
                { FontSize = 12, TextAlignment = TextAlignment.Left })
            { Padding = new Thickness(10) });
            g.Rows.Add(r);
            t.RowGroups.Add(g);
            doc.Blocks.Add(t);
        }

        private TableCell BuildInfoCell(string label, string value)
        {
            StackPanel sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin      = new Thickness(10, 6, 10, 6)
            };
            if (!string.IsNullOrEmpty(label))
            {
                sp.Children.Add(new TextBlock(new Run(label + ":  "))
                { FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(TextDarkColor) });
                sp.Children.Add(new TextBlock(new Run(value ?? ""))
                { Foreground = Brushes.Black });
            }
            return new TableCell(new BlockUIContainer(sp))
            {
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
        }

        private UIElement LoadLogo(string shopName)
        {
            try
            {
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.UriSource  = new Uri("pack://application:,,,/Presentation/Resources/Images/logo.png");
                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bmp.EndInit();
                return new Image { Source = bmp, Width = 110, Height = 90, Stretch = Stretch.Uniform, FlowDirection = FlowDirection.LeftToRight };
            }
            catch { } // best-effort: logo from pack:// may fail silently

            string path = DatabaseManager.Instance.GetSetting("logo_path", "");
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                try
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(path);
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    return new Image { Source = bmp, Width = 110, Height = 90, Stretch = Stretch.Uniform, FlowDirection = FlowDirection.LeftToRight };
                }
                catch { } // best-effort: logo from file path may be missing/corrupt
            }

            return new Border
            {
                Width = 50, Height = 50, CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(PrimaryColor),
                Child = new TextBlock(new Run(shopName.Length > 0 ? shopName.Substring(0, 1) : "م"))
                {
                    FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center
                }
            };
        }

        private static string GetPhonesText()
        {
            try
            {
                return Formatting.FormatPhonesForPrint(ServiceContainer.GetService<ISettingsService>()?.GetShopPhones());
            }
            catch // best-effort: phones formatting fallback
            {
                return string.Empty;
            }
        }
    }
}
