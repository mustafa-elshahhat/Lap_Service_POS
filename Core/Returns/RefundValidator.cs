namespace CarPartsShopWPF.Core.Returns
{

    public static class RefundValidator
    {

        public static bool CanRefundExceedPaid(decimal refundAmount, decimal paidAmount)
        {

            return refundAmount <= paidAmount;
        }

        public static bool ValidateReturnQuantity(int returnQuantity, int originalQuantity, int alreadyReturned)
        {
            int available = originalQuantity - alreadyReturned;
            return returnQuantity > 0 && returnQuantity <= available;
        }
    }
}

