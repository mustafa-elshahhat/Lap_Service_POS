using System;

namespace CarPartsShopWPF.Core.Accounting
{
    public static class FinancialCalculator
    {
        public static decimal CalculateNetProfit(decimal grossSalesProfit, decimal lostReturnProfit, decimal expenses)
            => grossSalesProfit - lostReturnProfit - expenses;

        public static decimal CalculateNetCash(decimal collected, decimal expenses, decimal returnsPaidOut)
            => collected - expenses - returnsPaidOut;

        public static decimal CalculateSaleProfit(decimal finalPrice, decimal purchasePrice, int quantity)
            => (finalPrice - purchasePrice) * quantity;

        public static decimal CalculateRemaining(decimal total, decimal paid)
            => total > paid ? total - paid : 0;

        public static decimal CalculateTotalWithDiscountAndMarkup(decimal subtotal, decimal discountAmount, decimal markupAmount)
            => subtotal - discountAmount + markupAmount;

        public static decimal CalculateDiscountPercent(decimal originalPrice, decimal finalPrice)
        {
            if (originalPrice == 0) return 0;
            return ((originalPrice - finalPrice) / originalPrice) * 100;
        }

        public static decimal CalculateMarkupPercent(decimal originalPrice, decimal finalPrice)
        {
            if (originalPrice == 0) return 0;
            return ((finalPrice - originalPrice) / originalPrice) * 100;
        }

        public static decimal CalculateTotalProfit(decimal[] itemProfits)
        {
            decimal total = 0;
            foreach (var profit in itemProfits) total += profit;
            return total;
        }



        public static decimal[] DistributeProportionally(decimal amountToDistribute, decimal[] itemTotals)
        {
            if (amountToDistribute <= 0 || itemTotals.Length == 0)
                return new decimal[itemTotals.Length];

            decimal totalSum = 0;
            foreach (var t in itemTotals) totalSum += t;

            if (totalSum <= 0) return new decimal[itemTotals.Length];

            decimal[] distributed = new decimal[itemTotals.Length];
            decimal remaining = amountToDistribute;

            for (int i = 0; i < itemTotals.Length; i++)
            {
                if (i == itemTotals.Length - 1)
                {
                    distributed[i] = remaining;
                }
                else
                {
                    decimal share = Math.Round(amountToDistribute * (itemTotals[i] / totalSum), 2);
                    distributed[i] = Math.Min(share, remaining);
                    remaining -= distributed[i];
                }
            }
            return distributed;
        }
    }
}
