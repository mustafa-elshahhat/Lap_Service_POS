using System.Collections.Generic;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Domain.Interfaces;

namespace CarPartsShopWPF.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;

        public PaymentService(IPaymentRepository paymentRepo)
        {
            _paymentRepo = paymentRepo;
        }

        public Dictionary<string, decimal> GetPaymentBreakdown(int saleId) => _paymentRepo.GetPaymentBreakdown(saleId);
    }
}
