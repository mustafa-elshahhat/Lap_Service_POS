using System.Collections.Generic;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Domain.Interfaces;

namespace AlJohary.ServiceHub.Application.Services
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
