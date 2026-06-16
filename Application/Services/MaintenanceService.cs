using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Application.DTOs;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Application.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly IRepairRepository    _repo;
        private readonly IProductRepository   _productRepo;
        private readonly ICustomerService     _customerService;
        private readonly IDbTransactionManager _txManager;
        private readonly IActivityLog         _activityLog;

        public MaintenanceService(
            IRepairRepository    repo,
            IProductRepository   productRepo,
            ICustomerService     customerService,
            IDbTransactionManager txManager,
            IActivityLog         activityLog = null)
        {
            _repo            = repo;
            _productRepo     = productRepo;
            _customerService = customerService;
            _txManager       = txManager;
            _activityLog     = activityLog;
        }

        private int? ResolveCustomer(string name, string phone)
        {
            return _customerService.GetOrCreateCustomer(name, phone);
        }

        public RepairOrder CreateOrder(RepairOrderInput input, int userId)
        {
            if (string.IsNullOrWhiteSpace(input.CustomerName))
                throw new InvalidOperationException("اسم العميل مطلوب.");

            _txManager.BeginTransaction();
            try
            {
                int? customerId = input.CustomerId ?? ResolveCustomer(input.CustomerName, input.CustomerPhone);

                var order = new RepairOrder
                {
                    OrderNumber    = _repo.GenerateRepairOrderNumber(),
                    CustomerId     = customerId,
                    CustomerName   = input.CustomerName?.Trim(),
                    CustomerPhone  = input.CustomerPhone?.Trim(),
                    TechnicianName = input.TechnicianName?.Trim(),
                    UserId         = userId,
                    OrderStatus    = RepairStatus.Received,
                    ExpectedDelivery = input.ExpectedDelivery,
                    IntakeDate     = DateTime.Now,
                    Notes          = input.Notes?.Trim()
                };

                long orderId = _repo.CreateOrder(order);
                order.Id = orderId;

                if (input.InitialPayment > 0)
                {
                    var payment = new RepairPayment
                    {
                        OrderId       = orderId,
                        Amount        = input.InitialPayment,
                        PaymentMethod = string.IsNullOrWhiteSpace(input.InitialPaymentMethod)
                                            ? PaymentMethods.Cash
                                            : input.InitialPaymentMethod,
                        PaymentDate   = DateTime.Now,
                        Notes         = "دفعة مقدمة عند الاستلام",
                        UserId        = userId
                    };
                    _repo.AddPayment(payment);
                }

                _txManager.CommitTransaction();
                Logger.LogInfo($"MaintenanceService: Order {order.OrderNumber} created (ID {orderId}).");
                return _repo.GetOrder(orderId);
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        public void UpdateOrderInfo(long orderId, RepairOrderInput input)
        {
            var order = _repo.GetOrder(orderId)
                ?? throw new InvalidOperationException("طلب الصيانة غير موجود.");

            order.CustomerId      = input.CustomerId ?? ResolveCustomer(input.CustomerName, input.CustomerPhone);
            order.CustomerName    = input.CustomerName?.Trim();
            order.CustomerPhone   = input.CustomerPhone?.Trim();
            order.TechnicianName  = input.TechnicianName?.Trim();
            order.ExpectedDelivery = input.ExpectedDelivery;
            order.Notes           = input.Notes?.Trim();

            _repo.UpdateOrder(order);
        }

        public long AddDevice(long orderId, RepairDeviceInput input)
        {
            if (string.IsNullOrWhiteSpace(input.ReportedIssue))
                throw new InvalidOperationException("وصف المشكلة المبلغ عنها مطلوب.");

            var device = new RepairDevice
            {
                OrderId       = orderId,
                DeviceType    = string.IsNullOrWhiteSpace(input.DeviceType) ? "laptop" : input.DeviceType,
                Brand         = input.Brand?.Trim(),
                Model         = input.Model?.Trim(),
                SerialNumber  = input.SerialNumber?.Trim(),
                Condition     = input.Condition?.Trim(),
                ReportedIssue = input.ReportedIssue.Trim(),
                Accessories   = input.Accessories?.Trim(),
                EstimatedCost = input.EstimatedCost,
                LaborCost     = input.LaborCost,
                DeviceStatus  = string.IsNullOrWhiteSpace(input.DeviceStatus) ? RepairStatus.Received : input.DeviceStatus,
                DiagnosisNotes = input.DiagnosisNotes?.Trim(),
                RepairNotes   = input.RepairNotes?.Trim()
            };

            // T-4: device insert + order-total recalculation must be atomic.
            _txManager.BeginTransaction();
            try
            {
                long deviceId = _repo.AddDevice(device);
                _repo.RecalculateOrderTotals(orderId);
                _txManager.CommitTransaction();
                return deviceId;
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        public void UpdateDevice(long deviceId, RepairDeviceInput input)
        {
            if (string.IsNullOrWhiteSpace(input.ReportedIssue))
                throw new InvalidOperationException("وصف المشكلة المبلغ عنها مطلوب.");

            var existing = _repo.GetDevice(deviceId)
                ?? throw new InvalidOperationException("الجهاز غير موجود.");

            long orderId = existing.OrderId;

            var device = new RepairDevice
            {
                Id            = deviceId,
                OrderId       = orderId,
                DeviceType    = string.IsNullOrWhiteSpace(input.DeviceType) ? "laptop" : input.DeviceType,
                Brand         = input.Brand?.Trim(),
                Model         = input.Model?.Trim(),
                SerialNumber  = input.SerialNumber?.Trim(),
                Condition     = input.Condition?.Trim(),
                ReportedIssue = input.ReportedIssue.Trim(),
                Accessories   = input.Accessories?.Trim(),
                EstimatedCost = input.EstimatedCost,
                LaborCost     = input.LaborCost,
                DeviceStatus  = string.IsNullOrWhiteSpace(input.DeviceStatus) ? RepairStatus.Received : input.DeviceStatus,
                DiagnosisNotes = input.DiagnosisNotes?.Trim(),
                RepairNotes   = input.RepairNotes?.Trim()
            };

            // T-4: device update + order-total recalculation must be atomic.
            _txManager.BeginTransaction();
            try
            {
                _repo.UpdateDevice(device);
                _repo.RecalculateOrderTotals(orderId);
                _txManager.CommitTransaction();
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        public void UpdateDeviceStatus(long deviceId, string newStatus, string notes)
        {
            var device = _repo.GetDevice(deviceId)
                ?? throw new InvalidOperationException("الجهاز غير موجود.");

            if (!RepairStatus.CanTransitionTo(device.DeviceStatus, newStatus))
                throw new InvalidOperationException(
                    $"لا يمكن الانتقال من حالة '{RepairStatus.ToArabic(device.DeviceStatus)}' إلى '{RepairStatus.ToArabic(newStatus)}'.");

            _repo.UpdateDeviceStatus(deviceId, newStatus, notes);
        }

        public void AddInventoryPart(long deviceId, long orderId, int productId, int qty, decimal unitCost)
        {
            if (qty <= 0) throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر.");

            var product = _productRepo.GetById(productId)
                ?? throw new InvalidOperationException("المنتج غير موجود.");

            if (product.Quantity < qty)
                throw new InvalidOperationException($"الكمية المتاحة في المخزون ({product.Quantity}) أقل من المطلوبة ({qty}).");

            _txManager.BeginTransaction();
            try
            {
                _productRepo.SetQuantity(productId, product.Quantity - qty);

                var part = new RepairPart
                {
                    DeviceId        = deviceId,
                    OrderId         = orderId,
                    ProductId       = productId,
                    PartName        = product.Name,
                    Quantity        = qty,
                    UnitCost        = unitCost,
                    TotalCost       = unitCost * qty,
                    PurchaseCost    = product.PurchasePrice,
                    IsFromInventory = true
                };

                _repo.AddPart(part);
                _repo.RecalculateOrderTotals(orderId);
                _txManager.CommitTransaction();

                Logger.LogInfo($"MaintenanceService: Inventory part '{product.Name}' x{qty} added to device {deviceId}. Stock: {product.Quantity} → {product.Quantity - qty}.");
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        public void AddCustomPart(long deviceId, long orderId, string name, int qty, decimal unitCost, decimal purchaseCost = 0)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("اسم القطعة مطلوب.");
            if (qty <= 0) throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر.");

            var part = new RepairPart
            {
                DeviceId        = deviceId,
                OrderId         = orderId,
                ProductId       = null,
                PartName        = name.Trim(),
                Quantity        = qty,
                UnitCost        = unitCost,
                TotalCost       = unitCost * qty,
                PurchaseCost    = purchaseCost,
                IsFromInventory = false
            };

            // T-4: custom-part insert + order-total recalculation must be atomic.
            _txManager.BeginTransaction();
            try
            {
                _repo.AddPart(part);
                _repo.RecalculateOrderTotals(orderId);
                _txManager.CommitTransaction();
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        public void RemovePart(long partId)
        {
            var part = _repo.GetPart(partId)
                ?? throw new InvalidOperationException("القطعة غير موجودة.");

            _txManager.BeginTransaction();
            try
            {
                if (part.IsFromInventory && part.ProductId.HasValue)
                {
                    var product = _productRepo.GetById(part.ProductId.Value);
                    if (product != null)
                    {
                        _productRepo.SetQuantity(part.ProductId.Value, product.Quantity + part.Quantity);
                        Logger.LogInfo($"MaintenanceService: Inventory restored for '{product.Name}' x{part.Quantity} after part removal.");
                    }
                }

                _repo.RemovePart(partId);
                _repo.RecalculateOrderTotals(part.OrderId);
                _txManager.CommitTransaction();
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        public void RegisterPayment(long orderId, decimal amount, string method, int userId, string notes = null)
        {
            if (amount <= 0) throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر.");

            var order = _repo.GetOrder(orderId)
                ?? throw new InvalidOperationException("طلب الصيانة غير موجود.");

            // R-11.5: never accept cash against a cancelled order.
            if (order.OrderStatus == RepairStatus.Cancelled)
                throw new InvalidOperationException("لا يمكن تسجيل دفعة على طلب ملغي.");

            if (amount > order.RemainingAmount + 0.01m)
            {
                Logger.LogWarning($"MaintenanceService: Overpayment attempt on order {order.OrderNumber}. Remaining={order.RemainingAmount}, Attempted={amount}.");
                throw new InvalidOperationException($"المبلغ المدخل ({Formatting.FormatCurrency(amount)}) يتجاوز المتبقي ({Formatting.FormatCurrency(order.RemainingAmount)}).");
            }

            var payment = new RepairPayment
            {
                OrderId       = orderId,
                Amount        = amount,
                PaymentMethod = string.IsNullOrWhiteSpace(method) ? PaymentMethods.Cash : method,
                PaymentDate   = DateTime.Now,
                Notes         = notes,
                UserId        = userId
            };

            // T-3: AddPayment does INSERT then RecalculateOrderTotals (multiple statements) — wrap so a
            // failure cannot leave the payment recorded while the order totals are stale.
            _txManager.BeginTransaction();
            try
            {
                _repo.AddPayment(payment);
                _txManager.CommitTransaction();
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
            Logger.LogInfo($"MaintenanceService: Payment {Formatting.FormatNumber(amount)} registered for order {order.OrderNumber}.");
        }

        public void CancelOrder(long orderId, int userId)
        {
            var order = _repo.GetOrder(orderId)
                ?? throw new InvalidOperationException("طلب الصيانة غير موجود.");

            if (RepairStatus.IsFinal(order.OrderStatus))
                throw new InvalidOperationException("لا يمكن إلغاء طلب مكتمل أو مسلَّم.");

            _txManager.BeginTransaction();
            try
            {
                var inventoryParts = new List<RepairPart>();
                foreach (var part in _repo.GetOrderParts(orderId))
                {
                    if (part.IsFromInventory && part.ProductId.HasValue)
                        inventoryParts.Add(part);
                }

                foreach (var part in inventoryParts)
                {
                    var product = _productRepo.GetById(part.ProductId.Value);
                    if (product != null)
                    {
                        _productRepo.SetQuantity(part.ProductId.Value, product.Quantity + part.Quantity);
                        Logger.LogInfo($"MaintenanceService: Cancel restored '{product.Name}' x{part.Quantity} to inventory.");
                    }
                }

                order.OrderStatus = RepairStatus.Cancelled;
                _repo.UpdateOrder(order);

                _activityLog.LogActivity(userId, "cancel_repair_order", "repair_orders",
                    (int)orderId, $"Order {order.OrderNumber} cancelled.");

                _txManager.CommitTransaction();
                Logger.LogInfo($"MaintenanceService: Order {order.OrderNumber} cancelled. {inventoryParts.Count} part(s) restored.");
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        public void MarkDelivered(long orderId, int userId)
        {
            var order = _repo.GetOrder(orderId)
                ?? throw new InvalidOperationException("طلب الصيانة غير موجود.");

            if (order.OrderStatus == RepairStatus.Delivered)
                throw new InvalidOperationException("الطلب تم تسليمه مسبقاً.");

            if (order.OrderStatus == RepairStatus.Cancelled)
                throw new InvalidOperationException("لا يمكن تسليم طلب ملغي.");

            order.OrderStatus  = RepairStatus.Delivered;
            order.DeliveryDate = DateTime.Now;

            // T-5: order update + activity log must be atomic.
            _txManager.BeginTransaction();
            try
            {
                _repo.UpdateOrder(order);

                _activityLog.LogActivity(userId, "deliver_repair_order", "repair_orders",
                    (int)orderId, $"Order {order.OrderNumber} delivered.");

                _txManager.CommitTransaction();
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }

            Logger.LogInfo($"MaintenanceService: Order {order.OrderNumber} marked as delivered.");
        }

        public void RemoveDevice(long deviceId, int userId)
        {
            var device = _repo.GetDevice(deviceId)
                ?? throw new InvalidOperationException("الجهاز غير موجود.");

            long orderId = device.OrderId;

            _txManager.BeginTransaction();
            try
            {
                foreach (var part in _repo.GetParts(deviceId))
                {
                    if (part.IsFromInventory && part.ProductId.HasValue)
                    {
                        var product = _productRepo.GetById(part.ProductId.Value);
                        if (product != null)
                        {
                            _productRepo.SetQuantity(part.ProductId.Value, product.Quantity + part.Quantity);
                            Logger.LogInfo($"MaintenanceService: RemoveDevice restored '{product.Name}' x{part.Quantity} to inventory.");
                        }
                    }
                }

                _repo.RemoveDevice(deviceId);
                _repo.RecalculateOrderTotals(orderId);
                _txManager.CommitTransaction();
                Logger.LogInfo($"MaintenanceService: Device {deviceId} removed from order {orderId}.");
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        public RepairOrder GetOrder(long orderId)              => _repo.GetOrder(orderId);
        public List<RepairDevice> GetDevices(long orderId)     => _repo.GetDevices(orderId);
        public List<RepairPart> GetOrderParts(long orderId)    => _repo.GetOrderParts(orderId);
        public List<RepairPart> GetDeviceParts(long deviceId)  => _repo.GetParts(deviceId);
        public List<RepairPayment> GetPayments(long orderId)   => _repo.GetPayments(orderId);

        public List<RepairOrder> GetOrders(string status = null, string search = null,
                                           string start = null, string end = null)
            => _repo.GetOrders(status, search, start, end);
    }
}
