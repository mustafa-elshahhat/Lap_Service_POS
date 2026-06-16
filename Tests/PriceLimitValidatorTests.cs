using AlJohary.ServiceHub.Core.Pricing;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    public class PriceLimitValidatorTests
    {
        // original 100, cost 60
        [Fact]
        public void EmployeeDiscountWithinLimit_Accepted()
        {
            // 10% discount limit -> floor 90; sell 95 is within range.
            var r = PriceLimitValidator.Validate(originalPrice: 100, cost: 60, finalPrice: 95,
                canBypassLimits: false, maxDiscountPercent: 10, maxMarkupPercent: 20);
            Assert.True(r.IsValid);
        }

        [Fact]
        public void EmployeeDiscountOverLimit_Rejected()
        {
            // 10% discount limit -> floor 90; sell 80 (20% off) is below the allowed minimum.
            var r = PriceLimitValidator.Validate(originalPrice: 100, cost: 60, finalPrice: 80,
                canBypassLimits: false, maxDiscountPercent: 10, maxMarkupPercent: 20);
            Assert.False(r.IsValid);
        }

        [Fact]
        public void EmployeeZeroLimits_PriceLocked()
        {
            // both limits 0 -> only the original price is allowed.
            Assert.True(PriceLimitValidator.Validate(100, 60, 100, false, 0, 0).IsValid);
            Assert.False(PriceLimitValidator.Validate(100, 60, 95, false, 0, 0).IsValid);
            Assert.False(PriceLimitValidator.Validate(100, 60, 110, false, 0, 0).IsValid);
        }

        [Fact]
        public void AdminBypassesPercentageCeiling_Accepted()
        {
            // 150 exceeds the 20% markup ceiling but admin bypasses %/markup limits.
            var r = PriceLimitValidator.Validate(originalPrice: 100, cost: 60, finalPrice: 150,
                canBypassLimits: true, maxDiscountPercent: 10, maxMarkupPercent: 20);
            Assert.True(r.IsValid);
        }

        [Fact]
        public void AdminCannotSellBelowCost_Rejected()
        {
            // Universal floor: even admin cannot sell below cost (60).
            var r = PriceLimitValidator.Validate(originalPrice: 100, cost: 60, finalPrice: 50,
                canBypassLimits: true, maxDiscountPercent: 100, maxMarkupPercent: 100);
            Assert.False(r.IsValid);
        }

        [Fact]
        public void EmployeeCannotSellBelowCost_Rejected()
        {
            var r = PriceLimitValidator.Validate(originalPrice: 100, cost: 60, finalPrice: 50,
                canBypassLimits: false, maxDiscountPercent: 90, maxMarkupPercent: 20);
            Assert.False(r.IsValid);
        }
    }
}
