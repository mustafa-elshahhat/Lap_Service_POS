using System;
using System.Collections.Generic;
using System.Linq;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Application.DTOs;
using AlJohary.ServiceHub.Shared.Helpers;
using AlJohary.ServiceHub.Core.Accounting;
using AlJohary.ServiceHub.Core.Pricing;

namespace AlJohary.ServiceHub.Application.Services
{
    public class SaleService : ISaleService
    {
        private readonly ISaleRepository _saleRepo;
        private readonly IProductRepository _productRepo;
        private readonly ICustomerService _customerService;
        private readonly IPaymentService _paymentService;
        private readonly IReturnService _returnService;
        private readonly IDbTransactionManager _txManager;
        private readonly IAuthService _auth;

        public SaleService(
            ISaleRepository saleRepo,
            IProductRepository productRepo,
            ICustomerService customerService,
            IPaymentService paymentService,
            IReturnService returnService,
            IDbTransactionManager txManager,
            IAuthService auth)
        {
            _saleRepo        = saleRepo;
            _productRepo     = productRepo;
            _customerService = customerService;
            _paymentService  = paymentService;
            _returnService   = returnService;
            _txManager       = txManager;
            _auth            = auth;
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

            // Invoice-level discount/markup guard: validate against below-cost floor and actor ceiling.
            // effectiveDiscountPct and effectiveMarkupPct are measured against subtotal,
            // which already reflects per-item price edits. The actor ceiling is applied
            // to the post-item-discount base.
            if (discountAmount > 0 || markupAmount > 0)
            {
                decimal totalCost = 0;
                foreach (var item in validatedItems)
                    totalCost += item.UnitPurchasePrice * item.Quantity;

                if (totalAmount < totalCost)
                    throw new Exception("لا يمكن أن يكون إجمالي الفاتورة بعد الخصم أقل من إجمالي التكلفة");

                if (!_auth.CanBypassPriceLimits && subtotal > 0)
                {
                    double effectiveDiscountPct = (double)(discountAmount / subtotal) * 100;
                    double effectiveMarkupPct = (double)(markupAmount / subtotal) * 100;
                    if (discountAmount > 0 && effectiveDiscountPct > _auth.GetMaxDiscount())
                        throw new Exception($"تجاوز حد الخصم المسموح به للفاتورة: الحد الأقصى {_auth.GetMaxDiscount():0.##}% - قيمة الخصم {effectiveDiscountPct:0.##}%");
                    if (markupAmount > 0 && effectiveMarkupPct > _auth.GetMaxMarkup())
                        throw new Exception($"تجاوز حد الإضافة المسموح به للفاتورة: الحد الأقصى {_auth.GetMaxMarkup():0.##}% - قيمة الإضافة {effectiveMarkupPct:0.##}%");
                }
            }

            // Cash-only invariant: credit sales and customer receivables are NOT supported.
            // Every supported sale must be paid in full at checkout — no invoice may leave the till
            // with a remaining balance. We therefore REJECT (not silently coerce) any sale that is
            // not a full-payment cash sale. Net effect: remaining_amount is provably always 0.
            if (saleType != "cash")
                throw new Exception("البيع الآجل غير مدعوم — يجب سداد الفاتورة بالكامل");

            decimal effectivePaid = paidAmount;
            if (paymentMethodsList != null && paymentMethodsList.Count > 0)
            {
                effectivePaid = 0;
                foreach (var pm in paymentMethodsList)
                    effectivePaid += SafeConvert.ToDecimal(pm["amount"]);
            }

            if (Math.Abs(effectivePaid - totalAmount) > 0.01m)
                throw new Exception("البيع الآجل غير مدعوم — يجب سداد الفاتورة بالكامل");

            // Supported sales are fully paid: persist remaining_amount = 0.
            paidAmount = totalAmount;
            decimal remainingAmount = 0;

            // Audit per-item price edits exactly once, here in the single persistence path.
            // ValidateItems may run more than once per sale (e.g. CreateCashSale validates to
            // compute the total, then CreateSaleInternal re-validates), so logging must NOT live
            // inside ValidateItems or it would write duplicate rows.
            LogPriceEdits(validatedItems, userId);

            decimal totalProfit = DistributeFinancialsToItems(validatedItems, subtotal, totalAmount, paidAmount);

            string invoiceNumber = _saleRepo.GenerateInvoiceNumber();
            string mainPaymentMethod = paymentMethod ?? PaymentMethods.Cash;

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
                _saleRepo.AddSalePayment(saleId, mainPaymentMethod, paidAmount, "دفعة تلقائية");
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
            decimal discountAmount = 0, decimal markupAmount = 0, string notes = null, string paymentMethod = null)
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

        private int? HandleCustomer(string name, string phone)
        {
            return _customerService.GetOrCreateCustomer(name, phone);
        }

