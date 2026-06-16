using System;
using System.Collections.Generic;
using System.Linq;
using AlJohary.ServiceHub.Application.DTOs;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;
using AlJohary.ServiceHub.Infrastructure.Data;

namespace AlJohary.ServiceHub.Application.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepo;
        private readonly IAuthService _auth;
        private readonly IDbTransactionManager _txManager;

        public SupplierService(ISupplierRepository supplierRepo, IAuthService auth, IDbTransactionManager txManager = null)
        {
            _supplierRepo = supplierRepo;
            _auth = auth;
            _txManager = txManager;
        }

        public List<Supplier> GetAllSuppliers()
        {
            return _supplierRepo.GetAllSuppliers();
        }

        public List<Supplier> SearchSuppliers(string query)
        {
            return _supplierRepo.SearchSuppliers(query);
        }
        
        public Supplier GetById(int id)
        {
            return _supplierRepo.GetById(id);
        }

        public void CreateSupplier(string name, string phone, string address)
        {
            var supplier = new Supplier
            {
                Name = name,
                Phone = phone,
                Address = address
            };
            _supplierRepo.CreateSupplier(supplier);
        }

        public void UpdateSupplier(int id, string name, string phone, string address)
        {
            var supplier = new Supplier
            {
                Id = id,
                Name = name,
                Phone = phone,
                Address = address
            };
            _supplierRepo.UpdateSupplier(supplier);
        }

        public void DeleteSupplier(int id)
        {
            _supplierRepo.DeleteSupplier(id);
        }

        public void AddSupplierPayment(int supplierId, decimal amount, string paymentMethod = null)
        {
            if (amount <= 0)
                throw new ArgumentException("قيمة السداد يجب أن تكون أكبر من الصفر");

            var supplier = _supplierRepo.GetById(supplierId);
            if (supplier == null)
                throw new Exception("المورد غير موجود");
            if (amount > supplier.TotalDebt)
                throw new InvalidOperationException(
                    $"قيمة السداد ({Formatting.FormatCurrency(amount)}) تتجاوز المديونية الحالية ({Formatting.FormatCurrency(supplier.TotalDebt)})");

            int userId = _auth.GetUserId();

            // T-1: the repository settlement is read-debt -> INSERT payment -> UPDATE debt (3 statements);
            // own the transaction here so a mid-flow failure can never leave an orphaned payment row.
            if (_txManager == null)
            {
                _supplierRepo.AddSupplierPayment(supplierId, amount, userId, paymentMethod);
                return;
            }

            _txManager.BeginTransaction();
            try
            {
                _supplierRepo.AddSupplierPayment(supplierId, amount, userId, paymentMethod);
                _txManager.CommitTransaction();
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        // LEGACY / DE-SCOPED: superseded by AddSupplierPurchaseWithItems (invoice-only line items).
        // Retained for compatibility but has no caller; wrapped defensively in case it is ever used.
        [Obsolete("Use AddSupplierPurchaseWithItems instead. This standalone purchase path is legacy.")]
        public void AddSupplierPurchase(int supplierId, decimal amount, string paymentMethod = null)
        {
            if (amount <= 0)
                throw new ArgumentException("قيمة المشتريات يجب أن تكون أكبر من الصفر");

            int userId = _auth.GetUserId();

            if (_txManager == null)
            {
                _supplierRepo.AddSupplierPurchase(supplierId, amount, userId, paymentMethod);
                return;
            }

            _txManager.BeginTransaction();
            try
            {
                _supplierRepo.AddSupplierPurchase(supplierId, amount, userId, paymentMethod);
                _txManager.CommitTransaction();
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        public SupplierPurchaseResult AddSupplierPurchaseWithItems(int supplierId, decimal totalAmount, decimal paidAmount, string paymentMethod, List<SupplierPurchaseLineInput> lines)
        {
            if (_txManager == null)
                throw new InvalidOperationException("مدير المعاملات غير مهيأ");

            lines = lines ?? new List<SupplierPurchaseLineInput>();
            _txManager.BeginTransaction();
            try
            {
                var supplier = _supplierRepo.GetById(supplierId);
                if (supplier == null)
                    throw new Exception("المورد غير موجود");

                var validLines = new List<SupplierPurchaseLineInput>();
                foreach (var line in lines)
                {
                    ValidateLine(line);
                    validLines.Add(line);
                }

                if (validLines.Count > 0)
                    totalAmount = validLines.Sum(l => l.Quantity * l.UnitPurchasePrice);

                if (totalAmount <= 0)
                    throw new ArgumentException("قيمة المشتريات يجب أن تكون أكبر من الصفر");
                if (paidAmount < 0 || paidAmount > totalAmount)
                    throw new ArgumentException("المبلغ المدفوع يجب أن يكون بين صفر وقيمة المشتريات");

                int userId = _auth.GetUserId();
                decimal purchaseBefore = supplier.TotalDebt;
                decimal purchaseAfter = purchaseBefore + totalAmount;
                long transactionId = _supplierRepo.AddSupplierPurchaseRow(
                    supplierId,
                    totalAmount,
                    paidAmount,
                    validLines.Count,
                    userId,
                    paymentMethod,
                    purchaseBefore,
                    purchaseAfter);

                foreach (var line in validLines)
                {
                    decimal lineTotal = line.Quantity * line.UnitPurchasePrice;
                    _supplierRepo.AddSupplierPurchaseItem(new SupplierPurchaseItem
                    {
                        SupplierTransactionId = (int)transactionId,
                        SupplierId = supplierId,
                        ProductName = line.ProductName?.Trim(),
                        Quantity = line.Quantity,
                        UnitPurchasePrice = line.UnitPurchasePrice,
                        LineTotal = lineTotal
                    });
                }

                if (paidAmount > 0)
                {
                    _supplierRepo.AddSupplierPayment(supplierId, paidAmount, userId, paymentMethod);
                }

                DatabaseManager.Instance.LogActivity(userId, "supplier_purchase", "supplier_transactions", (int)transactionId,
                    $"SupplierId={supplierId}; Total={totalAmount}; Paid={paidAmount}; Items={validLines.Count}");

                _txManager.CommitTransaction();
                return new SupplierPurchaseResult
                {
                    Success = true,
                    Message = "تم تسجيل المشتريات بنجاح",
                    TransactionId = transactionId,
                    ItemCount = validLines.Count,
                    TotalAmount = totalAmount,
                    PaidAmount = paidAmount,
                    RemainingAdded = totalAmount - paidAmount
                };
            }
            catch (Exception ex)
            {
                _txManager.RollbackTransaction();
                return new SupplierPurchaseResult { Success = false, Message = ex.Message };
            }
        }

        public List<SupplierPurchaseItem> GetPurchaseItems(int supplierTransactionId)
        {
            return _supplierRepo.GetPurchaseItems(supplierTransactionId);
        }

        private static void ValidateLine(SupplierPurchaseLineInput line)
        {
            if (line == null)
                throw new ArgumentException("سطر مشتريات غير صالح");
            if (string.IsNullOrWhiteSpace(line.ProductName))
                throw new ArgumentException("اسم المنتج مطلوب في كل سطر");
            if (line.Quantity <= 0)
                throw new ArgumentException("الكمية يجب أن تكون أكبر من صفر");
            if (line.UnitPurchasePrice < 0)
                throw new ArgumentException("سعر الشراء لا يمكن أن يكون سالباً");
        }

        public List<Dictionary<string, object>> GetSupplierTransactions(int supplierId)
        {
            return _supplierRepo.GetTransactions(supplierId);
        }
    }
}
