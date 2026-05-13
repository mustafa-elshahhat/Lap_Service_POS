using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CarPartsShopWPF.Shared.Helpers
{

    public static class Security
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 10000;

        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            byte[] hashBytes = new byte[SaltSize + KeySize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(key, 0, hashBytes, SaltSize, KeySize);

            return Convert.ToBase64String(hashBytes);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                byte[] hashBytes = Convert.FromBase64String(hashedPassword);
                if (hashBytes.Length != SaltSize + KeySize) return false;

                byte[] salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                byte[] key = new byte[KeySize];
                Array.Copy(hashBytes, SaltSize, key, 0, KeySize);

                byte[] keyToCheck = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    Iterations,
                    HashAlgorithmName.SHA256,
                    KeySize);

                return CryptographicOperations.FixedTimeEquals(key, keyToCheck);
            }
            catch
            {
                return false;
            }
        }
    }

    public static class Formatting
    {
        private const string CurrencySymbol = "ج.م";
        private const string DateFormat = "dd/MM/yyyy";
        private const string DateTimeFormat = "dd/MM/yyyy HH:mm:ss";

        public static string FormatCurrency(decimal? amount, bool includeSymbol = true)
        {
            decimal value = amount ?? 0;
            var nfi = new NumberFormatInfo
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = ".",
                NumberDecimalDigits = 2
            };
            string formatted = value.ToString("N2", nfi);
            return includeSymbol ? $"{formatted} {CurrencySymbol}" : formatted;
        }

        public static string FormatCurrency(double? amount, bool includeSymbol = true)
        {
            return FormatCurrency((decimal?)(amount ?? 0), includeSymbol);
        }

        public static string FormatCurrency(object amount, bool includeSymbol = true)
        {
            if (amount == null) return FormatCurrency(0m, includeSymbol);
            if (decimal.TryParse(amount.ToString(), out decimal value))
                return FormatCurrency(value, includeSymbol);
            return FormatCurrency(0m, includeSymbol);
        }

        public static string FormatDate(DateTime? date, bool includeTime = false)
        {
            if (date == null) return "";
            return date.Value.ToString(includeTime ? DateTimeFormat : DateFormat);
        }

        public static string FormatDate(object date, bool includeTime = false)
        {
            if (date == null) return "";
            if (date is DateTime dt)
                return FormatDate(dt, includeTime);
            if (DateTime.TryParse(date.ToString(), out DateTime parsed))
                return FormatDate(parsed, includeTime);
            return date.ToString();
        }

        public static string GetSaleTypeArabic(string saleType)
        {
            switch (saleType?.ToLower())
            {
                case "cash": return "كاش";
                case "credit": return "آجل";
                default: return saleType;
            }
        }

        public static string GetRoleArabic(string role)
        {
            switch (role?.ToLower())
            {
                case "admin": return "مدير";
                case "employee": return "موظف";
                default: return role;
            }
        }

        public static string Truncate(string text, int maxLength = 50)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }
    }

    public static class SafeConvert
    {
        public static double ToDouble(object value, double defaultValue = 0.0)
        {
            if (value == null) return defaultValue;
            if (double.TryParse(value.ToString(), out double result))
                return result;
            return defaultValue;
        }

        public static decimal ToDecimal(object value, decimal defaultValue = 0m)
        {
            if (value == null || value == DBNull.Value) return defaultValue;
            if (value is decimal d) return d;
            if (value is double db) return (decimal)db;
            if (value is float f) return (decimal)f;
            if (value is int i) return (decimal)i;
            if (value is long l) return (decimal)l;

            string s = value.ToString();
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;

            s = s.Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
                 .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');

            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;

            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                return result;

            var arEg = CultureInfo.GetCultureInfo("ar-EG");
            if (decimal.TryParse(s, NumberStyles.Any, arEg, out result))
                return result;
            if (s.Contains(",") && !s.Contains("."))
            {
                if (decimal.TryParse(s.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                    return result;
            }
            else if (s.Contains(".") && !s.Contains(","))
            {
                if (decimal.TryParse(s.Replace(".", ","), NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                    return result;
            }

            return defaultValue;
        }

        public static int ToInt(object value, int defaultValue = 0)
        {
            if (value == null) return defaultValue;
            if (value is int i) return i;
            if (int.TryParse(value.ToString(), out int result))
                return result;
            if (double.TryParse(value.ToString(), out double doubleResult))
                return (int)doubleResult;
            return defaultValue;
        }

        public static long ToLong(object value, long defaultValue = 0)
        {
            if (value == null) return defaultValue;
            if (value is long l) return l;
            if (long.TryParse(value.ToString(), out long result))
                return result;
            if (double.TryParse(value.ToString(), out double doubleResult))
                return (long)doubleResult;
            return defaultValue;
        }

        public static bool ToBool(object value, bool defaultValue = false)
        {
            if (value == null) return defaultValue;
            if (value is bool b) return b;
            if (value is int i) return i != 0;
            if (value is long l) return l != 0;
            return defaultValue;
        }

        public static string ToString(object value, string defaultValue = "")
        {
            return value?.ToString() ?? defaultValue;
        }

        public static DateTime? ToDateTime(object value)
        {
            if (value == null) return null;
            if (value is DateTime dt) return dt;
            if (DateTime.TryParse(value.ToString(), out DateTime result))
                return result;
            return null;
        }
    }

    public static class Calculations
    {

        public static decimal CalculateDiscount(decimal originalPrice, decimal discountPercent)
        {
            decimal discountAmount = originalPrice * (discountPercent / 100);
            return originalPrice - discountAmount;
        }

        public static decimal CalculateMarkup(decimal originalPrice, decimal markupPercent)
        {
            decimal markupAmount = originalPrice * (markupPercent / 100);
            return originalPrice + markupAmount;
        }

        public static decimal CalculateDiscountPercent(decimal originalPrice, decimal finalPrice)
        {
            if (originalPrice <= 0) return 0;
            return ((originalPrice - finalPrice) / originalPrice) * 100;
        }

        public static decimal CalculateMarkupPercent(decimal originalPrice, decimal finalPrice)
        {
            if (originalPrice <= 0) return 0;
            return ((finalPrice - originalPrice) / originalPrice) * 100;
        }
    }

    public static class Validation
    {

        public static bool ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return true;

            phone = phone.Replace(" ", "").Replace("-", "");
            
            foreach (char c in phone)
            {
                if (!char.IsDigit(c)) return false;
            }

            return phone.Length >= 10 && phone.Length <= 15;
        }

        public static bool ValidateRequired(string value, string fieldName, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                error = $"الحقل '{fieldName}' مطلوب";
                return false;
            }
            return true;
        }

        public static bool ValidatePositiveNumber(decimal value, string fieldName, bool allowZero, out string error)
        {
            error = null;
            if (allowZero && value < 0)
            {
                error = $"الحقل '{fieldName}' يجب أن يكون صفر أو أكبر";
                return false;
            }
            if (!allowZero && value <= 0)
            {
                error = $"الحقل '{fieldName}' يجب أن يكون أكبر من صفر";
                return false;
            }
            return true;
        }
    }
}
