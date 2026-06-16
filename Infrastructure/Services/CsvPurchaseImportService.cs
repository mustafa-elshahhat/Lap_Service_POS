using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AlJohary.ServiceHub.Application.DTOs;
using AlJohary.ServiceHub.Application.Interfaces;

namespace AlJohary.ServiceHub.Infrastructure.Services
{
    public class CsvPurchaseImportService : IPurchaseImportService
    {
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

            if (!string.Equals(Path.GetExtension(filePath), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("الاستيراد الحالي يدعم ملفات CSV فقط");
                return result;
            }

            var lines = File.ReadAllLines(filePath, Encoding.UTF8).ToList();
            if (lines.Count == 0)
            {
                result.Errors.Add("الملف فارغ");
                return result;
            }

            var headers = ParseCsvLine(lines[0]);
            var map = BuildHeaderMap(headers);
            var missing = new[] { "ProductName", "Quantity", "UnitPurchasePrice" }
                .Where(h => !map.ContainsKey(h))
                .ToList();
            if (missing.Count > 0)
            {
                result.Errors.Add("أعمدة مطلوبة غير موجودة: " + string.Join(", ", missing));
                return result;
            }

            for (int i = 1; i < lines.Count; i++)
            {
                var cells = ParseCsvLine(lines[i]);
                if (cells.All(string.IsNullOrWhiteSpace)) continue;

                int rowNumber = i + 1;
                string name = GetCell(cells, map, "ProductName");
                string qtyText = GetCell(cells, map, "Quantity");
                string priceText = GetCell(cells, map, "UnitPurchasePrice");

                bool valid = true;
                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Errors.Add($"السطر {rowNumber}: اسم المنتج مطلوب");
                    valid = false;
                }

                if (!TryParseInt(qtyText, out int quantity) || quantity <= 0)
                {
                    result.Errors.Add($"السطر {rowNumber}: الكمية يجب أن تكون رقماً صحيحاً أكبر من صفر");
                    valid = false;
                }

                if (!TryParseDecimal(priceText, out decimal unitPurchasePrice) || unitPurchasePrice < 0)
                {
                    result.Errors.Add($"السطر {rowNumber}: سعر الشراء يجب أن يكون رقماً غير سالب");
                    valid = false;
                }

                if (!valid) continue;

                result.Rows.Add(new SupplierPurchaseLineInput
                {
                    ProductName = name.Trim(),
                    Quantity = quantity,
                    UnitPurchasePrice = unitPurchasePrice
                });
            }

            return result;
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
            return string.Join(" ", (header ?? "").Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        }

        private static string GetCell(List<string> cells, Dictionary<string, int> map, string column)
        {
            if (!map.TryGetValue(column, out int index) || index < 0 || index >= cells.Count) return string.Empty;
            return cells[index];
        }

        private static List<string> ParseCsvLine(string line)
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
                else if (c == ',' && !inQuotes)
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
            return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
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
            if (s.Contains(',') && !s.Contains('.')) s = s.Replace(',', '.');
            else s = s.Replace(",", "");
            return s;
        }
    }
}
