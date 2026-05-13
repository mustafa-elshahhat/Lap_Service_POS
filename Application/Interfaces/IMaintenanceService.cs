using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Application.DTOs;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface IMaintenanceService
    {
        RepairOrder CreateOrder(RepairOrderInput input, int userId);
        void UpdateOrderInfo(long orderId, RepairOrderInput input);

        long AddDevice(long orderId, RepairDeviceInput input);
        void UpdateDevice(long deviceId, RepairDeviceInput input);
        void UpdateDeviceStatus(long deviceId, string newStatus, string notes);

        void AddInventoryPart(long deviceId, long orderId, int productId, int qty, decimal unitCost);
        void AddCustomPart(long deviceId, long orderId, string name, int qty, decimal unitCost, decimal purchaseCost = 0);
        void RemovePart(long partId);
        void RemoveDevice(long deviceId, int userId);

        void RegisterPayment(long orderId, decimal amount, string method, int userId, string notes = null);
        void CancelOrder(long orderId, int userId);
        void MarkDelivered(long orderId, int userId);

        RepairOrder GetOrder(long orderId);
        List<RepairDevice> GetDevices(long orderId);
        List<RepairPart> GetOrderParts(long orderId);
        List<RepairPart> GetDeviceParts(long deviceId);
        List<RepairPayment> GetPayments(long orderId);
        List<RepairOrder> GetOrders(string status = null, string search = null, string start = null, string end = null);
    }
}
