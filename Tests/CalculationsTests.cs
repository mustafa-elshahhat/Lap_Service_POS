using AlJohary.ServiceHub.Shared.Helpers;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    public class CalculationsTests
    {
        [Theory]
        [InlineData(100, 80, 20)]
        [InlineData(200, 150, 25)]
        [InlineData(50, 50, 0)]
        [InlineData(100, 0, 100)]
        public void CalculateDiscountPercent_ValidInputs_ReturnsCorrectPercent(decimal originalPrice, decimal finalPrice, decimal expected)
        {
            Assert.Equal(expected, Calculations.CalculateDiscountPercent(originalPrice, finalPrice));
        }

        [Fact]
        public void CalculateDiscountPercent_ZeroOriginalPrice_ReturnsZero()
        {
            Assert.Equal(0, Calculations.CalculateDiscountPercent(0, 100));
        }

        [Fact]
        public void CalculateDiscountPercent_NegativeOriginalPrice_ReturnsZero()
        {
            Assert.Equal(0, Calculations.CalculateDiscountPercent(-1, 100));
        }

        [Theory]
        [InlineData(100, 120, 20)]
        [InlineData(200, 250, 25)]
        [InlineData(50, 50, 0)]
        [InlineData(100, 200, 100)]
        public void CalculateMarkupPercent_ValidInputs_ReturnsCorrectPercent(decimal originalPrice, decimal finalPrice, decimal expected)
        {
            Assert.Equal(expected, Calculations.CalculateMarkupPercent(originalPrice, finalPrice));
        }

        [Fact]
        public void CalculateMarkupPercent_ZeroOriginalPrice_ReturnsZero()
        {
            Assert.Equal(0, Calculations.CalculateMarkupPercent(0, 100));
        }

        [Fact]
        public void CalculateMarkupPercent_NegativeOriginalPrice_ReturnsZero()
        {
            Assert.Equal(0, Calculations.CalculateMarkupPercent(-1, 100));
        }
    }
}
