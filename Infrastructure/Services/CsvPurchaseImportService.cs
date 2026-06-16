using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AlJohary.ServiceHub.Application.DTOs;
using AlJohary.ServiceHub.Application.Interfaces;
using ClosedXML.Excel;

namespace AlJohary.ServiceHub.Infrastructure.Services
{
    public class CsvPurchaseImportService : IPurchaseImportService
    {
        private static readonly string[] RequiredColumns = SupplierPurchaseImportColumns.Canonical;

        private static readonly Dictionary<string, string> HeaderAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "productname", "ProductName" },
            { "اسم المنتج", "ProductName" },
            { "quantity", "Quantity" },
            { "الكمية", "Quantity" },
            { "unitpurchaseprice", "UnitPurchasePrice" },
            { "سعر الشراء", "UnitPurchasePrice" }
        };

        public ExcelImportResult Import(string filePath)
        {
            var result = new ExcelImportResult();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                result.Errors.Add("الملف غير موجود");
                return result;
            }

            string extension = Path.GetExtension(filePath);
            if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                ImportCsv(filePath, result);
            }
            else if (string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ImportXlsx(filePath, result);
            }
            else
            {
                result.Errors.Add("يدعم الاستيراد ملفات CSV و XLSX فقط");
            }

            return result;
        }

        private static void ImportCsv(string filePath, ExcelImportResult result)
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8).ToList();
            if (lines.Count == 0)
            {
                result.Errors.Add("الملف فارغ");
                return;
            }

            char delimiter = DetectDelimiter(lines[0]);
            var headers = ParseCsvLine(lines[0], delimiter);
            var map = BuildHeaderMap(headers);
            if (!ValidateRequiredHeaders(map, result))
            {
                return;
            }

            for (int i = 1; i < lines.Count; i++)
            {
                var cells = ParseCsvLine(lines[i], delimiter);
                if (IsEmptyRow(cells)) continue;

                AddValidatedRow(result, i + 1,
                    GetCell(cells, map, "ProductName"),
                    GetCell(cells, map, "Quantity"),
                    GetCell(cells, map, "UnitPurchasePrice"));
            }

            FinalizeImportResult(result);
        }

        private static void ImportXlsx(string filePath, ExcelImportResult result)
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null || worksheet.FirstRowUsed() == null)
                {
                    result.Errors.Add("الملف فارغ");
                    return;
                }

                const int headerRowNumber = 1;
                int lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
                if (lastColumn == 0)
                {
                    result.Errors.Add("الملف فارغ");
                    return;
                }

                var headers = Enumerable.Range(1, lastColumn)
                    .Select(column => GetCellText(worksheet.Cell(headerRowNumber, column)))
                    .ToList();
                var map = BuildHeaderMap(headers);
                if (!ValidateRequiredHeaders(map, result))
                {
                    return;
                }

                int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRowNumber;
                for (int rowNumber = headerRowNumber + 1; rowNumber <= lastRow; rowNumber++)
                {
                    var cells = Enumerable.Range(1, lastColumn)
                        .Select(column => GetCellText(worksheet.Cell(rowNumber, column)))
                        .ToList();
                    if (IsEmptyRow(cells)) continue;

                    AddValidatedRow(result, rowNumber,
                        GetCell(cells, map, "ProductName"),
                        GetCell(cells, map, "Quantity"),
                        GetCell(cells, map, "UnitPurchasePrice"));
                }
            }

            FinalizeImportResult(result);
        }

        private static bool ValidateRequiredHeaders(Dictionary<string, int> map, ExcelImportResult result)
        {
            var missing = RequiredColumns.Where(h => !map.ContainsKey(h)).ToList();
            if (missing.Count == 0) return true;

            result.Errors.Add("Header غير صحيح. الأعمدة المطلوبة هي: " + string.Join(", ", RequiredColumns) + ". الأعمدة الناقصة: " + string.Join(", ", missing));
            return false;
        }

        private static void AddValidatedRow(ExcelImportResult result, int rowNumber, string name, string qtyText, string priceText)
        {
            bool valid = true;
            if (string.IsNullOrWhiteSpace(name))
            {
                result.Errors.Add($"السطر {rowNumber}: ProductName مطلوب ولا يمكن أن يكون فارغاً");
                valid = false;
            }

            if (!TryParseInt(qtyText, out int quantity) || quantity <= 0)
            {
                result.Errors.Add($"السطر {rowNumber}: Quantity يجب أن يكون رقماً صحيحاً أكبر من صفر بدون رموز عملة");
                valid = false;
            }

            if (!TryParseDecimal(priceText, out decimal unitPurchasePrice) || unitPurchasePrice < 0)
            {
                result.Errors.Add($"السطر {rowNumber}: UnitPurchasePrice يجب أن يكون رقماً أكبر من أو يساوي صفر بدون رموز عملة");
                valid = false;
            }

            if (!valid) return;

            result.Rows.Add(new SupplierPurchaseLineInput
            {
                ProductName = name.Trim(),
                Quantity = quantity,
                UnitPurchasePrice = unitPurchasePrice
            });
        }

        private static void FinalizeImportResult(ExcelImportResult result)
        {
            if (result.Errors.Count > 0)
            {
                result.Rows.Clear();
                return;
            }

            if (result.Rows.Count == 0)
            {
                result.Errors.Add("لم يتم العثور على صفوف صالحة للاستيراد");
            }
        }

        private static Dictionary<string, int> BuildHeaderMap(List<string> headers)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                string normalized = NormalizeHeader(headers[i]);
                if (HeaderAliases.TryGetValue(normalized, out string canonical) && !map.ContainsKey(canonical))
                {
                    map[canonical] = i;
                }
            }
            return map;
        }

        private static string NormalizeHeader(string header)
        {
            return string.Join(" ", (header ?? "").Trim().TrimStart('\uFEFF').Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        }

        private static string GetCell(List<string> cells, Dictionary<string, int> map, string column)
        {
            if (!map.TryGetValue(column, out int index) || index < 0 || index >= cells.Count) return string.Empty;
            return cells[index];
        }

        private static bool IsEmptyRow(List<string> cells)
        {
            return cells.All(string.IsNullOrWhiteSpace);
        }

        private static char DetectDelimiter(string headerLine)
        {
            var commaCells = ParseCsvLine(headerLine, ',');
            var semicolonCells = ParseCsvLine(headerLine, ';');
            return semicolonCells.Count > commaCells.Count ? ';' : ',';
        }

        private static List<string> ParseCsvLine(string line, char delimiter)
        {
            var cells = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < (line ?? "").Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    cells.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            cells.Add(current.ToString());
            return cells;
        }

        private static bool TryParseInt(string text, out int value)
        {
            value = 0;
            if (!TryParseDecimal(text, out decimal number)) return false;
            value = (int)number;
            return number == value;
        }

        private static bool TryParseDecimal(string text, out decimal value)
        {
            string normalized = NormalizeNumber(text);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                value = 0;
                return false;
            }

            return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
        }

        private static string NormalizeNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var sb = new StringBuilder();
            foreach (char c in text.Trim())
            {
                if (c >= '\u0660' && c <= '\u0669') sb.Append((char)('0' + c - '\u0660'));
                else if (c >= '\u06F0' && c <= '\u06F9') sb.Append((char)('0' + c - '\u06F0'));
                else if (char.IsWhiteSpace(c)) continue;
                else sb.Append(c);
            }

            string s = sb.ToString();
            if (s.Count(c => c == '.') + s.Count(c => c == ',') > 1) return string.Empty;
            s = s.Replace(',', '.');
            return s;
        }

        private static string GetCellText(IXLCell cell)
        {
            return cell?.Value.ToString() ?? string.Empty;
        }
    }
}