        // Pure validation: validates stock and price limits and derives each item's
        // discount/markup amounts. It does NOT write audit rows — price-edit auditing is done
        // once in CreateSaleInternal (see LogPriceEdits) because this method can run twice per sale.
        private List<SaleItem> ValidateItems(List<SaleItem> items)
        {
            if (items == null || items.Count == 0) throw new Exception("لا توجد أصناف في الفاتورة");

            bool canBypass = _auth != null && _auth.CanBypassPriceLimits;
            double maxDiscount = _auth?.GetMaxDiscount() ?? 0;
            double maxMarkup = _auth?.GetMaxMarkup() ?? 0;

            foreach (var item in items)
            {
                var product = _productRepo.GetById(item.ProductId);
                if (product == null) throw new Exception($"المنتج رقم {item.ProductId} غير موجود");
                if (product.Quantity < item.Quantity) throw new Exception($"الكمية المطلوبة من {product.Name} غير متوفرة. المتاح: {product.Quantity}");

                item.UnitPurchasePrice = product.PurchasePrice;
                item.UnitSellingPrice = product.SellingPrice;
                if (string.IsNullOrEmpty(item.ProductCode)) item.ProductCode = product.Code;
                if (string.IsNullOrEmpty(item.ProductName)) item.ProductName = product.Name;
                if (item.UnitFinalPrice <= 0) item.UnitFinalPrice = product.SellingPrice;

                // Authoritative service-layer price-limit enforcement (the UI is only a pre-check):
                // below-cost is blocked for everyone incl. admin; admin bypasses %/markup ceilings only.
                var check = PriceLimitValidator.Validate(
                    originalPrice: product.SellingPrice,
                    cost: product.PurchasePrice,
                    finalPrice: item.UnitFinalPrice,
                    canBypassLimits: canBypass,
                    maxDiscountPercent: maxDiscount,
                    maxMarkupPercent: maxMarkup,
                    productName: product.Name);
                if (!check.IsValid) throw new Exception(check.Message);

                // Persist traceable discount/markup derived from the final price (schema columns exist).
                decimal original = product.SellingPrice;
                if (item.UnitFinalPrice < original)
                {
                    item.DiscountAmount = (original - item.UnitFinalPrice) * item.Quantity;
                    item.MarkupAmount = 0;
                }
                else if (item.UnitFinalPrice > original)
                {
                    item.MarkupAmount = (item.UnitFinalPrice - original) * item.Quantity;
                    item.DiscountAmount = 0;
                }
                else
                {
                    item.DiscountAmount = 0;
                    item.MarkupAmount = 0;
                }
            }
            return items;
        }

        // Writes exactly one price-edit audit row per item whose final price differs from the
        // catalog price. Called once per sale from CreateSaleInternal (inside the sale transaction).
        private void LogPriceEdits(List<SaleItem> items, int userId)
        {
            foreach (var item in items)
            {
                if (item.DiscountAmount > 0)
                    // record_id = 0 is intentional because the sale item has not been persisted yet.
                    _saleRepo.LogActivity(userId, "تعديل السعر", "sale_items", 0, $"خصم على {item.ProductName} بقيمة {item.DiscountAmount:0.##}");
                else if (item.MarkupAmount > 0)
                    // record_id = 0 is intentional because the sale item has not been persisted yet.
                    _saleRepo.LogActivity(userId, "تعديل السعر", "sale_items", 0, $"إضافة على {item.ProductName} بقيمة {item.MarkupAmount:0.##}");
            }
        }

        public Sale GetSaleById(int id) => _saleRepo.GetById(id);
        public List<SaleItem> GetSaleItems(int saleId) => _saleRepo.GetItems(saleId);
        public List<Sale> GetSales(string query = null) => string.IsNullOrEmpty(query) ? _saleRepo.GetAll() : _saleRepo.Search(query);
        public List<Sale> GetSalesByCustomer(int customerId) => _saleRepo.GetByCustomerId(customerId);
        public List<Sale> GetSalesReport(string startDate, string endDate) => _saleRepo.GetSalesReport(startDate, endDate);
        public List<Dictionary<string, object>> GetSalePayments(int saleId) => _saleRepo.GetSalePayments(saleId);
        public Sale GetByInvoiceNumber(string invoiceNumber) => _saleRepo.GetByInvoiceNumber(invoiceNumber);
        public Dictionary<int, int> GetReturnedQuantities(int saleId) => _returnService.GetReturnedQuantities(saleId);
        public Dictionary<string, decimal> GetSalePaymentsBreakdown(int saleId) => _paymentService.GetPaymentBreakdown(saleId);
        public Dictionary<string, object> CreateReturn(int saleId, List<ReturnItem> items, int userId, string reason = null, string refundMethod = "نقدي")
            => _returnService.CreateReturn(saleId, items, userId, reason, refundMethod);

        public void SaveCustomer(Customer customer)
        {
            if (customer.Id > 0) _customerService.UpdateCustomer(customer);
            else _customerService.CreateCustomer(customer);
        }
    }
}
