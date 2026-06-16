using System;

namespace AlJohary.ServiceHub.Core.Pricing
{
    public class PriceLimitResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }

        public static PriceLimitResult Ok() => new PriceLimitResult { IsValid = true };
        public static PriceLimitResult Fail(string message) => new PriceLimitResult { IsValid = false, Message = message };
    }

    /// <summary>
    /// Pure, role-aware validator for the final per-item sale price. Mirrors the UI logic in
    /// POSViewModel.EditPrice and is the authoritative enforcement point (the UI is only a pre-check).
    ///
    /// Rules (see financial-flow plan §9):
    /// - Below-cost sales are blocked for EVERYONE, including admin/manager. The purchase-price floor
    ///   is universal and non-bypassable.
    /// - Admin/manager (canBypassLimits) bypasses the discount %/markup % ceilings ONLY.
    /// - Employee: final price must be within [max(original*(1-maxDiscount%), cost), original*(1+maxMarkup%)].
    /// - Both limits 0 => the window collapses to the original price (price cannot move).
    /// </summary>
    public static class PriceLimitValidator
    {
        private const decimal Epsilon = 0.01m;

        public static PriceLimitResult Validate(
            decimal originalPrice,
            decimal cost,
            decimal finalPrice,
            bool canBypassLimits,
            double maxDiscountPercent,
            double maxMarkupPercent,
            string productName = null)
        {
            string label = string.IsNullOrEmpty(productName) ? string.Empty : $" ({productName})";

            // Universal, non-bypassable floor: never sell below cost — applies to admin too.
            if (finalPrice < cost - Epsilon)
                return PriceLimitResult.Fail($"غير مسموح بالبيع بأقل من سعر الشراء{label}: {cost:0.##}");

            // Admin/manager bypasses only the discount/markup percentage ceilings.
            if (canBypassLimits)
                return PriceLimitResult.Ok();

            decimal minByDiscount = originalPrice * (1 - (decimal)(maxDiscountPercent / 100));
            decimal minPrice = Math.Max(minByDiscount, cost);
            decimal maxPrice = originalPrice * (1 + (decimal)(maxMarkupPercent / 100));

            if (finalPrice < minPrice - Epsilon)
                return PriceLimitResult.Fail($"السعر أقل من الحد المسموح (نسبة الخصم){label}: {minPrice:0.##}");

            if (finalPrice > maxPrice + Epsilon)
                return PriceLimitResult.Fail($"السعر أعلى من الحد المسموح{label}: {maxPrice:0.##}");

            return PriceLimitResult.Ok();
        }
    }
}
