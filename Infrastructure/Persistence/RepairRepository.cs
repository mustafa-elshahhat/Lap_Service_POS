using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.Persistence
{
    public class RepairRepository : IRepairRepository
    {
        private readonly DatabaseManager _db = DatabaseManager.Instance;

        public long CreateOrder(RepairOrder order)
        {
            return _db.ExecuteAndGetId(@"
                INSERT INTO repair_orders
                    (order_number, customer_id, customer_name, customer_phone, technician_name,
                     user_id, total_amount, paid_amount, remaining_amount, order_status,
                     expected_delivery, intake_date, notes, created_at, updated_at)
                VALUES
                    (@num, @cid, @cname, @cphone, @tech,
                     @uid, 0, 0, 0, @status,
                     @delivery, @intake, @notes, datetime('now','localtime'), datetime('now','localtime'))",
                new Dictionary<string, object>
                {
                    { "@num",      order.OrderNumber },
                    { "@cid",      order.CustomerId },
                    { "@cname",    order.CustomerName },
                    { "@cphone",   order.CustomerPhone },
                    { "@tech",     order.TechnicianName },
                    { "@uid",      order.UserId },
                    { "@status",   order.OrderStatus ?? RepairStatus.Received },
                    { "@delivery", order.ExpectedDelivery?.ToString("yyyy-MM-dd") },
                    { "@intake",   order.IntakeDate.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "@notes",    order.Notes }
                });
        }

        public void UpdateOrder(RepairOrder order)
        {
            _db.Execute(@"
                UPDATE repair_orders SET
                    customer_id      = @cid,
                    customer_name    = @cname,
                    customer_phone   = @cphone,
                    technician_name  = @tech,
                    order_status     = @status,
                    expected_delivery= @delivery,
                    delivery_date    = COALESCE(@deliveryDate, delivery_date),
                    notes            = @notes,
                    updated_at       = datetime('now','localtime')
                WHERE id = @id",
                new Dictionary<string, object>
                {
                    { "@id",           order.Id },
                    { "@cid",          order.CustomerId },
                    { "@cname",        order.CustomerName },
                    { "@cphone",       order.CustomerPhone },
                    { "@tech",         order.TechnicianName },
                    { "@status",       order.OrderStatus },
                    { "@delivery",     order.ExpectedDelivery?.ToString("yyyy-MM-dd") },
                    { "@deliveryDate", order.DeliveryDate?.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "@notes",        order.Notes }
                });
        }

        public long AddDevice(RepairDevice device)
        {
            return _db.ExecuteAndGetId(@"
                INSERT INTO repair_devices
                    (order_id, device_type, brand, model, serial_number, condition,
                     reported_issue, accessories, estimated_cost, service_cost, labor_cost,
                     device_status, diagnosis_notes, repair_notes, created_at)
                VALUES
                    (@oid, @dtype, @brand, @model, @serial, @cond,
                     @issue, @acc, @est, @svc, @labor,
                     @status, @diag, @repair, datetime('now','localtime'))",
                new Dictionary<string, object>
                {
                    { "@oid",    device.OrderId },
                    { "@dtype",  device.DeviceType },
                    { "@brand",  device.Brand },
                    { "@model",  device.Model },
                    { "@serial", device.SerialNumber },
                    { "@cond",   device.Condition },
                    { "@issue",  device.ReportedIssue },
                    { "@acc",    device.Accessories },
                    { "@est",    device.EstimatedCost },
                    { "@svc",    device.ServiceCost },
                    { "@labor",  device.LaborCost },
                    { "@status", device.DeviceStatus ?? RepairStatus.Received },
                    { "@diag",   device.DiagnosisNotes },
                    { "@repair", device.RepairNotes }
                });
        }

        public RepairDevice GetDevice(long deviceId)
        {
            var row = _db.FetchOne("SELECT * FROM repair_devices WHERE id = @id",
                new Dictionary<string, object> { { "@id", deviceId } });
            return row == null ? null : MapDevice(row);
        }

        public void RemoveDevice(long deviceId)
        {
            _db.Execute("DELETE FROM repair_devices WHERE id = @id",
                new Dictionary<string, object> { { "@id", deviceId } });
        }

        public void UpdateDeviceStatus(long deviceId, string newStatus, string notes)
        {
            _db.Execute(@"
                UPDATE repair_devices
                SET device_status = @status,
                    repair_notes  = @notes
                WHERE id = @id",
                new Dictionary<string, object>
                {
                    { "@id",     deviceId },
                    { "@status", newStatus },
                    { "@notes",  notes }
                });
        }

        public void UpdateDevice(RepairDevice device)
        {
            _db.Execute(@"
                UPDATE repair_devices SET
                    device_type      = @dtype,
                    brand            = @brand,
                    model            = @model,
                    serial_number    = @serial,
                    condition        = @cond,
                    reported_issue   = @issue,
                    accessories      = @acc,
                    estimated_cost   = @est,
                    service_cost     = @svc,
                    labor_cost       = @labor,
                    device_status    = @status,
                    diagnosis_notes  = @diag,
                    repair_notes     = @repair
                WHERE id = @id",
                new Dictionary<string, object>
                {
                    { "@id",     device.Id },
                    { "@dtype",  device.DeviceType },
                    { "@brand",  device.Brand },
                    { "@model",  device.Model },
                    { "@serial", device.SerialNumber },
                    { "@cond",   device.Condition },
                    { "@issue",  device.ReportedIssue },
                    { "@acc",    device.Accessories },
                    { "@est",    device.EstimatedCost },
                    { "@svc",    device.ServiceCost },
                    { "@labor",  device.LaborCost },
                    { "@status", device.DeviceStatus },
                    { "@diag",   device.DiagnosisNotes },
                    { "@repair", device.RepairNotes }
                });
        }

        public long AddPart(RepairPart part)
        {
            return _db.ExecuteAndGetId(@"
                INSERT INTO repair_parts
                    (device_id, order_id, product_id, part_name, quantity, unit_cost, total_cost, purchase_cost, is_from_inventory, created_at)
                VALUES
                    (@did, @oid, @pid, @name, @qty, @ucost, @tcost, @pcost, @inv, datetime('now','localtime'))",
                new Dictionary<string, object>
                {
                    { "@did",   part.DeviceId },
                    { "@oid",   part.OrderId },
                    { "@pid",   part.ProductId },
                    { "@name",  part.PartName },
                    { "@qty",   part.Quantity },
                    { "@ucost", part.UnitCost },
                    { "@tcost", part.TotalCost },
                    { "@pcost", part.PurchaseCost },
                    { "@inv",   part.IsFromInventory ? 1 : 0 }
                });
        }

        public void RemovePart(long partId)
        {
            _db.Execute("DELETE FROM repair_parts WHERE id = @id",
                new Dictionary<string, object> { { "@id", partId } });
        }

        public long AddPayment(RepairPayment payment)
        {
            long id = _db.ExecuteAndGetId(@"
                INSERT INTO repair_payments (order_id, amount, payment_method, payment_date, notes, user_id, created_at)
                VALUES (@oid, @amt, @method, @date, @notes, @uid, datetime('now','localtime'))",
                new Dictionary<string, object>
                {
                    { "@oid",    payment.OrderId },
                    { "@amt",    payment.Amount },
                    { "@method", payment.PaymentMethod },
                    { "@date",   payment.PaymentDate.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "@notes",  payment.Notes },
                    { "@uid",    payment.UserId }
                });

            RecalculateOrderTotals(payment.OrderId);
            return id;
        }

        public void RecalculateOrderTotals(long orderId)
        {
            var totalRow = _db.FetchOne(@"
                SELECT
                    COALESCE(SUM(d.labor_cost), 0) +
                    COALESCE((SELECT SUM(p.total_cost) FROM repair_parts p WHERE p.order_id = @id), 0) as total
                FROM repair_devices d WHERE d.order_id = @id",
                new Dictionary<string, object> { { "@id", orderId } });

            decimal total = SafeConvert.ToDecimal(totalRow?["total"]);

            var paidRow = _db.FetchOne(@"
                SELECT COALESCE(SUM(amount), 0) as paid FROM repair_payments WHERE order_id = @id",
                new Dictionary<string, object> { { "@id", orderId } });

            decimal paid = SafeConvert.ToDecimal(paidRow?["paid"]);

            _db.Execute(@"
                UPDATE repair_orders SET
                    total_amount     = @total,
                    paid_amount      = @paid,
                    remaining_amount = @rem,
                    updated_at       = datetime('now','localtime')
                WHERE id = @id",
                new Dictionary<string, object>
                {
                    { "@total", total },
                    { "@paid",  paid },
                    { "@rem",   Math.Max(0, total - paid) },
                    { "@id",    orderId }
                });
        }

        public RepairOrder GetOrder(long orderId)
        {
            var row = _db.FetchOne(@"
                SELECT ro.*, u.full_name as user_name,
                    (SELECT COUNT(*) FROM repair_devices rd WHERE rd.order_id = ro.id) as device_count
                FROM repair_orders ro
                LEFT JOIN users u ON ro.user_id = u.id
                WHERE ro.id = @id",
                new Dictionary<string, object> { { "@id", orderId } });

            return row == null ? null : MapOrder(row);
        }

        public List<RepairDevice> GetDevices(long orderId)
        {
            var rows = _db.FetchAll(@"
                SELECT * FROM repair_devices WHERE order_id = @id ORDER BY id",
                new Dictionary<string, object> { { "@id", orderId } });

            var list = new List<RepairDevice>();
            foreach (var r in rows) list.Add(MapDevice(r));
            return list;
        }

        public List<RepairPart> GetParts(long deviceId)
        {
            var rows = _db.FetchAll(@"
                SELECT * FROM repair_parts WHERE device_id = @id ORDER BY id",
                new Dictionary<string, object> { { "@id", deviceId } });

            var list = new List<RepairPart>();
            foreach (var r in rows) list.Add(MapPart(r));
            return list;
        }

        public List<RepairPart> GetOrderParts(long orderId)
        {
            var rows = _db.FetchAll(@"
                SELECT * FROM repair_parts WHERE order_id = @id ORDER BY device_id, id",
                new Dictionary<string, object> { { "@id", orderId } });

            var list = new List<RepairPart>();
            foreach (var r in rows) list.Add(MapPart(r));
            return list;
        }

        public List<RepairPayment> GetPayments(long orderId)
        {
            var rows = _db.FetchAll(@"
                SELECT * FROM repair_payments WHERE order_id = @id ORDER BY payment_date DESC",
                new Dictionary<string, object> { { "@id", orderId } });

            var list = new List<RepairPayment>();
            foreach (var r in rows) list.Add(MapPayment(r));
            return list;
        }

        public RepairPart GetPart(long partId)
        {
            var row = _db.FetchOne("SELECT * FROM repair_parts WHERE id = @id",
                new Dictionary<string, object> { { "@id", partId } });
            return row == null ? null : MapPart(row);
        }

        public List<RepairOrder> GetOrders(string statusFilter, string search, string startDate, string endDate)
        {
            var args = new Dictionary<string, object>();
            string where = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
            {
                where += " AND ro.order_status = @status";
                args["@status"] = statusFilter;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                where += " AND (ro.order_number LIKE @s OR ro.customer_name LIKE @s OR ro.customer_phone LIKE @s)";
                args["@s"] = $"%{search}%";
            }

            if (!string.IsNullOrWhiteSpace(startDate))
            {
                where += " AND ro.intake_date >= @start";
                args["@start"] = startDate + " 00:00:00";
            }

            if (!string.IsNullOrWhiteSpace(endDate))
            {
                where += " AND ro.intake_date < @end";
                args["@end"] = DateTime.Parse(endDate).AddDays(1).ToString("yyyy-MM-dd 00:00:00");
            }

            var rows = _db.FetchAll($@"
                SELECT ro.*, u.full_name as user_name,
                    (SELECT COUNT(*) FROM repair_devices rd WHERE rd.order_id = ro.id) as device_count
                FROM repair_orders ro
                LEFT JOIN users u ON ro.user_id = u.id
                {where}
                ORDER BY ro.intake_date DESC",
                args);

            var list = new List<RepairOrder>();
            foreach (var r in rows) list.Add(MapOrder(r));
            return list;
        }

        private static RepairOrder MapOrder(Dictionary<string, object> r) => new RepairOrder
        {
            Id               = SafeConvert.ToLong(r["id"]),
            OrderNumber      = SafeConvert.ToString(r["order_number"]),
            CustomerId       = r["customer_id"] == null ? (int?)null : SafeConvert.ToInt(r["customer_id"]),
            CustomerName     = SafeConvert.ToString(r["customer_name"]),
            CustomerPhone    = SafeConvert.ToString(r["customer_phone"]),
            TechnicianName   = SafeConvert.ToString(r["technician_name"]),
            UserId           = SafeConvert.ToInt(r["user_id"]),
            TotalAmount      = SafeConvert.ToDecimal(r["total_amount"]),
            PaidAmount       = SafeConvert.ToDecimal(r["paid_amount"]),
            RemainingAmount  = SafeConvert.ToDecimal(r["remaining_amount"]),
            OrderStatus      = SafeConvert.ToString(r["order_status"]),
            ExpectedDelivery = SafeConvert.ToDateTime(r["expected_delivery"]),
            IntakeDate       = SafeConvert.ToDateTime(r["intake_date"]) ?? DateTime.Now,
            DeliveryDate     = SafeConvert.ToDateTime(r["delivery_date"]),
            Notes            = SafeConvert.ToString(r["notes"]),
            CreatedAt        = SafeConvert.ToDateTime(r["created_at"]) ?? DateTime.Now,
            UserName         = SafeConvert.ToString(r.ContainsKey("user_name") ? r["user_name"] : null),
            DeviceCount      = r.ContainsKey("device_count") ? SafeConvert.ToInt(r["device_count"]) : 0
        };

        private static RepairDevice MapDevice(Dictionary<string, object> r) => new RepairDevice
        {
            Id              = SafeConvert.ToLong(r["id"]),
            OrderId         = SafeConvert.ToLong(r["order_id"]),
            DeviceType      = SafeConvert.ToString(r["device_type"]),
            Brand           = SafeConvert.ToString(r["brand"]),
            Model           = SafeConvert.ToString(r["model"]),
            SerialNumber    = SafeConvert.ToString(r["serial_number"]),
            Condition       = SafeConvert.ToString(r["condition"]),
            ReportedIssue   = SafeConvert.ToString(r["reported_issue"]),
            Accessories     = SafeConvert.ToString(r["accessories"]),
            EstimatedCost   = SafeConvert.ToDecimal(r["estimated_cost"]),
            ServiceCost     = SafeConvert.ToDecimal(r["service_cost"]),
            LaborCost       = SafeConvert.ToDecimal(r["labor_cost"]),
            DeviceStatus    = SafeConvert.ToString(r["device_status"]),
            DiagnosisNotes  = SafeConvert.ToString(r["diagnosis_notes"]),
            RepairNotes     = SafeConvert.ToString(r["repair_notes"]),
            CreatedAt       = SafeConvert.ToDateTime(r["created_at"]) ?? DateTime.Now
        };

        private static RepairPart MapPart(Dictionary<string, object> r) => new RepairPart
        {
            Id              = SafeConvert.ToLong(r["id"]),
            DeviceId        = SafeConvert.ToLong(r["device_id"]),
            OrderId         = SafeConvert.ToLong(r["order_id"]),
            ProductId       = r["product_id"] == null ? (int?)null : SafeConvert.ToInt(r["product_id"]),
            PartName        = SafeConvert.ToString(r["part_name"]),
            Quantity        = SafeConvert.ToInt(r["quantity"]),
            UnitCost        = SafeConvert.ToDecimal(r["unit_cost"]),
            TotalCost       = SafeConvert.ToDecimal(r["total_cost"]),
            PurchaseCost    = SafeConvert.ToDecimal(r.ContainsKey("purchase_cost") ? r["purchase_cost"] : 0),
            IsFromInventory = SafeConvert.ToInt(r["is_from_inventory"]) == 1,
            CreatedAt       = SafeConvert.ToDateTime(r["created_at"]) ?? DateTime.Now
        };

        private static RepairPayment MapPayment(Dictionary<string, object> r) => new RepairPayment
        {
            Id            = SafeConvert.ToLong(r["id"]),
            OrderId       = SafeConvert.ToLong(r["order_id"]),
            Amount        = SafeConvert.ToDecimal(r["amount"]),
            PaymentMethod = SafeConvert.ToString(r["payment_method"]),
            PaymentDate   = SafeConvert.ToDateTime(r["payment_date"]) ?? DateTime.Now,
            Notes         = SafeConvert.ToString(r["notes"]),
            UserId        = r["user_id"] == null ? (int?)null : SafeConvert.ToInt(r["user_id"]),
            CreatedAt     = SafeConvert.ToDateTime(r["created_at"]) ?? DateTime.Now
        };
    }
}
