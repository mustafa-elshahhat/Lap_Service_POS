using System;
using System.Collections.Generic;
using System.Linq;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Domain.Interfaces;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Application.DTOs;
using CarPartsShopWPF.Shared.Helpers;
using CarPartsShopWPF.Core.Accounting;

namespace CarPartsShopWPF.Application.Services
{
    public class SaleService : ISaleService
    {
        private readonly ISaleRepository _saleRepo;
        private readonly IProductRepository _productRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IPaymentService _paymentService;
        private readonly IReturnService _returnService;
        private readonly IDbTransactionManager _txManager;
        private readonly IAuthService _auth;

        public SaleService(
            ISaleRepository saleRepo,
            IProductRepository productRepo,
            ICustomerRepository customerRepo,
            IPaymentService paymentService,
            IReturnService returnService,
            IDbTransactionManager txManager,
            IAuthService auth)
        {
            _saleRepo = saleRepo;
            _productRepo = productRepo;
            _customerRepo = customerRepo;
            _paymentService = paymentService;
            _returnService = returnService;
            _txManager = txManager;
            _auth = auth;
        }

        public SaleOperationResult CreateSale(string saleType, int userId, List<SaleItem> items,
            int? customerId = null, decimal discountAmount = 0, decimal markupAmount = 0,
            decimal paidAmount = 0, string notes = null,
            string paymentMethod = null,
            List<Dictionary<string, object>> paymentMethodsList = null)
        {
            _txManager.BeginTransaction();
            try
            {
                var result = CreateSaleInternal(saleType, userId, items, customerId, discountAmount,
                    markupAmount, paidAmount, notes, paymentMethod, paymentMethodsList);
                _txManager.CommitTransaction();
                return result;
            }
            catch (Exception ex)
            {
                _txManager.RollbackTransaction();
                return new SaleOperationResult { Success = false, Message = ex.Message };
            }
        }

        private SaleOperationResult CreateSaleInternal(string saleType, int userId, List<SaleItem> items,
            int? customerId = null, decimal discountAmount = 0, decimal markupAmount = 0,
            decimal paidAmount = 0, string notes = null,
            string paymentMethod = null,
            List<Dictionary<string, object>> paymentMethodsList = null)
        {
            var validatedItems = ValidateItems(items);

            decimal subtotal = 0;
            foreach (var item in validatedItems)
                subtotal += item.Quantity * item.UnitFinalPrice;

            decimal totalAmount = FinancialCalculator.CalculateTotalWithDiscountAndMarkup(subtotal, discountAmount, markupAmount);
            decimal remainingAmount = FinancialCalculator.CalculateRemaining(totalAmount, paidAmount);

            if (saleType == "cash" && remainingAmount > 0 && !customerId.HasValue)
            {
                paidAmount = totalAmount;
                remainingAmount = 0;
            }

            decimal totalProfit = DistributeFinancialsToItems(validatedItems, subtotal, totalAmount, paidAmount);

            string invoiceNumber = _saleRepo.GenerateInvoiceNumber();
            string mainPaymentMethod = paymentMethod ?? (remainingAmount == 0 ? "كاش" : "آجل");

            var sale = new Sale
            {
                InvoiceNumber = invoiceNumber,
                SaleType = saleType,
                CustomerId = customerId,
                UserId = userId,
                Subtotal = subtotal,
                DiscountAmount = discountAmount,
                MarkupAmount = markupAmount,
                TotalAmount = totalAmount,
                PaidAmount = paidAmount,
                RemainingAmount = remainingAmount,
                Profit = totalProfit,
                Notes = notes,
                PaymentMethod = mainPaymentMethod
            };

            long saleId = _saleRepo.Create(sale);

            foreach (var item in validatedItems)
            {
                _saleRepo.AddSaleItem(saleId, item);
                _productRepo.UpdateQuantity(item.ProductId, -item.Quantity);
            }

            if (paymentMethodsList != null && paymentMethodsList.Count > 0)
            {
                foreach (var pm in paymentMethodsList)
                {
                    _saleRepo.AddSalePayment(saleId, SafeConvert.ToString(pm["method"]), SafeConvert.ToDecimal(pm["amount"]), "دفعة أولية");
                }
            }
            else if (paidAmount > 0)
            {
                _saleRepo.AddSalePayment(saleId, mainPaymentMethod == "آجل" ? "كاش" : mainPaymentMethod, paidAmount, "دفعة تلقائية");
            }

            if (customerId.HasValue && remainingAmount > 0)
            {
                _customerRepo.UpdateCredit(customerId.Value, remainingAmount);
            }

            _saleRepo.LogActivity(userId, "عملية بيع", "sales", (int)saleId, $"فاتورة {invoiceNumber} - إجمالي: {totalAmount} - مدفوع: {paidAmount}");

            return new SaleOperationResult
            {
                SaleId = saleId,
                InvoiceNumber = invoiceNumber,
                TotalAmount = totalAmount,
                PaidAmount = paidAmount,
                RemainingAmount = remainingAmount,
                Profit = totalProfit,
                Success = true
            };
        }

