using System;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.Models
{
    public class PriceEditPolicy
    {
        private readonly IAuthService _auth;

        public PriceEditPolicy(IAuthService auth)
        {
            _auth = auth;
        }

        public bool CanBypassLimits => _auth.CanBypassPriceLimits;

        public (decimal minPrice, decimal maxPrice) GetPriceBounds(CartItem item)
        {
            double maxDiscount = _auth.GetMaxDiscount();
            double maxMarkup = _auth.GetMaxMarkup();
            decimal discountLimit = item.OriginalPrice * (1 - (decimal)(maxDiscount / 100));
            decimal minPrice = Math.Max(discountLimit, item.PurchasePrice);
            decimal maxPrice = item.OriginalPrice * (1 + (decimal)(maxMarkup / 100));
            return (minPrice, maxPrice);
        }

        public string Validate(CartItem item, decimal newPrice)
        {
            if (newPrice <= 0)
                return "يجب أن يكون السعر أكبر من صفر";

            if (newPrice < item.PurchasePrice)
                return $"غير مسموح بالبيع بأقل من سعر الشراء: {Formatting.FormatCurrency(item.PurchasePrice)}";

            if (!_auth.CanBypassPriceLimits)
            {
                var (minPrice, maxPrice) = GetPriceBounds(item);

                if (newPrice < minPrice)
                    return $"السعر أقل من الحد المسموح (نسبة الخصم): {Formatting.FormatCurrency(minPrice)}";

                if (newPrice > maxPrice)
                    return $"السعر أعلى من الحد المسموح: {Formatting.FormatCurrency(maxPrice)}";
            }

            return null;
        }
    }
}
