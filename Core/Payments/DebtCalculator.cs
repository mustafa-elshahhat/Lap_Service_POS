namespace CarPartsShopWPF.Core.Payments
{

    public static class DebtCalculator
    {

        public static decimal CalculateRemainingDebt(decimal totalDebt, decimal paymentAmount)
        {
            decimal remaining = totalDebt - paymentAmount;
            return remaining > 0 ? remaining : 0;
        }

        public static bool ValidatePaymentAmount(decimal debtAmount, decimal paymentAmount)
        {
            return paymentAmount > 0 && paymentAmount <= debtAmount;
        }
    }
}