        private static decimal DistributeFinancialsToItems(List<SaleItem> items, decimal subtotal, decimal totalAmount, decimal paidAmount)
        {
            if (items == null || items.Count == 0) return 0;

            decimal totalProfit = 0;
            decimal remainingAdjusted = totalAmount;
            decimal remainingPaid = paidAmount;

            for (int i = 0; i < items.Count; i++)
            {
                bool isLast = i == items.Count - 1;
                decimal rawItemTotal = items[i].Quantity * items[i].UnitFinalPrice;

                decimal adjustedItemTotal = isLast
                    ? remainingAdjusted
                    : Math.Round(subtotal > 0 ? totalAmount * rawItemTotal / subtotal : rawItemTotal, 2);

                items[i].TotalPrice = Math.Max(0, adjustedItemTotal);

                decimal itemPaid = isLast
                    ? remainingPaid
                    : Math.Min(Math.Round(totalAmount > 0 ? paidAmount * adjustedItemTotal / totalAmount : 0, 2), remainingPaid);

                items[i].PaidAmount = Math.Max(0, itemPaid);
                items[i].RemainingAmount = Math.Max(0, adjustedItemTotal - itemPaid);

                items[i].Profit = items[i].TotalPrice - (items[i].UnitPurchasePrice * items[i].Quantity);
                totalProfit += items[i].Profit;

                if (!isLast)
                {
                    remainingAdjusted -= adjustedItemTotal;
                    remainingPaid -= itemPaid;
                }
            }

            return totalProfit;
        }

        public SaleOperationResult CreateCashSale(List<SaleItem> items, string customerName = null, string customerPhone = null,
            decimal discountAmount = 0, decimal markupAmount = 0, string notes = null, string paymentMethod = "كاش")
        {
            _txManager.BeginTransaction();
            try
            {
                var validatedItems = ValidateItems(items);
                decimal subtotal = validatedItems.Sum(i => i.Quantity * i.UnitFinalPrice);
                decimal total = FinancialCalculator.CalculateTotalWithDiscountAndMarkup(subtotal, discountAmount, markupAmount);

                int? customerId = HandleCustomer(customerName, customerPhone);

                var result = CreateSaleInternal("cash", _auth.GetUserId(), validatedItems, customerId,
                    discountAmount, markupAmount, total, notes, paymentMethod);

                _txManager.CommitTransaction();
                return result;
            }
            catch (Exception ex)
            {
                _txManager.RollbackTransaction();
                return new SaleOperationResult { Success = false, Message = ex.Message };
            }
        }

