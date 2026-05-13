using System;
using System.Collections.Generic;
using CarPartsShopWPF.Domain.Interfaces;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Core.Returns;

namespace CarPartsShopWPF.Application.Services
{
    public class ReturnService : IReturnService
    {
        private readonly ISaleRepository _saleRepo;
        private readonly IProductRepository _productRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IPaymentService _paymentService;
        private readonly IDbTransactionManager _txManager;

        public ReturnService(
            ISaleRepository saleRepo,
            IProductRepository productRepo,
            ICustomerRepository customerRepo,
            IPaymentService paymentService,
            IDbTransactionManager txManager)
        {
            _saleRepo = saleRepo;
            _productRepo = productRepo;
            _customerRepo = customerRepo;
            _paymentService = paymentService;
            _txManager = txManager;
        }

        public Dictionary<string, object> CreateReturn(int saleId, List<ReturnItem> items, int userId,
            string reason = null, string refundMethod = "نقدي")
        {
            if (items == null || items.Count == 0) throw new Exception("لا توجد عناصر للإرجاع");

            var groupedItems = new List<ReturnItem>();
            foreach (var it in items)
            {
                var existing = groupedItems.Find(x => x.SaleItemId == it.SaleItemId);
                if (existing != null)
                {
                    existing.Quantity += it.Quantity;
                }
                else
                {
                    groupedItems.Add(new ReturnItem { SaleItemId = it.SaleItemId, Quantity = it.Quantity });
                }
            }
            items = groupedItems;

            _txManager.BeginTransaction();
            try
            {
                var sale = _saleRepo.GetById(saleId);
                if (sale == null) throw new Exception("عملية البيع غير موجودة");

                var originalItems = _saleRepo.GetItems(saleId);
                var previousReturns = _saleRepo.GetReturnedQuantities(saleId);

                foreach (var it in items)
                {
                    if (it.Quantity <= 0) continue;

                    var saleItem = originalItems.Find(x => x.Id == it.SaleItemId);
                    if (saleItem == null) throw new Exception("عنصر الفاتورة غير موجود");

                    int alreadyReturned = previousReturns.ContainsKey(it.SaleItemId) ? previousReturns[it.SaleItemId] : 0;
                    if (!RefundValidator.ValidateReturnQuantity(it.Quantity, saleItem.Quantity, alreadyReturned))
                        throw new Exception($"الكمية غير متاحة للإرجاع للمنتج {saleItem.ProductName}. المرتجع سابقاً: {alreadyReturned}, المتاح: {saleItem.Quantity - alreadyReturned}");
                }

                decimal[] itemReturnedValues = new decimal[items.Count];
                decimal[] itemReturnedPaidParts = new decimal[items.Count];
                decimal[] itemReturnedRemainingParts = new decimal[items.Count];

                decimal totalItemsValue = 0;
                decimal totalCashRefund = 0;
                decimal totalDebtDeduction = 0;
                decimal totalProfitReturn = 0;

                for (int j = 0; j < items.Count; j++)
                {
                    var it = items[j];
                    var saleItem = originalItems.Find(x => x.Id == it.SaleItemId);

                    int alreadyReturned = previousReturns.ContainsKey(it.SaleItemId) ? previousReturns[it.SaleItemId] : 0;
                    int remainingQtyBeforeReturn = saleItem.Quantity - alreadyReturned;

                    decimal itemReturnValue = saleItem.Quantity > 0
                        ? saleItem.TotalPrice * it.Quantity / saleItem.Quantity
                        : 0;

                    decimal itemReturnedPaidPart = remainingQtyBeforeReturn > 0
                        ? saleItem.PaidAmount * it.Quantity / remainingQtyBeforeReturn
                        : 0;

                    decimal itemReturnedRemainingPart = remainingQtyBeforeReturn > 0
                        ? saleItem.RemainingAmount * it.Quantity / remainingQtyBeforeReturn
                        : 0;

                    decimal itemReturnedProfit = saleItem.Quantity > 0
                        ? saleItem.Profit * it.Quantity / saleItem.Quantity
                        : 0;

                    itemReturnedValues[j] = itemReturnValue;
                    itemReturnedPaidParts[j] = itemReturnedPaidPart;
                    itemReturnedRemainingParts[j] = itemReturnedRemainingPart;

                    totalItemsValue += itemReturnValue;
                    totalCashRefund += itemReturnedPaidPart;
                    totalDebtDeduction += itemReturnedRemainingPart;
                    totalProfitReturn += itemReturnedProfit;
                }

                decimal cashRefund = Math.Min(sale.PaidAmount, totalCashRefund);
                decimal debtDeduction = Math.Min(sale.RemainingAmount, totalDebtDeduction);

                string returnNumber = _saleRepo.GenerateReturnNumber();
                long returnId = _saleRepo.CreateReturn(returnNumber, (int)sale.Id, sale.CustomerId, userId, totalItemsValue, cashRefund, debtDeduction, reason, refundMethod);

                decimal newPaid = Math.Max(0, sale.PaidAmount - cashRefund);
                decimal newRemaining = Math.Max(0, sale.RemainingAmount - debtDeduction);

                _saleRepo.updatePaymentStatus(saleId, newPaid, newRemaining);

                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    var saleItem = originalItems.Find(x => x.Id == it.SaleItemId);

                    _saleRepo.AddReturnItem(returnId, it.SaleItemId, saleItem.ProductId, saleItem.ProductCode, saleItem.ProductName, it.Quantity, saleItem.UnitFinalPrice, itemReturnedValues[i]);
                    _productRepo.UpdateQuantity(saleItem.ProductId, it.Quantity);

                    decimal newItemPaid = Math.Max(0, saleItem.PaidAmount - itemReturnedPaidParts[i]);
                    decimal newItemRemaining = Math.Max(0, saleItem.RemainingAmount - itemReturnedRemainingParts[i]);

                    _saleRepo.UpdateSaleItemFinancials(saleItem.Id, newItemPaid, newItemRemaining);
                }

                if (sale.CustomerId.HasValue)
                {
                    var customer = _customerRepo.GetById(sale.CustomerId.Value);
                    decimal balanceBefore = customer?.TotalCredit ?? 0;

                    if (debtDeduction > 0)
                        _customerRepo.UpdateCredit(sale.CustomerId.Value, -debtDeduction);

                    if (debtDeduction > 0)
                    {
                        decimal balanceAfter = Math.Max(0, balanceBefore - debtDeduction);
                        _paymentService.AddPaymentHistory(sale.CustomerId.Value, saleId, "return",
                            debtDeduction, balanceBefore, balanceAfter, userId,
                            $"مرتجع فاتورة {sale.InvoiceNumber}");
                    }
                }

                _saleRepo.LogActivity(userId, "مرتجع", "returns", (int)returnId, $"مرتجع رقم {returnNumber}");
                _txManager.CommitTransaction();

                return new Dictionary<string, object> { { "id", returnId }, { "return_number", returnNumber }, { "total_amount", totalItemsValue }, { "cash_refund", cashRefund } };
            }
            catch (Exception)
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        public Dictionary<string, object> GetReturnById(int returnId) => _saleRepo.GetReturnById(returnId);

        public List<Dictionary<string, object>> GetReturnItems(int returnId) => _saleRepo.GetReturnItems(returnId);
        public Dictionary<int, int> GetReturnedQuantities(int saleId) => _saleRepo.GetReturnedQuantities(saleId);
        public List<Return> GetReturns(string query = null) => _saleRepo.GetReturns(query);
        public List<Return> GetReturnsReport(string startDate, string endDate) => _saleRepo.GetReturnsReport(startDate, endDate);
    }
}
