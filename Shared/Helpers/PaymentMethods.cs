using System.Collections.Generic;

namespace CarPartsShopWPF.Shared.Helpers
{
    public static class PaymentMethods
    {
        public const string Cash = "نقدي";
        public const string EWallet = "محافظ إلكترونية";
        public const string InstaPay = "إنستا باي";

        public static List<string> GetAll()
        {
            return new List<string>
            {
                Cash,
                EWallet,
                InstaPay
            };
        }
    }
}