        public SaleOperationResult CreateCreditSale(List<SaleItem> items, string customerName, string customerPhone = null,
            decimal paidAmount = 0, decimal discountAmount = 0, decimal markupAmount = 0, string notes = null, string paymentMethod = "كاش")
        {
            if (string.IsNullOrWhiteSpace(customerName)) throw new ArgumentException("اسم العميل مطلوب لعمليات البيع الآجل");

            _txManager.BeginTransaction();
            try
            {
                int? customerId = HandleCustomer(customerName, customerPhone);
                var result = CreateSaleInternal("credit", _auth.GetUserId(), ValidateItems(items), customerId,
                    discountAmount, markupAmount, paidAmount, notes, paymentMethod);

                _txManager.CommitTransaction();
                return result;
            }
            catch (Exception ex)
            {
                _txManager.RollbackTransaction();
                return new SaleOperationResult { Success = false, Message = ex.Message };
            }
        }

        private int? HandleCustomer(string name, string phone)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var customer = !string.IsNullOrEmpty(phone) ? _customerRepo.GetByPhone(phone) : null;
            if (customer == null)
            {
                return (int)_customerRepo.Create(new Customer { Name = name.Trim(), Phone = phone?.Trim() });
            }

            if (customer.Name != name.Trim())
            {
                customer.Name = name.Trim();
                _customerRepo.Update(customer);
            }
            return customer.Id;
        }

        private List<SaleItem> ValidateItems(List<SaleItem> items)
        {
            if (items == null || items.Count == 0) throw new Exception("لا توجد أصناف في الفاتورة");
            foreach (var item in items)
            {
                var product = _productRepo.GetById(item.ProductId);
                if (product == null) throw new Exception($"المنتج رقم {item.ProductId} غير موجود");
                if (product.Quantity < item.Quantity) throw new Exception($"الكمية المطلوبة من {product.Name} غير متوفرة. المتاح: {product.Quantity}");

                item.UnitPurchasePrice = product.PurchasePrice;
                if (item.UnitFinalPrice <= 0) item.UnitFinalPrice = product.SellingPrice;
            }
            return items;
        }

        public Sale GetSaleById(int id) => _saleRepo.GetById(id);
        public List<SaleItem> GetSaleItems(int saleId) => _saleRepo.GetItems(saleId);
        public List<Sale> GetSales(string query = null) => string.IsNullOrEmpty(query) ? _saleRepo.GetAll() : _saleRepo.Search(query);
        public List<Sale> GetSalesByCustomer(int customerId) => _saleRepo.GetByCustomerId(customerId);
        public List<Sale> GetUnpaidInvoices(int customerId) => _saleRepo.GetByCustomerId(customerId).Where(s => s.RemainingAmount > 0).ToList();
        public List<Sale> GetSalesReport(string startDate, string endDate) => _saleRepo.GetSalesReport(startDate, endDate);
        public List<Dictionary<string, object>> GetSalePayments(int saleId) => _saleRepo.GetSalePayments(saleId);
        public void PayInvoiceAmount(int saleId, decimal amount, string method, string notes) => _paymentService.AddPayment(saleId, amount, _auth.GetUserId(), notes, method);

        public Sale GetByInvoiceNumber(string invoiceNumber) => _saleRepo.GetByInvoiceNumber(invoiceNumber);
        public Dictionary<int, int> GetReturnedQuantities(int saleId) => _returnService.GetReturnedQuantities(saleId);
        public Dictionary<string, decimal> GetSalePaymentsBreakdown(int saleId) => _paymentService.GetPaymentBreakdown(saleId);
        public Dictionary<string, object> CreateReturn(int saleId, List<ReturnItem> items, int userId, string reason = null, string refundMethod = "نقدي")
            => _returnService.CreateReturn(saleId, items, userId, reason, refundMethod);

        public void SaveCustomer(Customer customer)
        {
            if (customer.Id > 0) _customerRepo.Update(customer);
            else _customerRepo.Create(customer);
        }
    }
}
