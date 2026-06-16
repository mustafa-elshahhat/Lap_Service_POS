using System;
using System.Collections.ObjectModel;
using System.Linq;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Presentation.ViewModels;

namespace AlJohary.ServiceHub.Presentation.Models
{
    public class CartModel : BaseViewModel
    {
        private decimal _total;
        private CartItem _selectedItem;

        public ObservableCollection<CartItem> Items { get; } = new ObservableCollection<CartItem>();

        public decimal Total
        {
            get => _total;
            set => SetProperty(ref _total, value);
        }

        public CartItem SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public CartModel()
        {
            Items.CollectionChanged += (s, e) => RecalculateTotal();
        }

        public bool IsEmpty => Items.Count == 0;

        // Caller guarantees the item is new (existing items are routed to UpdateQuantity).
        public void AddItem(int productId, string productName, string productCode, int quantity, decimal unitPrice, decimal originalPrice, decimal purchasePrice)
        {
            var item = new CartItem
            {
                Index = Items.Count + 1,
                ProductId = productId,
                ProductName = productName,
                Quantity = quantity,
                UnitPrice = unitPrice,
                OriginalPrice = originalPrice,
                PurchasePrice = purchasePrice,
                ProductCode = productCode
            };
            item.Total = item.Quantity * item.UnitPrice;
            Items.Add(item);
        }

        public void RemoveItem(CartItem item)
        {
            Items.Remove(item);
            RecalculateIndices();
        }

        public void Clear()
        {
            Items.Clear();
        }

        public void UpdateQuantity(CartItem item, int newQuantity)
        {
            item.Quantity = newQuantity;
            item.Total = item.Quantity * item.UnitPrice;
            RefreshItem(item);
        }

        public void UpdatePrice(CartItem item, decimal newPrice)
        {
            item.UnitPrice = newPrice;
            item.Total = item.Quantity * item.UnitPrice;
            RefreshItem(item);
        }

        public void RefreshItem(CartItem item)
        {
            int idx = Items.IndexOf(item);
            if (idx >= 0)
            {
                Items[idx] = item;
            }
        }

        public void RecalculateTotal()
        {
            Total = Items.Sum(item => item.Total);
        }

        private void RecalculateIndices()
        {
            int idx = 1;
            foreach (var item in Items)
            {
                item.Index = idx++;
            }
        }

        public System.Collections.Generic.List<SaleItem> ConvertToSaleItems()
        {
            return Items.Select(c => new SaleItem
            {
                ProductId = c.ProductId,
                ProductCode = c.ProductCode,
                ProductName = c.ProductName,
                Quantity = c.Quantity,
                UnitPurchasePrice = c.PurchasePrice,
                UnitSellingPrice = c.OriginalPrice,
                UnitFinalPrice = c.UnitPrice
            }).ToList();
        }
    }
}
