using System;
using System.IO;
using System.Linq;
using System.Text;
using AlJohary.ServiceHub.Infrastructure.Services;
using ClosedXML.Excel;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    public class SupplierPurchaseImportServiceTests
    {
        [Fact]
        public void Import_ValidCsv_ImportsRows()
        {
            string path = WriteTempFile(".csv", "ProductName,Quantity,UnitPurchasePrice\nMouse,2,150.5\nKeyboard,1,300");
            var service = new CsvPurchaseImportService();

            var result = service.Import(path);

            Assert.Empty(result.Errors);
            Assert.Equal(2, result.Rows.Count);
            Assert.Equal("Mouse", result.Rows[0].ProductName);
            Assert.Equal(2, result.Rows[0].Quantity);
            Assert.Equal(150.5m, result.Rows[0].UnitPurchasePrice);
        }

        [Fact]
        public void Import_ValidCsvWithBomAndSemicolon_ImportsRows()
        {
            string path = WriteTempFile(".csv", "\uFEFFProductName;Quantity;UnitPurchasePrice\nCharger;3;250,75");
            var service = new CsvPurchaseImportService();

            var result = service.Import(path);

            Assert.Empty(result.Errors);
            Assert.Single(result.Rows);
            Assert.Equal("Charger", result.Rows[0].ProductName);
            Assert.Equal(3, result.Rows[0].Quantity);
            Assert.Equal(250.75m, result.Rows[0].UnitPurchasePrice);
        }

        [Fact]
        public void Import_ValidXlsx_ImportsFirstSheetOnly()
        {
            string path = WriteTempXlsx(workbook =>
            {
                var first = workbook.AddWorksheet("First");
                WriteHeader(first);
                first.Cell(2, 1).Value = "SSD";
                first.Cell(2, 2).Value = 4;
                first.Cell(2, 3).Value = 900;

                var second = workbook.AddWorksheet("Second");
                WriteHeader(second);
                second.Cell(2, 1).Value = "Ignored";
                second.Cell(2, 2).Value = 1;
                second.Cell(2, 3).Value = 1;
            });
            var service = new CsvPurchaseImportService();

            var result = service.Import(path);

            Assert.Empty(result.Errors);
            Assert.Single(result.Rows);
            Assert.Equal("SSD", result.Rows[0].ProductName);
        }

        [Fact]
        public void Import_UnsupportedExtension_ReturnsClearError()
        {
            string path = WriteTempFile(".txt", "ProductName,Quantity,UnitPurchasePrice\nMouse,1,10");
            var service = new CsvPurchaseImportService();

            var result = service.Import(path);

            Assert.Contains("CSV و XLSX", result.Errors.Single());
            Assert.Empty(result.Rows);
        }

        [Fact]
        public void Import_MissingRequiredHeader_ReturnsRequiredColumns()
        {
            string path = WriteTempFile(".csv", "ProductName,Quantity\nMouse,1");
            var service = new CsvPurchaseImportService();

            var result = service.Import(path);

            Assert.Contains("ProductName, Quantity, UnitPurchasePrice", result.Errors.Single());
            Assert.Contains("UnitPurchasePrice", result.Errors.Single());
            Assert.Empty(result.Rows);
        }

        [Fact]
        public void Import_NonNumericQuantity_ReturnsRowNumberAndReason()
        {
            string path = WriteTempFile(".csv", "ProductName,Quantity,UnitPurchasePrice\nMouse,two,10");
            var service = new CsvPurchaseImportService();

            var result = service.Import(path);

            Assert.Contains("السطر 2", result.Errors.Single());
            Assert.Contains("Quantity", result.Errors.Single());
            Assert.Empty(result.Rows);
        }

        [Fact]
        public void Import_NonNumericUnitPurchasePrice_ReturnsRowNumberAndReason()
        {
            string path = WriteTempFile(".csv", "ProductName,Quantity,UnitPurchasePrice\nMouse,2,10 EGP");
            var service = new CsvPurchaseImportService();

            var result = service.Import(path);

            Assert.Contains("السطر 2", result.Errors.Single());
            Assert.Contains("UnitPurchasePrice", result.Errors.Single());
            Assert.Empty(result.Rows);
        }

        [Fact]
        public void Import_EmptyRows_AreIgnored()
        {
            string path = WriteTempFile(".csv", "ProductName,Quantity,UnitPurchasePrice\nMouse,2,10\n,,\nKeyboard,1,20");
            var service = new CsvPurchaseImportService();

            var result = service.Import(path);

            Assert.Empty(result.Errors);
            Assert.Equal(2, result.Rows.Count);
            Assert.Equal(new[] { "Mouse", "Keyboard" }, result.Rows.Select(r => r.ProductName).ToArray());
        }

        private static string WriteTempFile(string extension, string content)
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);
            File.WriteAllText(path, content, new UTF8Encoding(false));
            return path;
        }

        private static string WriteTempXlsx(Action<XLWorkbook> configure)
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xlsx");
            using (var workbook = new XLWorkbook())
            {
                configure(workbook);
                workbook.SaveAs(path);
            }

            return path;
        }

        private static void WriteHeader(IXLWorksheet worksheet)
        {
            worksheet.Cell(1, 1).Value = "ProductName";
            worksheet.Cell(1, 2).Value = "Quantity";
            worksheet.Cell(1, 3).Value = "UnitPurchasePrice";
        }
    }
}
