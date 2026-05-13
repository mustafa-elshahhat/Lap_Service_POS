using System;
using System.Collections.Generic;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Domain.Interfaces;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Core.Accounting;
using CarPartsShopWPF.Core.Payments;

namespace CarPartsShopWPF.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ISaleRepository _saleRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IDbTransactionManager _txManager;

        public PaymentService(
            ISaleRepository saleRepo,
            ICustomerRepository customerRepo,
            IPaymentRepository paymentRepo,
            IDbTransactionManager txManager)
        {
            _saleRepo = saleRepo;
            _customerRepo = customerRepo;
            _paymentRepo = paymentRepo;
            _txManager = txManager;
        }

        public Dictionary<string, object> AddPayment(int saleId, decimal amount, int receivedBy,
            string notes = null, string paymentMethod = "نقدي")
        {
            _txManager.BeginTransaction();
            try
            {
                var sale = _saleRepo.GetById(saleId);
                if (sale == null) throw new Exception("عملية البيع غير موجودة");

                if (amount <= 0) throw new Exception("قيمة الدفعة يجب أن تكون أكبر من الصفر");
                if (amount > sale.RemainingAmount) throw new Exception("قيمة الدفعة أكبر من المبلغ المتبقي");

                decimal newPaid = sale.PaidAmount + amount;
                decimal newRemaining = FinancialCalculator.CalculateRemaining(sale.RemainingAmount, amount);

                _saleRepo.updatePaymentStatus(saleId, newPaid, newRemaining);
                _saleRepo.AddSalePayment(saleId, paymentMethod, amount, notes ?? "دفعة جزئية/آجلة");

                var items = _saleRepo.GetItems(saleId);
                if (items.Count > 0)
                {
                    decimal[] itemRemainingAmounts = new decimal[items.Count];
                    for (int i = 0; i < items.Count; i++) itemRemainingAmounts[i] = items[i].RemainingAmount;

                    decimal[] distributedPayments = FinancialCalculator.DistributeProportionally(amount, itemRemainingAmounts);

                    for (int i = 0; i < items.Count; i++)
                    {
                        if (distributedPayments[i] > 0)
                        {
                            decimal itemNewPaid = items[i].PaidAmount + distributedPayments[i];
                            decimal itemNewRemaining = items[i].RemainingAmount - distributedPayments[i];
                            _saleRepo.UpdateSaleItemFinancials(items[i].Id, itemNewPaid, itemNewRemaining);
                        }
                    }
                }

                if (sale.CustomerId.HasValue)
                {
                    var customer = _customerRepo.GetById(sale.CustomerId.Value);
                    decimal balanceBefore = customer?.TotalCredit ?? 0;

                    decimal creditDeduction = Math.Min(amount, balanceBefore);
                    decimal balanceAfter = Math.Max(0, balanceBefore - creditDeduction);

                    if (creditDeduction > 0)
                    {
                        _customerRepo.UpdateCredit(sale.CustomerId.Value, -creditDeduction);
                    }

                    _paymentRepo.AddPaymentHistory(new Payment
                    {
                        CustomerId = sale.CustomerId.Value,
                        SaleId = saleId,
                        PaymentType = "credit",
                        Amount = amount,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = balanceAfter,
                        ReceivedBy = receivedBy,
                        Notes = notes
                    });
                }


                _txManager.CommitTransaction();

                return new Dictionary<string, object>
                {
                    { "paid_amount", newPaid },
                    { "remaining_amount", newRemaining }
                };
            }
            catch (Exception)
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        public bool ValidatePaymentAmount(int saleId, decimal paymentAmount)
        {
             var sale = _saleRepo.GetById(saleId);
             if (sale == null) return false;
             return DebtCalculator.ValidatePaymentAmount(sale.RemainingAmount, paymentAmount);
        }

        public Dictionary<string, decimal> GetPaymentBreakdown(int saleId) => _paymentRepo.GetPaymentBreakdown(saleId);

        public List<Dictionary<string, object>> GetCustomerPaymentHistory(int customerId)
        {
            var payments = _paymentRepo.GetPaymentHistory(customerId);
            var list = new List<Dictionary<string, object>>();
            foreach (var p in payments)
            {
                list.Add(new Dictionary<string, object> {
                    { "id", p.Id },
                    { "payment_date", p.PaymentDate },
                    { "amount", p.Amount },
                    { "payment_type", p.PaymentType },
                    { "notes", p.Notes },
                    { "balance_before", p.BalanceBefore },
                    { "balance_after", p.BalanceAfter }
                });
            }
            return list;
        }

        public void AddPaymentHistory(int customerId, int saleId, string type, decimal amount,
            decimal before, decimal after, int receivedBy, string notes)
        {
             _paymentRepo.AddPaymentHistory(new Payment
             {
                 CustomerId = customerId,
                 SaleId = saleId,
                 PaymentType = type,
                 Amount = amount,
                 BalanceBefore = before,
                 BalanceAfter = after,
                 ReceivedBy = receivedBy,
                 Notes = notes
             });
        }
    }
}
