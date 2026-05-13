using System.Collections.Generic;
using CarPartsShopWPF.Domain.Entities;

namespace CarPartsShopWPF.Domain.Interfaces
{
    public interface IRepairRepository
    {
        long CreateOrder(RepairOrder order);
        void UpdateOrder(RepairOrder order);
        long AddDevice(RepairDevice device);
        void UpdateDevice(RepairDevice device);
        long AddPart(RepairPart part);
        void RemovePart(long partId);
        long AddPayment(RepairPayment payment);
        void RecalculateOrderTotals(long orderId);

        RepairOrder GetOrder(long orderId);
        List<RepairDevice> GetDevices(long orderId);
        List<RepairPart> GetParts(long deviceId);
        List<RepairPart> GetOrderParts(long orderId);
        List<RepairPayment> GetPayments(long orderId);
        List<RepairOrder> GetOrders(string statusFilter, string search, string startDate, string endDate);
        RepairPart GetPart(long partId);
    }
}
