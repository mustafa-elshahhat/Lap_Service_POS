using AlJohary.ServiceHub.Shared.Helpers;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    public class FormattingTests
    {
        [Theory]
        [InlineData(100, "100")]
        [InlineData(0, "0")]
        public void FormatNumber_WholeNumbers_TrimTrailingDecimals(int value, string expected)
        {
            Assert.Equal(expected, Formatting.FormatNumber((decimal)value));
        }

        [Theory]
        [InlineData("100.50", "100,5")]
        [InlineData("100.25", "100,25")]
        public void FormatNumber_Decimals_KeepOnlyNeededDigits(string input, string expected)
        {
            Assert.Equal(expected, Formatting.FormatNumber(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture)));
        }

        [Fact]
        public void FormatNumber_Null_ReturnsZero()
        {
            Assert.Equal("0", Formatting.FormatNumber((decimal?)null));
        }

        [Fact]
        public void FormatNumber_Output_RoundTripsThroughSafeConvert()
        {
            decimal value = 1234.5m;
            string formatted = Formatting.FormatNumber(value);

            Assert.Equal(value, SafeConvert.ToDecimal(formatted));
        }

        [Fact]
        public void FormatNumber_WholeGroupedOutput_RoundTripsThroughSafeConvert()
        {
            decimal value = 1234m;
            string formatted = Formatting.FormatNumber(value);

            Assert.Equal(value, SafeConvert.ToDecimal(formatted));
        }
    }
}
